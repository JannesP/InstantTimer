﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Application = System.Windows.Application;

namespace JJA.Anperi.WpfUtility
{
    public static class WpfUtil
    {
        private static string RegistryAutostartKey => @"Software\Microsoft\Windows\CurrentVersion\Run";

        private static string _syncNamesBase;
        private static string MutexName => _syncNamesBase + "_isFirstInstanceMutex";
        private static string EventSignalName => _syncNamesBase + "_notifyFirstInstanceEvent";
        private static Mutex _mutexCheckIfFirstInstance;
        private static EventWaitHandle _evtNotifiedFromOtherProcess;
        private static CancellationTokenSource _cancellationTokenSource;
        private static bool _isFirstInstance;
        private static bool _isInitialized;
        private static string _autostartKeyName;

        public static event EventHandler SecondInstanceStarted;

        /// <summary>
        /// Initializes all the functionality surrounding the IsFirstInstance and SecondInstance. Be sure to call <see cref="Dispose"/> before the program ends.
        /// </summary>
        /// <param name="systemUniqueIdentifierName">A system unique identifier for this program.</param>
        /// <param name="autostartRegKeyName">The name for autostart entry</param>
        /// <param name="notifyIfNotFirst">If the first program should be notified that this instance got started.</param>
        public static void Init(string systemUniqueIdentifierName, bool notifyIfNotFirst = true)
        {
            if (_isInitialized) throw new InvalidOperationException("You already initialized this class.");
            _syncNamesBase = systemUniqueIdentifierName ?? throw new ArgumentException("systemUniqueIdentifierName must have a value", nameof(systemUniqueIdentifierName));
            _cancellationTokenSource = new CancellationTokenSource();
            
            bool isFirst = CheckIfFirstInstance(MutexName);
            if (!isFirst && notifyIfNotFirst)
            {
                try
                {
                    _evtNotifiedFromOtherProcess = EventWaitHandle.OpenExisting(EventSignalName);
                    _evtNotifiedFromOtherProcess.Set();
                }
                catch(Exception) { //ignored
                }
            }
            else if (isFirst)
            {
                try
                {
                    _evtNotifiedFromOtherProcess = new EventWaitHandle(false, EventResetMode.ManualReset,
                        EventSignalName, out bool createdNew);
                    if (createdNew)
                    {
                        Task.Run(() => WaitForEvent(_evtNotifiedFromOtherProcess, _cancellationTokenSource.Token));
                    }
                }
                catch (Exception ex)
                {
                    throw new IOException("Error creating event for listening to notify SecondInstanceStarted. The base exception is the innerException.", ex);
                }
            }
            _isInitialized = true;
        }

