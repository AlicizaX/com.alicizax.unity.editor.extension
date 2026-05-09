#if !UNITY_6000_3_OR_NEWER

using UnityEngine.UIElements;

namespace Paps.UnityToolbarExtenderUIToolkit
{
    internal class NativeToolbarElement
    {
        public string Id { get; }
        public VisualElement VisualElement { get; }

        public NativeToolbarElement(string id, VisualElement visualElement)
        {
            Id = id;
            VisualElement = visualElement;
        }
    }
}
#endif // !UNITY_6000_3_OR_NEWER
