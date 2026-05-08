#if !UNITY_6000_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Paps.UnityToolbarExtenderUIToolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AlicizaX.Editor.Extension
{
    [MainToolbarElement("EditorQuickToolBar", alignment: ToolbarAlign.Right, order: 1)]
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
            var editorTp = EditorToolFunctionAttributeCollector.Attributes[menuIdx];
            if (editorTp.MethodInfo != null && editorTp.MethodInfo.IsStatic)
            {
                editorTp.MethodInfo.Invoke(null, null);
            }
            else
            {
                Debug.LogError("Method is not static or not found.");
            }
        }
    }
}

#endif
