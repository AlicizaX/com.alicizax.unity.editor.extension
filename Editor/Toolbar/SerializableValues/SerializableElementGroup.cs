#if !UNITY_6000_3_OR_NEWER

using System.Collections.Generic;

namespace Paps.UnityToolbarExtenderUIToolkit
{
    internal class SerializableElementGroup
    {
        public Dictionary<string, SerializableElement> SerializableElements = new Dictionary<string, SerializableElement>();
    }
}
#endif // !UNITY_6000_3_OR_NEWER
