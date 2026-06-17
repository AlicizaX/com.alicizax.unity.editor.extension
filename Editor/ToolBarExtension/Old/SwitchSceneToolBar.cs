#if !UNITY_6000_3_OR_NEWER
using System.Collections.Generic;
using System.IO;
using AlicizaX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class SwitchSceneToolBar : IMGUIContainer
{
    private GUIContent switchSceneBtContent;

    public void InitializeElement()
    {
        var curOpenSceneName = EditorSceneManager.GetActiveScene().name;
        switchSceneBtContent = EditorGUIUtility.TrTextContentWithIcon(
            string.IsNullOrEmpty(curOpenSceneName) ? "Switch Scene" : curOpenSceneName, "切换场景", "UnityLogo");
        onGUIHandler = MyGUIMethod;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        switchSceneBtContent.text = scene.name;
    }

    private static List<string> sceneAssetList = new List<string>();

    private void MyGUIMethod()
    {
        GUILayout.BeginHorizontal();

        if (EditorGUILayout.DropdownButton(switchSceneBtContent, FocusType.Passive, EditorStyles.toolbarPopup,
                GUILayout.MaxWidth(150)))
        {
            DrawSwithSceneDropdownMenus();
        }

        GUILayout.EndHorizontal();
    }

    static string[] ScenePath = new[] { "Assets/Bundles/", "Assets/Scenes/" };
    private static string RootScenePath = "Assets/Bundles/";
    private static string BundleScenePath = "Assets/Scenes/";

    static void DrawSwithSceneDropdownMenus()
    {
        GenericMenu popMenu = new GenericMenu();
        popMenu.allowDuplicateNames = true;
        var sceneGuids = AssetDatabase.FindAssets("t:Scene", ScenePath);
        sceneAssetList.Clear();
        for (int i = 0; i < sceneGuids.Length; i++)
        {
            var scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
            sceneAssetList.Add(scenePath);
            string fileDir = Path.GetDirectoryName(scenePath);
            bool isInRootDir = Utility.Path.GetRegularPath(BundleScenePath).TrimEnd('/') ==
                               Utility.Path.GetRegularPath(fileDir).TrimEnd('/');
            var sceneName = Path.GetFileNameWithoutExtension(scenePath);
            string displayName = sceneName;
            if (!isInRootDir)
            {
                var sceneDir = Path.GetRelativePath(RootScenePath, fileDir);
                displayName = $"{sceneDir}/{sceneName}";
            }

            popMenu.AddItem(new GUIContent(displayName), false, menuIdx => { SwitchScene((int)menuIdx); }, i);
        }

        popMenu.ShowAsContext();
    }

    private static void SwitchScene(int menuIdx)
    {
        if (menuIdx >= 0 && menuIdx < sceneAssetList.Count)
        {
            var scenePath = sceneAssetList[menuIdx];
            var curScene = EditorSceneManager.GetActiveScene();
            if (curScene != null && curScene.isDirty)
            {
                int opIndex =
                    EditorUtility.DisplayDialogComplex("警告", $"当前场景{curScene.name}未保存,是否保存?", "保存", "取消", "不保存");
                switch (opIndex)
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
    }
}

#endif
