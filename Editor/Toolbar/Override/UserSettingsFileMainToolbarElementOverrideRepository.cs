#if !UNITY_6000_3_OR_NEWER

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Paps.UnityToolbarExtenderUIToolkit
{
    internal class UserSettingsFileMainToolbarElementOverrideRepository : IMainToolbarElementOverrideRepository
    {
        private static readonly string DIRECTORY = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "UserSettings/", "unity-toolbar-extender-ui-toolkit", "overrides");
        private static readonly string FILE = Path.Combine(DIRECTORY, "overrides.json");

        private struct SerializableOverride
        {
            public string ElementId;
            public bool Visible;
        }

        private Dictionary<string, MainToolbarElementOverride> _overrides = new Dictionary<string, MainToolbarElementOverride>();

        public UserSettingsFileMainToolbarElementOverrideRepository()
        {
            _overrides = LoadOverrides();
        }

        public void Clear()
        {
            _overrides.Clear();
            DeleteSave();
        }

        public MainToolbarElementOverride? Get(string elementId)
        {
            if(_overrides.ContainsKey(elementId))
                return _overrides[elementId];

            return null;
        }

        public MainToolbarElementOverride[] GetAll()
        {
            return _overrides.Values.ToArray();
        }

        public void Save(MainToolbarElementOverride elementOverride)
        {
            _overrides[elementOverride.ElementId] = elementOverride;
            SaveOverrides();
        }

        private MainToolbarElementOverride FromSerialized(SerializableOverride serializableOverride)
        {
            return new MainToolbarElementOverride(serializableOverride.ElementId, serializableOverride.Visible);
        }

        private SerializableOverride ToSerialized(MainToolbarElementOverride mainToolbarElementOverride)
        {
            return new SerializableOverride() {
                ElementId = mainToolbarElementOverride.ElementId,
                Visible = mainToolbarElementOverride.Visible
            };
        }

        private void DeleteSave()
        {
            File.Delete(FILE);
        }

        private Dictionary<string, MainToolbarElementOverride> LoadOverrides()
        {
            if (!Directory.Exists(DIRECTORY))
                Directory.CreateDirectory(DIRECTORY);

            if (!File.Exists(FILE))
                return JsonConvert.DeserializeObject<Dictionary<string, MainToolbarElementOverride>>("{}");

            var json = File.ReadAllText(FILE);

            var serializedDictionary = JsonConvert.DeserializeObject<Dictionary<string, SerializableOverride>>(json);

            return serializedDictionary.Values
                .ToDictionary(serializedOverride => serializedOverride.ElementId,
                serializedOverride => FromSerialized(serializedOverride));
        }

        private void SaveOverrides()
        {
            var serializableDictionary = _overrides.Values.ToDictionary(
                mainToolbarElementOverride => mainToolbarElementOverride.ElementId,
                mainToolbarElementOverride => ToSerialized(mainToolbarElementOverride)
                );

            var json = JsonConvert.SerializeObject(serializableDictionary);

            File.WriteAllText(FILE, json);
        }
    }
}
#endif // !UNITY_6000_3_OR_NEWER
