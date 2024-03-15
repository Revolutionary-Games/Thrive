using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Grabs keyboard navigation focus with specific rules to a node that makes sense in the GUI
/// </summary>
public partial class FocusGrabber : Control
{
    [Export(PropertyHint.None, "Active highest priority grabber gets the focus")]
    public int Priority;

    [Export]
    public NodePath? NodeToGiveFocusTo;

    /// <summary>
    ///   If true then this always grabs focus even if something is already focused
    /// </summary>
    [Export]
    public bool AlwaysOverrideFocus;

    /// <summary>
    ///   If true then this always grabs focus when this becomes visible, even ignoring
    ///   <see cref="SkipOverridingFocusForElements"/>
    /// </summary>
    [Export]
    public bool GrabFocusWhenBecomingVisible;

    private double elapsed;
    private bool reportedState;
    private Godot.Collections.Array<NodePath>? skipOverridingFocusForElements;
    private IEnumerable<string> skipOverridingStringConverted = Array.Empty<string>();

    private bool wantsToGrabFocusOnce;

    /// <summary>
    ///   Any <see cref="NodePath"/> listed here (and child paths as well) will skip the focus override. This allows
    ///   creating areas that steal focus from other parts of the GUI when they are visible.
    /// </summary>
    [Export]
    public Godot.Collections.Array<NodePath>? SkipOverridingFocusForElements
    {
        get => skipOverridingFocusForElements;
        set
        {
            skipOverridingFocusForElements = value;

            UpdateOverrideFocusStrings();
        }
    }

    public IEnumerable<string> SkipOverridingFocusForElementStrings => skipOverridingStringConverted;

    public override void _Ready()
    {
        if (string.IsNullOrWhiteSpace(NodeToGiveFocusTo))
            throw new ArgumentException("Focus grabber must have the node to focus set");
    }

    public override void _EnterTree()
    {
        UpdateOverrideFocusStrings();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (reportedState)
        {
            GUIFocusSetter.Instance.ReportRemovedGrabber(this);
        }
    }

    public override void _Process(double delta)
    {
        elapsed += delta;

        if (elapsed < Constants.GUI_FOCUS_GRABBER_PROCESS_INTERVAL)
            return;

        elapsed = 0;

        // We need to do this as NotificationVisibilityChanged is only for the current node and not the entire tree
        if (IsVisibleInTree() != reportedState)
        {
            reportedState = !reportedState;

            if (reportedState && GrabFocusWhenBecomingVisible)
            {
                wantsToGrabFocusOnce = true;
            }

            GUIFocusSetter.Instance.ReportGrabberState(this, reportedState);
        }
    }

    public bool CheckWantsToStealFocus()
    {
        if (AlwaysOverrideFocus)
            return true;

        if (wantsToGrabFocusOnce)
            return true;

        return false;
    }

    public bool CheckAlwaysOverrideFocusAndReset()
    {
        if (wantsToGrabFocusOnce)
        {
            wantsToGrabFocusOnce = false;
            return true;
        }

        return false;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            NodeToGiveFocusTo?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void UpdateOverrideFocusStrings()
    {
        // We can't resolve relative paths before we are inside the tree
        if (!IsInsideTree())
            return;

        if (skipOverridingFocusForElements == null)
        {
            skipOverridingStringConverted = Array.Empty<string>();
        }
        else
        {
            // To convert relative paths to absolute ones, we need to do this
            skipOverridingStringConverted =
                skipOverridingFocusForElements.Select(n => GetNode(n).GetPath().ToString()).ToList();
        }
    }
}
