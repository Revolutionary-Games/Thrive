using Godot;
using Newtonsoft.Json;

/// <summary>
///   A hotkey representing and visualizing an input for in-game actions.
/// </summary>
public partial class ActionButton : Button
{
#pragma warning disable CA2213
    private Panel highlight = null!;
    private TextureRect? iconRect;
    private KeyPrompt? keyPrompt;

    private Texture2D? actionIcon;
#pragma warning restore CA2213

    private bool highlighted;

    private string actionName = string.Empty;

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
    public Texture2D? ActionIcon
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

    /// <summary>
    ///   When <see cref="ActionName"/> is set this is the resolved StringName variant of that for easy access
    ///   without needing to construct string names constantly
    /// </summary>
    [JsonIgnore]
    public StringName? ActionNameAsStringName => keyPrompt?.ResolvedAction;

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
        iconRect.Modulate = highlighted || ButtonPressed ?
            new Color(1.0f, 1.0f, 1.0f, 0.78f) :
            new Color(0.70f, 0.70f, 0.70f, 0.59f);
        highlight.Visible = ButtonPressed;
    }

    private void UpdateKeyPrompt()
    {
        if (keyPrompt == null || keyPrompt.ActionName == actionName)
            return;

        keyPrompt.ActionName = actionName;
        keyPrompt.Refresh();
    }
}
