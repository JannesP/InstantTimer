using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstantTimer.Settings
{
    interface ISettingsProvider
    {
        /// <summary>
        /// Resets the settings back to their default values. And removes the ability to get them back with Load().
        /// </summary>
        void Reset();
        /// <summary>
        /// the settings
        /// </summary>
        SettingsData Settings { get; }
        /// <summary>
        /// Saves the current settings so that they can later be loaded with Load().
        /// </summary>
        void Save();
        /// <summary>
        /// Loads the settings.
        /// </summary>
        void Load();
    }
}
