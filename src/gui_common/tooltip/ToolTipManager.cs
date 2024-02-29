using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Holds and handles a collection of custom tooltip Controls.
/// </summary>
public class ToolTipManager : CanvasLayer
{
    /// <summary>
    ///   This must be in sync with the name of the default group node in the ToolTipManager scene.
    /// </summary>
    public const string DEFAULT_GROUP_NAME = "default";

    private static ToolTipManager? instance;

    /// <summary>
    ///   Collection of tooltips in a group, with Key being the group node
    ///   and Value the tooltips inside it
    /// </summary>
    private readonly Dictionary<Control, List<ICustomToolTip>> tooltips = new();

    private readonly Dictionary<string, Control> groupsByName = new();

#pragma warning disable CA2213
    private Control groupHolder = null!;
#pragma warning restore CA2213

    private bool display;
    private float displayTimer;
    private float hideTimer;

    /// <summary>
    ///   Flags whether MainToolTip should be shown temporarily (automatically hides once timer reaches threshold).
    /// </summary>
    private bool currentIsTemporary;

    private Vector2 lastMousePosition;

    private ICustomToolTip? mainToolTip;
    private ICustomToolTip? previousToolTip;

    private bool nodeReferencesResolved;

    private ToolTipManager()
    {
        instance = this;
    }

    public static ToolTipManager Instance => instance ?? throw new InstanceNotLoadedYetException();

    /// <summary>
    ///   The tooltip to be shown
    /// </summary>
    public ICustomToolTip? MainToolTip
    {
        get => mainToolTip;
        set
        {
            previousToolTip = mainToolTip;
            mainToolTip = value;
        }
    }

    /// <summary>
    ///   If true displays the current tooltip with a set delay of <see cref="ICustomToolTip.DisplayDelay"/>.
    ///   It's preferable to set this rather than directly from the tooltip
    /// </summary>
    public bool Display
    {
        get => display;
        set
        {
            display = value;

            if (previousToolTip != null)
                UpdateToolTipVisibility(previousToolTip, false);

            if (display)
            {
                if (MainToolTip == null)
                    throw new InvalidOperationException("Can't set display to true without main tooltip");

                // Set timer
                displayTimer = MainToolTip.DisplayDelay;
            }
        }
    }

    public override void _Ready()
    {
        if (tooltips.Count < 1)
            GD.PrintErr("No tooltips have been detected");

        ResolveNodeReferences();

        // Make sure the tooltip parent control is visible
        groupHolder.Show();
    }

    public void ResolveNodeReferences()
    {
        if (nodeReferencesResolved)
            return;

        groupHolder = GetNode<Control>("GroupHolder");

        nodeReferencesResolved = true;
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        // This node may not be added back to the scene after removing so we don't guard against double initialization
        // here

        ResolveNodeReferences();

        // The tooltip initialization logic needs to run in _EnterTree as the initial scene may want to already
        // register tooltips before _Ready methods are called
        FetchToolTips();
    }

    public override void _Process(float delta)
    {
        if (MainToolTip == null)
            return;

        // Wait for duration of the delay and then show the tooltip
        if (displayTimer >= 0 && !MainToolTip.ToolTipNode.Visible)
        {
            displayTimer -= delta;

            // To avoid tooltip "jumping" to the correct position once it's visible
            UpdateCurrentTooltip(0);

            if (displayTimer < 0)
            {
                lastMousePosition = GetViewport().GetMousePosition();
                UpdateToolTipVisibility(MainToolTip, true);
            }
        }

        if (MainToolTip.ToolTipNode.Visible)
            UpdateCurrentTooltip(delta);
    }

    public override void _Input(InputEvent @event)
    {
        if (MainToolTip?.HideOnMouseAction == true)
        {
            // For mouse press, only trigger when it's button down, we don't want the tooltip to be hidden
            // the second the mouse is released
            // NOTE: Notice mouse motion needs extra check for zero vector relative value,
            // see: https://github.com/godotengine/godot/issues/20357
            if ((@event is InputEventMouseButton button && button.Pressed) ||
                (@event is InputEventMouseMotion motion && motion.Relative != Vector2.Zero))
            {
                // This is to avoid flickering when smashing mouse press multiple times
                // on the transient tooltip
                if (currentIsTemporary && !MainToolTip.ToolTipNode.Visible)
                    return;

                UpdateToolTipVisibility(MainToolTip, false);
                displayTimer = MainToolTip.DisplayDelay;

                if (currentIsTemporary)
                {
                    currentIsTemporary = false;
                    MainToolTip = null;
                }
            }
        }
    }

