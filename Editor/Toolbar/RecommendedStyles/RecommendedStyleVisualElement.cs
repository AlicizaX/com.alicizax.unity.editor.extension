#if !UNITY_6000_3_OR_NEWER

using UnityEngine.UIElements;

namespace Paps.UnityToolbarExtenderUIToolkit
{
    internal class RecommendedStyleVisualElement
    {
        public VisualElement VisualElement { get; }
        public bool IsInsideGroup { get; }
        
        public RecommendedStyleVisualElement(VisualElement visualElement, bool isInsideGroup)
        {
            VisualElement = visualElement;
            IsInsideGroup = isInsideGroup;
        }
    }
}
#endif // !UNITY_6000_3_OR_NEWER
