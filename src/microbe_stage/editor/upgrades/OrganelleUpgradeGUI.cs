using Godot;

public class OrganelleUpgradeGUI : Control
{
    [Export]
    public NodePath PopupPath = null!;

    [Export]
    public NodePath OrganelleSpecificContentPath = null!;

    [Export]
    public NodePath ScrollContainerPath = null!;

#pragma warning disable CA2213
    private CustomConfirmationDialog popup = null!;
    private Container organelleSpecificContent = null!;
    private ScrollContainer scrollContainer = null!;
#pragma warning restore CA2213

    private ICellEditorData? storedEditor;
    private IOrganelleUpgrader? upgrader;

    [Signal]
    public delegate void Accepted();

    public override void _Ready()
    {
        popup = GetNode<CustomConfirmationDialog>(PopupPath);
        organelleSpecificContent = GetNode<Container>(OrganelleSpecificContentPath);
        scrollContainer = GetNode<ScrollContainer>(ScrollContainerPath);
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

        scrollContainer.RectMinSize = upgrader.GetMinDialogSize();

        popup.PopupCenteredShrink();

        scrollContainer.ScrollVertical = 0;
        upgrader.OnStartFor(organelle);
        storedEditor = editor;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            PopupPath.Dispose();
            OrganelleSpecificContentPath.Dispose();
            ScrollContainerPath.Dispose();
        }

        base.Dispose(disposing);
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

        EmitSignal(nameof(Accepted));
    }

    private void OnCancel()
    {
        GUICommon.Instance.PlayButtonPressSound();
    }
}
