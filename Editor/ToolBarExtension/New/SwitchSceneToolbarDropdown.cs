#if UNITY_6000_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;

namespace AlicizaX.Editor.Extension
{
    [InitializeOnLoad]
    public static class SwitchSceneToolbarDropdown
    {
        private const string ElementPath = "AlicizaX/SwitchScene";
        private const string Tooltip = "Switch the active scene";

        private static readonly string[] SearchRoots =
        {
            "Assets/Bundles/",
            "Assets/Scenes/"
        };

        private static readonly Texture2D UnityLogoIcon;

        private struct SceneEntry
        {
            public string ScenePath;
            public string DisplayName;
        }

        static SwitchSceneToolbarDropdown()
        {
            UnityLogoIcon = GetIcon("UnityLogo");
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
        }

        [MainToolbarElement(ElementPath, defaultDockPosition = MainToolbarDockPosition.Left, defaultDockIndex = 1)]
        public static MainToolbarElement CreateElement()
        {
            return new MainToolbarDropdown(
                new MainToolbarContent(GetToolbarLabel(), UnityLogoIcon, Tooltip),
                ShowDropdownMenu);
        }

        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            MainToolbar.Refresh(ElementPath);
        }

        private static void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene previousScene, UnityEngine.SceneManagement.Scene newScene)
        {
            MainToolbar.Refresh(ElementPath);
        }

        private static void ShowDropdownMenu(Rect dropdownRect)
        {
            var menu = new GenericMenu
            {
                allowDuplicateNames = true
            };

            var scenes = CollectScenes();
            var activeScenePath = EditorSceneManager.GetActiveScene().path;

            if (scenes.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No scenes found"));
                menu.DropDown(dropdownRect);
                return;
            }

            for (var i = 0; i < scenes.Count; i++)
            {
                var sceneEntry = scenes[i];
                menu.AddItem(
                    new GUIContent(sceneEntry.DisplayName),
                    string.Equals(sceneEntry.ScenePath, activeScenePath, StringComparison.OrdinalIgnoreCase),
                    () => SwitchScene(sceneEntry.ScenePath));
            }

            menu.DropDown(dropdownRect);
        }

        private static List<SceneEntry> CollectScenes()
        {
            var sceneEntries = new List<SceneEntry>();
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", SearchRoots);

            foreach (var sceneGuid in sceneGuids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuid);
                sceneEntries.Add(new SceneEntry
                {
                    ScenePath = scenePath,
                    DisplayName = BuildDisplayName(scenePath)
                });
            }

            sceneEntries.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));
            return sceneEntries;
        }

        private static string BuildDisplayName(string scenePath)
        {
            var sceneName = Path.GetFileNameWithoutExtension(scenePath);
            var sceneDirectory = NormalizePath(Path.GetDirectoryName(scenePath));

            foreach (var searchRoot in SearchRoots)
            {
                var normalizedRoot = NormalizePath(searchRoot).TrimEnd('/');
                if (!sceneDirectory.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(sceneDirectory, normalizedRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return sceneName;
                }

                var relativeDirectory = Path.GetRelativePath(normalizedRoot, sceneDirectory)
                    .Replace('\\', '/')
                    .Trim('/');

                return string.IsNullOrEmpty(relativeDirectory)
                    ? sceneName
                    : $"{relativeDirectory}/{sceneName}";
            }

            return sceneName;
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path)
                ? string.Empty
                : path.Replace('\\', '/');
        }

        private static void SwitchScene(string scenePath)
        {
            var currentScene = EditorSceneManager.GetActiveScene();
            if (currentScene.IsValid() && currentScene.isDirty)
            {
                var optionIndex = EditorUtility.DisplayDialogComplex(
                    "Warning",
                    $"Current scene '{currentScene.name}' has unsaved changes. Save before switching?",
                    "Save",
                    "Cancel",
                    "Don't Save");

                switch (optionIndex)
                {
                    case 0:
                        if (!EditorSceneManager.SaveOpenScenes())
                        {
                            return;
                        }

                        break;
                    case 1:
                        return;
                }
            }

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        private static string GetToolbarLabel()
        {
            var activeSceneName = EditorSceneManager.GetActiveScene().name;
            return string.IsNullOrEmpty(activeSceneName) ? "Switch Scene" : activeSceneName;
        }

        private static Texture2D GetIcon(string iconName)
        {
            return EditorGUIUtility.IconContent(iconName).image as Texture2D;
        }
    }
}
#endif
