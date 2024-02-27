using System.ComponentModel;
using Godot;
using Container = Godot.Container;

/// <summary>
///   Info and possible actions on a space structure
/// </summary>
public partial class SpaceStructureInfoPopup : CustomWindow
{
    [Export]
    public NodePath? StructureStatusTextLabelPath;

    [Export]
    public NodePath InteractionButtonContainerPath = null!;

#pragma warning disable CA2213
    private Label structureStatusTextLabel = null!;

    private Container interactionButtonContainer = null!;
#pragma warning restore CA2213

    private ChildObjectCache<InteractionType, CreatedInteractionButton> interactionButtons = null!;

    private EntityReference<PlacedSpaceStructure> managedStructure = new();

    private double elapsed = 1;

    public override void _Ready()
    {
        base._Ready();

        structureStatusTextLabel = GetNode<Label>(StructureStatusTextLabelPath);
        interactionButtonContainer = GetNode<Container>(InteractionButtonContainerPath);

        interactionButtons =
            new ChildObjectCache<InteractionType, CreatedInteractionButton>(interactionButtonContainer,
                CreateInteractionButton);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!Visible)
            return;

        if (!managedStructure.IsAlive)
        {
            GD.Print("Closing space structure popup as target is gone");
            Close();
            return;
        }

        elapsed += delta;

        if (elapsed > Constants.SPACE_STAGE_STRUCTURE_PROCESS_INTERVAL)
        {
            elapsed = 0;

            UpdateInfo();
        }
    }

    /// <summary>
    ///   Opens this screen for a structure
    /// </summary>
    public void ShowForStructure(PlacedSpaceStructure structure)
    {
        if (Visible)
        {
            Close();
        }

        managedStructure = new EntityReference<PlacedSpaceStructure>(structure);
        elapsed = 1;

        UpdateInfo();
        Show();
    }

    protected override void OnHidden()
    {
        base.OnHidden();

        // Clear the buttons to have them in an expected order when this is reopened potentially for a different type
        interactionButtons.Clear();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (StructureStatusTextLabelPath != null)
            {
                StructureStatusTextLabelPath.Dispose();
                InteractionButtonContainerPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void UpdateInfo()
    {
        var target = managedStructure.Value;

        if (target == null)
            return;

        WindowTitle = target.ReadableName;
        structureStatusTextLabel.Text = target.StructureExtraDescription;

        UpdateInteractionButtons(target);
    }

    private void UpdateInteractionButtons(PlacedSpaceStructure targetStructure)
    {
        interactionButtons.UnMarkAll();

        foreach (var (type, disabled) in targetStructure.GetAvailableActions())
        {
            var button = interactionButtons.GetChild(type);

            // TODO: somehow ensure the buttons are in the correct order (right now new actions becoming available
            // cause the buttons to be in inconsistent order compared to reopening the popup)

            if (disabled != null)
            {
                if (button.Disabled != true)
                {
                    button.Disabled = true;
                    button.Text = disabled;
                }
            }
            else if (button.Disabled)
            {
                button.Disabled = false;
                button.Text = Localization.Translate(type.GetAttribute<DescriptionAttribute>().Description);
            }
        }

        interactionButtons.DeleteUnmarked();
    }

    private void OnActionSelected(InteractionType action)
    {
        var target = managedStructure.Value;

        if (target == null)
        {
            GD.Print("Interaction space target has disappeared");
            return;
        }

        if (!target.PerformAction(action))
        {
            GD.PrintErr("Failed to perform interaction on space structure: ", action);
        }
    }

    private CreatedInteractionButton CreateInteractionButton(InteractionType type)
    {
        var button = new CreatedInteractionButton
        {
            // TODO: make this react to language change (probably needs a new attribute to save the thing and a
            // listener for language change event
            Text = Localization.Translate(type.GetAttribute<DescriptionAttribute>().Description),
            SizeFlagsHorizontal = 0,
        };

        button.Connect(BaseButton.SignalName.Pressed, Callable.From(() => OnActionSelected(type)));

        return button;
    }

    private partial class CreatedInteractionButton : Button
    {
    }
}
