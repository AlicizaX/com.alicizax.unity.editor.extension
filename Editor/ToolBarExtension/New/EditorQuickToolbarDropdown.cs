#if UNITY_6000_3_OR_NEWER
using System;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace AlicizaX.Editor.Extension
{
    [InitializeOnLoad]
    public static class EditorQuickToolbarDropdown
    {
        private const string ElementPath = "AlicizaX/EditorQuickTools";
        private const string Tooltip = "Open editor quick tools";

        private static readonly Texture2D ToolIcon;

        static EditorQuickToolbarDropdown()
        {
            ToolIcon = GetIcon("CustomTool") ?? GetIcon("Settings");
        }

        [MainToolbarElement(ElementPath, defaultDockPosition = MainToolbarDockPosition.Right, defaultDockIndex = 1)]
        public static MainToolbarElement CreateElement()
        {
            return new MainToolbarDropdown(
                new MainToolbarContent("Tools", ToolIcon, Tooltip),
                ShowDropdownMenu);
        }

        private static void ShowDropdownMenu(Rect dropdownRect)
        {
            var menu = new GenericMenu();

            if (EditorToolFunctionAttributeCollector.Attributes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No tools found"));
                menu.DropDown(dropdownRect);
                return;
            }

            foreach (var toolEntry in EditorToolFunctionAttributeCollector.Attributes)
            {
                var capturedToolEntry = toolEntry;
                menu.AddItem(
                    new GUIContent(capturedToolEntry.ToolMenuPath),
                    false,
                    () => InvokeTool(capturedToolEntry));
            }

            menu.DropDown(dropdownRect);
        }

        private static void InvokeTool(EditorToolFunctionAttribute toolEntry)
        {
            if (toolEntry.MethodInfo == null || !toolEntry.MethodInfo.IsStatic)
            {
                Debug.LogError("Tool method is not static or could not be found.");
                return;
            }

            if (toolEntry.MethodInfo.GetParameters().Length != 0)
            {
                Debug.LogError($"Tool method '{toolEntry.MethodInfo.Name}' must be parameterless.");
                return;
            }

            try
            {
                toolEntry.MethodInfo.Invoke(null, null);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static Texture2D GetIcon(string iconName)
        {
            return EditorGUIUtility.IconContent(iconName).image as Texture2D;
        }
    }
}
#endif
