#if !UNITY_6000_3_OR_NEWER

using UnityEditor;

namespace Paps.UnityToolbarExtenderUIToolkit
{
    internal static class MenuItems
    {
        [MenuItem(ToolInfo.EDITOR_MENU_BASE + "/Refresh Toolbar Extender", priority = 1)]
        public static void Refresh()
        {
            MainToolbarAutomaticExtender.Refresh();
        }

        [MenuItem(ToolInfo.EDITOR_MENU_BASE + "/Main Toolbar Control Panel", priority = 12)]
        public static void OpenControlPanel()
        {
            MainToolbarControlPanelWindow.OpenWindow();
        }
    }
}
#endif // !UNITY_6000_3_OR_NEWER
