﻿using InstantTimer.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace InstantTimer.ViewModel
{
    public class HookObserverViewModel : INotifyPropertyChanged, IDisposable
    {
        public HookObserverViewModel()
        {
            if (HookManager.Instance != null)
            {
                HookManager.Instance.ModifiersChanged += Instance_ModifiersChanged;
                HookManager.Instance.ModifierStateChanged += Instance_ModifierStateChanged;
                HookManager.Instance.KeyEvent += Instance_KeyEvent;
            }
        }

        public IEnumerable<KeyValuePair<Key, bool>> Modifiers { get => HookManager.Instance.ModifierStates.AsEnumerable().ToList(); }

        private void Instance_KeyEvent(object sender, Utility.KeyboardHookEventArgs e)
        {
            Console.WriteLine($"Key: {e.Key} just did: {e.Type}.");
        }

        private void Instance_ModifierStateChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(Modifiers));
        }

        private void Instance_ModifiersChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(Modifiers));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    HookManager.Instance.ModifiersChanged -= Instance_ModifiersChanged;
                    HookManager.Instance.ModifierStateChanged -= Instance_ModifierStateChanged;
                    HookManager.Instance.KeyEvent -= Instance_KeyEvent;
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}
