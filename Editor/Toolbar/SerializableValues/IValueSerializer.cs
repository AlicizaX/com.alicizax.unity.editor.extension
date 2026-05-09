#if !UNITY_6000_3_OR_NEWER

namespace Paps.UnityToolbarExtenderUIToolkit
{
    internal interface IValueSerializer
    {
        string Serialize<T>(T value);
        T Deserialize<T>(string serializedValue);
    }
}
#endif // !UNITY_6000_3_OR_NEWER
