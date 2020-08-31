using System;
using Godot;

/// <summary>
///   Shows a key prompt that reacts to being pressed down
/// </summary>
public class KeyPrompt : Control
{
    /// <summary>
    ///   Name of the action this key prompt shows
    /// </summary>
    [Export]
    public string ActionName;

    /// <summary>
    ///   If true reacts when the user presses the key
    /// </summary>
    [Export]
    public bool ShowPress = true;

    /// <summary>
    ///   Colour modulation when unpressed
    /// </summary>
    [Export]
    public Color UnpressedColour = new Color(1, 1, 1, 1);

    /// <summary>
    ///   Colour modulation when pressed
    /// </summary>
    [Export]
    public Color PressedColour = new Color(0.7f, 0.7f, 0.7f, 1);

    private TextureRect icon;

    // public override void _Ready()
    // {
    //
    // }

    public override void _EnterTree()
    {
        if (icon == null)
        {
            icon = GetNode<TextureRect>("Icon");
        }

        // TODO: should this rather happen in _Ready and unregister happen in dispose?
        KeyPromptHelper.IconsChanged += OnIconsChanged;
        Refresh();
    }

    public override void _ExitTree()
    {
        KeyPromptHelper.IconsChanged -= OnIconsChanged;
    }

    /// <summary>
    ///   Refreshes this buttons icon. If you change ActionName you need to call this
    /// </summary>
    public void Refresh()
    {
        if (string.IsNullOrEmpty(ActionName))
        {
            icon.Texture = null;
        }
        else
        {
            icon.Texture = KeyPromptHelper.GetTextureForAction(ActionName);
        }
    }

    public override void _Process(float delta)
    {
        if (!ShowPress)
            return;

        if (string.IsNullOrEmpty(ActionName) || !Input.IsActionPressed(ActionName))
        {
            icon.SelfModulate = UnpressedColour;
        }
        else
        {
            icon.SelfModulate = PressedColour;
        }
    }

    private void OnIconsChanged(object sender, EventArgs args)
    {
        Refresh();
    }
}
