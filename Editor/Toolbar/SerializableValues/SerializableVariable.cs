#if !UNITY_6000_3_OR_NEWER

using System;

namespace Paps.UnityToolbarExtenderUIToolkit
{
    internal struct SerializableVariable
    {
        public ValueHolderType Type;
        public string Key;
        public Type ValueType;
        public object Value;
    }
}
#endif // !UNITY_6000_3_OR_NEWER
