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

    [Export]
    public NodePath ExtraInfoLabelPath = null!;

#pragma warning disable CA2213
    [Export]
    public Font InteractionButtonFont = null!;

    private CustomWindow popup = null!;
    private Container buttonsContainer = null!;
    private Button cancelButton = null!;

    private Label extraInfoLabel = null!;
#pragma warning restore CA2213

    private IInteractableEntity? openedFor;

    /// <summary>
    ///   Godot can't handle this interface type to be passed through its signals, so we use a native C# signal here
    /// </summary>
    public delegate void OnInteractionSelected(IInteractableEntity entity, InteractionType interactionType);

    public OnInteractionSelected? OnInteractionSelectedHandler { get; set; }

    public override void _Ready()
    {
        popup = GetNode<CustomWindow>(PopupPath);
        buttonsContainer = GetNode<Container>(ButtonsContainerPath);
        cancelButton = GetNode<Button>(CancelButtonPath);
        extraInfoLabel = GetNode<Label>(ExtraInfoLabelPath);

        // This is invisible in the editor to make it nicer to edit things
        Visible = true;
    }

    public void ShowForInteractable(IInteractableEntity entity,
        IEnumerable<(InteractionType Interaction, bool Enabled, string? TextOverride)> availableInteractions)
    {
        openedFor = entity;

        var extraText = openedFor.ExtraInteractionPopupDescription;

        if (!string.IsNullOrEmpty(extraText))
        {
            extraInfoLabel.Text = extraText;
            extraInfoLabel.Visible = true;
        }
        else
        {
            extraInfoLabel.Visible = false;
        }

        buttonsContainer.QueueFreeChildren();
        Button? firstButton = null;

        // TODO: add a selection wheel as an alternative for more sane controller input

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

        if (firstButton == null)
        {
            GD.Print("No actions available for an interactable in the popup");
            cancelButton.GrabFocusInOpeningPopup();
        }
        else
        {
            firstButton.GrabFocusInOpeningPopup();
        }
    }

    /// <summary>
    ///   Selects the currently selected option for interaction (or the close button) if any are currently selected
    /// </summary>
    /// <returns>True when something could be selected, false if this is not open or focus is somewhere else</returns>
    public bool SelectCurrentOptionIfOpen()
    {
        if (!popup.Visible)
            return false;

        var focused = GetFocusOwner();

        if (focused == null)
            return false;

        // Because we don't really store our buttons in the popup, we need to do a check to confirm that something
        // in our popup is focused. And if something is focused there, we can click that.
        using var popupPath = popup.GetPath();
        using var focusedPath = focused.GetPath();

        if (focusedPath.ToString().StartsWith(popupPath.ToString()))
        {
            focused.EmitSignal("pressed");
            return true;
        }

        // Something else than our popup is focused, so don't do anything to not mess with unexpected parts of the game
        return false;
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
                ExtraInfoLabelPath.Dispose();
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