    /// <summary>
    ///   Shows a tooltip (popup) with specified message for a given duration.
    /// </summary>
    public void ShowPopup(string message, float duration)
    {
        var popup = GetToolTip<DefaultToolTip>("popup");

        if (popup == null)
        {
            popup = ToolTipHelper.GetDefaultToolTip();
            popup.Name = "popup";
            AddToolTip(popup);
        }

        popup.Description = message;
        popup.HideOnMouseAction = true;
        popup.TransitionType = ToolTipTransitioning.Fade;
        popup.DisplayDelay = 0;

        MainToolTip = popup;
        Display = true;

        currentIsTemporary = true;
        hideTimer = duration;
    }

    /// <summary>
    ///   Adds tooltip into collection. Creates a new group if group node with the given name doesn't exist
    /// </summary>
    public void AddToolTip(ICustomToolTip tooltip, string group = DEFAULT_GROUP_NAME)
    {
        tooltip.ToolTipNode.Visible = false;

        var groupNode = GetGroup(group, false) ?? AddGroup(group);

        tooltips[groupNode].Add(tooltip);
        groupNode.AddChild(tooltip.ToolTipNode);
    }

    /// <summary>
    ///   Removes a tooltip based on the name, if it is a default type tooltip it is returned to the cache
    /// </summary>
    /// <param name="name">Name of the tooltip</param>
    /// <param name="group">Group to look for in for the tooltip</param>
    public void RemoveToolTip(string name, string group = DEFAULT_GROUP_NAME)
    {
        var tooltip = GetToolTip(name, group);

        if (tooltip == null)
        {
            GD.PrintErr("Can't remove non-existent tooltip ", name, " in group ", group);
            return;
        }

        tooltip.ToolTipNode.Detach();

        if (tooltip is DefaultToolTip defaultToolTip)
        {
            ToolTipHelper.ReturnDefaultToolTip(defaultToolTip);
        }
        else
        {
            tooltip.ToolTipNode.QueueFree();
        }

        var retrievedGroup = GetGroup(group);
        if (retrievedGroup == null)
        {
            GD.PrintErr("Failed to retrieve group (", group, ") of an existing tooltip");
            return;
        }

        tooltips[retrievedGroup]?.Remove(tooltip);
    }

