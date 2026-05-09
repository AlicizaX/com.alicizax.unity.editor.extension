#if !UNITY_6000_3_OR_NEWER

namespace Paps.UnityToolbarExtenderUIToolkit
{
    internal interface IGroupDefinitionRepository
    {
        public GroupDefinition[] GetAll();
    }
}
#endif // !UNITY_6000_3_OR_NEWER
