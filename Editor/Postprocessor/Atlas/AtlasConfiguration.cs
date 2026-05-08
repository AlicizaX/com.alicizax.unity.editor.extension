#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Serialization;

[AlicizaX.Editor.Setting.FilePath("ProjectSettings/AtlasConfiguration.asset")]
public class AtlasConfiguration : AlicizaX.Editor.Setting.ScriptableSingleton<AtlasConfiguration>
{
    [Tooltip("生成的图集输出目录")] public string outputAtlasDir = "Assets/Art/Atlas";

    [Tooltip("需要生成图集的UI根目录")] public string sourceAtlasRoot = "Assets/Bundles/UIRaw/Atlas";


    [Tooltip("平台格式设置")] public TextureImporterFormat androidFormat = TextureImporterFormat.ASTC_6x6;
    public TextureImporterFormat iosFormat = TextureImporterFormat.ASTC_5x5;
    public TextureImporterFormat webglFormat = TextureImporterFormat.ASTC_6x6;

    [Tooltip("PackingSetting")] public int padding = 2;
    public bool enableRotation = true;
    public int blockOffset = 1;
    public bool tightPacking = true;

    [Tooltip("其他设置")] public int compressionQuality = 50;
    public bool autoGenerate = true;
    public bool enableLogging = true;
    public bool enableV2 = true;

    [Tooltip("排除关键词")] public string[] excludeKeywords = { "_Delete", "_Temp" };
    [Tooltip("不需要生成图集的UI目录")] public string[] excludeFolders = { "Assets/Bundles/UIRaw/Raw" };
}
#endif