    /// <summary>
    ///   Deletes all tooltip from a group. Note that this automatically returns default tooltips to the cache.
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
            groupNode.DetachAndQueueFree();
            tooltips.Remove(groupNode);
        }
    }

    /// <summary>
    ///   Returns tooltip with the given name and group.
    /// </summary>
    /// <param name="name">The name of the tooltip's node (not display name)</param>
    /// <param name="group">The name of the group the tooltip belongs to</param>
    public ICustomToolTip? GetToolTip(string name, string group = DEFAULT_GROUP_NAME)
    {
        var retrievedGroup = GetGroup(group);
        if (retrievedGroup == null)
        {
            GD.PrintErr("Failed to find tooltip group: ", group);
            return null;
        }

        var tooltip = tooltips[retrievedGroup].Find(t => t.ToolTipNode.Name == name);

        if (tooltip == null)
        {
            GD.PrintErr("Couldn't find tooltip with node name: " + name + " in group " + group);
            return null;
        }

        return tooltip;
    }

    /// <summary>
    ///   Tries to find a tooltip but doesn't consider it an error if one is not found. Useful when caching tooltips
    ///   and checking if one already exists is needed.
    /// </summary>
    /// <param name="name">The name of the tooltip's node (not display name)</param>
    /// <param name="group">The name of the group the tooltip belongs to</param>
    public ICustomToolTip? GetToolTipIfExists(string name, string group = DEFAULT_GROUP_NAME)
    {
        var retrievedGroup = GetGroup(group, false);
        if (retrievedGroup == null)
            return null;

        var tooltip = tooltips[retrievedGroup].Find(t => t.ToolTipNode.Name == name);

        return tooltip;
    }

    /// <summary>
    ///   Generic version of <see cref="GetToolTip"/> method.
    /// </summary>
    public T? GetToolTip<T>(string name, string group = DEFAULT_GROUP_NAME)
        where T : ICustomToolTip
    {
        return (T?)GetToolTip(name, group);
    }

    /// <summary>
    ///   Generic version of <see cref="GetToolTipIfExists"/> method.
    /// </summary>
    public T? GetToolTipIfExists<T>(string name, string group = DEFAULT_GROUP_NAME)
        where T : ICustomToolTip
    {
        return (T?)GetToolTipIfExists(name, group);
    }

    /// <summary>
    ///   Creates a new group node to contain tooltips
    /// </summary>
    public Control AddGroup(string name)
    {
        var groupNode = new Control();
        groupNode.Name = name;
        groupNode.MouseFilter = Control.MouseFilterEnum.Ignore;
        groupHolder.AddChild(groupNode);
        groupsByName.Add(name, groupNode);

        tooltips.Add(groupNode, new List<ICustomToolTip>());

        return groupNode;
    }

    /// <summary>
    ///   Adjusts <see cref="MainToolTip"/>'s position and size.
    /// </summary>
    private void UpdateCurrentTooltip(float delta)
    {
        if (MainToolTip == null)
            return;

        Vector2 position;
        var offset = new Vector2(Constants.TOOLTIP_OFFSET, Constants.TOOLTIP_OFFSET);

        switch (MainToolTip.Positioning)
        {
            case ToolTipPositioning.LastMousePosition:
                position = lastMousePosition;
                break;
            case ToolTipPositioning.FollowMousePosition:
                position = GetViewport().GetMousePosition();
                break;
            case ToolTipPositioning.ControlBottomRightCorner:
            {
                var control = ToolTipHelper.GetControlAssociatedWithToolTip(MainToolTip);
                if (control != null)
                {
                    position = new Vector2(control.RectGlobalPosition.x + control.RectSize.x,
                        control.RectGlobalPosition.y);
                    offset = new Vector2(0, control.RectSize.y);
                }
                else
                {
                    position = new Vector2(0, 0);
                    GD.PrintErr("Failed to find control associated with main tooltip");
                }

                break;
            }

            default:
                throw new Exception("Invalid tooltip positioning type");
        }

        var screenRect = GetViewport().GetVisibleRect();
        var newPos = new Vector2(position.x + offset.x, position.y + offset.y);
        var tooltipSize = MainToolTip.ToolTipNode.RectSize;

        if (newPos.x + tooltipSize.x > screenRect.Size.x)
        {
            newPos.x -= tooltipSize.x + offset.x;

            if (newPos.x < screenRect.Position.x)
                newPos.x = position.x + offset.x;
        }

        if (newPos.y + tooltipSize.y > screenRect.Size.y)
        {
            newPos.y -= tooltipSize.y + offset.y;

            if (newPos.y < screenRect.Position.y)
                newPos.y = position.y + offset.y;
        }

        // Clamp tooltip position so it doesn't go offscreen
        // TODO: Take into account viewport (window) resizing for the offsetting.
        MainToolTip.ToolTipNode.RectPosition = new Vector2(Mathf.Clamp(newPos.x, 0, screenRect.Size.x - tooltipSize.x),
            Mathf.Clamp(newPos.y, 0, screenRect.Size.y - tooltipSize.y));

        MainToolTip.ToolTipNode.RectSize = Vector2.Zero;

        // Handle temporary tooltips/popup
        if (currentIsTemporary && hideTimer >= 0)
        {
            hideTimer -= delta;

            if (hideTimer < 0)
            {
                currentIsTemporary = false;
                UpdateToolTipVisibility(MainToolTip, false);
                MainToolTip = null;
            }
        }
    }

    private Control? GetGroup(string name, bool verbose = true)
    {
        if (!groupsByName.TryGetValue(name, out var group) && verbose)
            GD.PrintErr("Tooltip group with name '" + name + "' not found");

        return group;
    }

    /// <summary>
    ///   Gets all the existing groups and tooltips into the dictionary
    /// </summary>
    private void FetchToolTips()
    {
        foreach (Control group in groupHolder.GetChildren())
        {
            var collectedTooltips = new List<ICustomToolTip>();

            foreach (ICustomToolTip tooltip in group.GetChildren())
            {
                tooltip.ToolTipNode.Visible = false;
                collectedTooltips.Add(tooltip);
            }

            groupsByName.Add(group.Name, group);
            tooltips.Add(group, collectedTooltips);
        }
    }

    private void UpdateToolTipVisibility(ICustomToolTip tooltip, bool visible)
    {
        if (tooltip.ToolTipNode.Visible == visible)
            return;

        switch (tooltip.TransitionType)
        {
            case ToolTipTransitioning.Immediate:
            {
                tooltip.ToolTipNode.Visible = visible;
                break;
            }

            case ToolTipTransitioning.Fade:
            {
                // TODO: Fix fading when tooltip display delay is less than the tooltip fade speed, some kind of
                // flickering happens
                if (visible)
                {
                    GUICommon.Instance.ModulateFadeIn(tooltip.ToolTipNode, Constants.TOOLTIP_FADE_SPEED);
                }
                else
                {
                    GUICommon.Instance.ModulateFadeOut(tooltip.ToolTipNode, Constants.TOOLTIP_FADE_SPEED);
                }

                break;
            }

            default:
                throw new Exception("Invalid tooltip visibility transition type");
        }
    }

    private void HideAllToolTips()
    {
        foreach (var group in tooltips.Keys)
        {
            foreach (var tooltip in tooltips[group])
            {
                UpdateToolTipVisibility(tooltip, false);
            }
        }
    }
}
