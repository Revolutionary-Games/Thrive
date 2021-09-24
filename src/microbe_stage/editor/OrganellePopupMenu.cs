using System;
using System.Globalization;
using Godot;

/// <summary>
///   Manages a custom context menu solely for showing list of options for a placed organelle
///   in the microbe editor.
/// </summary>
public class OrganellePopupMenu : PopupPanel
{
    [Export]
    public NodePath SelectedOrganelleNameLabelPath;

    [Export]
    public NodePath DeleteButtonPath;

    [Export]
    public NodePath MoveButtonPath;

    [Export]
    public NodePath MicrobeEditorPath;

    private Label selectedOrganelleNameLabel;
    private Button deleteButton;
    private Button moveButton;
    private MicrobeEditor microbeEditor;

    private bool showPopup;
    private OrganelleTemplate selectedOrganelle;
    private bool enableDelete = true;
    private bool enableMove = true;

    [Signal]
    public delegate void DeletePressed();

    [Signal]
    public delegate void MovePressed();

    public bool ShowPopup
    {
        get => showPopup;
        set
        {
            showPopup = value;

            // Popups should work with the pause menu
            // TODO: See #1857
            if (ShowPopup)
            {
                RectPosition = GetViewport().GetMousePosition();
                ShowModal();
                SetAsMinsize();
            }
            else
            {
                Hide();
            }

            UpdateDeleteButton();
            UpdateMoveButton();
        }
    }

    /// <summary>
    ///   The placed organelle to be shown options of.
    /// </summary>
    public OrganelleTemplate SelectedOrganelle
    {
        get => selectedOrganelle;
        set
        {
            selectedOrganelle = value;
            UpdateOrganelleNameLabel();
        }
    }

    public bool EnableDeleteOption
    {
        get => enableDelete;
        set
        {
            enableDelete = value;
            UpdateDeleteButton();
        }
    }

    public bool EnableMoveOption
    {
        get => enableMove;
        set
        {
            enableMove = value;
            UpdateMoveButton();
        }
    }

    public override void _Ready()
    {
        selectedOrganelleNameLabel = GetNode<Label>(SelectedOrganelleNameLabelPath);
        deleteButton = GetNode<Button>(DeleteButtonPath);
        moveButton = GetNode<Button>(MoveButtonPath);
        microbeEditor = GetNode<MicrobeEditor>(MicrobeEditorPath);

        UpdateOrganelleNameLabel();
        UpdateDeleteButton();
        UpdateMoveButton();
    }

    public override void _EnterTree()
    {
        InputManager.RegisterReceiver(this);
        base._EnterTree();
    }

    public override void _ExitTree()
    {
        InputManager.UnregisterReceiver(this);
        base._ExitTree();
    }

    [RunOnKeyDown("e_delete", Priority = 1)]
    public bool OnDeleteKeyPressed()
    {
        if (Visible)
        {
            EmitSignal(nameof(DeletePressed));

            Hide();

            return true;
        }

        // Return false to indicate that the key input wasn't handled.
        return false;
    }

    [RunOnKeyDown("e_move", Priority = 1)]
    public bool OnMoveKeyPressed()
    {
        if (Visible)
        {
            EmitSignal(nameof(MovePressed));

            Hide();

            return true;
        }

        // Return false to indicate that the key input wasn't handled.
        return false;
    }

    private void OnDeletePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(nameof(DeletePressed));

        Hide();
    }

    private void OnMovePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(nameof(MovePressed));

        Hide();
    }

    private void OnModifyPressed()
    {
        throw new NotImplementedException();
    }

    private void UpdateButtonContentsColour(string optionName, bool pressed)
    {
        var icon = GetNode<TextureRect>("VBoxContainer/" + optionName + "/MarginContainer/HBoxContainer/Icon");
        var nameLabel = GetNode<Label>("VBoxContainer/" + optionName + "/MarginContainer/HBoxContainer/Name");
        var mpLabel = GetNode<Label>("VBoxContainer/" + optionName + "/MarginContainer/HBoxContainer/MpCost");

        if (pressed)
        {
            icon.Modulate = new Color(0, 0, 0);
            nameLabel.AddColorOverride("font_color", new Color(0, 0, 0));
            mpLabel.AddColorOverride("font_color", new Color(0, 0, 0));
        }
        else
        {
            icon.Modulate = new Color(1, 1, 1);
            nameLabel.AddColorOverride("font_color", new Color(1, 1, 1));
            mpLabel.AddColorOverride("font_color", new Color(1, 1, 1));
        }
    }

    private void UpdateOrganelleNameLabel()
    {
        if (selectedOrganelleNameLabel == null)
            return;

        selectedOrganelleNameLabel.Text = SelectedOrganelle?.Definition.Name;
    }

    private void UpdateDeleteButton()
    {
        if (deleteButton == null)
            return;

        float mpCost;
        if (SelectedOrganelle == null)
        {
            mpCost = 0;
        }
        else
        {
            mpCost = microbeEditor.History.WhatWouldActionCost(
                new RemoveActionData(SelectedOrganelle, SelectedOrganelle.Position, SelectedOrganelle.Orientation));
        }

        var mpLabel = deleteButton.GetNode<Label>("MarginContainer/HBoxContainer/MpCost");

        mpLabel.Text = string.Format(CultureInfo.CurrentCulture,
            TranslationServer.Translate("MP_COST"), mpCost > 0 ? "+" + mpCost : "-" + -mpCost);

        deleteButton.Disabled = !EnableDeleteOption;
    }

    private void UpdateMoveButton()
    {
        if (moveButton == null)
            return;

        float mpCost;
        if (SelectedOrganelle == null)
        {
            mpCost = 0;
        }
        else
        {
            mpCost = microbeEditor.History.WhatWouldActionCost(
                new MoveActionData(SelectedOrganelle, SelectedOrganelle.Position, SelectedOrganelle.Position,
                    SelectedOrganelle.Orientation, SelectedOrganelle.Orientation));
        }

        var mpLabel = moveButton.GetNode<Label>("MarginContainer/HBoxContainer/MpCost");

        mpLabel.Text = string.Format(CultureInfo.CurrentCulture,
            TranslationServer.Translate("MP_COST"),
            mpCost.ToString(CultureInfo.CurrentCulture));

        moveButton.Disabled = !EnableMoveOption;
    }
}
