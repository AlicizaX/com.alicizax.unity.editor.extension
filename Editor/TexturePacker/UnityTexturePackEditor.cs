using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public class UnityTexturePackerEditor : EditorWindow
{
    private static readonly string[] MaxAtlasSizeLabels = { "1024", "2048", "4096" };
    private static readonly int[] MaxAtlasSizeValues = { 1024, 2048, 4096 };

    private List<Texture2D> textures = new List<Texture2D>();
    private Vector2 scrollPos;
    private string outputFolder = "Assets/TexturePackerOutput";
    private string atlasFileName = "atlas.png";
    private string jsonFileName = "atlas.json";
    private int maxAtlasSize = 4096;
    private int padding = 2;
    private bool overwriteExisting = true;
    private string lastJson = "";
    private string lastAtlasPath = "";

    [MenuItem("AlicizaX/Extension/Texture Packer")]
    public static void ShowWindow()
    {
        var w = GetWindow<UnityTexturePackerEditor>("Texture Packer");
        w.minSize = new Vector2(450, 300);
    }

    private void OnGUI()
    {
        GUILayout.Label("Texture Packer (for TMP-compatible atlases)", EditorStyles.boldLabel);
        DrawDragDropArea();
        GUILayout.Space(6);
        GUILayout.Label($"Textures ({textures.Count})", EditorStyles.label);
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(160));
        for (int i = 0; i < textures.Count; i++)
        {
            GUILayout.BeginHorizontal("box");
            Texture2D tex = textures[i];
            Texture preview = AssetPreview.GetAssetPreview(tex) ?? tex;
            GUILayout.Label(preview, GUILayout.Width(64), GUILayout.Height(64));
            GUILayout.BeginVertical();
            GUILayout.Label(tex.name, EditorStyles.boldLabel);
            string path = AssetDatabase.GetAssetPath(tex);
            GUILayout.Label($"{tex.width}x{tex.height}  —  {path}");
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("▲", GUILayout.Width(24))) { if (i > 0) { var t = textures[i - 1]; textures[i - 1] = textures[i]; textures[i] = t; } }
            if (GUILayout.Button("▼", GUILayout.Width(24))) { if (i < textures.Count - 1) { var t = textures[i + 1]; textures[i + 1] = textures[i]; textures[i] = t; } }
            if (GUILayout.Button("Remove", GUILayout.Width(64))) { textures.RemoveAt(i); }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
        GUILayout.Space(8);
        GUILayout.Label("Output Settings", EditorStyles.boldLabel);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Choose..."))
        {
            string folder = EditorUtility.OpenFolderPanel("Select output folder", "", "");
            if (!string.IsNullOrEmpty(folder))
            {
                if (folder.StartsWith(Application.dataPath))
                {
                    outputFolder = "Assets" + folder.Substring(Application.dataPath.Length);
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid Folder", "Please select a folder inside this Unity project (under Assets).", "OK");
                }
            }
        }
        GUILayout.EndHorizontal();
        atlasFileName = EditorGUILayout.TextField("Atlas Filename", atlasFileName);
        jsonFileName = EditorGUILayout.TextField("JSON Filename", jsonFileName);
        maxAtlasSize = EditorGUILayout.IntPopup("Max Atlas Size", maxAtlasSize, MaxAtlasSizeLabels, MaxAtlasSizeValues);
        padding = EditorGUILayout.IntField("Padding (px)", padding);
        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
        GUILayout.Space(6);
        if (GUILayout.Button("Build Atlas", GUILayout.Height(30)))
        {
            if (textures.Count == 0)
            {
                EditorUtility.DisplayDialog("No textures", "Please drag textures into the list before building.", "OK");
            }
            else
            {
                BuildAtlas();
            }
        }
        GUILayout.Space(10);
        GUILayout.Label("Last result", EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(lastAtlasPath, GUILayout.Height(16));
        EditorGUILayout.TextArea(lastJson, GUILayout.Height(120));
        GUILayout.FlexibleSpace();
        GUILayout.Label("Notes: Output atlas is created as an uncompressed PNG and imported as multiple sprites so TextMeshPro can use it.", EditorStyles.wordWrappedLabel);
    }

    private void DrawDragDropArea()
    {
        var evt = Event.current;
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 80.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag textures here (Project or Explorer)", EditorStyles.helpBox);
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition)) return;
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj is Texture2D t)
                        {
                            AddTexture(t);
                        }
                        else if (obj is DefaultAsset)
                        {
                            string path = AssetDatabase.GetAssetPath(obj);
                            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { path });
                            foreach (var g in guids)
                            {
                                string p = AssetDatabase.GUIDToAssetPath(g);
                                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                                AddTexture(tex);
                            }
                        }
                        else
                        {
                            string p = AssetDatabase.GetAssetPath(obj);
                            if (!string.IsNullOrEmpty(p))
                            {
                                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                                if (tex != null) AddTexture(tex);
                            }
                        }
                    }
                }
                Event.current.Use();
                break;
        }
    }

    private void AddTexture(Texture2D tex)
    {
        if (tex == null) return;
        if (!textures.Contains(tex)) textures.Add(tex);
    }

    [Serializable]
    private class AtlasEntry
    {
        public string name;
        public string sourcePath;
        public int x;
        public int y;
        public int w;
        public int h;
        public int sourceW;
        public int sourceH;
    }

    private class ImporterBackup
    {
        public string path;
        public bool isReadable;
        public TextureImporterType type;
        public TextureImporterCompression compression;
        public int maxSize;
    }

    private void BuildAtlas()
    {
        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            if (!outputFolder.StartsWith("Assets"))
            {
                EditorUtility.DisplayDialog("Invalid output folder", "Output folder must be inside the project's Assets folder.", "OK");
                return;
            }
            string[] parts = outputFolder.Split('/');
            string parent = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                string sub = string.Join("/", parts, 0, i + 1);
                if (!AssetDatabase.IsValidFolder(sub))
                {
                    AssetDatabase.CreateFolder(parent, parts[i]);
                }
                parent = sub;
            }
        }
        var backups = new List<ImporterBackup>();
        var readableTextures = new List<Texture2D>();
        foreach (var t in textures)
        {
            string p = AssetDatabase.GetAssetPath(t);
            var ti = TextureImporter.GetAtPath(p) as TextureImporter;
            if (ti == null) continue;
            var b = new ImporterBackup();
            b.path = p;
            b.isReadable = ti.isReadable;
            b.type = ti.textureType;
            b.compression = ti.textureCompression;
            b.maxSize = ti.maxTextureSize;
            backups.Add(b);
            ti.isReadable = true;
            ti.textureType = TextureImporterType.Default;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.maxTextureSize = GetValidImporterMaxSize(t.width, t.height);
            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();
            var loaded = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            if (loaded != null) readableTextures.Add(loaded);
        }
        try
        {
            Texture2D atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Rect[] rects = atlas.PackTextures(readableTextures.ToArray(), padding, maxAtlasSize);
            if (rects == null || rects.Length == 0)
            {
                EditorUtility.DisplayDialog("Pack failed", "Could not pack textures. Try increasing max atlas size or reduce number of textures.", "OK");
                return;
            }
            int usedW = atlas.width;
            int usedH = atlas.height;
            byte[] png = atlas.EncodeToPNG();
            string atlasPath = Path.Combine(outputFolder, atlasFileName).Replace("\\", "/");
            if (File.Exists(atlasPath) && !overwriteExisting)
            {
                if (!EditorUtility.DisplayDialog("File exists", $"{atlasPath} already exists. Overwrite?", "Yes", "No"))
                {
                    return;
                }
            }
            File.WriteAllBytes(atlasPath, png);
            AssetDatabase.ImportAsset(atlasPath);
            var atlasImporter = TextureImporter.GetAtPath(atlasPath) as TextureImporter;
            if (atlasImporter == null)
            {
                throw new InvalidOperationException("Atlas import failed: " + atlasPath);
            }
            atlasImporter.textureType = TextureImporterType.Sprite;
            atlasImporter.spriteImportMode = SpriteImportMode.Multiple;
            atlasImporter.spritePixelsPerUnit = 100;
            SpriteRect[] spriteRects = new SpriteRect[readableTextures.Count];
            SpriteNameFileIdPair[] nameFileIdPairs = new SpriteNameFileIdPair[readableTextures.Count];
            List<AtlasEntry> mapping = new List<AtlasEntry>();
            for (int i = 0; i < readableTextures.Count; i++)
            {
                var src = readableTextures[i];
                Rect r = rects[i];
                int px = Mathf.RoundToInt(r.x * atlas.width);
                int py = Mathf.RoundToInt(r.y * atlas.height);
                int pw = Mathf.RoundToInt(r.width * atlas.width);
                int ph = Mathf.RoundToInt(r.height * atlas.height);
                GUID spriteID = GUID.Generate();
                SpriteRect spriteRect = new SpriteRect();
                spriteRect.name = src.name;
                spriteRect.pivot = new Vector2(0.5f, 0.5f);
                spriteRect.rect = new Rect(px, py, pw, ph);
                spriteRect.alignment = SpriteAlignment.Center;
                spriteRect.spriteID = spriteID;
                spriteRects[i] = spriteRect;
                nameFileIdPairs[i] = new SpriteNameFileIdPair(src.name, spriteID);
                AtlasEntry e = new AtlasEntry();
                e.name = src.name;
                e.sourcePath = AssetDatabase.GetAssetPath(src);
                e.x = px; e.y = py; e.w = pw; e.h = ph;
                e.sourceW = src.width; e.sourceH = src.height;
                mapping.Add(e);
            }
            ApplySpriteRects(atlasImporter, spriteRects, nameFileIdPairs);
            EditorUtility.SetDirty(atlasImporter);
            atlasImporter.SaveAndReimport();
            string jsonPath = Path.Combine(outputFolder, jsonFileName).Replace("\\", "/");
            WriteTexturePackerJson(jsonPath, mapping, atlas.width, atlas.height, atlasFileName);
            AssetDatabase.ImportAsset(jsonPath);
            lastAtlasPath = atlasPath;
            lastJson = File.ReadAllText(jsonPath);
            EditorUtility.DisplayDialog("Success", $"Atlas created: {atlasPath}\nJSON: {jsonPath}", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError("Error while building atlas: " + ex.Message + "\n" + ex.StackTrace);
            EditorUtility.DisplayDialog("Error", ex.Message, "OK");
        }
        finally
        {
            RestoreImporters(backups);
            AssetDatabase.Refresh();
        }
    }

    private void WriteTexturePackerJson(string jsonPath, List<AtlasEntry> entries, int atlasW, int atlasH, string atlasFileName)
    {
        var sb = new StringBuilder();
        sb.Append('{');
        sb.Append("\"frames\":[");
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            int jsonY = atlasH - (e.y + e.h);
            string filename = EscapeJsonString(e.name + ".png");
            sb.Append('{');
            sb.AppendFormat("\"filename\":\"{0}\",", filename);
            sb.AppendFormat("\"frame\":{{\"x\":{0},\"y\":{1},\"w\":{2},\"h\":{3}}},", e.x, jsonY, e.w, e.h);
            sb.Append("\"rotated\":false,");
            sb.Append("\"trimmed\":false,");
            sb.AppendFormat("\"spriteSourceSize\":{{\"x\":0,\"y\":0,\"w\":{0},\"h\":{1}}},", e.sourceW, e.sourceH);
            sb.AppendFormat("\"sourceSize\":{{\"w\":{0},\"h\":{1}}},", e.sourceW, e.sourceH);
            sb.Append("\"pivot\":{\"x\":0.5,\"y\":0.5}");
            sb.Append('}');
            if (i < entries.Count - 1) sb.Append(',');
        }
        sb.Append(']');
        sb.Append(',');
        sb.Append("\"meta\":{");
        sb.AppendFormat("\"app\":\"Unity TexturePacker Export\",\"version\":\"1.0\",\"image\":\"{0}\",\"format\":\"RGBA8888\",\"size\":{{\"w\":{1},\"h\":{2}}},\"scale\":\"1\"", EscapeJsonString(atlasFileName), atlasW, atlasH);
        sb.Append('}');
        sb.Append('}');
        File.WriteAllText(jsonPath, sb.ToString());
    }

    private void ApplySpriteRects(TextureImporter atlasImporter, SpriteRect[] spriteRects, SpriteNameFileIdPair[] nameFileIdPairs)
    {
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(atlasImporter);
        if (dataProvider == null)
        {
            throw new InvalidOperationException("Could not create sprite data provider for atlas importer.");
        }

        dataProvider.InitSpriteEditorDataProvider();
        dataProvider.SetSpriteRects(spriteRects);
        var nameFileIdProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameFileIdProvider != null)
        {
            nameFileIdProvider.SetNameFileIdPairs(nameFileIdPairs);
        }
        dataProvider.Apply();
    }

    private int GetValidImporterMaxSize(int width, int height)
    {
        int size = Mathf.Max(width, height, 32);
        int validSize = 32;
        while (validSize < size && validSize < 16384)
        {
            validSize <<= 1;
        }
        return validSize;
    }

    private string EscapeJsonString(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var sb = new StringBuilder(value.Length + 8);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            switch (c)
            {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    if (c < ' ')
                    {
                        sb.Append("\\u");
                        sb.Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        return sb.ToString();
    }

    private void RestoreImporters(List<ImporterBackup> backups)
    {
        foreach (var b in backups)
        {
            var ti = TextureImporter.GetAtPath(b.path) as TextureImporter;
            if (ti == null) continue;
            ti.isReadable = b.isReadable;
            ti.textureType = b.type;
            ti.textureCompression = b.compression;
            ti.maxTextureSize = b.maxSize;
            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();
        }
    }
}
