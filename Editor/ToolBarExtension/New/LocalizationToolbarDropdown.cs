#if UNITY_6000_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AlicizaX.Localization.Editor;
using AlicizaX.Localization.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;

namespace AlicizaX.Editor.Extension
{
    [InitializeOnLoad]
    public static class LocalizationToolbarDropdown
    {
        private const string ElementPath = "AlicizaX/Localization";
        private const string Tooltip = "Switch editor localization preview language";

        private static readonly Texture2D SettingsIcon;
        private static string s_LastKnownLanguage;
        private static int s_LastKnownLanguageCount;

        static LocalizationToolbarDropdown()
        {
            SettingsIcon = GetIcon("Settings");
            s_LastKnownLanguage = GetSelectedLanguage();
            s_LastKnownLanguageCount = GetLanguageTypeNames().Count;
            EditorApplication.update += RefreshWhenLanguageChanges;
        }

        [MainToolbarElement(ElementPath, defaultDockPosition = MainToolbarDockPosition.Right, defaultDockIndex = 0)]
        public static MainToolbarElement CreateElement()
        {
            return new MainToolbarDropdown(
                new MainToolbarContent(GetToolbarLabel(), SettingsIcon, Tooltip),
                ShowDropdownMenu);
        }

        private static void RefreshWhenLanguageChanges()
        {
            var selectedLanguage = GetSelectedLanguage();
            var languageCount = GetLanguageTypeNames().Count;
            if (selectedLanguage == s_LastKnownLanguage && languageCount == s_LastKnownLanguageCount)
            {
                return;
            }

            s_LastKnownLanguage = selectedLanguage;
            s_LastKnownLanguageCount = languageCount;
            MainToolbar.Refresh(ElementPath);
        }

        private static void ShowDropdownMenu(Rect dropdownRect)
        {
            var selectedLanguage = GetSelectedLanguage();
            var menu = new GenericMenu();
            var languageNames = GetLanguageTypeNames();

            if (languageNames.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No language options"));
                menu.DropDown(dropdownRect);
                return;
            }

            foreach (var languageName in languageNames)
            {
                var capturedLanguageName = languageName;
                menu.AddItem(
                    new GUIContent(capturedLanguageName),
                    string.Equals(selectedLanguage, capturedLanguageName, StringComparison.Ordinal),
                    () => SetSelectedLanguage(capturedLanguageName));
            }

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                menu.DropDown(dropdownRect);
            }
        }

        private static void SetSelectedLanguage(string languageName)
        {
            EditorPrefs.SetString(LocalizationComponent.PrefsKey, languageName);
            s_LastKnownLanguage = languageName;
            InvokeOnValidateInScene();
            MainToolbar.Refresh(ElementPath);
        }

        private static IReadOnlyList<string> GetLanguageTypeNames()
        {
            return LanguageTypes.Languages;
        }

        private static string GetSelectedLanguage()
        {
            return EditorPrefs.GetString(LocalizationComponent.PrefsKey, "None");
        }

        private static string GetToolbarLabel()
        {
            return GetSelectedLanguage();
        }

        private static void InvokeOnValidateInScene()
        {
            var targetType = FindType("UnityEngine.UI.UXTextMeshPro");
            if (targetType == null)
            {
                Debug.LogWarning("Could not find type UnityEngine.UI.UXTextMeshPro.");
                return;
            }

            var onValidateMethod = targetType.GetMethod(
                "OnValidate",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (onValidateMethod == null)
            {
                Debug.LogWarning($"Could not find OnValidate on {targetType.Name}.");
                return;
            }

            var components = GameObject.FindObjectsOfType(targetType)
                .OfType<Component>()
                .ToList();

            if (components.Count == 0)
            {
                return;
            }

            var sceneToMarkDirty = EditorSceneManager.GetActiveScene();

            foreach (var component in components)
            {
                Undo.RecordObject(component, "Invoke OnValidate");
                onValidateMethod.Invoke(component, null);
                EditorUtility.SetDirty(component);
            }

            EditorSceneManager.MarkSceneDirty(sceneToMarkDirty);
        }

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static Texture2D GetIcon(string iconName)
        {
            return EditorGUIUtility.IconContent(iconName).image as Texture2D;
        }
    }
}
#endif
