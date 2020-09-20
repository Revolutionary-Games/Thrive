using System.Collections.Generic;
using Godot;

/// <summary>
///   Holds and handles a collection of custom tooltip Controls in a single scene.
///   The tooltips show/hide callbacks are to be handled elsewhere
/// </summary>
public class TooltipHandler : CanvasLayer
{
    /// <summary>
    ///   The tooltip to be shown
    /// </summary>
    public ICustomTooltip MainTooltip;

    private List<ICustomTooltip> tooltips = new List<ICustomTooltip>();

    private Control holder;
    private Tween tween;

    private bool display;
    private float displayTimer;

    private Vector2 lastMousePosition;

    /// <summary>
    ///   Displays the current tooltip if set true. It's preferable to set this
    ///   rather than directly from the tooltip
    /// </summary>
    public bool Display
    {
        get => display;
        set
        {
            display = value;
            UpdateTooltipVisibility();
        }
    }

    public override void _Ready()
    {
        holder = GetNode<Control>("Holder");
        tween = GetNode<Tween>("Tween");

        UpdateLists();
    }

    public override void _Process(float delta)
    {
        if (MainTooltip == null || !Display)
            return;

        // Wait for duration of the delay and then show the tooltip
        if (displayTimer >= 0 && !MainTooltip.TooltipVisible)
        {
            displayTimer -= delta;

            if (displayTimer < 0)
            {
                lastMousePosition = GetViewport().GetMousePosition();
                OnDisplay();
            }
        }

        // Adjust position and size
        if (MainTooltip.TooltipVisible)
        {
            var screenSize = GetViewport().GetVisibleRect().Size;

            // Clamp tooltip position so it doesn't go offscreen
            var adjustedPosition = new Vector2(
                Mathf.Clamp(lastMousePosition.x + 20, 0, screenSize.x -
                MainTooltip.Size.x),
                Mathf.Clamp(lastMousePosition.y + 20, 0, screenSize.y -
                MainTooltip.Size.y));

            MainTooltip.Position = adjustedPosition;
            MainTooltip.Size = Vector2.Zero;
        }
    }

    /// <summary>
    ///   Helper for displaying the default styled tooltip
    /// </summary>
    public void ShowDefaultTooltip(string description, float delay = Constants.TOOLTIP_DEFAULT_DELAY)
    {
        MainTooltip = GetTooltip("Default");
        MainTooltip.TooltipDescription = description;
        MainTooltip.DisplayDelay = delay;

        Display = true;
    }

    public void AddTooltip(ICustomTooltip tooltip)
    {
        tooltips.Add(tooltip);
        holder.AddChild(tooltip.TooltipNode);
    }

    public void RemoveTooltip(string name)
    {
        var found = GetTooltip(name);
        found.TooltipNode.QueueFree();
        tooltips.Remove(found);
    }

    public ICustomTooltip GetTooltip(string name)
    {
        var tooltip = tooltips.Find(found => found.TooltipName == name);

        if (tooltip == null)
        {
            GD.PrintErr("Couldn't find tooltip: " + name);
            return null;
        }

        return tooltip;
    }

    private void UpdateLists()
    {
        // Get all the existing children into the tooltips list
        foreach (ICustomTooltip child in holder.GetChildren())
        {
            child.TooltipName = child.TooltipNode.Name;
            child.TooltipVisible = false;
            tooltips.Add(child);
        }
    }

    private void UpdateTooltipVisibility()
    {
        if (MainTooltip == null)
            return;

        // TODO: Fix the current tooltip changing while still fading out
        // when quickly mousing over multiple closely positioned elements

        if (Display)
        {
            displayTimer = MainTooltip.DisplayDelay;
        }
        else
        {
            if (!MainTooltip.TooltipVisible)
                return;

            OnHide();
        }
    }

    private void OnDisplay()
    {
        holder.Show();

        tween.InterpolateProperty(holder, "modulate", new Color(1, 1, 1, 0), new Color(1, 1, 1, 1),
            Constants.TOOLTIP_FADE_SPEED);
        tween.Start();

        tween.Connect("tween_started", this, nameof(OnFadeInStarted), null, (int)ConnectFlags.Oneshot);
    }

    private void OnHide()
    {
        tween.InterpolateProperty(holder, "modulate", new Color(1, 1, 1, 1), new Color(1, 1, 1, 0),
            Constants.TOOLTIP_FADE_SPEED);
        tween.Start();

        if (!tween.IsConnected("tween_completed", this, nameof(OnFadeOutFinished)))
            tween.Connect("tween_completed", this, nameof(OnFadeOutFinished), null, (int)ConnectFlags.Oneshot);
    }

    private void HideAllTooltips()
    {
        tooltips.ForEach(tooltip => tooltip.TooltipVisible = false);
    }

    private void OnFadeInStarted(Object obj, NodePath key)
    {
        _ = obj;
        _ = key;

        MainTooltip.TooltipVisible = true;
    }

    private void OnFadeOutFinished(Object obj, NodePath key)
    {
        _ = obj;
        _ = key;

        holder.Hide();

        HideAllTooltips();
    }
}
