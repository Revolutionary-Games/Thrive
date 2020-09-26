using System.Collections.Generic;
using Godot;
using Object = Godot.Object;

/// <summary>
///   Holds and handles a collection of custom tooltip Controls
/// </summary>
public class ToolTipManager : CanvasLayer
{
    private static ToolTipManager instance;

    /// <summary>
    ///   The tooltip to be shown
    /// </summary>
    public ICustomToolTip MainToolTip;

    /// <summary>
    ///   Collection of tooltips in a group
    /// </summary>
    private Dictionary<Control, List<ICustomToolTip>> tooltips =
        new Dictionary<Control, List<ICustomToolTip>>();

    private Control holder;
    private Tween tween;

    private bool display;
    private float displayTimer;

    private Vector2 lastMousePosition;

    private ToolTipManager()
    {
        instance = this;
    }

    public static ToolTipManager Instance => instance;

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
            UpdateToolTipVisibility();
        }
    }

    public override void _Ready()
    {
        holder = GetNode<Control>("Holder");
        tween = GetNode<Tween>("Tween");

        FetchToolTips();
    }

    public override void _Process(float delta)
    {
        if (MainToolTip == null || !Display)
            return;

        // Wait for duration of the delay and then show the tooltip
        if (displayTimer >= 0 && !MainToolTip.ToolTipVisible)
        {
            displayTimer -= delta;

            if (displayTimer < 0)
            {
                lastMousePosition = GetViewport().GetMousePosition();
                DisplayToolTip();
            }
        }

        // Adjust position and size
        if (MainToolTip.ToolTipVisible)
        {
            var screenSize = GetViewport().GetVisibleRect().Size;

            // Clamp tooltip position so it doesn't go offscreen
            var adjustedPosition = new Vector2(
                Mathf.Clamp(lastMousePosition.x + Constants.TOOLTIP_OFFSET, 0, screenSize.x -
                    MainToolTip.Size.x),
                Mathf.Clamp(lastMousePosition.y + Constants.TOOLTIP_OFFSET, 0, screenSize.y -
                    MainToolTip.Size.y));

            MainToolTip.Position = adjustedPosition;
            MainToolTip.Size = Vector2.Zero;
        }
    }

    /// <summary>
    ///   Helper method for registering a Control mouse enter/exit event to display a tooltip
    /// </summary>
    /// <param name="control">The Control to register the tooltip to</param>
    /// <param name="callbackDatas">List to store the callbacks to keep them from unloading</param>
    /// <param name="tooltip">The tooltip to register with</param>
    public static void RegisterToolTipForControl(Control control, List<ToolTipCallbackData> callbackDatas,
        ICustomToolTip tooltip)
    {
        // Skip if already registered
        if (callbackDatas.Find(match => match.ToolTip == tooltip) != null)
            return;

        var toolTipCallbackData = new ToolTipCallbackData(tooltip);

        control.Connect("mouse_entered", toolTipCallbackData, nameof(ToolTipCallbackData.OnMouseEnter));
        control.Connect("mouse_exited", toolTipCallbackData, nameof(ToolTipCallbackData.OnMouseExit));
        control.Connect("hide", toolTipCallbackData, nameof(ToolTipCallbackData.OnMouseExit));

        callbackDatas.Add(toolTipCallbackData);
    }

    public void AddToolTip(ICustomToolTip tooltip, string group = "general")
    {
        tooltip.ToolTipVisible = false;

        var groupNode = GetGroup(group);

        tooltips[groupNode].Add(tooltip);
        groupNode.AddChild(tooltip.ToolTipNode);
    }

    public void RemoveToolTip(string name, string group = "general")
    {
        var tooltip = GetToolTip(name, group);

        tooltip.ToolTipNode.QueueFree();
        tooltips[GetGroup(group)].Remove(tooltip);
    }

    /// <summary>
    ///   Returns tooltip with the given name and group (default is "general")
    /// </summary>
    /// <param name="name">The name of the tooltip's node (not display name)</param>
    /// <param name="group">The name of the group the tooltip belongs to</param>
    public ICustomToolTip GetToolTip(string name, string group = "general")
    {
        var tooltip = tooltips[GetGroup(group)].Find(found => found.ToolTipNode.Name == name);

        if (tooltip == null)
        {
            GD.PrintErr("Couldn't find tooltip with node name: " + name + " in group " + group);
            return null;
        }

        return tooltip;
    }

    /// <summary>
    ///   Creates a new group node to contain tooltips
    /// </summary>
    public void AddGroup(string name)
    {
        var groupNode = new Control();
        groupNode.MouseFilter = Control.MouseFilterEnum.Ignore;
        holder.AddChild(groupNode);

        tooltips.Add(groupNode, new List<ICustomToolTip>());
    }

    private Control GetGroup(string name)
    {
        foreach (var group in tooltips.Keys)
        {
            if (group.Name == name)
                return group;
        }

        GD.PrintErr("Tooltip group with name '" + name + "' not found");
        return null;
    }

    /// <summary>
    ///   Get all the existing groups and tooltips into the dictionary
    /// </summary>
    private void FetchToolTips()
    {
        foreach (Control group in holder.GetChildren())
        {
            var collectedTooltips = new List<ICustomToolTip>();

            foreach (ICustomToolTip tooltip in group.GetChildren())
            {
                tooltip.ToolTipVisible = false;
                collectedTooltips.Add(tooltip);
            }

            tooltips.Add(group, collectedTooltips);
        }
    }

    private void UpdateToolTipVisibility()
    {
        if (MainToolTip == null)
            return;

        // TODO: Fix the current tooltip changing while still fading out
        // when quickly mousing over multiple closely positioned elements
        // (Happens when a single tooltip is registered to multiple Controls)

        if (Display)
        {
            displayTimer = MainToolTip.DisplayDelay;
        }
        else
        {
            if (!MainToolTip.ToolTipVisible)
                return;

            HideToolTip();
        }
    }

    private void DisplayToolTip()
    {
        holder.Show();

        tween.InterpolateProperty(holder, "modulate", new Color(1, 1, 1, 0), new Color(1, 1, 1, 1),
            Constants.TOOLTIP_FADE_SPEED, Tween.TransitionType.Sine, Tween.EaseType.In);
        tween.Start();

        tween.Connect("tween_started", this, nameof(OnFadeInStarted), null, (int)ConnectFlags.Oneshot);
    }

    private void HideToolTip()
    {
        tween.InterpolateProperty(holder, "modulate", new Color(1, 1, 1, 1), new Color(1, 1, 1, 0),
            Constants.TOOLTIP_FADE_SPEED, Tween.TransitionType.Sine, Tween.EaseType.Out);
        tween.Start();

        if (!tween.IsConnected("tween_completed", this, nameof(OnFadeOutFinished)))
            tween.Connect("tween_completed", this, nameof(OnFadeOutFinished), null, (int)ConnectFlags.Oneshot);
    }

    private void HideAllToolTips()
    {
        foreach (var group in tooltips.Keys)
            tooltips[group].ForEach(tooltip => tooltip.ToolTipVisible = false);
    }

    private void OnFadeInStarted(Object obj, NodePath key)
    {
        _ = obj;
        _ = key;

        MainToolTip.ToolTipVisible = true;
    }

    private void OnFadeOutFinished(Object obj, NodePath key)
    {
        _ = obj;
        _ = key;

        holder.Hide();

        HideAllToolTips();
    }
}
