﻿using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class FocusGrabber : Control
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
    ///   If true then this always grabs focus when this becomes visible
    /// </summary>
    [Export]
    public bool GrabFocusWhenBecomingVisible;

    private float elapsed;
    private bool reportedState;
    private List<NodePath>? skipOverridingFocusForElements;
    private IEnumerable<string> skipOverridingStringConverted = Array.Empty<string>();

    private bool wantsToGrabFocusOnce;

    /// <summary>
    ///   Any paths listed here (and child paths as well) will skip the focus override. This allows creating areas that
    ///   steal focus from other parts of the GUI when they are visible.
    /// </summary>
    [Export]
    public List<NodePath>? SkipOverridingFocusForElements
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

        UpdateOverrideFocusStrings();
    }

    public override void _Process(float delta)
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

    public override void _ExitTree()
    {
        base._ExitTree();

        if (reportedState)
        {
            GUIFocusSetter.Instance.ReportRemovedGrabber(this);
        }
    }

    public bool CheckWantsToStealFocusAndReset()
    {
        if (AlwaysOverrideFocus)
            return true;

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
            // If we ran into problems where this would need to be updated while inside the tree it might be better to
            // use IsAParentOf()
            skipOverridingStringConverted =
                skipOverridingFocusForElements.Select(n => GetNode(n).GetPath().ToString()).ToList();
        }
    }
}
