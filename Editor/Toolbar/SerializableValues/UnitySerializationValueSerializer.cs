#if !UNITY_6000_3_OR_NEWER

using Newtonsoft.Json;

namespace Paps.UnityToolbarExtenderUIToolkit
{
    internal class UnitySerializationValueSerializer : IValueSerializer
    {
        public T Deserialize<T>(string serializedValue)
        {
            return JsonConvert.DeserializeObject<T>(serializedValue);
        }

        public string Serialize<T>(T value)
        {
            return JsonConvert.SerializeObject(value, Formatting.Indented);
        }
    }
}
#endif // !UNITY_6000_3_OR_NEWER
