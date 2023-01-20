using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Manages setting the right GUI control to grab focus based on <see cref="FocusGrabber"/> Nodes that are visible
/// </summary>
public class GUIFocusSetter : Control
{
    private static GUIFocusSetter? instance;

    private readonly List<FocusGrabber> activeGrabbers = new();
    private bool dirty = true;
    private float elapsed;

    private GUIFocusSetter()
    {
        instance = this;
        MouseFilter = MouseFilterEnum.Ignore;
        PauseMode = PauseModeEnum.Process;
    }

    public static GUIFocusSetter Instance => instance ?? throw new InstanceNotLoadedYetException();

    public override void _Process(float delta)
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
        var grabber = activeGrabbers.OrderByDescending(g => g.Priority).FirstOrDefault();

        if (grabber == null)
        {
            // No active grabbers
            return;
        }

        var currentlyFocused = GetFocusOwner();

        if (currentlyFocused != null && currentlyFocused.IsVisibleInTree())
        {
            // We may not want to override the current focus
            if (!grabber.CheckWantsToStealFocusAndReset())
                return;

            var currentPath = currentlyFocused.GetPath().ToString();

            if (grabber.SkipOverridingFocusForElementStrings.Any(p => currentPath.StartsWith(p)))
                return;

            // We want to override focus anyway
        }

        var targetNode = grabber.GetNode<Control>(grabber.NodeToGiveFocusTo);

        if (targetNode != null)
        {
            targetNode.GrabFocus();
        }
        else
        {
            GD.PrintErr("Node to set focus to based on focus grabber not found: ", grabber.NodeToGiveFocusTo);
        }
    }
}
