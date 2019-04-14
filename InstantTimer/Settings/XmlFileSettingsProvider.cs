using InstantTimer.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace InstantTimer.Settings
{
    class XmlFileSettingsProvider : ISettingsProvider
    {
        private string _fileName = "settings.xml";
        private SettingsData _settings = new SettingsData();

        private string FullFilePath => Path.Combine(WpfUtil.AssemblyDirectory, _fileName);
        public SettingsData Settings
        {
            get => _settings;
            private set
            {
                _settings.CopyFrom(value);
            }
        }

        public void Load()
        {
            if (File.Exists(FullFilePath))
            {
                XmlSerializer s = new XmlSerializer(typeof(SettingsData));
                using (TextReader tr = new StreamReader(FullFilePath, Encoding.UTF8))
                {
                    SettingsData newSettings = s.Deserialize(tr) as SettingsData;
                    if (newSettings != null) Settings = newSettings;
                }
            }
        }

        public void Reset()
        {
            Settings = new SettingsData();
            Save();
        }

        public void Save()
        {
            XmlSerializer s = new XmlSerializer(typeof(SettingsData));
            using (TextWriter w = new StreamWriter(FullFilePath, false))
            {
                s.Serialize(w, Settings);
                w.Flush();
            }
        }
    }
}
