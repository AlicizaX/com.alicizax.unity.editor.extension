#if UNITY_6000_3_OR_NEWER
using AlicizaX.Resource.Runtime;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace AlicizaX.Editor.Extension
{
    [InitializeOnLoad]
    public static class ResourceModeToolbarDropdown
    {
        private const string ElementPath = "AlicizaX/ResourceMode";
        private const string Tooltip = "配置 App 运行时资源模式";

        private static readonly string[] ResourceModeNames =
        {
            "Editor",
            "Offline",
            "Host",
            "Webgl"
        };

        private static readonly Texture2D SettingsIcon;
        private static int s_LastKnownModeIndex;

        static ResourceModeToolbarDropdown()
        {
            SettingsIcon = EditorGUIUtility.IconContent("Settings").image as Texture2D;
            s_LastKnownModeIndex = GetSelectedModeIndex();
            EditorApplication.update += RefreshWhenModeChanged;
        }

        [MainToolbarElement(ElementPath, defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = 101)]
        public static MainToolbarElement CreateElement()
        {
            return new MainToolbarDropdown(
                new MainToolbarContent(GetToolbarLabel(), SettingsIcon, Tooltip),
                ShowDropdownMenu);
        }

        private static void RefreshWhenModeChanged()
        {
            var selectedModeIndex = GetSelectedModeIndex();
            if (selectedModeIndex == s_LastKnownModeIndex)
            {
                return;
            }

            s_LastKnownModeIndex = selectedModeIndex;
            MainToolbar.Refresh(ElementPath);
        }

        private static void ShowDropdownMenu(Rect dropdownRect)
        {
            var currentIndex = GetSelectedModeIndex();
            var menu = new GenericMenu();

            for (var i = 0; i < ResourceModeNames.Length; i++)
            {
                var modeIndex = i;
                menu.AddItem(
                    new GUIContent(ResourceModeNames[modeIndex]),
                    currentIndex == modeIndex,
                    () => SetSelectedMode(modeIndex));
            }

            menu.DropDown(dropdownRect);
        }

        private static void SetSelectedMode(int modeIndex)
        {
            var safeModeIndex = Mathf.Clamp(modeIndex, 0, ResourceModeNames.Length - 1);
            EditorPrefs.SetInt(ResourceComponent.PrefsKey, safeModeIndex);
            s_LastKnownModeIndex = safeModeIndex;
            MainToolbar.Refresh(ElementPath);
        }

        private static int GetSelectedModeIndex()
        {
            var storedIndex = EditorPrefs.GetInt(ResourceComponent.PrefsKey, 0);
            return Mathf.Clamp(storedIndex, 0, ResourceModeNames.Length - 1);
        }

        private static string GetToolbarLabel()
        {
            return $"Res:{ResourceModeNames[GetSelectedModeIndex()]}";
        }
    }
}
#endif
