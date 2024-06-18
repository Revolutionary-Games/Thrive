using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Shows a key prompt that reacts to being pressed down
/// </summary>
/// <remarks>
///   <para>
///     This is a <see cref="CenterContainer"/> so that this can show two images layered on top of each other
///   </para>
/// </remarks>
public partial class KeyPrompt : CenterContainer
{
    /// <summary>
    ///   If true reacts when the user presses the key
    /// </summary>
    [Export]
    public bool ShowPress = true;

    /// <summary>
    ///   Colour modulation when unpressed
    /// </summary>
    [Export]
    public Color UnpressedColour = new(1, 1, 1, 1);

    /// <summary>
    ///   Colour modulation when pressed
    /// </summary>
    [Export]
    public Color PressedColour = new(0.7f, 0.7f, 0.7f, 1);

#pragma warning disable CA2213
    protected TextureRect? primaryIcon;
    protected TextureRect secondaryIcon = null!;
#pragma warning restore CA2213

    private string actionName = string.Empty;
    private StringName? resolvedActionName;

    /// <summary>
    ///   Name of the action this key prompt shows
    /// </summary>
    [Export]
    public string ActionName
    {
        get => actionName;
        set
        {
            if (value == actionName)
                return;

            actionName = value;

            if (!string.IsNullOrEmpty(value))
            {
                resolvedActionName = new StringName(value);
            }
            else
            {
                resolvedActionName?.Dispose();
                resolvedActionName = null;
            }
        }
    }

    [JsonIgnore]
    public StringName? ResolvedAction => resolvedActionName;

    public override void _Ready()
    {
        base._Ready();

        primaryIcon = GetNode<TextureRect>("Primary");
        secondaryIcon = GetNode<TextureRect>("Secondary");

        Refresh();
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        // TODO: should this rather happen in _Ready and unregister happen in dispose? (seems to perform fine enough
        // currently)
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

    public override void _Process(double delta)
    {
        // Skip processing when not visible to save quite a bit of processing time from any existing prompts
        // Note this doesn't use IsVisibleInTree as that is also a processing intensive method. Instead, this relies on
        // the tutorial etc. to set this hidden itself.
        if (!Visible)
            return;

        if (!ShowPress)
        {
            // TODO: reset the pressed status colour if it was previously pressed?
            return;
        }

        if (resolvedActionName == null || !Input.IsActionPressed(resolvedActionName))
        {
            primaryIcon!.SelfModulate = UnpressedColour;
        }
        else
        {
            primaryIcon!.SelfModulate = PressedColour;
        }
    }

    public override void _Notification(int what)
    {
        base._Notification(what);

        if (what == NotificationResized)
        {
            if (primaryIcon != null)
                ApplySize();
        }
    }

    /// <summary>
    ///   Refreshes this buttons icon. If you change ActionName you need to call this
    /// </summary>
    public void Refresh()
    {
        if (primaryIcon == null)
            return;

        ApplySize();

        if (string.IsNullOrEmpty(ActionName))
        {
            primaryIcon.Texture = null;
            secondaryIcon.Visible = false;
        }
        else
        {
            var (primaryTexture, secondaryTexture) = KeyPromptHelper.GetTextureForAction(ActionName);

            primaryIcon.Texture = primaryTexture;

            if (secondaryTexture != null)
            {
                // TODO: we need to somehow scale the primary icon down when it is the mouse wheel up or down action...

                secondaryIcon.Texture = secondaryTexture;
                secondaryIcon.Visible = true;
            }
            else
            {
                secondaryIcon.Visible = false;
            }
        }
    }

    protected virtual void ApplySize()
    {
        var size = Size;
        primaryIcon!.CustomMinimumSize = size;
        secondaryIcon.CustomMinimumSize = size;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            resolvedActionName?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnIconsChanged(object? sender, EventArgs args)
    {
        Refresh();
    }
}
