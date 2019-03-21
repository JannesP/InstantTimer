using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using System.Xml.Serialization;

namespace InstantTimer.Settings
{
    [XmlRoot("Settings")]
    public sealed class SettingsData : INotifyPropertyChanged
    {
        private Key[] _instantTimerModifierKeys = new[] { Key.RightCtrl, Key.RightShift };

        public Key[] InstantTimerModifierKeys
        {
            get => _instantTimerModifierKeys;
            set
            {
                _instantTimerModifierKeys = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void CopyFrom(SettingsData other)
        {
            IEnumerable<PropertyInfo> props = typeof(SettingsData).GetProperties().Where(p => p.CanRead && p.CanWrite && p.GetSetMethod(true).IsPublic);
            foreach (PropertyInfo pi in props)
            {
                pi.SetValue(this, pi.GetValue(other));
            }
        }
    }
}
