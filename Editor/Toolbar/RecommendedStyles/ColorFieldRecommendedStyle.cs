#if !UNITY_6000_3_OR_NEWER

using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Paps.UnityToolbarExtenderUIToolkit
{
    internal class ColorFieldRecommendedStyle : RecommendedStyle
    {
        private readonly ColorField _colorField;

        public ColorFieldRecommendedStyle(ColorField colorField)
        {
            _colorField = colorField;
        }

        protected override void ApplyRootElementStyle() 
        {
            _colorField.labelElement.style.minWidth = Length.Auto();
        }
    }
}
#endif // !UNITY_6000_3_OR_NEWER
