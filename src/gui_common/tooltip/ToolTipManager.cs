using System.Collections.Generic;
using Godot;

/// <summary>
///   Holds and handles a collection of custom tooltip Controls
/// </summary>
public class ToolTipManager : CanvasLayer
{
    /// <summary>
    ///   The tooltip to be shown
    /// </summary>
    public ICustomToolTip MainToolTip;

    private static ToolTipManager instance;

    /// <summary>
    ///   Collection of tooltips in a group, with Key being the group node
    ///   and Value the tooltips inside it
    /// </summary>
    private Dictionary<Control, List<ICustomToolTip>> tooltips =
        new Dictionary<Control, List<ICustomToolTip>>();

    private Control groupHolder;

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
        groupHolder = GetNode<Control>("GroupHolder");

        // Make sure the tooltip parent control is visible
        groupHolder.Show();

        FetchToolTips();
    }

    public override void _Process(float delta)
    {
        if (MainToolTip == null)
            return;

        // Wait for duration of the delay and then show the tooltip
        if (displayTimer >= 0 && !MainToolTip.ToolTipVisible)
        {
            displayTimer -= delta;

            if (displayTimer < 0)
            {
                lastMousePosition = GetViewport().GetMousePosition();
                MainToolTip.OnDisplay();
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
    ///   Add tooltip into collection. Creates a new group if group node with the given name doesn't exist
    /// </summary>
    public void AddToolTip(ICustomToolTip tooltip, string group = "general")
    {
        tooltip.ToolTipVisible = false;

        var groupNode = GetGroup(group, false);

        if (groupNode == null)
            groupNode = AddGroup(group);

        tooltips[groupNode].Add(tooltip);
        groupNode.AddChild(tooltip.ToolTipNode);
    }

    public void RemoveToolTip(string name, string group = "general")
    {
        var tooltip = GetToolTip(name, group);

        tooltip?.ToolTipNode.QueueFree();
        tooltips[GetGroup(group)]?.Remove(tooltip);
    }

    /// <summary>
    ///   Deletes all tooltip from a group
    /// </summary>
    /// <param name="group">Name of the group node</param>
    /// <param name="deleteGroup">Removes the group node if true</param>
    public void ClearToolTips(string group, bool deleteGroup = false)
    {
        var groupNode = GetGroup(group, false);

        if (groupNode == null)
            return;

        var tooltipList = tooltips[groupNode];

        if (tooltipList == null || tooltipList.Count <= 0)
            return;

        var intermediateList = new List<ICustomToolTip>(tooltipList);

        foreach (var item in intermediateList)
        {
            RemoveToolTip(item.ToolTipNode.Name, group);
        }

        if (deleteGroup)
        {
            groupNode.QueueFree();
            tooltips.Remove(groupNode);
        }
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
    public Control AddGroup(string name)
    {
        GD.Print("Creating new tooltip group: '" + name + "'");

        var groupNode = new Control();
        groupNode.Name = name;
        groupNode.MouseFilter = Control.MouseFilterEnum.Ignore;
        groupHolder.AddChild(groupNode);

        tooltips.Add(groupNode, new List<ICustomToolTip>());

        return groupNode;
    }

    private Control GetGroup(string name, bool verbose = true)
    {
        foreach (var group in tooltips.Keys)
        {
            if (group.Name == name)
                return group;
        }

        if (verbose)
            GD.PrintErr("Tooltip group with name '" + name + "' not found");

        return null;
    }

    /// <summary>
    ///   Get all the existing groups and tooltips into the dictionary
    /// </summary>
    private void FetchToolTips()
    {
        foreach (Control group in groupHolder.GetChildren())
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
        // TODO: Fix the current tooltip changing while still fading out
        // when quickly mousing over multiple closely positioned elements
        // (Happens when a single tooltip is registered to multiple Controls)

        // Make sure to hide any other tooltips that are still visible
        HideAllToolTips();

        if (Display)
        {
            // Set timer
            displayTimer = MainToolTip.DisplayDelay;
        }
    }

    private void HideAllToolTips()
    {
        foreach (var group in tooltips.Keys)
            tooltips[group].ForEach(tooltip => tooltip.OnHide());
    }
}
