#if !UNITY_6000_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AlicizaX.Editor.Extension
{
    internal static class EditorToolFunctionAttributeCollector
    {
        public static List<EditorToolFunctionAttribute> Attributes = new List<EditorToolFunctionAttribute>();

        static void Register(EditorToolFunctionAttribute attribute)
        {
            Attributes.Add(attribute);
            Attributes.Sort((x, y) => x.MenuOrder.CompareTo(y.MenuOrder));
        }

        /// <summary>
        /// 扫描所有程序集中的类和方法，自动注册带有 EditorToolFunctionAttribute 的方法。
        /// </summary>
        [InitializeOnLoadMethod]
        static void ScanAndRegisterAllMethods()
        {
            // 获取当前应用程序域中的所有程序集
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    if (assembly.FullName.Contains("Sirenix")) continue;
                    // 获取程序集中的所有类型
                    var types = assembly.GetTypes();

                    foreach (var type in types)
                    {
                        try
                        {
                            // 获取类型中的所有方法
                            var methods =
                                type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                            foreach (var method in methods)
                            {
                                // 检查方法是否带有 EditorToolFunctionAttribute
                                var attribute = method.GetCustomAttribute<EditorToolFunctionAttribute>();
                                if (attribute != null)
                                {
                                    // 设置方法的 MethodInfo
                                    attribute.SetMethodInfo(method);
                                    Register(attribute);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Failed to process type {type.FullName}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to process assembly {assembly.FullName}: {ex.Message}");
                }
            }

            // Debug.Log($"Registered {Attributes.Count} methods with EditorToolFunctionAttribute.");
        }
    }
}

#endif
