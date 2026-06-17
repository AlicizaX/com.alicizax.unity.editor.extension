#if !UNITY_6000_3_OR_NEWER

using System;
using System.Reflection;
using AlicizaX.Editor.Extension;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[InitializeOnLoad]
internal static class OldToolbarMinimalExtender
{
    private const string ToolbarRootFieldName = "m_Root";
    private const string CenterContainerName = "ToolbarZonePlayMode";
    private const string LeftContainerName = "ToolbarZoneLeftAlign";
    private const string RightContainerName = "ToolbarZoneRightAlign";
    private const string LeftExtensionName = "OldToolbarMinimalExtenderLeft";
    private const string RightExtensionName = "OldToolbarMinimalExtenderRight";

    private static readonly Type ToolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
    private static ScriptableObject toolbarObject;
    private static VisualElement toolbarRoot;

    static OldToolbarMinimalExtender()
    {
        EditorApplication.update -= TryInstall;
        EditorApplication.update += TryInstall;
    }

    private static void TryInstall()
    {
        if (ToolbarType == null)
        {
            EditorApplication.update -= TryInstall;
            return;
        }

        var currentToolbar = FindToolbar();
        if (currentToolbar == null)
            return;

        var currentRoot = GetToolbarRoot(currentToolbar);
        if (currentRoot == null)
            return;

        if (toolbarObject == currentToolbar && toolbarRoot == currentRoot && currentRoot.Q(LeftExtensionName) != null &&
            currentRoot.Q(RightExtensionName) != null)
        {
            EditorApplication.update -= TryInstall;
            return;
        }

        toolbarObject = currentToolbar;
        toolbarRoot = currentRoot;
        Install(currentRoot);
    }

    private static ScriptableObject FindToolbar()
    {
        var toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
        return toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
    }

    private static VisualElement GetToolbarRoot(ScriptableObject toolbar)
    {
        var rootField = toolbar.GetType().GetField(ToolbarRootFieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        return rootField?.GetValue(toolbar) as VisualElement;
    }

    private static void Install(VisualElement root)
    {
        root.Q(LeftExtensionName)?.RemoveFromHierarchy();
        root.Q(RightExtensionName)?.RemoveFromHierarchy();

        var centerContainer = root.Q(CenterContainerName);
        if (centerContainer == null)
            return;

        ConfigureToolbarContainers(root, centerContainer);

        var leftExtension = CreateExtensionContainer(LeftExtensionName, FlexDirection.RowReverse);
        var rightExtension = CreateExtensionContainer(RightExtensionName, FlexDirection.Row);

        var switchSceneToolbar = new SwitchSceneToolBar();
        switchSceneToolbar.InitializeElement();
        leftExtension.Add(switchSceneToolbar);

        var resourceModeDropdown = new ResourceModeDropdownField();
        resourceModeDropdown.InitializeElement();
        rightExtension.Add(resourceModeDropdown);

        var localizationDropdown = new LocalizationToolbarDropdown();
        localizationDropdown.InitializeElement();
        rightExtension.Add(localizationDropdown);

        var editorQuickToolbar = new EditorQuickToolBar();
        editorQuickToolbar.InitializeElement();
        rightExtension.Add(editorQuickToolbar);

        centerContainer.Insert(0, leftExtension);
        centerContainer.Add(rightExtension);
    }

    private static VisualElement CreateExtensionContainer(string name, FlexDirection flexDirection)
    {
        var container = new VisualElement
        {
            name = name
        };

        container.style.flexDirection = flexDirection;
        container.style.flexGrow = 1;
        container.style.width = 0;
        container.style.alignItems = Align.Center;
        container.style.paddingLeft = 5;
        container.style.paddingRight = 5;

        return container;
    }

    private static void ConfigureToolbarContainers(VisualElement root, VisualElement centerContainer)
    {
        var leftContainer = root.Q(LeftContainerName);
        if (leftContainer != null)
        {
            leftContainer.style.flexGrow = 0;
            leftContainer.style.width = Length.Auto();
        }

        var rightContainer = root.Q(RightContainerName);
        if (rightContainer != null)
        {
            rightContainer.style.flexGrow = 0;
            rightContainer.style.width = Length.Auto();
        }

        centerContainer.style.flexGrow = 1;

        if (centerContainer.parent == null)
            return;

        centerContainer.parent.style.paddingTop = 0;
        centerContainer.parent.style.paddingBottom = 0;
    }
}

#endif
