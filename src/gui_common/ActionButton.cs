using Godot;

/// <summary>
///   A hotkey representing and visualizing an input for in-game actions.
/// </summary>
public class ActionButton : Button
{
    private Panel highlight;
    private TextureRect iconRect;
    private KeyPrompt keyPrompt;

    private bool highlighted;

    private Texture actionIcon;
    private string actionName;

    public bool Highlighted
    {
        get => highlighted;
        set
        {
            highlighted = value;
            UpdateIcon();
        }
    }

    /// <summary>
    ///   The icon for the action, displayed prominently at the center of the button.
    /// </summary>
    [Export]
    public Texture ActionIcon
    {
        get => actionIcon;
        set
        {
            actionIcon = value;
            UpdateIcon();
        }
    }

    /// <summary>
    ///   The assigned Godot action event for the input.
    /// </summary>
    [Export]
    public string ActionName
    {
        get => actionName;
        set
        {
            actionName = value;
            UpdateKeyPrompt();
        }
    }

    public override void _Ready()
    {
        highlight = GetNode<Panel>("Highlight");
        iconRect = GetNode<TextureRect>("MarginContainer/VBoxContainer/Icon");
        keyPrompt = GetNode<KeyPrompt>("MarginContainer/VBoxContainer/KeyPrompt");

        UpdateIcon();
        UpdateKeyPrompt();
    }

    public override void _Draw()
    {
        UpdateIcon();
        UpdateKeyPrompt();
    }

    private void OnMouseEntered()
    {
        Highlighted = true;
    }

    private void OnMouseExited()
    {
        Highlighted = false;
    }

    private void UpdateIcon()
    {
        if (iconRect == null)
            return;

        iconRect.Texture = ActionIcon;
        iconRect.Modulate = highlighted || Pressed ?
            new Color(1.0f, 1.0f, 1.0f, 0.78f) :
            new Color(0.70f, 0.70f, 0.70f, 0.59f);
        highlight.Visible = Pressed;
    }

    private void UpdateKeyPrompt()
    {
        if (keyPrompt == null || keyPrompt.ActionName == actionName)
            return;

        keyPrompt.ActionName = actionName;
        keyPrompt.Refresh();
    }
}
