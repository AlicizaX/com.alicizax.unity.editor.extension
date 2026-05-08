#if !UNITY_6000_3_OR_NEWER
using System.Collections.Generic;
using System.Linq;
using AlicizaX.Resource.Runtime;
using Paps.UnityToolbarExtenderUIToolkit;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace AlicizaX.Editor.Extension
{
    [MainToolbarElement("ResourceModeDropdownField", alignment: ToolbarAlign.Right, order: 0)]
    public class ResourceModeDropdownField : IMGUIContainer
    {
        private GUIContent appConfigBtContent;

        private readonly string[] _resourceModeNames =
        {
            "Editor",
            "Offline",
            "Host",
            "Webgl"
        };

        public void InitializeElement()
        {
            appConfigBtContent = EditorGUIUtility.TrTextContentWithIcon("Res:", "配置App运行时资源模式", "Settings");
            onGUIHandler = MyGUIMethod;
        }

        private void MyGUIMethod()
        {
            GUILayout.BeginHorizontal();
            string title = "Res:" + _resourceModeNames[EditorPrefs.GetInt(ResourceComponent.PrefsKey, 0)];
            appConfigBtContent.text = title;
            if (EditorGUILayout.DropdownButton(appConfigBtContent, FocusType.Passive, EditorStyles.toolbarPopup, GUILayout.MaxWidth(90)))
            {
                DrawEditorToolDropdownMenus();
            }

            GUILayout.Space(5);
            GUILayout.EndHorizontal();
        }

        void DrawEditorToolDropdownMenus()
        {
            int index = EditorPrefs.GetInt(ResourceComponent.PrefsKey, 0);
            GenericMenu popMenu = new GenericMenu();
            for (int i = 0; i < _resourceModeNames.Length; i++)
            {
                var selected = index == i;
                var toolAttr = _resourceModeNames[i];
                popMenu.AddItem(new GUIContent(toolAttr), selected, menuIdx => { ClickToolsSubmenu((int)menuIdx); }, i);
            }

            popMenu.ShowAsContext();
        }

        void ClickToolsSubmenu(int menuIdx)
        {
            EditorPrefs.SetInt(ResourceComponent.PrefsKey, menuIdx);
        }
    }
}

#endif
