using System;
using Godot;

/// <summary>
///   The top level tabs of the <see cref="MicrobeEditor"/> (and <see cref="MulticellularEditor"/>)
/// </summary>
public partial class MicrobeEditorTabButtons : MarginContainer
{
    [Export]
    [ExportCategory("Configuration")]
    public bool IsForMulticellular;

#pragma warning disable CA2213

    // Editor tab selector buttons
    [Export]
    private Button reportTabButton = null!;
    [Export]
    private Button patchMapButton = null!;
    [Export]
    private Button cellEditorButton = null!;
    [Export]
    private Button cellTypeTab = null!;
#pragma warning restore CA2213

    private EditorTab selectedTab = EditorTab.Report;

    private bool showReportTab = true;
    private bool showMapTab = true;

    [Signal]
    public delegate void OnTabSelectedEventHandler(EditorTab selectedTab);

    [Export]
    [ExportCategory("TabConfig")]
    public bool ShowReportTab
    {
        get => showReportTab;
        set
        {
            showReportTab = value;

            if (reportTabButton != null)
                reportTabButton.Visible = showReportTab;

            if (!value && selectedTab == EditorTab.Report)
            {
                GD.Print("Forcing cell editor tab to be selected as report tab is not visible");
                SetEditorTab(nameof(EditorTab.CellEditor));
            }
        }
    }

    [Export]
    public bool ShowMapTab
    {
        get => showMapTab;
        set
        {
            showMapTab = value;

            if (patchMapButton != null)
                patchMapButton.Visible = showMapTab;

            if (!value && selectedTab == EditorTab.PatchMap)
            {
                GD.Print("Forcing cell editor tab to be selected as map tab is not visible");
                SetEditorTab(nameof(EditorTab.CellEditor));
            }
        }
    }

    public override void _Ready()
    {
        cellTypeTab.Visible = IsForMulticellular;
        reportTabButton.Visible = showReportTab;
        patchMapButton.Visible = showMapTab;
    }

    public void SetCurrentTab(EditorTab tab)
    {
        if (selectedTab == tab)
            return;

        selectedTab = tab;
        ApplyButtonStates();
    }

    private void SetEditorTab(string tab)
    {
        var selection = (EditorTab)Enum.Parse(typeof(EditorTab), tab);

        if (selection == selectedTab)
            return;

        GUICommon.Instance.PlayButtonPressSound();

        selectedTab = selection;

        ApplyButtonStates();

        EmitSignal(SignalName.OnTabSelected, (int)selectedTab);
    }

    private void ApplyButtonStates()
    {
        if (reportTabButton != null)
            reportTabButton.ButtonPressed = selectedTab == EditorTab.Report;

        if (patchMapButton != null)
            patchMapButton.ButtonPressed = selectedTab == EditorTab.PatchMap;

        cellEditorButton.ButtonPressed = selectedTab == EditorTab.CellEditor;
        cellTypeTab.ButtonPressed = selectedTab == EditorTab.CellTypeEditor;
    }
}
