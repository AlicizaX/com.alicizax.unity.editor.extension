#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public static class EditorSpriteSaveInfo
{
    private static readonly HashSet<string> DirtyAtlasNames = new HashSet<string>(StringComparer.Ordinal);
    private static readonly Dictionary<string, HashSet<string>> AtlasMap =
        new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
    private static readonly List<string> ProcessBuffer = new List<string>();
    private static readonly List<string> PendingV2ImporterPaths = new List<string>();

    private static bool _initialized;
    private static bool _isProcessing;
    private static bool _isScanning;
    private static bool _processScheduled;

    private static AtlasConfiguration Config => AtlasConfiguration.Instance;

    static EditorSpriteSaveInfo()
    {
        Initialize();
    }

    private static string NormalizedSourceRoot => NormalizePath(Config.sourceAtlasRoot).TrimEnd('/');

    public static bool PrepareSpriteImporter(TextureImporter importer, string assetPath)
    {
        if (importer == null || !ShouldProcess(assetPath))
            return false;

        var modified = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            modified = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            modified = true;
        }

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        if (settings.spriteGenerateFallbackPhysicsShape)
        {
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            modified = true;
        }

        return modified;
    }

    public static void ConvertToSprite(string assetPath)
    {
        if (!ShouldProcess(assetPath))
            return;

        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        if (PrepareSpriteImporter(importer, assetPath))
            importer.SaveAndReimport();
    }

    public static void OnImportSprite(string assetPath)
    {
        if (!ShouldProcess(assetPath))
            return;

        EnsureInitialized();

        var atlasName = GetAtlasName(assetPath);
        if (string.IsNullOrEmpty(atlasName))
            return;

        AddSpritePath(atlasName, assetPath);
        MarkDirty(atlasName);
        MarkParentAtlasesDirty(assetPath);
        QueueProcessDirtyAtlases();
    }

    public static void OnDeleteSprite(string assetPath)
    {
        if (!ShouldProcess(assetPath))
            return;

        EnsureInitialized();

        var atlasName = GetAtlasName(assetPath);
        if (string.IsNullOrEmpty(atlasName))
            return;

        if (AtlasMap.TryGetValue(atlasName, out var spritePaths))
        {
            spritePaths.Remove(assetPath);
            if (spritePaths.Count == 0)
                AtlasMap.Remove(atlasName);
        }

        MarkDirty(atlasName);
        MarkParentAtlasesDirty(assetPath);
        QueueProcessDirtyAtlases();
    }

    [MenuItem("AlicizaX/Extension/图集工具/ForceGenerateAll")]
    public static void ForceGenerateAll()
    {
        RebuildCache(markDirty: true);
        ProcessDirtyAtlases(force: true);
    }

    public static void ClearCache()
    {
        DirtyAtlasNames.Clear();
        AtlasMap.Clear();
        ProcessBuffer.Clear();
        PendingV2ImporterPaths.Clear();
        _initialized = false;
        _processScheduled = false;
    }

    public static void MarkParentAtlasesDirty(string assetPath)
    {
        var currentPath = NormalizePath(Path.GetDirectoryName(assetPath));
        var rootPath = NormalizedSourceRoot;

        while (!string.IsNullOrEmpty(currentPath) &&
               currentPath.StartsWith(rootPath + "/", StringComparison.Ordinal))
        {
            var atlasName = GetAtlasNameForDirectory(currentPath);
            if (!string.IsNullOrEmpty(atlasName))
                MarkDirty(atlasName);

            currentPath = NormalizePath(Path.GetDirectoryName(currentPath));
        }
    }

    public static bool ShouldProcess(string assetPath)
    {
        return IsImageFile(assetPath) && !IsExcluded(assetPath) && IsUnderSourceRoot(assetPath);
    }

    private static void Initialize()
    {
        if (_initialized)
            return;

        RebuildCache(markDirty: false);
    }

    private static void EnsureInitialized()
    {
        if (!_initialized)
            Initialize();
    }

    private static void RebuildCache(bool markDirty)
    {
        DirtyAtlasNames.Clear();
        AtlasMap.Clear();

        ScanExistingSprites();

        if (markDirty)
        {
            foreach (var atlasName in AtlasMap.Keys)
                DirtyAtlasNames.Add(atlasName);

            MarkExistingOutputAtlasesDirty();
        }

        _initialized = true;
    }

    private static void ScanExistingSprites()
    {
        var sourceRoot = NormalizedSourceRoot;
        if (string.IsNullOrEmpty(sourceRoot))
            return;

        _isScanning = true;
        try
        {
            var guids = AssetDatabase.FindAssets("t:Sprite", new[] { sourceRoot });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!ShouldProcess(path))
                    continue;

                var atlasName = GetAtlasName(path);
                if (!string.IsNullOrEmpty(atlasName))
                    AddSpritePath(atlasName, path);
            }
        }
        finally
        {
            _isScanning = false;
        }
    }

    private static void MarkExistingOutputAtlasesDirty()
    {
        var outputDir = NormalizePath(Config.outputAtlasDir).TrimEnd('/');
        if (string.IsNullOrEmpty(outputDir) || !AssetDatabase.IsValidFolder(outputDir))
            return;

        var guids = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { outputDir });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var atlasName = Path.GetFileNameWithoutExtension(path);
            if (!string.IsNullOrEmpty(atlasName))
                DirtyAtlasNames.Add(atlasName);
        }
    }

    private static void QueueProcessDirtyAtlases()
    {
        if (_isScanning || _processScheduled || DirtyAtlasNames.Count == 0)
            return;

        _processScheduled = true;
        EditorApplication.delayCall += DelayedProcessDirtyAtlases;
    }

    private static void DelayedProcessDirtyAtlases()
    {
        _processScheduled = false;

        if (_isProcessing || DirtyAtlasNames.Count == 0)
            return;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            QueueProcessDirtyAtlases();
            return;
        }

        ProcessDirtyAtlases();
    }

    private static void ProcessDirtyAtlases(bool force = false)
    {
        if (DirtyAtlasNames.Count == 0)
            return;

        EnsureOutputDirectory();

        _isProcessing = true;
        try
        {
            while (DirtyAtlasNames.Count > 0)
            {
                ProcessBuffer.Clear();
                ProcessBuffer.AddRange(DirtyAtlasNames);
                DirtyAtlasNames.Clear();
                PendingV2ImporterPaths.Clear();

                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (var atlasName in ProcessBuffer)
                    {
                        if (force || ShouldUpdateAtlas(atlasName))
                            GenerateAtlas(atlasName, force);
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }

                ApplyPendingV2ImportSettings();
            }

            AssetDatabase.SaveAssets();
        }
        finally
        {
            ProcessBuffer.Clear();
            PendingV2ImporterPaths.Clear();
            _isProcessing = false;

            if (DirtyAtlasNames.Count > 0)
                QueueProcessDirtyAtlases();
        }
    }

    private static void GenerateAtlas(string atlasName, bool force)
    {
        var outputPath = GetAtlasOutputPath(atlasName);
        var loadResult = LoadValidSprites(atlasName);

        if (loadResult.PendingImport)
        {
            if (Config.enableLogging)
                Debug.Log($"Skip atlas '{atlasName}': source sprites are still importing.");
            return;
        }

        if (loadResult.Sprites.Count == 0)
        {
            if (loadResult.HasTrackedSources)
                return;

            DeleteAtlas(outputPath);
            DeleteAtlas(GetLegacyAtlasOutputPath(atlasName));
            return;
        }

        var existingAtlas = LoadExistingAtlas(outputPath);
        if (existingAtlas == null && AtlasAssetExists(outputPath))
        {
            if (Config.enableLogging)
                Debug.Log($"Skip atlas '{atlasName}': atlas asset exists but is not imported yet.");
            return;
        }

        var spriteObjects = loadResult.Sprites.ToArray();
        if (!force && existingAtlas != null && PackablesMatch(existingAtlas.GetPackables(), spriteObjects))
            return;

        if (Config.enableV2)
        {
            GenerateAtlasV2(outputPath, existingAtlas, spriteObjects);
        }
        else
        {
            GenerateAtlasV1(outputPath, existingAtlas, spriteObjects);
        }

        if (Config.enableLogging)
            Debug.Log($"Generated atlas: {atlasName} ({spriteObjects.Length} sprites)");
    }

    private static void GenerateAtlasV2(string outputPath, SpriteAtlas existingAtlas, UnityEngine.Object[] spriteObjects)
    {
        var spriteAtlasAsset = existingAtlas == null ? new SpriteAtlasAsset() : SpriteAtlasAsset.Load(outputPath);
        if (spriteAtlasAsset == null)
            spriteAtlasAsset = new SpriteAtlasAsset();

        if (existingAtlas != null)
        {
            var oldPackables = existingAtlas.GetPackables();
            if (oldPackables != null && oldPackables.Length > 0)
                spriteAtlasAsset.Remove(oldPackables);
        }

#if !UNITY_2022_1_OR_NEWER
        ConfigureAtlasV2Settings(spriteAtlasAsset);
#endif
        spriteAtlasAsset.Add(spriteObjects);
        SpriteAtlasAsset.Save(spriteAtlasAsset, outputPath);

#if UNITY_2022_1_OR_NEWER
        PendingV2ImporterPaths.Add(outputPath);
#endif
    }

    private static void GenerateAtlasV1(string outputPath, SpriteAtlas existingAtlas, UnityEngine.Object[] spriteObjects)
    {
        if (existingAtlas == null)
        {
            var atlas = new SpriteAtlas();
            ConfigureAtlasSettings(atlas);
            atlas.Add(spriteObjects);
            atlas.SetIsVariant(false);
            AssetDatabase.CreateAsset(atlas, outputPath);
            return;
        }

        var oldPackables = existingAtlas.GetPackables();
        if (oldPackables != null && oldPackables.Length > 0)
            existingAtlas.Remove(oldPackables);

        ConfigureAtlasSettings(existingAtlas);
        existingAtlas.Add(spriteObjects);
        existingAtlas.SetIsVariant(false);
        EditorUtility.SetDirty(existingAtlas);
    }

    private static SpriteLoadResult LoadValidSprites(string atlasName)
    {
        var result = new SpriteLoadResult();
        if (!AtlasMap.TryGetValue(atlasName, out var spritePaths) || spritePaths.Count == 0)
            return result;

        result.HasTrackedSources = true;

        foreach (var assetPath in spritePaths)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
            {
                result.Sprites.Add(sprite);
                continue;
            }

            var foundSubSprite = false;
            var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
            foreach (var subAsset in subAssets)
            {
                if (subAsset is Sprite subSprite)
                {
                    result.Sprites.Add(subSprite);
                    foundSubSprite = true;
                }
            }

            if (!foundSubSprite && File.Exists(assetPath))
                result.PendingImport = true;
        }

        return result;
    }

    private static SpriteAtlas LoadExistingAtlas(string outputPath)
    {
        return AssetDatabase.LoadAssetAtPath<SpriteAtlas>(outputPath);
    }

    private static bool AtlasAssetExists(string outputPath)
    {
        return !string.IsNullOrEmpty(outputPath) && File.Exists(outputPath);
    }

    private static bool PackablesMatch(UnityEngine.Object[] current, UnityEngine.Object[] expected)
    {
        if (current == null)
            return expected == null || expected.Length == 0;

        if (expected == null || current.Length != expected.Length)
            return false;

        var currentIds = new HashSet<int>();
        for (var i = 0; i < current.Length; i++)
        {
            if (current[i] == null)
                return false;

            currentIds.Add(current[i].GetInstanceID());
        }

        for (var i = 0; i < expected.Length; i++)
        {
            if (expected[i] == null || !currentIds.Contains(expected[i].GetInstanceID()))
                return false;
        }

        return true;
    }

    private sealed class SpriteLoadResult
    {
        public readonly List<UnityEngine.Object> Sprites = new List<UnityEngine.Object>();
        public bool PendingImport;
        public bool HasTrackedSources;
    }

    private static void ApplyPendingV2ImportSettings()
    {
#if UNITY_2022_1_OR_NEWER
        foreach (var outputPath in PendingV2ImporterPaths)
        {
            var importer = AssetImporter.GetAtPath(outputPath) as SpriteAtlasImporter;
            if (importer == null)
                continue;

            if (ConfigureAtlasV2Settings(importer))
            {
                AssetDatabase.WriteImportSettingsIfDirty(outputPath);
                importer.SaveAndReimport();
            }
            else
            {
                AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport);
            }
        }
