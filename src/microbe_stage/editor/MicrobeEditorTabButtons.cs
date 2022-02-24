using System;
using Godot;

public class MicrobeEditorTabButtons : MarginContainer
{
    [Export]
    public bool IsForMulticellular;

    [Export]
    public NodePath CellTypeTabPath = null!;

    private Button cellTypeTab = null!;

    private EditorTab selectedTab = EditorTab.Report;

    [Signal]
    public delegate void OnTabSelected(EditorTab selectedTab);

    public override void _Ready()
    {
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

        EmitSignal(nameof(OnTabSelected), selectedTab);
    }
}
