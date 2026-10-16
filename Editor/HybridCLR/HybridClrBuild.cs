#if HYBIRDCLR_SUPPORT
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AlicizaX;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Installer;
using UnityEditor;
using UnityEngine;

public static class HybridClrBuild
{
    private static string DllAssetDirectory => Path.Combine(Application.dataPath, "Bundles", "DLL");

    public static void Install()
    {
        var installer = new InstallerController();
        if (installer.HasInstalledHybridCLR())
        {
            return;
        }

        installer.InstallDefaultHybridCLR();
        if (!installer.HasInstalledHybridCLR())
        {
            throw new Exception("HybridCLR 安装失败");
        }
    }

    public static void Generate()
    {
        PrebuildCommand.GenerateAll();
    }

    public static void CompileHotUpdate(BuildTarget target)
    {
        CompileDllCommand.CompileDll(target);
        CopyHotUpdate(target);
        AssetDatabase.Refresh();
    }

    public static void CopyAot(BuildTarget target)
    {
        IReadOnlyList<string> assemblies = ReadPatchedAotAssemblies();
        if (assemblies.Count == 0)
        {
            throw new Exception("AOT 补充程序集列表为空");
        }

        Directory.CreateDirectory(DllAssetDirectory);
        string sourceDirectory = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
        foreach (string dll in assemblies)
        {
            CopyDll(Path.Combine(sourceDirectory, dll), dll);
        }

        AssetDatabase.Refresh();
    }

    private static void CopyHotUpdate(BuildTarget target)
    {
        IReadOnlyList<string> assemblies = SettingsUtil.HotUpdateAssemblyFilesExcludePreserved;
        if (assemblies == null || assemblies.Count == 0)
        {
            throw new Exception("热更程序集列表为空");
        }

        Directory.CreateDirectory(DllAssetDirectory);
        string sourceDirectory = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
        foreach (string dll in assemblies)
        {
            CopyDll(Path.Combine(sourceDirectory, dll), dll);
        }
    }

    private static void CopyDll(string source, string dll)
    {
        if (!File.Exists(source))
        {
            throw new FileNotFoundException("DLL 不存在: " + dll, source);
        }

        string destination = Path.Combine(DllAssetDirectory, dll + ".bytes");
        File.Copy(source, destination, true);
        Debug.Log($"复制 DLL: {source} -> {destination}");
    }

    private static IReadOnlyList<string> ReadPatchedAotAssemblies()
    {
        string relativePath = SettingsUtil.HybridCLRSettings.outputAOTGenericReferenceFile;
        string scriptPath = Path.Combine(Application.dataPath, relativePath);
        if (!File.Exists(scriptPath))
        {
            throw new FileNotFoundException("AOT 引用文件不存在", scriptPath);
        }

        string scriptTypeName = Path.GetFileNameWithoutExtension(scriptPath);
        Type targetType = Utility.Assembly.GetType(scriptTypeName);
        if (targetType == null)
        {
            throw new Exception("找不到 AOT 引用类型: " + scriptTypeName);
        }

        FieldInfo field = targetType.GetField("PatchedAOTAssemblyList", BindingFlags.Public | BindingFlags.Static);
        if (field == null)
        {
            throw new Exception(targetType.Name + " 缺少 PatchedAOTAssemblyList");
        }

        return field.GetValue(null) as IReadOnlyList<string>
            ?? throw new Exception("PatchedAOTAssemblyList 为空");
    }
}
#endif
