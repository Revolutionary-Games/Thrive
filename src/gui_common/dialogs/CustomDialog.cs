using System;
using System.Collections;
using System.Collections.Generic;
using Godot;

public class CustomDialog : WindowDialog
{
    protected static int exclusiveDialogOpenCount;

    protected bool isExclusive = true;

    protected bool isEscapeCloseable = true;

    protected readonly bool showButton = false;

    public static bool HasExclusiveDialogOpen { get => exclusiveDialogOpenCount > 0; }

    public override void _Ready()
    {
        base._Ready();

        if (!showButton)
        {
            var button = GetCloseButton();
            button.Hide();
        }
    }

    public override void _Process(float delta)
    {
        var children = GetChildren();
        base._Process(delta);
    }

    public void OnAboutToShow()
    {
        if (isExclusive)
            exclusiveDialogOpenCount++;
    }

    public void OnHide()
    {
        if (exclusiveDialogOpenCount <= 0)
            throw new InvalidOperationException("Should not be any other open dialogs.");

        exclusiveDialogOpenCount--;
    }

    [RunOnKeyDown("ui_cancel")]
    public bool OnEscapePressed()
    {
        if (!Visible || !isEscapeCloseable)
        {
            return false;
        }

        Hide();
        return true;
    }
}
