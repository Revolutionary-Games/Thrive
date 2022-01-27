using Godot;

public class OrganelleUpgradeGUI : Control
{
    [Export]
    public NodePath PopupPath;

    [Export]
    public NodePath OrganelleSpecificContentPath;

    [Export]
    public NodePath ScrollContainerPath;

    private CustomConfirmationDialog popup;
    private Container organelleSpecificContent;
    private ScrollContainer scrollContainer;

    private MicrobeEditor storedEditor;
    private IOrganelleUpgrader upgrader;

    public override void _Ready()
    {
        popup = GetNode<CustomConfirmationDialog>(PopupPath);
        organelleSpecificContent = GetNode<Container>(OrganelleSpecificContentPath);
        scrollContainer = GetNode<ScrollContainer>(ScrollContainerPath);
    }

    public void OpenForOrganelle(OrganelleTemplate organelle, string upgraderScene, MicrobeEditor editor)
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

        scrollContainer.ScrollVertical = 0;
        upgrader.OnStartFor(organelle);
        storedEditor = editor;
    }

    private void OnAccept()
    {
        GUICommon.Instance.PlayButtonPressSound();
        upgrader.ApplyChanges(storedEditor);
    }

    private void OnCancel()
    {
        GUICommon.Instance.PlayButtonPressSound();
    }
}
