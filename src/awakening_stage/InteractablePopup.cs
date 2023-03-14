using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Collections;
using Container = Godot.Container;

public class InteractablePopup : Control
{
    [Export]
    public NodePath? PopupPath;

    [Export]
    public NodePath ButtonsContainerPath = null!;

    [Export]
    public NodePath CancelButtonPath = null!;

#pragma warning disable CA2213
    [Export]
    public Font InteractionButtonFont = null!;

    private CustomDialog popup = null!;
    private Container buttonsContainer = null!;
    private Button cancelButton = null!;
#pragma warning restore CA2213

    private IInteractableEntity? openedFor;

    /// <summary>
    ///   Godot can't handle this interface type to be passed through its signals, so we use a native C# signal here
    /// </summary>
    public delegate void OnInteractionSelected(IInteractableEntity entity, InteractionType interactionType);

    public OnInteractionSelected? OnInteractionSelectedHandler { get; set; }

    public override void _Ready()
    {
        popup = GetNode<CustomDialog>(PopupPath);
        buttonsContainer = GetNode<Container>(ButtonsContainerPath);
        cancelButton = GetNode<Button>(CancelButtonPath);

        // This is invisible in the editor to make it nicer to edit things
        Visible = true;
    }

    public void ShowForInteractable(IInteractableEntity entity,
        IEnumerable<(InteractionType Interaction, bool Enabled, string? TextOverride)> availableInteractions)
    {
        openedFor = entity;

        buttonsContainer.QueueFreeChildren();
        Button? firstButton = null;

        foreach (var (interactionType, enabled, textOverride) in availableInteractions)
        {
            // TODO: add a different visual style to reduce the visual complexity from all of these being full-blown
            // buttons
            var button = new Button
            {
                SizeFlagsHorizontal = 0,
                Text = textOverride ??
                    TranslationServer.Translate(interactionType.GetAttribute<DescriptionAttribute>().Description),
            };

            button.AddFontOverride("font", InteractionButtonFont);

            buttonsContainer.AddChild(button);

            if (!enabled)
            {
                button.Disabled = true;
                continue;
            }

            var binds = new Array();
            binds.Add(interactionType);
            button.Connect("pressed", this, nameof(OptionSelected), binds);

            firstButton ??= button;
        }

        popup.WindowTitle = entity.ReadableName;
        popup.PopupCenteredShrink();

        // Focus needs to be adjusted after opening
        if (firstButton == null)
        {
            GD.Print("No actions available for an interactable in the popup");
            cancelButton.GrabFocus();
            cancelButton.GrabClickFocus();
        }
        else
        {
            firstButton.GrabFocus();
            firstButton.GrabClickFocus();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (PopupPath != null)
            {
                PopupPath.Dispose();
                ButtonsContainerPath.Dispose();
                CancelButtonPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void OptionSelected(InteractionType interactionType)
    {
        if (openedFor == null)
        {
            GD.PrintErr("Interaction popup got option selection without being opened for an entity");
            return;
        }

        GUICommon.Instance.PlayButtonPressSound();
        popup.Hide();

        OnInteractionSelectedHandler?.Invoke(openedFor, interactionType);
    }
}
