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
    public NodePath SelectedOrganelleNameLabelPath = null!;

    [Export]
    public NodePath DeleteButtonPath = null!;

    [Export]
    public NodePath MoveButtonPath = null!;

    [Export]
    public NodePath ModifyButtonPath = null!;

    private Label? selectedOrganelleNameLabel;
    private Button? deleteButton;
    private Button? moveButton;
    private Button? modifyButton;

    private bool showPopup;
    private OrganelleTemplate? selectedOrganelle;
    private bool enableDelete = true;
    private bool enableMove = true;
    private bool enableModify;

    [Signal]
    public delegate void DeletePressed();

    [Signal]
    public delegate void MovePressed();

    [Signal]
    public delegate void ModifyPressed();

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
        get => selectedOrganelle ??
            throw new InvalidOperationException("OrganellePopup was not opened with organelle set");
        set
        {
            selectedOrganelle = value ?? throw new ArgumentNullException();
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

    public bool EnableModifyOption
    {
        get => enableModify;
        set
        {
            enableModify = value;
            UpdateModifyButton();
        }
    }

    public override void _Ready()
    {
        selectedOrganelleNameLabel = GetNode<Label>(SelectedOrganelleNameLabelPath);
        deleteButton = GetNode<Button>(DeleteButtonPath);
        moveButton = GetNode<Button>(MoveButtonPath);
        modifyButton = GetNode<Button>(ModifyButtonPath);

        // Skip things that use the organelle to work on if we aren't open (no selected organelle set)
        if (selectedOrganelle != null)
        {
            UpdateOrganelleNameLabel();
            UpdateDeleteButton();
            UpdateMoveButton();
        }

        UpdateModifyButton();
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
        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(nameof(ModifyPressed));

        Hide();
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

        selectedOrganelleNameLabel.Text = SelectedOrganelle.Definition.Name;
    }

    private void UpdateDeleteButton()
    {
        if (deleteButton == null)
            return;

        var mpLabel = deleteButton.GetNode<Label>("MarginContainer/HBoxContainer/MpCost");

        mpLabel.Text = string.Format(CultureInfo.CurrentCulture,
            TranslationServer.Translate("MP_COST"),
            SelectedOrganelle is { PlacedThisSession: true } ?
                "+" + SelectedOrganelle.Definition.MPCost :
                "-" + Constants.ORGANELLE_REMOVE_COST);

        deleteButton.Disabled = !EnableDeleteOption;
    }

    private void UpdateMoveButton()
    {
        if (moveButton == null)
            return;

        var mpLabel = moveButton.GetNode<Label>("MarginContainer/HBoxContainer/MpCost");

        // The organelle is free to move if it was added (placed) this session or already moved this session
        bool isFreeToMove = SelectedOrganelle.MovedThisSession || SelectedOrganelle.PlacedThisSession;
        mpLabel.Text = string.Format(CultureInfo.CurrentCulture,
            TranslationServer.Translate("MP_COST"),
            isFreeToMove ? "-0" : "-" + Constants.ORGANELLE_MOVE_COST.ToString(CultureInfo.CurrentCulture));

        moveButton.Disabled = !EnableMoveOption;
    }

    private void UpdateModifyButton()
    {
        if (modifyButton == null)
            return;

        modifyButton.Disabled = !enableModify;
    }
}
