using Godot;
using System;

public abstract class ThriveopediaPage : PanelContainer
{
    public abstract string PageName { get; }

    public Action<string> OpenPage = null!;
}