using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Helper class for custom tooltips
/// </summary>
public static class ToolTipHelper
{
    private static readonly PackedScene DefaultTipScene = GD.Load<PackedScene>(
        "res://src/gui_common/tooltip/DefaultToolTip.tscn");

    private static readonly Stack<DefaultToolTip> DefaultToolTipCache = new();

    /// <summary>
    ///   A helper static member to store enter/exit callback for registered tooltips to keep it from unloading.
    /// </summary>
    private static readonly List<ToolTipCallbackData> ToolTipCallbacks = new();

    /// <summary>
    ///   Gets a default tooltip instance from cache, or instantiates a default tooltip scene if the cache is empty.
    /// </summary>
    public static DefaultToolTip GetDefaultToolTip()
    {
        if (DefaultToolTipCache.Count < 1)
        {
            return (DefaultToolTip)DefaultTipScene.Instantiate();
        }

        // TODO: https://github.com/Revolutionary-Games/Thrive/issues/3799
        return DefaultToolTipCache.Pop();
    }

    /// <summary>
    ///   Returns a default tooltip instance to the cache for <see cref="GetDefaultToolTip"/>
    /// </summary>
    /// <param name="toolTip">The tooltip to return to the cache</param>
    public static void ReturnDefaultToolTip(DefaultToolTip toolTip)
    {
        // Automatically remove the tooltip from the parent to prepare it for new use
        var parent = toolTip.GetParent();
        parent?.RemoveChild(toolTip);

        // TODO: https://github.com/Revolutionary-Games/Thrive/issues/3799

#if DEBUG
        foreach (var cachedToolTip in DefaultToolTipCache)
        {
            if (cachedToolTip == toolTip)
                throw new ArgumentException("Can't return the same tooltip multiple times");
        }
#endif

        DefaultToolTipCache.Push(toolTip);
    }

    /// <summary>
    ///   Registers a Control mouse enter and exit event if hasn't already yet to the callbacks for the given
    ///   custom tooltip. Note that the code calling this must call
    ///   <see cref="UnRegisterToolTipForControl(Godot.Control,ICustomToolTip?)"/> once
    ///   the scene containing the tooltip is removed (unless <see cref="autoUnregister"/> is true).
    /// </summary>
    /// <param name="control">The Control to register the tooltip to.</param>
    /// <param name="tooltip">
    ///   The tooltip to register with. Null is not valid but it's nullable to make a few other places in the code
    ///   easier and there isn't much of a difference if we print the error here rather than if our callers did it.
    /// </param>
    /// <param name="autoUnregister">
    ///   When true the tooltip is automatically detached when the control exits the tree. Note that this is only
    ///   usable if you know that the control this is used for is never reattached to the scene tree after detaching.
    /// </param>
    public static void RegisterToolTipForControl(this Control control, ICustomToolTip? tooltip, bool autoUnregister)
    {
        if (tooltip == null)
        {
            GD.PrintErr($"Can't register Control: '{control.Name}' with a nonexistent tooltip");
            return;
        }

        // Skip if already registered
        if (control.IsToolTipRegistered(tooltip))
            return;

        var toolTipCallbackData = new ToolTipCallbackData(control, tooltip, autoUnregister);

        control.Connect(Control.SignalName.MouseEntered,
            new Callable(toolTipCallbackData, nameof(ToolTipCallbackData.OnMouseEnter)));
        control.Connect(Control.SignalName.MouseExited,
            new Callable(toolTipCallbackData, nameof(ToolTipCallbackData.OnMouseExit)));
        control.Connect(CanvasItem.SignalName.Hidden,
            new Callable(toolTipCallbackData, nameof(ToolTipCallbackData.OnMouseExit)));
        control.Connect(Node.SignalName.TreeExiting,
            new Callable(toolTipCallbackData, nameof(ToolTipCallbackData.OnExitingTree)));

        ToolTipCallbacks.Add(toolTipCallbackData);
    }

    /// <summary>
    ///   Registers a Control mouse enter/exit event to display a custom tooltip from the given tooltip and group name.
    ///   This variant is for tooltips defined in the tooltip manager and not dynamically created during runtime.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This variant defaults to auto unregistering the tooltips as this is used with predefined tooltips so often
    ///     they don't want to be tracked manually for unregistering, as such this sets the auto unregister to require
    ///     less code. But be aware that if anything that needs to support detaching and re-attaching to the scene tree
    ///     will not work correctly with auto unregistering enabled.
    ///   </para>
    /// </remarks>
    public static void RegisterToolTipForControl(this Control control, string tooltip, string group =
        ToolTipManager.DEFAULT_GROUP_NAME, bool autoUnregister = true)
    {
        control.RegisterToolTipForControl(ToolTipManager.Instance.GetToolTip(tooltip, group), autoUnregister);
    }