        /// <summary>
        /// Restarts the current executable. You can use this if you used the WpfUtil Mutex to handle multiple instances.
        /// </summary>
        public static void Restart()
        {
            Application.Current.Exit += (o, args) =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = FullAssemblyPath,
                    WorkingDirectory = Process.GetCurrentProcess().StartInfo.WorkingDirectory
                });
            };
            Application.Current.Shutdown();
            Dispose();
        }

        /// <summary>
        /// Adds the program to the autostart in the registry. You should probably use some command line args to prevent the gui from opening.
        /// If the regkeyname is not set it will use the executable name without the extension.
        /// </summary>
        /// <param name="additionalArgs">additional command line args to be used</param>
        /// <param name="regKeyName">If null it will use the executable name. Note that removeFromAutostart always uses the last regKeyName that was used.</param>
        /// <returns>if the key was set successfully</returns>
        public static bool AddToAutostart(string additionalArgs = null, string regKeyName = null)
        {
            _autostartKeyName = regKeyName ?? AssemblyNameWithoutExtension;
            string args;
            if (additionalArgs == null)
            {
                args = "";
            }
            else
            {
                args = " " + additionalArgs;
            }
            
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryAutostartKey, true))
                {
                    if (key != null)
                    {
                        key.SetValue(_autostartKeyName, $"\"{FullAssemblyPath}\"{args}");
                        return true;
                    }
                    else
                    {
                        Trace.TraceError("Autostart key couldn't be opened!");
                    }
                }
            }
            catch (SecurityException ex)
            {
                Trace.TraceError("Error AddToAutostart:\n{0}", ex.GetType().Name + ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Remnoves the program from autostart, deleting the registry key. It will use the last key name that was used to set in this instance.
        /// </summary>
        /// <returns>if the key was removed successfully</returns>
        public static bool RemoveFromAutostart(string regKeyName = null)
        {
            _autostartKeyName = regKeyName ?? AssemblyNameWithoutExtension;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryAutostartKey, true))
                {
                    if (key != null)
                    {
                        if (key.GetValue(_autostartKeyName) != null)
                        {
                            key.DeleteValue(_autostartKeyName);
                        }
                        return true;
                    }
                    else
                    {
                        Trace.TraceError("Autostart key couldn't be opened!");
                    }
                }
            }
            catch (SecurityException ex)
            {
                Trace.TraceError("Error RemoveFromAutostart:\n{0}", ex.GetType().Name + ex.Message);
            }
            return false;
        }

        private static void WaitForEvent(EventWaitHandle ewh, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                ewh.WaitOne();
                if (!token.IsCancellationRequested)
                {
                    Application.Current?.Dispatcher?.Invoke(OnSecondInstanceStarted);
                    ewh.Reset();
                }
            }
        } 

        /// <summary>
        /// Disposes of all used native resources.
        /// </summary>
        public static void Dispose()
        {
            try
            {
                _evtNotifiedFromOtherProcess?.Dispose();
                _evtNotifiedFromOtherProcess = null;
                if (IsFirstInstance)
                {
                    _mutexCheckIfFirstInstance?.ReleaseMutex();
                }
                _mutexCheckIfFirstInstance?.Dispose();
                _mutexCheckIfFirstInstance = null;
            }
            catch (Exception ex)
            {
                TraceException("Error disposing WpfUtil", ex);
            }
        }

        /// <summary>
        /// Shows the main window or creates it with the given Type.
        /// </summary>
        /// <typeparam name="TWindow">The type for window to use. Throws an exception if the current Window exists and doesnt have correct type.</typeparam>
        /// <param name="createdNew">If a new window was created or if the MainWindow already had a window.</param>
        /// <returns></returns>
        public static TWindow ShowCreateMainWindow<TWindow>(this Application app, out bool createdNew) where TWindow : Window
        {
            createdNew = app.MainWindow == null;
            if (createdNew)
            {
                TWindow window = Activator.CreateInstance<TWindow>();
                window.Show();
                Application.Current.MainWindow = window;
                return window;
            }
            else
            {
                TWindow currentWindow = app.MainWindow as TWindow;
                if (currentWindow == null)
                {
                    throw new InvalidOperationException(
                        $"The current window is not of the type {typeof(TWindow).FullName}");
                }
                if (currentWindow.WindowState == WindowState.Minimized) currentWindow.WindowState = WindowState.Normal;
                currentWindow.Activate();
                return currentWindow;
            }
        }

        /// <summary>
        /// Shows the main window or creates it with the given Type.
        /// </summary>
        /// <typeparam name="TWindow">The type for window to use. Throws an exception if the current Window exists and doesnt have correct type.</typeparam>
        /// <returns></returns>
        public static TWindow ShowCreateMainWindow<TWindow>(this Application app) where TWindow : Window
        {
            return app.ShowCreateMainWindow<TWindow>(out bool _);
        }

        /// <summary>
        /// Can only be used after <see cref="Init"/> has been called once.
        /// </summary>
        public static bool IsFirstInstance => _mutexCheckIfFirstInstance == null
            ? throw new InvalidOperationException("You need to call InitSingleInstanceChecks first.")
            : _isFirstInstance;

        /// <summary>
        /// This checks if a mutex with the given name already exists. Effectively checking if this is the first Instance.
        /// For all following checks after the first one use <see cref="IsFirstInstance"/>
        /// </summary>
        /// <param name="mutexName">A system-unique name for your program mutex.</param>
        /// <returns>If this is the first instance.</returns>
        private static bool CheckIfFirstInstance(string mutexName)
        {
            if (_mutexCheckIfFirstInstance != null) throw new InvalidOperationException("You can only call this once. For following checks use IsFirstInstance");
            try
            {
                _mutexCheckIfFirstInstance = new Mutex(false, mutexName);
                _isFirstInstance = _mutexCheckIfFirstInstance.WaitOne(0, false);
            }
            catch (AbandonedMutexException)
            {
                /* ignored because we got it and it doesn't protect any data 
                 * (aside the data of the previous program (from which the UI thread apparently already died)*/
                _isFirstInstance = true;
            }
            return _isFirstInstance;
        }

        public class RelayCommand : ICommand
        {
            private readonly Action<object> _action;
            private readonly bool _canExecute;

            public RelayCommand(Action<object> action, bool canExecute = true)
            {
                _action = action;
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter)
            {
                return _canExecute;
            }

            public void Execute(object parameter)
            {
                if (_canExecute) _action(parameter);
            }
#pragma warning disable 0067
            public event EventHandler CanExecuteChanged;
#pragma warning restore 0067
        }

        private static void OnSecondInstanceStarted()
        {
            SecondInstanceStarted?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Writes a formatted exception to the trace error stream.
        /// </summary>
        /// <param name="line1">The first line followed by a :</param>
        /// <param name="ex">The exception to write to the log</param>
        public static void TraceException(string line1, Exception ex)
        {
            Trace.TraceError($"{line1}: {ex.GetType()}\nMessage: {ex.Message}\nStack Trace:\n{ex.StackTrace}");
        }

        public static string AssemblyDirectory
        {
            get { return Path.GetDirectoryName(FullAssemblyPath); }
        }

        /// <summary>
        /// Needed because the Assembly.Location doesn't handle all special characters (like '#' for example).
        /// </summary>
        public static string FullAssemblyPath
        {
            get
            {
                string codeBasePseudoUrl = Assembly.GetEntryAssembly().CodeBase;
                const string filePrefix3 = @"file:///";
                if (codeBasePseudoUrl.StartsWith(filePrefix3))
                {
                    string sPath = codeBasePseudoUrl.Substring(filePrefix3.Length);
                    return sPath.Replace('/', '\\');
                }
                return Assembly.GetEntryAssembly().Location;
            }
        }

        public static string AssemblyName
        {
            get { return Path.GetFileName(FullAssemblyPath); }
        }

        public static string AssemblyNameWithoutExtension
        {
            get { return Path.GetFileNameWithoutExtension(FullAssemblyPath); }
        }
    }
}