using System;
using System.Collections.Generic;
using System.Linq;
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
    private List<OrganelleTemplate>? selectedOrganelles;
    private bool enableDelete = true;
    private bool enableMove = true;
    private bool enableModify;
    private float editorCostFactor = 1.0f;

    [Signal]
    public delegate void DeletePressed();

    [Signal]
    public delegate void MovePressed();

    [Signal]
    public delegate void ModifyPressed();

    public float EditorCostFactor
    {
        get => editorCostFactor;
        set => editorCostFactor = value;
    }

    public Func<IEnumerable<EditorCombinableActionData>, int>? GetActionPrice { get; set; }

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
    ///   The organelle the user explicitly selected
    /// </summary>
    public OrganelleTemplate? MainOrganelle => selectedOrganelles?[0];

    /// <summary>
    ///   The placed organelles to be shown options of.
    /// </summary>
    public List<OrganelleTemplate> SelectedOrganelles
    {
        get => selectedOrganelles ??
            throw new InvalidOperationException("OrganellePopup was not opened with organelle set");
        set
        {
            selectedOrganelles = value;
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
        if (selectedOrganelles != null)
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

        var names = SelectedOrganelles.Select(p => p.Definition.Name).Distinct().ToList();

        if (names.Count == 1)
        {
            selectedOrganelleNameLabel.Text = names[0];
        }
        else
        {
            selectedOrganelleNameLabel.Text = TranslationServer.Translate("MULTIPLE_ORGANELLES");
        }
    }

    private void UpdateDeleteButton()
    {
        if (deleteButton == null)
            return;

        var mpCost = GetActionPrice?.Invoke(
            SelectedOrganelles
                .Select(o => (EditorCombinableActionData)new RemoveActionData(o))
                .ToList()) ?? throw new ArgumentException($"{nameof(GetActionPrice)} not set");

        var mpLabel = deleteButton.GetNode<Label>("MarginContainer/HBoxContainer/MpCost");
        mpCost = (int)(mpCost * editorCostFactor);

        mpLabel.Text = new LocalizedString("MP_COST", -mpCost).ToString();

        deleteButton.Disabled = !EnableDeleteOption;
    }

    private void UpdateMoveButton()
    {
        if (moveButton == null)
            return;

        var mpCost = GetActionPrice?.Invoke(SelectedOrganelles.Select(o =>
                (EditorCombinableActionData)new OrganelleMoveActionData(o, o.Position, o.Position, o.Orientation,
                    o.Orientation))
            .ToList()) ?? throw new ArgumentException($"{nameof(GetActionPrice)} not set");

        var mpLabel = moveButton.GetNode<Label>("MarginContainer/HBoxContainer/MpCost");
        mpCost = (int)(mpCost * editorCostFactor);

        mpLabel.Text = new LocalizedString("MP_COST", mpCost).ToString();

        moveButton.Disabled = !EnableMoveOption;
    }

    private void UpdateModifyButton()
    {
        if (modifyButton == null)
            return;

        modifyButton.Disabled = !enableModify;
    }
}