#endif
    }

#if UNITY_2022_1_OR_NEWER
    private static bool ConfigureAtlasV2Settings(SpriteAtlasImporter atlasImporter)
    {
        var modified = false;
        modified |= SetPlatform(atlasImporter, "Android", Config.androidFormat);
        modified |= SetPlatform(atlasImporter, "iPhone", Config.iosFormat);
        modified |= SetPlatform(atlasImporter, "WebGL", Config.webglFormat);

        var packingSettings = atlasImporter.packingSettings;
        if (packingSettings.padding != Config.padding ||
            packingSettings.enableRotation != Config.enableRotation ||
            packingSettings.blockOffset != Config.blockOffset ||
            packingSettings.enableTightPacking != Config.tightPacking ||
            !packingSettings.enableAlphaDilation)
        {
            atlasImporter.packingSettings = new SpriteAtlasPackingSettings
            {
                padding = Config.padding,
                enableRotation = Config.enableRotation,
                blockOffset = Config.blockOffset,
                enableTightPacking = Config.tightPacking,
                enableAlphaDilation = true
            };
            modified = true;
        }

        return modified;
    }

    private static bool SetPlatform(SpriteAtlasImporter atlasImporter, string platform, TextureImporterFormat format)
    {
        var settings = atlasImporter.GetPlatformSettings(platform);
        if (settings == null)
            return false;

        if (settings.overridden &&
            settings.format == format &&
            settings.compressionQuality == Config.compressionQuality)
        {
            return false;
        }

        settings.overridden = true;
        settings.format = format;
        settings.compressionQuality = Config.compressionQuality;
        atlasImporter.SetPlatformSettings(settings);
        return true;
    }
