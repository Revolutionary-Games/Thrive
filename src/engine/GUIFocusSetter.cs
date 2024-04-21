using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Manages setting the right GUI control to grab focus based on <see cref="FocusGrabber"/> Nodes that are visible
/// </summary>
[GodotAutoload]
public partial class GUIFocusSetter : Control
{
    private static GUIFocusSetter? instance;

    private readonly List<FocusGrabber> activeGrabbers = new();
    private bool dirty = true;
    private double elapsed;

    private GUIFocusSetter()
    {
        if (Engine.IsEditorHint())
            return;

        instance = this;
        MouseFilter = MouseFilterEnum.Ignore;
        ProcessMode = ProcessModeEnum.Always;
    }

    public static GUIFocusSetter Instance => instance ?? throw new InstanceNotLoadedYetException();

    public override void _Process(double delta)
    {
        elapsed += delta;

        // If we don't need an immediate update (due to changed state) and time hasn't elapsed so much that we want to
        // check the GUI state anyway, we skip processing
        if (!dirty && elapsed < Constants.GUI_FOCUS_SETTER_PROCESS_INTERVAL)
            return;

        dirty = false;
        elapsed = 0;
        CheckNodeToGiveFocusTo();
    }

    public void ReportGrabberState(FocusGrabber grabber, bool wantsFocus)
    {
        if (wantsFocus)
        {
            if (!activeGrabbers.Contains(grabber))
            {
                activeGrabbers.Add(grabber);
                dirty = true;
            }
        }
        else
        {
            ReportRemovedGrabber(grabber);
        }
    }

    public void ReportRemovedGrabber(FocusGrabber grabber)
    {
        if (activeGrabbers.Remove(grabber))
        {
            dirty = true;
        }
    }

    private void CheckNodeToGiveFocusTo()
    {
        // Highest priority grabber has focus
        float highestPriorityGrabber = float.MinValue;
        FocusGrabber? grabber = null;

        foreach (var activeGrabber in activeGrabbers)
        {
            if (activeGrabber.Priority > highestPriorityGrabber)
            {
                highestPriorityGrabber = activeGrabber.Priority;
                grabber = activeGrabber;
            }
        }

        if (grabber == null)
        {
            // No active grabbers
            return;
        }

        var currentlyFocused = GetViewport().GuiGetFocusOwner();

        if (currentlyFocused != null && currentlyFocused.IsVisibleInTree())
        {
            // We may not want to override the current focus
            if (!grabber.CheckWantsToStealFocus())
                return;

            var currentPath = currentlyFocused.GetPath().ToString();

            // The rules for where not to steal focus only applies when the full override option is not used
            if (!grabber.CheckAlwaysOverrideFocusAndReset())
            {
                if (grabber.SkipOverridingFocusForElementStrings.Any(p => currentPath.StartsWith(p)))
                    return;
            }

            // We want to override focus anyway
        }

        var targetNode = grabber.GetNode<Control>(grabber.NodeToGiveFocusTo);

        if (targetNode != null)
        {
            if (targetNode != currentlyFocused)
            {
                // Only try to grab focus when the target is actually visible
                if (targetNode.IsVisibleInTree())
                {
                    targetNode.GrabFocus();
                }
            }
        }
        else
        {
            GD.PrintErr("Node to set focus to based on focus grabber not found: ", grabber.NodeToGiveFocusTo);
        }
    }
}
