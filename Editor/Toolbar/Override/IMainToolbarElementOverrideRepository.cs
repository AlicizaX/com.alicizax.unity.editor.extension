#if !UNITY_6000_3_OR_NEWER

namespace Paps.UnityToolbarExtenderUIToolkit
{
    internal interface IMainToolbarElementOverrideRepository
    {
        public MainToolbarElementOverride? Get(string elementId);
        public MainToolbarElementOverride[] GetAll();
        public void Save(MainToolbarElementOverride elementOverride);
        public void Clear();
    }
}
#endif // !UNITY_6000_3_OR_NEWER
