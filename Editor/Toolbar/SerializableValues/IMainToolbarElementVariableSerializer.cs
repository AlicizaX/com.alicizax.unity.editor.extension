#if !UNITY_6000_3_OR_NEWER

namespace Paps.UnityToolbarExtenderUIToolkit
{
    internal interface IMainToolbarElementVariableSerializer
    {
        public string Serialize(SerializableElementGroup serializableElementGroup);
        public SerializableElementGroup Deserialize(string serializedElementGroup);
    }
}
#endif // !UNITY_6000_3_OR_NEWER
