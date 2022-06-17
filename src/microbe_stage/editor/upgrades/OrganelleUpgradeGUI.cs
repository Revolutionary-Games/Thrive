using Godot;

public class OrganelleUpgradeGUI : Control, IOrganelleUpgradeDialog
{
    [Export]
    public NodePath PopupPath = null!;

    [Export]
    public NodePath OrganelleSpecificContentPath = null!;

    private CustomConfirmationDialog popup = null!;
    private Container organelleSpecificContent = null!;

    private ICellEditorData? storedEditor;
    private IOrganelleUpgrader? upgrader;

    public override void _Ready()
    {
        popup = GetNode<CustomConfirmationDialog>(PopupPath);
        organelleSpecificContent = GetNode<Container>(OrganelleSpecificContentPath);
    }

    public void OpenForOrganelle(OrganelleTemplate organelle, string upgraderScene, ICellEditorData editor)
    {
        var scene = GD.Load<PackedScene>(upgraderScene);

        if (scene == null)
        {
            GD.PrintErr($"Failed to load upgrader scene for organelle of type {organelle.Definition.InternalName}");
            return;
        }

        var instance = scene.Instance();
        upgrader = (IOrganelleUpgrader)instance;

        organelleSpecificContent.FreeChildren();
        organelleSpecificContent.AddChild(instance);

        popup.PopupCenteredShrink();

        upgrader.OnStartFor(organelle, this);
        storedEditor = editor;
    }

    public void Redraw()
    {
        popup.PopupCenteredShrink();
    }

    private void OnAccept()
    {
        if (upgrader == null || storedEditor == null)
        {
            GD.PrintErr("Can't apply organelle upgrades as this upgrade GUI was not opened properly");
            return;
        }

        GUICommon.Instance.PlayButtonPressSound();
        upgrader.ApplyChanges(storedEditor);
    }

    private void OnCancel()
    {
        GUICommon.Instance.PlayButtonPressSound();
    }
}
