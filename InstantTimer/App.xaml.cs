using InstantTimer.Model;
using InstantTimer.Settings;
using InstantTimer.Utility;
using JJA.Anperi.WpfUtility;
using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace InstantTimer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Environment.CurrentDirectory = WpfUtil.AssemblyDirectory;

#if DEBUG
            if (!Directory.Exists("logs")) Directory.CreateDirectory("logs");
            Trace.Listeners.Add(new FileLogTraceListener("filelistener") { Location = LogFileLocation.Custom, CustomLocation = "logs", Append = true, BaseFileName = "InstantTimer", AutoFlush = true, TraceOutputOptions = TraceOptions.DateTime, Delimiter = "\t|\t" });
#endif
            Trace.TraceInformation("OnStartup called.");

            try
            {
                WpfUtil.Init("jp.instanttimer");
                if (!WpfUtil.IsFirstInstance)
                {
                    Shutdown(0);
                    return;
                }
            }
            catch (Exception)
            {
                //ignored
            }
            WpfUtil.SecondInstanceStarted += WpfUtil_SecondInstanceStarted;

            SetupDi();
            HookManager.InitInstance();
            Injector.Get<ISettingsProvider>().Load();

            this.ShowCreateMainWindow<MainWindow>(out bool _);

            base.OnStartup(e);
        }

        private void WpfUtil_SecondInstanceStarted(object sender, EventArgs e)
        {
            this.ShowCreateMainWindow<MainWindow>(out bool _);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            if (e.IsTerminating)
            {
                MessageBox.Show($"Unhandled exception occured:\n\t{e.ExceptionObject.GetType()}: {ex?.Message ?? "<Error getting exception message>"}\nExiting ...",
                    "Unhandled exception in AnperiRemote", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            WpfUtil.TraceException("Unhandled exception in AppDomain", ex);
            try
            {
                HookManager.DisposeInstance();
            }
            catch (Exception) { }
            Trace.Flush();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Unhandled exception in UI Dispatcher:\n\t{e.Exception.GetType()}: {e.Exception.Message}",
                "Unhandled UI Dispatcher exception in AnperiRemote", MessageBoxButton.OK, MessageBoxImage.Error);
            WpfUtil.TraceException("Unhandled exception in UI Dispatcher", e.Exception);
            try
            {
                HookManager.DisposeInstance();
            }
            catch (Exception) { }
            Trace.Flush();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Trace.TraceInformation("OnExit called.");
            Injector.Get<ISettingsProvider>().Save();
            HookManager.DisposeInstance();
            Injector.DisposeContent();
            base.OnExit(e);
        }

        private void SetupDi()
        {
            Injector.Put<ISettingsProvider>(new XmlFileSettingsProvider());
        }
    }
}
