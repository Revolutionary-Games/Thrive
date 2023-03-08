﻿using System;
using Godot;

public class MicrobeEditorTabButtons : MarginContainer
{
    [Export]
    public bool IsForMulticellular;

    [Export]
    public NodePath? TabButtonsPath;

    [Export]
    public NodePath ReportTabButtonPath = null!;

    [Export]
    public NodePath PatchMapButtonPath = null!;

    [Export]
    public NodePath CellEditorButtonPath = null!;

    [Export]
    public NodePath CellTypeTabPath = null!;

#pragma warning disable CA2213

    // Editor tab selector buttons
    private Button reportTabButton = null!;
    private Button patchMapButton = null!;
    private Button cellEditorButton = null!;
    private Button cellTypeTab = null!;
#pragma warning restore CA2213

    private EditorTab selectedTab = EditorTab.Report;

    [Signal]
    public delegate void OnTabSelected(EditorTab selectedTab);

    public override void _Ready()
    {
        if (TabButtonsPath == null)
            throw new MissingExportVariableValueException();

        var tabButtons = GetNode<TabButtons>(TabButtonsPath);

        reportTabButton = GetNode<Button>(tabButtons.GetAdjustedButtonPath(TabButtonsPath, ReportTabButtonPath));
        patchMapButton = GetNode<Button>(tabButtons.GetAdjustedButtonPath(TabButtonsPath, PatchMapButtonPath));
        cellEditorButton = GetNode<Button>(tabButtons.GetAdjustedButtonPath(TabButtonsPath, CellEditorButtonPath));
        cellTypeTab = GetNode<Button>(tabButtons.GetAdjustedButtonPath(TabButtonsPath, CellTypeTabPath));

        cellTypeTab.Visible = IsForMulticellular;
    }

    public void SetCurrentTab(EditorTab tab)
    {
        if (selectedTab == tab)
            return;

        selectedTab = tab;
        ApplyButtonStates();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (TabButtonsPath != null)
            {
                TabButtonsPath.Dispose();
                ReportTabButtonPath.Dispose();
                PatchMapButtonPath.Dispose();
                CellEditorButtonPath.Dispose();
                CellTypeTabPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void SetEditorTab(string tab)
    {
        var selection = (EditorTab)Enum.Parse(typeof(EditorTab), tab);

        if (selection == selectedTab)
            return;

        GUICommon.Instance.PlayButtonPressSound();

        selectedTab = selection;

        ApplyButtonStates();

        EmitSignal(nameof(OnTabSelected), selectedTab);
    }

    private void ApplyButtonStates()
    {
        reportTabButton.Pressed = selectedTab == EditorTab.Report;
        patchMapButton.Pressed = selectedTab == EditorTab.PatchMap;
        cellEditorButton.Pressed = selectedTab == EditorTab.CellEditor;
        cellTypeTab.Pressed = selectedTab == EditorTab.CellTypeEditor;
    }
}
