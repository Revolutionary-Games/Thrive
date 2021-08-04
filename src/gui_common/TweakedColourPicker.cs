using System;
using Godot;

/// <summary>
///   Tweaked color picker defines some custom ColorPicker functions.
/// </summary>
public class TweakedColourPicker : ColorPicker
{
    private CheckButton hsvCheckButton;
    private CheckButton rawCheckButton;

    public bool HSVCheckButtonDisabled
    {
        get
        {
            // In case that HSV Check is removed (moved) by Godot.
            if (hsvCheckButton == null)
                return true;

            return hsvCheckButton.Disabled;
        }
        set
        {
            if (hsvCheckButton == null)
                return;

            hsvCheckButton.Disabled = value;
        }
    }

    public bool RawCheckButtonDisabled
    {
        get
        {
            // In case that Raw Check is removed / moved by Godot.
            if (rawCheckButton == null)
                return true;

            return rawCheckButton.Disabled;
        }
        set
        {
            if (rawCheckButton == null)
                return;

            rawCheckButton.Disabled = value;
        }
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        try
        {
            var baseNode = GetChild(4).GetChild(4);
            hsvCheckButton = baseNode.GetChild<CheckButton>(0);
            rawCheckButton = baseNode.GetChild<CheckButton>(1);
        }
        catch (Exception e)
        {
            GD.PrintErr(e.Message, "Godot may have moved this elsewhere.");
        }
    }
}
