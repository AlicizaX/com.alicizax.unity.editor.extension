#if !UNITY_6000_3_OR_NEWER

namespace Paps.UnityToolbarExtenderUIToolkit
{
    internal interface IMainToolbarElementVariableRepository
    {
        public void Set(SerializableElement serializableElement);
        public void SetAll(SerializableElement[] serializableElements);
        public SerializableElement[] GetAll();
        public void Save();
    }
}
#endif // !UNITY_6000_3_OR_NEWER
