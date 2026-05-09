#if !UNITY_6000_3_OR_NEWER

namespace Paps.UnityToolbarExtenderUIToolkit
{
    internal interface IMainToolbarElementRepository
    {
        public MainToolbarElement[] GetAll();
    }
}
#endif // !UNITY_6000_3_OR_NEWER
