using System;
using Godot;

/// <summary>
///   Shows a key prompt that reacts to being pressed down
/// </summary>
public class KeyPrompt : TextureRect
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

    // public override void _Ready()
    // {
    //
    // }

    public override void _EnterTree()
    {
        base._EnterTree();

        // TODO: should this rather happen in _Ready and unregister happen in dispose?
        KeyPromptHelper.IconsChanged += OnIconsChanged;
        InputDataList.InputsRemapped += OnIconsChanged;
        Refresh();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        KeyPromptHelper.IconsChanged -= OnIconsChanged;
        InputDataList.InputsRemapped -= OnIconsChanged;
    }

    /// <summary>
    ///   Refreshes this buttons icon. If you change ActionName you need to call this
    /// </summary>
    public void Refresh()
    {
        if (string.IsNullOrEmpty(ActionName))
        {
            Texture = null;
        }
        else
        {
            Texture = KeyPromptHelper.GetTextureForAction(ActionName);
        }
    }

    public override void _Process(float delta)
    {
        if (!ShowPress)
            return;

        if (string.IsNullOrEmpty(ActionName) || !Input.IsActionPressed(ActionName))
        {
            SelfModulate = UnpressedColour;
        }
        else
        {
            SelfModulate = PressedColour;
        }
    }

    private void OnIconsChanged(object sender, EventArgs args)
    {
        Refresh();
    }
}
