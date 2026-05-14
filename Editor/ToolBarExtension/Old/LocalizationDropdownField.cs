#if !UNITY_6000_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AlicizaX.Localization.Runtime;
using AlicizaX;
using AlicizaX.Localization.Editor;
using Paps.UnityToolbarExtenderUIToolkit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

[MainToolbarElement("LocalizationDropdownField", alignment: ToolbarAlign.Right, order: 0)]
public class LocalizationDropdownField : IMGUIContainer
{
    private GUIContent appConfigBtContent;

    private string[] _languageTypeNames;

    public void InitializeElement()
    {
        _languageTypeNames = LanguageTypes.Languages.ToArray();

        appConfigBtContent =
            EditorGUIUtility.TrTextContentWithIcon("", "",
                "Settings");
        onGUIHandler = MyGUIMethod;
    }

    private void MyGUIMethod()
    {
        GUILayout.BeginHorizontal();
        string title = GetPrefsStr();
        appConfigBtContent.text = title;
        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (EditorGUILayout.DropdownButton(appConfigBtContent, FocusType.Passive, EditorStyles.toolbarPopup, GUILayout.MaxWidth(120)))
            {
                DrawEditorToolDropdownMenus();
            }
        }

        GUILayout.Space(5);
        GUILayout.EndHorizontal();
    }

    void DrawEditorToolDropdownMenus()
    {
        _languageTypeNames = LanguageTypes.Languages.ToArray();
        string langaugeName = GetPrefsStr();
        GenericMenu popMenu = new GenericMenu();
        for (int i = 0; i < _languageTypeNames.Length; i++)
        {
            var toolAttr = _languageTypeNames[i];
            var selected = langaugeName == toolAttr;
            popMenu.AddItem(new GUIContent(toolAttr), selected, menuIdx => { ClickToolsSubmenu(toolAttr); }, toolAttr);
        }

        popMenu.ShowAsContext();
    }

    void ClickToolsSubmenu(string langaugeName)
    {
        EditorPrefs.SetString(LocalizationComponent.PrefsKey, langaugeName);
        InvokeOnValidateInScene();
        Debug.Log(langaugeName);
    }

    string GetPrefsStr()
    {
        return EditorPrefs.GetString(LocalizationComponent.PrefsKey, "None");
    }


    public static void InvokeOnValidateInScene()
    {
        Type targetType = Utility.Assembly.GetType("UnityEngine.UI.UXTextMeshPro");
        MethodInfo onValidateMethod = targetType.GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        if (onValidateMethod == null)
        {
            Debug.LogWarning($"类型 {targetType.Name} 没有找到 OnValidate 方法（可能是用其他签名或不存在）");
            return;
        }

        List<Component> components = new List<Component>();

        components = GameObject.FindObjectsOfType(targetType).ToList().Select(t => (t as Component)).ToList();

        if (components.Count == 0)
        {
            Debug.Log("未在场景中找到任何目标组件。");
            return;
        }

        int count = 0;

        var scenesToMarkDirty = EditorSceneManager.GetActiveScene();

        foreach (var comp in components)
        {
            Undo.RecordObject(comp, "Invoke OnValidate");
            onValidateMethod.Invoke(comp, null);
            EditorUtility.SetDirty(comp);
            count++;
        }

        if (count > 0)
        {
            EditorSceneManager.MarkSceneDirty(scenesToMarkDirty);
        }
    }
}

#endif
