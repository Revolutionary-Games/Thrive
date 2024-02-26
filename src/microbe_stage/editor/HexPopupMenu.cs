using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Base class for more specialized right click popup menus for the editor
/// </summary>
public abstract partial class HexPopupMenu : CustomPopupMenu
{
    [Export]
    public NodePath? TitleLabelPath;

    [Export]
    public NodePath DeleteButtonPath = null!;

    [Export]
    public NodePath MoveButtonPath = null!;

    [Export]
    public NodePath ModifyButtonPath = null!;

#pragma warning disable CA2213
    protected Label? titleLabel;
    protected Button? deleteButton;
    protected Button? moveButton;
    protected Button? modifyButton;
#pragma warning restore CA2213

    private bool showPopup;
    private bool enableDelete = true;
    private bool enableMove = true;
    private bool enableModify;

    private string? deleteTooltip;

    [Signal]
    public delegate void DeletePressedEventHandler();

    [Signal]
    public delegate void MovePressedEventHandler();

    [Signal]
    public delegate void ModifyPressedEventHandler();

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
                Position = GetViewport().GetMousePosition();
                OpenModal();
            }
            else
            {
                Close();
            }

            UpdateDeleteButton();
            UpdateMoveButton();
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

    public virtual bool EnableModifyOption
    {
        get => enableModify;
        set
        {
            enableModify = value;
            UpdateModifyButton();
        }
    }

    public string? DeleteOptionTooltip
    {
        get => deleteTooltip;
        set
        {
            deleteTooltip = value;
            UpdateDeleteButton();
        }
    }

    private bool IsInteractable => Visible && !Closing;

    public override void _Ready()
    {
        base._Ready();

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
        if (IsInteractable)
        {
            EmitSignal(nameof(DeletePressedEventHandler));

            Close();

            return true;
        }

        // Return false to indicate that the key input wasn't handled.
        return false;
    }

    [RunOnKeyDown("e_move", Priority = 1)]
    public bool OnMoveKeyPressed()
    {
        if (IsInteractable)
        {
            EmitSignal(nameof(MovePressedEventHandler));

            Close();

            return true;
        }

        // Return false to indicate that the key input wasn't handled.
        return false;
    }

    protected override void ResolveNodeReferences()
    {
        titleLabel = GetNode<Label>(TitleLabelPath);
        deleteButton = GetNode<Button>(DeleteButtonPath);
        moveButton = GetNode<Button>(MoveButtonPath);
        modifyButton = GetNode<Button>(ModifyButtonPath);
    }

    protected abstract void UpdateTitleLabel();

    protected abstract void UpdateDeleteButton();

    protected abstract void UpdateMoveButton();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (TitleLabelPath != null)
            {
                TitleLabelPath.Dispose();
                DeleteButtonPath.Dispose();
                MoveButtonPath.Dispose();
                ModifyButtonPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void UpdateModifyButton()
    {
        if (modifyButton == null)
            return;

        modifyButton.Disabled = !EnableModifyOption;
    }

    private void UpdateButtonContentsColour(string optionName, bool pressed)
    {
        var prefix = "PanelContainer/VBoxContainer/";
        var icon = GetNode<TextureRect>(prefix + optionName + "/MarginContainer/HBoxContainer/Icon");
        var nameLabel = GetNode<Label>(prefix + optionName + "/MarginContainer/HBoxContainer/Name");
        var mpLabel = GetNode<Label>(prefix + optionName + "/MarginContainer/HBoxContainer/MpCost");

        if (pressed)
        {
            icon.Modulate = new Color(0, 0, 0);
            nameLabel.AddThemeColorOverride("font_color", new Color(0, 0, 0));
            mpLabel.AddThemeColorOverride("font_color", new Color(0, 0, 0));
        }
        else
        {
            icon.Modulate = new Color(1, 1, 1);
            nameLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1));
            mpLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1));
        }
    }

    private void OnDeletePressed()
    {
        if (!IsInteractable)
            return;

        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(nameof(DeletePressedEventHandler));

        Close();
    }

    private void OnMovePressed()
    {
        if (!IsInteractable)
            return;

        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(nameof(MovePressedEventHandler));

        Close();
    }

    private void OnModifyPressed()
    {
        if (!IsInteractable)
            return;

        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(nameof(ModifyPressedEventHandler));

        Close();
    }
}
