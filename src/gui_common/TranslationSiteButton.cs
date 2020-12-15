using Godot;
using System;

/// <summary>
///   Shows a version label
/// </summary>
public class TranslationSiteButton : Button
{
    public override void _Process(float delta)
    {
        if (this.Pressed)
            OS.ShellOpen("https://translate.revolutionarygamesstudio.com/");
    }
}
