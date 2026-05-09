#if !UNITY_6000_3_OR_NEWER

namespace Paps.UnityToolbarExtenderUIToolkit
{
    internal class SerializableElement
    {
        public string ElementFullTypeName;
        public SerializableVariable[] Variables = new SerializableVariable[0];
    }
}
#endif // !UNITY_6000_3_OR_NEWER
