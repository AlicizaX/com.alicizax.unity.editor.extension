#if !UNITY_6000_3_OR_NEWER

namespace Paps.UnityToolbarExtenderUIToolkit
{
    internal abstract class RecommendedStyle
    {
        public void Apply(bool isInsideGroup)
        {
            if (isInsideGroup)
                ApplyInsideGroupStyle();
            else
                ApplyRootElementStyle();
        }

        protected virtual void ApplyRootElementStyle() { }
        protected virtual void ApplyInsideGroupStyle() { }
    }
}
#endif // !UNITY_6000_3_OR_NEWER
