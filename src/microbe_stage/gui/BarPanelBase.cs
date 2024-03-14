using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Base class for <see cref="EnvironmentPanel"/> and related panels. These are toggleable panels that have bars
///   in them representing values with icons. Also has a feature to toggle compact and full views.
/// </summary>
[GodotAbstract]
public partial class BarPanelBase : VBoxContainer
{
    protected readonly List<CompoundProgressBar> primaryBars = new();

#pragma warning disable CA2213

    [Export]
    protected BaseButton? expandButton;

    [Export]
    protected BaseButton compressButton = null!;

    [Export]
    protected GridContainer primaryBarContainer = null!;

    [Export]
    protected AnimationPlayer? panelHideAnimationPlayer;

#pragma warning restore CA2213

    private bool panelCompressed;

    private bool showPanels = true;

    [Export]
    public bool PanelCompressed
    {
        get => panelCompressed;
        set
        {
            if (panelCompressed == value)
                return;

            panelCompressed = value;
            UpdatePanelState();
        }
    }

    [Export]
    public bool ShowPanel
    {
        get => showPanels;
        set
        {
            if (showPanels == value)
                return;

            showPanels = value;
            UpdatePanelShowAnimation();
        }
    }

    public override void _Ready()
    {
        // To allow setting panel state before
        UpdatePanelState();

        if (!showPanels)
        {
            HideImmediately();
        }
    }

    public virtual void AddPrimaryBar(CompoundProgressBar bar)
    {
        if (expandButton == null)
            throw new InvalidOperationException("Needs to be in tree first");

        primaryBarContainer.AddChild(bar);
        primaryBars.Add(bar);
    }

    protected void OnCompressPressed()
    {
        if (PanelCompressed)
            return;

        GUICommon.Instance.PlayButtonPressSound();
        PanelCompressed = true;
    }

    protected void OnExpandPressed()
    {
        if (!PanelCompressed)
            return;

        GUICommon.Instance.PlayButtonPressSound();
        PanelCompressed = false;
    }

    protected virtual void UpdatePanelState()
    {
        if (expandButton == null)
            return;

        compressButton.ButtonPressed = PanelCompressed;
        expandButton.ButtonPressed = !PanelCompressed;
    }

    protected virtual void HideImmediately()
    {
        Visible = false;
    }

    protected virtual void UpdatePanelShowAnimation()
    {
        if (panelHideAnimationPlayer == null)
            return;

        // TODO: store these string names to avoid allocations
        if (!showPanels)
        {
            panelHideAnimationPlayer.Play("Hide");
        }
        else
        {
            panelHideAnimationPlayer.Play("Show");
        }
    }
}