#else
    private static void ConfigureAtlasV2Settings(SpriteAtlasAsset spriteAtlasAsset)
    {
        SetPlatform(spriteAtlasAsset, "Android", Config.androidFormat);
        SetPlatform(spriteAtlasAsset, "iPhone", Config.iosFormat);
        SetPlatform(spriteAtlasAsset, "WebGL", Config.webglFormat);

        spriteAtlasAsset.SetPackingSettings(new SpriteAtlasPackingSettings
        {
            padding = Config.padding,
            enableRotation = Config.enableRotation,
            blockOffset = Config.blockOffset,
            enableTightPacking = Config.tightPacking,
            enableAlphaDilation = true
        });
    }

    private static void SetPlatform(SpriteAtlasAsset spriteAtlasAsset, string platform, TextureImporterFormat format)
    {
        var settings = spriteAtlasAsset.GetPlatformSettings(platform);
        if (settings == null)
            return;

        settings.overridden = true;
        settings.format = format;
        settings.compressionQuality = Config.compressionQuality;
        spriteAtlasAsset.SetPlatformSettings(settings);
    }
#endif

    private static void ConfigureAtlasSettings(SpriteAtlas atlas)
    {
        SetPlatform(atlas, "Android", Config.androidFormat);
        SetPlatform(atlas, "iPhone", Config.iosFormat);
        SetPlatform(atlas, "WebGL", Config.webglFormat);

        atlas.SetPackingSettings(new SpriteAtlasPackingSettings
        {
            padding = Config.padding,
            enableRotation = Config.enableRotation,
            blockOffset = Config.blockOffset,
            enableTightPacking = Config.tightPacking
        });
    }

    private static void SetPlatform(SpriteAtlas atlas, string platform, TextureImporterFormat format)
    {
        var settings = atlas.GetPlatformSettings(platform);
        settings.overridden = true;
        settings.format = format;
        settings.compressionQuality = Config.compressionQuality;
        atlas.SetPlatformSettings(settings);
    }

    private static string GetAtlasName(string assetPath)
    {
        var normalizedPath = NormalizePath(assetPath);
        var rootPath = NormalizedSourceRoot;

        if (!normalizedPath.StartsWith(rootPath + "/", StringComparison.Ordinal))
            return null;

        var relativePath = normalizedPath.Substring(rootPath.Length + 1).Split('/');
        if (relativePath.Length < 2)
            return null;

        var atlasNamePart = string.Join("_", relativePath, 0, relativePath.Length - 1);
        var rootFolderName = Path.GetFileName(rootPath);
        return $"{rootFolderName}_{atlasNamePart}";
    }

    private static string GetAtlasNameForDirectory(string directoryPath)
    {
        var normalizedPath = NormalizePath(directoryPath);
        var rootPath = NormalizedSourceRoot;

        if (!normalizedPath.StartsWith(rootPath + "/", StringComparison.Ordinal))
            return null;

        var relativePath = normalizedPath.Substring(rootPath.Length + 1).Split('/');
        if (relativePath.Length == 0)
            return null;

        var atlasNamePart = string.Join("_", relativePath);
        var rootFolderName = Path.GetFileName(rootPath);
        return $"{rootFolderName}_{atlasNamePart}";
    }

    private static void AddSpritePath(string atlasName, string assetPath)
    {
        if (!AtlasMap.TryGetValue(atlasName, out var spritePaths))
        {
            spritePaths = new HashSet<string>(StringComparer.Ordinal);
            AtlasMap[atlasName] = spritePaths;
        }

        spritePaths.Add(assetPath);
    }

    private static bool IsExcluded(string path)
    {
        var normalizedPath = NormalizePath(path);
        var excludeFolders = Config.excludeFolders;
        if (excludeFolders != null)
        {
            foreach (var folder in excludeFolders)
            {
                if (!string.IsNullOrEmpty(folder) &&
                    normalizedPath.StartsWith(NormalizePath(folder), StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        var excludeKeywords = Config.excludeKeywords;
        if (excludeKeywords != null)
        {
            foreach (var keyword in excludeKeywords)
            {
                if (!string.IsNullOrEmpty(keyword) &&
                    normalizedPath.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsImageFile(string path)
    {
        var extension = Path.GetExtension(path);
        if (string.IsNullOrEmpty(extension))
            return false;

        switch (extension.ToLowerInvariant())
        {
            case ".png":
            case ".jpg":
            case ".jpeg":
                return true;
            default:
                return false;
        }
    }

    private static bool IsUnderSourceRoot(string assetPath)
    {
        var normalizedPath = NormalizePath(assetPath);
        var rootPath = NormalizedSourceRoot;
        return !string.IsNullOrEmpty(rootPath) &&
               normalizedPath.StartsWith(rootPath + "/", StringComparison.Ordinal);
    }

    private static void MarkDirty(string atlasName)
    {
        if (!string.IsNullOrEmpty(atlasName))
            DirtyAtlasNames.Add(atlasName);
    }

    private static bool ShouldUpdateAtlas(string atlasName)
    {
        return !string.IsNullOrEmpty(atlasName);
    }

    private static void DeleteAtlas(string assetPath)
    {
        if (!string.IsNullOrEmpty(assetPath) && !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath)))
            AssetDatabase.DeleteAsset(assetPath);
    }

    private static void EnsureOutputDirectory()
    {
        var outputPath = NormalizePath(Config.outputAtlasDir).TrimEnd('/');
        if (string.IsNullOrEmpty(outputPath) || AssetDatabase.IsValidFolder(outputPath))
            return;

        var pathParts = outputPath.Split('/');
        if (pathParts.Length == 0 || pathParts[0] != "Assets")
            return;

        var currentPath = pathParts[0];
        for (var i = 1; i < pathParts.Length; i++)
        {
            var nextPath = $"{currentPath}/{pathParts[i]}";
            if (!AssetDatabase.IsValidFolder(nextPath))
                AssetDatabase.CreateFolder(currentPath, pathParts[i]);

            currentPath = nextPath;
        }
    }

    private static string GetAtlasOutputPath(string atlasName)
    {
        var extension = Config.enableV2 ? ".spriteatlasv2" : ".spriteatlas";
        return $"{NormalizePath(Config.outputAtlasDir).TrimEnd('/')}/{atlasName}{extension}";
    }

    private static string GetLegacyAtlasOutputPath(string atlasName)
    {
        var legacyExtension = Config.enableV2 ? ".spriteatlas" : ".spriteatlasv2";
        return $"{NormalizePath(Config.outputAtlasDir).TrimEnd('/')}/{atlasName}{legacyExtension}";
    }

    private static string NormalizePath(string path)
    {
        return string.IsNullOrEmpty(path) ? string.Empty : path.Replace("\\", "/");
    }
}
#endif
