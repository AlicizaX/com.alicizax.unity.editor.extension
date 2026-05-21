using UnityEditor;
using UnityEngine;

public class SpritePostprocessor : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        var config = AtlasConfiguration.Instance;
        if (!config.autoGenerate)
            return;

        if (!ShouldProcessAsset(assetPath))
            return;

        EditorSpriteSaveInfo.PrepareSpriteImporter(assetImporter as TextureImporter, assetPath);
    }

    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        var config = AtlasConfiguration.Instance;
        if (!config.autoGenerate)
            return;

        try
        {
            ProcessAssetChanges(
                importedAssets: importedAssets,
                deletedAssets: deletedAssets,
                movedAssets: movedAssets,
                movedFromPaths: movedFromAssetPaths
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Atlas processing error: {e.Message}\n{e.StackTrace}");
        }
    }

    private static void ProcessAssetChanges(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromPaths)
    {
        ProcessAssets(importedAssets, path =>
        {
            EditorSpriteSaveInfo.ConvertToSprite(path);
            EditorSpriteSaveInfo.OnImportSprite(path);
            LogProcessed("[Added]", path);
        });

        ProcessAssets(deletedAssets, path =>
        {
            EditorSpriteSaveInfo.OnDeleteSprite(path);
            LogProcessed("[Deleted]", path);
        });

        ProcessMovedAssets(movedFromPaths, movedAssets);
    }

    private static void ProcessAssets(string[] assets, System.Action<string> processor)
    {
        if (assets == null)
            return;

        foreach (var asset in assets)
        {
            if (ShouldProcessAsset(asset))
                processor?.Invoke(asset);
        }
    }

    private static void ProcessMovedAssets(string[] oldPaths, string[] newPaths)
    {
        if (oldPaths == null || newPaths == null)
            return;

        for (var i = 0; i < oldPaths.Length; i++)
        {
            if (ShouldProcessAsset(oldPaths[i]))
            {
                EditorSpriteSaveInfo.OnDeleteSprite(oldPaths[i]);
                LogProcessed("[Moved From]", oldPaths[i]);
            }

            if (ShouldProcessAsset(newPaths[i]))
            {
                EditorSpriteSaveInfo.ConvertToSprite(newPaths[i]);
                EditorSpriteSaveInfo.OnImportSprite(newPaths[i]);
                LogProcessed("[Moved To]", newPaths[i]);
            }
        }
    }

    private static bool ShouldProcessAsset(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
            return false;

        if (assetPath.StartsWith("Packages/"))
            return false;

        return EditorSpriteSaveInfo.ShouldProcess(assetPath);
    }

    private static void LogProcessed(string operation, string path)
    {
        if (AtlasConfiguration.Instance.enableLogging)
            Debug.Log($"{operation} {System.IO.Path.GetFileName(path)}\nPath: {path}");
    }
}