    /// <summary>
    ///   Disconnects signal connections and removes stored callback data for the given tooltip.
    ///   When passed a null value this warns
    /// </summary>
    public static void UnRegisterToolTipForControl(this Control control, ICustomToolTip? tooltip)
    {
        if (tooltip == null)
        {
            GD.PrintErr($"Null tooltip passed to unregister for control: {control.Name}");
            return;
        }

        if (!control.IsToolTipRegistered(tooltip))
            return;

        var data = GetToolTipCallbackData(control, tooltip);

        control.Disconnect("mouse_entered", new Callable(data, nameof(ToolTipCallbackData.OnMouseEnter)));
        control.Disconnect("mouse_exited", new Callable(data, nameof(ToolTipCallbackData.OnMouseExit)));
        control.Disconnect("hide", new Callable(data, nameof(ToolTipCallbackData.OnMouseExit)));
        control.Disconnect("tree_exiting", new Callable(data, nameof(ToolTipCallbackData.OnExitingTree)));

        ToolTipCallbacks.Remove(data);

        data.Unregistered = true;
        data.Dispose();
    }

    /// <summary>
    ///   Unregister variant for tooltips that are looked up by name
    /// </summary>
    public static void UnRegisterToolTipForControl(this Control control, string tooltip, string group =
        ToolTipManager.DEFAULT_GROUP_NAME)
    {
        control.UnRegisterToolTipForControl(ToolTipManager.Instance.GetToolTip(tooltip, group));
    }

    /// <summary>
    ///   Disconnects the first registered tooltip for a <see cref="Control"/>
    /// </summary>
    public static void UnRegisterFirstToolTipForControl(this Control control)
    {
        foreach (var toolTipCallbackData in GetAllToolTipsForControl(control))
        {
            // Not the most efficient as we've already looked up the data, but this isn't used that much so the extra
            // slowness shouldn't matter
            UnRegisterToolTipForControl(control, toolTipCallbackData.ToolTip);

            // If this is converted to unregister all, then the list of things to unregister needs to be enumerated
            // first. Here we can get by with a return;
            return;
        }
    }

    public static bool IsToolTipRegistered(this Control control, ICustomToolTip tooltip)
    {
        return ToolTipCallbacks.Contains(GetToolTipCallbackData(control, tooltip));
    }

    public static int CountRegisteredToolTips()
    {
        return ToolTipCallbacks.Count;
    }

    /// <summary>
    ///   Get the control the given tooltip is registered to. Doesn't take into account controls with multiple
    ///   registered tooltips.
    /// </summary>
    public static Control? GetControlAssociatedWithToolTip(ICustomToolTip? tooltip)
    {
        var callbackData = ToolTipCallbacks.Find(c => c.ToolTip == tooltip);

        return callbackData?.ToolTipable;
    }

    /// <summary>
    ///   Get the control the given tooltip is registered to. Doesn't take into account controls with multiple
    ///   registered tooltips.
    /// </summary>
    public static Control? GetControlAssociatedWithToolTip(string name, string group =
        ToolTipManager.DEFAULT_GROUP_NAME)
    {
        var tooltip = ToolTipManager.Instance.GetToolTip(name, group);

        // No point in trying to find a control with a null tooltip
        if (tooltip == null)
            return null;

        return GetControlAssociatedWithToolTip(tooltip);
    }

    /// <summary>
    ///   Releases the tooltip cache. Called when the game is closing.
    /// </summary>
    internal static void ReleaseToolTipsCache()
    {
        while (DefaultToolTipCache.Count > 0)
        {
            var tooltip = DefaultToolTipCache.Pop();
            tooltip.Free();

            // TODO: https://github.com/Revolutionary-Games/Thrive/issues/3799
        }
    }

    private static ToolTipCallbackData GetToolTipCallbackData(Control control, ICustomToolTip tooltip)
    {
        return ToolTipCallbacks.Find(c => c.ToolTipable == control && c.ToolTip == tooltip);
    }

    private static IEnumerable<ToolTipCallbackData> GetAllToolTipsForControl(Control control)
    {
        return ToolTipCallbacks.Where(c => c.ToolTipable == control);
    }
}
