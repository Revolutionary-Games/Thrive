using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Base class for more specialized right click popup menus for the editor
/// </summary>
public abstract class HexPopupMenu : CustomPopupMenu
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
    protected Font? normalFont;
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
    public delegate void DeletePressed();

    [Signal]
    public delegate void MovePressed();

    [Signal]
    public delegate void ModifyPressed();

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
        normalFont = GetFont("normal", "Fonts");
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
            EmitSignal(nameof(DeletePressed));

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
            EmitSignal(nameof(MovePressed));

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

    protected override Vector2 CalculateSize()
    {
        var size = base.CalculateSize();
        if (titleLabel != null && normalFont != null)
        {
            // reset size of label to update it
            // otherwise rect height doesn't update to new value soon enough
            titleLabel.SetSize(Vector2.Zero);

            size = new Vector2(size.x,
                size.y +
                Mathf.Abs(
                    (titleLabel.GetVisibleLineCount() *
                        normalFont.GetHeight()) -
                    titleLabel.RectSize.y));

            // make the container visible because for whatever
            // reason resetting size of text makes it invisible
            // I don't know why this works, but it just works
            container.Visible = false;
            container.Visible = true;
        }

        return size;
    }

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
        var prefix = "Panel/Control/VBoxContainer/";
        var icon = GetNode<TextureRect>(prefix + optionName + "/MarginContainer/HBoxContainer/Icon");
        var nameLabel = GetNode<Label>(prefix + optionName + "/MarginContainer/HBoxContainer/Name");
        var mpLabel = GetNode<Label>(prefix + optionName + "/MarginContainer/HBoxContainer/MpCost");

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

    private void OnDeletePressed()
    {
        if (!IsInteractable)
            return;

        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(nameof(DeletePressed));

        Close();
    }

    private void OnMovePressed()
    {
        if (!IsInteractable)
            return;

        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(nameof(MovePressed));

        Close();
    }

    private void OnModifyPressed()
    {
        if (!IsInteractable)
            return;

        GUICommon.Instance.PlayButtonPressSound();

        EmitSignal(nameof(ModifyPressed));

        Close();
    }
}
