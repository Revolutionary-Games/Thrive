using System;
using Godot;

public class MicrobeEditorTabButtons : MarginContainer
{
    [Export]
    public bool IsForMulticellular;

    [Export]
    public NodePath ReportTabButtonPath = null!;

    [Export]
    public NodePath PatchMapButtonPath = null!;

    [Export]
    public NodePath CellEditorButtonPath = null!;

    [Export]
    public NodePath CellTypeTabPath = null!;

    private EditorTab selectedTab = EditorTab.Report;

    // Editor tab selector buttons
    private Button reportTabButton = null!;
    private Button patchMapButton = null!;
    private Button cellEditorButton = null!;
    private Button cellTypeTab = null!;

    [Signal]
    public delegate void OnTabSelected(EditorTab selectedTab);

    public override void _Ready()
    {
        reportTabButton = GetNode<Button>(ReportTabButtonPath);
        patchMapButton = GetNode<Button>(PatchMapButtonPath);
        cellEditorButton = GetNode<Button>(CellEditorButtonPath);
        cellTypeTab = GetNode<Button>(CellTypeTabPath);

        cellTypeTab.Visible = IsForMulticellular;
    }

    private void SetEditorTab(string tab)
    {
        var selection = (EditorTab)Enum.Parse(typeof(EditorTab), tab);

        if (selection == selectedTab)
            return;

        GUICommon.Instance.PlayButtonPressSound();

        selectedTab = selection;

        reportTabButton.Pressed = selectedTab == EditorTab.Report;
        patchMapButton.Pressed = selectedTab == EditorTab.PatchMap;
        cellEditorButton.Pressed = selectedTab == EditorTab.CellEditor;
        cellTypeTab.Pressed = selectedTab == EditorTab.CellTypeEditor;

        EmitSignal(nameof(OnTabSelected), selectedTab);
    }
}
