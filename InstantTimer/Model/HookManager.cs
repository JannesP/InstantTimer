using InstantTimer.NativeImports;
using InstantTimer.Settings;
using InstantTimer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace InstantTimer.Model
{
    sealed class HookManager : IDisposable
    {
        public event EventHandler ModifiersChanged;
        public event EventHandler ModifierStateChanged;
        public event EventHandler<KeyboardHookEventArgs> KeyEvent;

        private readonly object _syncRootMods = new object();
        private Dictionary<Key, bool> _modifierStates;
        public static HookManager Instance { get; private set; }
        public IEnumerable<Key> Modifiers
        {
            get
            {
                lock (_syncRootMods) return ModifierStates.Keys.AsEnumerable();
            }
            set
            {
                lock (_syncRootMods)
                {
                    if (ModifierStates == null) ModifierStates = new Dictionary<Key, bool>();
                    if (value == null)
                    {
                        ModifierStates.Clear();
                    }
                    else
                    {
                        List<KeyValuePair<Key, bool>> pressedKeys = ModifierStates.Where(kvp => kvp.Value == true).ToList();
                        ModifierStates.Clear();
                        foreach (Key k in value)
                        {
                            ModifierStates.Add(k, pressedKeys.Any(kvp => kvp.Key == k));
                        }
                    }
                }
                ModifiersChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public Dictionary<Key, bool> ModifierStates
        {
            get
            {
                lock (_syncRootMods) return _modifierStates;
            }
            private set
            {
                lock (_syncRootMods) _modifierStates = value;
            }
        }


        public static void InitInstance()
        {
            if (Instance != null) throw new InvalidOperationException("There is already an instance of HookManager active.");
            Instance = new HookManager();
        }

        public static void DisposeInstance()
        {
            Instance?.Dispose();
            Instance = null;
        }

        public bool AllModifiersActive
        {
            get
            {
                return ModifierStates.All(kvp => kvp.Value);
            }
        }

        private HookManager()
        {
            ISettingsProvider settings = Injector.Get<ISettingsProvider>();
            settings.Settings.PropertyChanged += Settings_PropertyChanged;
            Modifiers = settings.Settings.InstantTimerModifierKeys;
            NativeHookWrapper.KeyboardHookCalled += NativeHookWrapper_KeyboardHookCalled;
            NativeHookWrapper.ActivateKeyboardHook();
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SettingsData s = sender as SettingsData;
            if (s == null) throw new Exception("Expected a correct sender.");
            switch(e.PropertyName)
            {
                case nameof(SettingsData.InstantTimerModifierKeys):
                    Modifiers = s.InstantTimerModifierKeys;
                    break;
            }
        }

        private void NativeHookWrapper_KeyboardHookCalled(object sender, KeyboardHookEventArgs e)
        {
            lock (_syncRootMods)
            {
                if (ModifierStates.ContainsKey(e.Key))
                {
                    bool newValue = e.Type == KeyboardHookEventArgs.EventType.KeyDown;
                    if (ModifierStates[e.Key] != newValue)
                    {
                        ModifierStates[e.Key] = newValue;
                        ModifierStateChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
                else
                {
                    if (e.Type == KeyboardHookEventArgs.EventType.KeyDown)
                    {
                        KeyEvent?.Invoke(this, e);
                    }
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        
        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    NativeHookWrapper.DisableKeyboardHook();
                    Instance = null;
                }

                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
