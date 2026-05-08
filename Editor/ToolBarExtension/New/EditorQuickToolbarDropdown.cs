#if UNITY_6000_3_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace AlicizaX.Editor.Extension
{
    [InitializeOnLoad]
    public static class EditorQuickToolbarDropdown
    {
        private const string ElementPath = "AlicizaX/EditorQuickTools";
        private const string Tooltip = "Open editor quick tools";

        private static readonly Texture2D ToolIcon;
        private static readonly List<ToolEntry> ToolEntries;

        private sealed class ToolEntry
        {
            public string MenuPath;
            public int MenuOrder;
            public MethodInfo MethodInfo;
        }

        static EditorQuickToolbarDropdown()
        {
            ToolIcon = GetIcon("CustomTool") ?? GetIcon("Settings");
            ToolEntries = CollectToolEntries();
        }

        [MainToolbarElement(ElementPath, defaultDockPosition = MainToolbarDockPosition.Right, defaultDockIndex = 1)]
        public static MainToolbarElement CreateElement()
        {
            return new MainToolbarDropdown(
                new MainToolbarContent("Tools", ToolIcon, Tooltip),
                ShowDropdownMenu);
        }

        private static void ShowDropdownMenu(Rect dropdownRect)
        {
            var menu = new GenericMenu();

            if (ToolEntries.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No tools found"));
                menu.DropDown(dropdownRect);
                return;
            }

            foreach (var toolEntry in ToolEntries)
            {
                var capturedToolEntry = toolEntry;
                menu.AddItem(
                    new GUIContent(capturedToolEntry.MenuPath),
                    false,
                    () => InvokeTool(capturedToolEntry));
            }

            menu.DropDown(dropdownRect);
        }

        private static List<ToolEntry> CollectToolEntries()
        {
            var toolEntries = new List<ToolEntry>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.Contains("Sirenix", StringComparison.Ordinal))
                {
                    continue;
                }

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    types = exception.Types.Where(type => type != null).ToArray();
                }
                catch
                {
                    continue;
                }

                foreach (var type in types)
                {
                    MethodInfo[] methods;
                    try
                    {
                        methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (var method in methods)
                    {
                        var attribute = method.GetCustomAttribute<EditorToolFunctionAttribute>();
                        if (attribute == null)
                        {
                            continue;
                        }

                        toolEntries.Add(new ToolEntry
                        {
                            MenuPath = attribute.ToolMenuPath,
                            MenuOrder = attribute.MenuOrder,
                            MethodInfo = method
                        });
                    }
                }
            }

            toolEntries.Sort((left, right) =>
            {
                var orderCompare = left.MenuOrder.CompareTo(right.MenuOrder);
                return orderCompare != 0
                    ? orderCompare
                    : string.Compare(left.MenuPath, right.MenuPath, StringComparison.OrdinalIgnoreCase);
            });

            return toolEntries;
        }

        private static void InvokeTool(ToolEntry toolEntry)
        {
            if (toolEntry.MethodInfo == null || !toolEntry.MethodInfo.IsStatic)
            {
                Debug.LogError("Tool method is not static or could not be found.");
                return;
            }

            if (toolEntry.MethodInfo.GetParameters().Length != 0)
            {
                Debug.LogError($"Tool method '{toolEntry.MethodInfo.Name}' must be parameterless.");
                return;
            }

            try
            {
                toolEntry.MethodInfo.Invoke(null, null);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static Texture2D GetIcon(string iconName)
        {
            return EditorGUIUtility.IconContent(iconName).image as Texture2D;
        }
    }
}
#endif
