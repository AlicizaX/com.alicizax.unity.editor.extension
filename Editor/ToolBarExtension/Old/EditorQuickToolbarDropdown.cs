#if !UNITY_6000_3_OR_NEWER
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AlicizaX.Editor.Extension
{
    public class EditorQuickToolBar : IMGUIContainer
    {
        private GUIContent toolsDropBtContent;

        public void InitializeElement()
        {
            toolsDropBtContent = EditorGUIUtility.TrTextContentWithIcon("Tools", "工具箱", "CustomTool");
            onGUIHandler = MyGUIMethod;
        }

        private void MyGUIMethod()
        {
            GUILayout.BeginHorizontal();
            if (EditorGUILayout.DropdownButton(toolsDropBtContent, FocusType.Passive, EditorStyles.toolbarPopup,
                    GUILayout.MaxWidth(90)))
            {
                DrawEditorToolDropdownMenus();
            }

            GUILayout.Space(5);
            GUILayout.EndHorizontal();
        }

        void DrawEditorToolDropdownMenus()
        {
            GenericMenu popMenu = new GenericMenu();
            if (EditorToolFunctionAttributeCollector.Attributes.Count == 0)
            {
                popMenu.AddDisabledItem(new GUIContent("No tools found"));
                popMenu.ShowAsContext();
                return;
            }

            for (int i = 0; i < EditorToolFunctionAttributeCollector.Attributes.Count; i++)
            {
                var toolAttr = EditorToolFunctionAttributeCollector.Attributes[i];
                popMenu.AddItem(new GUIContent(toolAttr.ToolMenuPath), false,
                    menuIdx => { ClickToolsSubmenu((int)menuIdx); }, i);
            }

            popMenu.ShowAsContext();
        }

        void ClickToolsSubmenu(int menuIdx)
        {
            InvokeTool(EditorToolFunctionAttributeCollector.Attributes[menuIdx]);
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
    }
}

#endif
