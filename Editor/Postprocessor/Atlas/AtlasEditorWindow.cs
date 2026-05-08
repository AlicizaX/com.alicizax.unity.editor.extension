#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class AtlasConfigWindow : EditorWindow
{
    [MenuItem("AlicizaX/Extension/图集工具/Configuration Panel")]
    public static void ShowWindow()
    {
        GetWindow<AtlasConfigWindow>("Atlas Config").minSize = new Vector2(500, 600);
    }

    private Vector2 scrollPosition;
    private ReorderableList keywordsList;
    private ReorderableList foldersList;
    private bool showExclusionSettings = true;

    private void OnEnable()
    {
        var config = AtlasConfiguration.Instance;

        // 初始化关键词列表
        keywordsList = CreateKeywordsList(config);

        // 初始化目录列表
        foldersList = CreateFoldersList(config);
    }

    private void OnGUI()
    {
        var config = AtlasConfiguration.Instance;

        using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition))
        {
            scrollPosition = scroll.scrollPosition;

            EditorGUI.BeginChangeCheck();

            DrawDirectorySettings(config);
            DrawPlatformSettings(config);
            DrawPackingSettings(config);
            DrawCompressionSettings(config);
            DrawExclusionSettings(config);
            DrawFeatureToggles(config);

            if (EditorGUI.EndChangeCheck())
            {
                AtlasConfiguration.Save(true);
            }
        }

        DrawActionButtons();
    }

    #region List Creation
    private ReorderableList CreateKeywordsList(AtlasConfiguration config)
    {
        var list = new ReorderableList(config.excludeKeywords, typeof(string), true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Excluded Keywords"),
            elementHeight = EditorGUIUtility.singleLineHeight + 4,
            drawElementCallback = (rect, index, active, focused) =>
            {
                rect.y += 2;
                config.excludeKeywords[index] = EditorGUI.TextField(
                    new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                    config.excludeKeywords[index]
                );
            },
            onAddCallback = _ =>
            {
                ArrayUtility.Add(ref config.excludeKeywords, "");
                EditorGUI.FocusTextInControl(null);
            },
            onRemoveCallback = _ => ArrayUtility.RemoveAt(ref config.excludeKeywords, keywordsList.index)
        };

        return list;
    }

    private ReorderableList CreateFoldersList(AtlasConfiguration config)
    {
        var list = new ReorderableList(config.excludeFolders, typeof(string), true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Excluded Folders"),
            elementHeight = EditorGUIUtility.singleLineHeight + 4,
            drawElementCallback = (rect, index, active, focused) =>
            {
                rect.y += 2;
                var textRect = new Rect(rect.x, rect.y, rect.width - 60, EditorGUIUtility.singleLineHeight);
                var buttonRect = new Rect(rect.x + rect.width - 55, rect.y, 50, EditorGUIUtility.singleLineHeight);

                config.excludeFolders[index] = EditorGUI.TextField(textRect, config.excludeFolders[index]);

                if (GUI.Button(buttonRect, "Browse"))
                {
                    var path = EditorUtility.OpenFolderPanel("Select Folder", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        config.excludeFolders[index] = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                }
            },
            onAddCallback = _ =>
            {
                ArrayUtility.Add(ref config.excludeFolders, "");
                EditorGUI.FocusTextInControl(null);
            },
            onRemoveCallback = _ => ArrayUtility.RemoveAt(ref config.excludeFolders, foldersList.index)
        };

        return list;
    }
    #endregion

    #region Drawing Methods
    private void DrawDirectorySettings(AtlasConfiguration config)
    {
        GUILayout.Label("Directory Settings", EditorStyles.boldLabel);
        config.outputAtlasDir = DrawFolderField("Output Directory", config.outputAtlasDir);
        config.sourceAtlasRoot = DrawFolderField("Source Root", config.sourceAtlasRoot);
        EditorGUILayout.Space();
    }

    private void DrawPlatformSettings(AtlasConfiguration config)
    {
        GUILayout.Label("Platform Settings", EditorStyles.boldLabel);
        config.androidFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("Android Format", config.androidFormat);
        config.iosFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("iOS Format", config.iosFormat);
        config.webglFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("WebGL Format", config.webglFormat);
        EditorGUILayout.Space();
    }

    private void DrawPackingSettings(AtlasConfiguration config)
    {
        GUILayout.Label("Packing Settings", EditorStyles.boldLabel);
        config.padding = EditorGUILayout.IntPopup("Padding", config.padding,
            new[] { "2", "4", "8" }, new[] { 2, 4, 8 });
        config.blockOffset = EditorGUILayout.IntField("Block Offset", config.blockOffset);
        config.enableRotation = EditorGUILayout.Toggle("Enable Rotation", config.enableRotation);
        config.tightPacking = EditorGUILayout.Toggle("Trim Transparency", config.tightPacking);
        EditorGUILayout.Space();
    }

    private void DrawCompressionSettings(AtlasConfiguration config)
    {
        GUILayout.Label("Compression", EditorStyles.boldLabel);
        config.compressionQuality = EditorGUILayout.IntSlider("Quality", config.compressionQuality, 0, 100);
        EditorGUILayout.Space();
    }

    private void DrawExclusionSettings(AtlasConfiguration config)
    {
        showExclusionSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showExclusionSettings, "Exclusion Settings");
        if (showExclusionSettings)
        {
            EditorGUILayout.HelpBox("Items matching these criteria will be excluded from atlas generation",
                MessageType.Info);

            keywordsList.DoLayoutList();
            EditorGUILayout.Space(10);
            foldersList.DoLayoutList();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void DrawFeatureToggles(AtlasConfiguration config)
    {
        GUILayout.Label("Features", EditorStyles.boldLabel);
        config.autoGenerate = EditorGUILayout.Toggle("Auto Generate", config.autoGenerate);
        config.enableLogging = EditorGUILayout.Toggle("Enable Logging", config.enableLogging);
        config.enableV2 = EditorGUILayout.Toggle("Use V2 Packing", config.enableV2);
        EditorGUILayout.Space();
    }

    private string DrawFolderField(string label, string path)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            path = EditorGUILayout.TextField(label, path);
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                var newPath = EditorUtility.OpenFolderPanel(label, Application.dataPath, "");
                if (!string.IsNullOrEmpty(newPath))
                {
                    path = "Assets" + newPath.Substring(Application.dataPath.Length);
                }
            }
        }
        return path;
    }

    private void DrawActionButtons()
    {
        EditorGUILayout.Space(20);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Generate All Atlases", GUILayout.Height(30)))
            {
                EditorSpriteSaveInfo.ForceGenerateAll();
            }

            if (GUILayout.Button("Clear Cache", GUILayout.Height(30)))
            {
                EditorSpriteSaveInfo.ClearCache();
            }
        }
    }
    #endregion
}
#endif
