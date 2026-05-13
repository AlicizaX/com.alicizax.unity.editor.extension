#if HYBIRDCLR_SUPPORT
using System;
using System.Collections.Generic;
using System.IO;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using System.Reflection;
using AlicizaX;
using AlicizaX.Editor;
using UnityEditor;
using UnityEngine;

public static class BuildDLLCommand
{
    private const string EnableHybridClrScriptingDefineSymbol = "ENABLE_HYBRIDCLR";
    public static string AssemblyTextAssetPath = Application.dataPath + "/" + "Bundles/DLL";

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


    [MenuItem("HybridCLR/Tools/BuildAssets And CopyTo AssemblyTextAssetPath",false,300)]
    public static void BuildAndCopyDlls()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        CompileDllCommand.CompileDll(target);
        CopyAOTHotUpdateDlls(target);
    }


    public static void GenerateAll()
    {
        PrebuildCommand.GenerateAll();
    }

    public static void CopyAOTHotUpdateDlls(BuildTarget target)
    {
        CopyAOTAssembliesToAssetPath();
        CopyHotUpdateAssembliesToAssetPath();
        AssetDatabase.Refresh();
    }

    public static void CopyAOTAssembliesToAssetPath()
    {
        IReadOnlyList<string> patchedAotAssembliesPath = GetStaticFieldValue();
        if (patchedAotAssembliesPath == null)
        {
            Debug.Log("请先生成AOT补充脚本");
            return;
        }

        var target = EditorUserBuildSettings.activeBuildTarget;
        string aotAssembliesSrcDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
        string aotAssembliesDstDir = AssemblyTextAssetPath;

        foreach (var dll in patchedAotAssembliesPath)
        {
            string srcDllPath = $"{aotAssembliesSrcDir}/{dll}";
            if (!System.IO.File.Exists(srcDllPath))
            {
                Debug.LogError(
                    $"ab中添加AOT补充元数据dll:{srcDllPath} 时发生错误,文件不存在。裁剪后的AOT dll在BuildPlayer时才能生成，因此需要你先构建一次游戏App后再打包。");
                continue;
            }

            string dllBytesPath = $"{aotAssembliesDstDir}/{dll}.bytes";
            System.IO.File.Copy(srcDllPath, dllBytesPath, true);
            Debug.Log($"[CopyAOTAssembliesToStreamingAssets] copy AOT dll {srcDllPath} -> {dllBytesPath}");
        }
    }

    private static IReadOnlyList<string> GetStaticFieldValue()
    {
        var gs = SettingsUtil.HybridCLRSettings;
        string scriptPath = $"{Application.dataPath}/{gs.outputAOTGenericReferenceFile}";
        if (!File.Exists(scriptPath))
        {
            return null;
        }

        FileInfo info = new FileInfo(scriptPath);
        string scriptTypeName = info.Name.Replace(".cs", "");

        Type targetType = Utility.Assembly.GetType(scriptTypeName);
        if (targetType == null)
        {
            Debug.LogError($"can not find script type: {scriptTypeName}");
            return null;
        }

        // 反射获取静态字段
        FieldInfo field = targetType.GetField("PatchedAOTAssemblyList",
            BindingFlags.Public | BindingFlags.Static);

        if (field == null)
        {
            Debug.LogError($"Field 'PatchedAOTAssemblyList' not found in {targetType.Name}");
            return null;
        }

        // 获取字段值并打印
        return field.GetValue(null) as IReadOnlyList<string>;
    }

    public static void CopyHotUpdateAssembliesToAssetPath()
    {
        var target = EditorUserBuildSettings.activeBuildTarget;

        string hotfixDllSrcDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
        string hotfixAssembliesDstDir = AssemblyTextAssetPath;
        foreach (var dll in SettingsUtil.HotUpdateAssemblyFilesExcludePreserved)
        {
            string dllPath = $"{hotfixDllSrcDir}/{dll}";
            string dllBytesPath = $"{hotfixAssembliesDstDir}/{dll}.bytes";
            System.IO.File.Copy(dllPath, dllBytesPath, true);
            Debug.Log($"[CopyHotUpdateAssembliesToStreamingAssets] copy hotfix dll {dllPath} -> {dllBytesPath}");
        }
    }
}
#endif
