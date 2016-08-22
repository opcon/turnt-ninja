using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using Gwen.Control;
using Substructio.Core.Settings;

namespace TurntNinja.Core.Settings
{
    class PropertySettings : IGameSettings
    {
        private bool _loaded;
        private Dictionary<string, SettingsPropertyValue> _settings;

        public object this[string key]
        {
            get
            {
                if (!_loaded) throw new Exception("GameSettings was not loaded before access");
                if (_settings.ContainsKey(key)) return _settings[key].PropertyValue;
                throw new GameSettingNotFoundException("The game setting with key {0} was not found", key);
            }
            set { if (_settings.ContainsKey(key)) _settings[key].PropertyValue = value; }
        }

        public void Save()
        {
            Properties.Settings.Default.Save();

        }

        public void Load()
        {
            if (_loaded) throw new Exception("Game settings already loaded, can't load again");
            if (Properties.Settings.Default.UpgradeRequired)
            {
                Console.WriteLine("Upgrading settings");
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeRequired = false;
                //Properties.Settings.Default.Save();
            }

            Properties.Settings.Default.Reload();
            //load all the settings
            _settings = new Dictionary<string, SettingsPropertyValue>();
            SettingsPropertyValue[] values = new SettingsPropertyValue[Properties.Settings.Default.PropertyValues.Count];
            Properties.Settings.Default.PropertyValues.CopyTo(values, 0);
            foreach (SettingsPropertyValue propertyValue in values)
            {
                _settings.Add(propertyValue.Name, propertyValue);
            }
            _loaded = true;
        }
    }

    public class GameSettingNotFoundException : Exception
    {
        public GameSettingNotFoundException() : base()
        {
        }

        public GameSettingNotFoundException(string message) : base(message)
        {
        }

        public GameSettingNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public GameSettingNotFoundException(string format, params object[] args) : base(string.Format(format, args))
        {
        }
    }
}
