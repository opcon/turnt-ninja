using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Substructio.Core.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace TurntNinja.Core.Settings
{
    class JsonSettings : IGameSettings
    {
        string _settingsFile;
        PropertySettings _propertySettings;
        Dictionary<string, object> _defaultSettings;
        Dictionary<string, object> _settings;
        bool _loaded;

        public JsonSettings(PropertySettings ps, string settingsFile)
        {
            _propertySettings = ps;
            _settingsFile = settingsFile;
        }
        public object this[string key]
        {
            get
            {
                if (!_loaded) throw new Exception("GameSettings was not loaded before access");
                if (_settings.ContainsKey(key)) return _settings[key];
                throw new GameSettingNotFoundException("The game setting with key {0} was not found", key);
            }
            set
            {
                if (_settings.ContainsKey(key)) _settings[key] = value;
            }
        }

        public void Load()
        {
            _defaultSettings = _propertySettings.GetAllSettings();
            _settings = new Dictionary<string, object>();

            foreach (var kvp in _defaultSettings)
            {
                _settings.Add(kvp.Key, kvp.Value);
            }

            if (File.Exists(_settingsFile))
            {
                var jsonSettings = JArray.Parse(File.ReadAllText(_settingsFile));
                foreach (var jobj in jsonSettings)
                {
                    var value = jobj["Value"].ToObject<string>();
                    var name = jobj["Name"].ToObject<string>();
                    var type = jobj["Type"].ToObject<string>();

                    var valType = Type.GetType(type);
                    var setting = JsonConvert.DeserializeObject(value, valType);

                    // If this setting doesn't exist anymore, skip it
                    if (!_settings.ContainsKey(name)) continue;

                    _settings[name] = setting;
                }
            }
            _loaded = true;
        }

        public void Save()
        {
            var jsonSettings = new JArray();
            foreach (var kvp in _settings)
            {
                // Skip this setting if it has not changed
                if (kvp.Value == _defaultSettings[kvp.Key]) continue;

                // Otherwise setting has changed, save it
                var seralized = JsonConvert.SerializeObject(kvp.Value);
                var jobj = new JObject();
                jobj.Add("Value", seralized);
                jobj.Add(new JProperty("Name", kvp.Key));
                jobj.Add(new JProperty("Type", kvp.Value.GetType().AssemblyQualifiedName));
                jsonSettings.Add(jobj);
            }
            using (StreamWriter file = File.CreateText(_settingsFile))
            using (JsonTextWriter writer = new JsonTextWriter(file))
            {
                writer.Formatting = Formatting.Indented;
                jsonSettings.WriteTo(writer);
            }
        }

        public Dictionary<string, object> GetAllSettings()
        {
            return new Dictionary<string, object>(_settings);
        }
    }
}
