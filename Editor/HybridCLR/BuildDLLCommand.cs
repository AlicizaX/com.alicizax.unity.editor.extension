#if HYBIRDCLR_SUPPORT
using AlicizaX.Editor;
using UnityEditor;

public static class BuildDLLCommand
{
    private const string EnableHybridClrScriptingDefineSymbol = "ENABLE_HYBRIDCLR";

    /// <summary>
    /// 禁用HybridCLR宏定义。
    /// </summary>
    [MenuItem("HybridCLR/Tools/Disable HybridCLR", false, 100)]
    public static void Disable()
    {
        ScriptingDefineSymbols.RemoveScriptingDefineSymbol(EnableHybridClrScriptingDefineSymbol);
        HybridCLR.Editor.SettingsUtil.Enable = false;
    }

    /// <summary>
    /// 开启HybridCLR宏定义。
    /// </summary>
    [MenuItem("HybridCLR/Tools/Enable HybridCLR", false, 200)]
    public static void Enable()
    {
        ScriptingDefineSymbols.RemoveScriptingDefineSymbol(EnableHybridClrScriptingDefineSymbol);
        ScriptingDefineSymbols.AddScriptingDefineSymbol(EnableHybridClrScriptingDefineSymbol);
        HybridCLR.Editor.SettingsUtil.Enable = true;
    }


    [MenuItem("HybridCLR/Tools/BuildAssets And CopyTo AssemblyTextAssetPath", false, 300)]
    public static void BuildAndCopyDlls()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        HybridClrBuild.CompileHotUpdate(target);
        HybridClrBuild.CopyAot(target);
    }

    public static void GenerateAll()
    {
        HybridClrBuild.Generate();
    }
}
#endif
