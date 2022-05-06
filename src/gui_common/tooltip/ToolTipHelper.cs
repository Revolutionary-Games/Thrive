﻿using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Helper class for custom tooltips
/// </summary>
public static class ToolTipHelper
{
    private static readonly PackedScene DefaultTipScene = GD.Load<PackedScene>(
        "res://src/gui_common/tooltip/DefaultToolTip.tscn");

    /// <summary>
    ///   A helper static member to store enter/exit callback for registered tooltips to keep it from unloading.
    /// </summary>
    private static readonly List<ToolTipCallbackData> ToolTipCallbacks = new();

    /// <summary>
    ///   Instantiates a default tooltip scene
    /// </summary>
    public static DefaultToolTip CreateDefaultToolTip()
    {
        return (DefaultToolTip)DefaultTipScene.Instance();
    }

    /// <summary>
    ///   Registers a Control mouse enter and exit event if hasn't already yet to the callbacks for the given
    ///   custom tooltip.
    /// </summary>
    /// <param name="control">The Control to register the tooltip to.</param>
    /// <param name="tooltip">
    ///   The tooltip to register with. Null is not valid but it's nullable to make a few other places in the code
    ///   easier and there isn't much of a difference if we print the error here rather than if our callers did it.
    /// </param>
    public static void RegisterToolTipForControl(this Control control, ICustomToolTip? tooltip)
    {
        if (tooltip == null)
        {
            GD.PrintErr($"Can't register Control: '{control.Name}' with a nonexistent tooltip");
            return;
        }

        // Skip if already registered
        if (control.IsToolTipRegistered(tooltip))
            return;

        var toolTipCallbackData = new ToolTipCallbackData(control, tooltip);

        control.Connect("mouse_entered", toolTipCallbackData, nameof(ToolTipCallbackData.OnMouseEnter));
        control.Connect("mouse_exited", toolTipCallbackData, nameof(ToolTipCallbackData.OnMouseExit));
        control.Connect("hide", toolTipCallbackData, nameof(ToolTipCallbackData.OnMouseExit));
        control.Connect("tree_exiting", toolTipCallbackData, nameof(ToolTipCallbackData.OnExitingTree));

        ToolTipCallbacks.Add(toolTipCallbackData);
    }

    /// <summary>
    ///   Disconnects signal connections and removes stored callback data for the given tooltip.
    /// </summary>
    public static void UnRegisterToolTipForControl(this Control control, ICustomToolTip tooltip)
    {
        if (!control.IsToolTipRegistered(tooltip))
            return;

        var data = GetToolTipCallbackData(control, tooltip);

        control.Disconnect("mouse_entered", data, nameof(ToolTipCallbackData.OnMouseEnter));
        control.Disconnect("mouse_exited", data, nameof(ToolTipCallbackData.OnMouseExit));
        control.Disconnect("hide", data, nameof(ToolTipCallbackData.OnMouseExit));
        control.Disconnect("tree_exiting", data, nameof(ToolTipCallbackData.OnExitingTree));

        ToolTipCallbacks.Remove(data);
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

    /// <summary>
    ///   Registers a Control mouse enter/exit event to display a custom tooltip from the given tooltip and group name.
    /// </summary>
    public static void RegisterToolTipForControl(this Control control, string tooltip, string group =
        ToolTipManager.DEFAULT_GROUP_NAME)
    {
        control.RegisterToolTipForControl(ToolTipManager.Instance.GetToolTip(tooltip, group));
    }

    /// <summary>
    ///   Get the control the given tooltip is registered to. Doesn't take into account controls with multiple
    ///   registered tooltips.
    /// </summary>
    public static Control? GetControlAssociatedWithToolTip(ICustomToolTip? tooltip)
    {
        var callbackData = ToolTipCallbacks.Find(match => match.ToolTip == tooltip);

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

    private static ToolTipCallbackData GetToolTipCallbackData(Control control, ICustomToolTip tooltip)
    {
        return ToolTipCallbacks.Find(match => match.ToolTipable == control && match.ToolTip == tooltip);
    }

    private static IEnumerable<ToolTipCallbackData> GetAllToolTipsForControl(Control control)
    {
        return ToolTipCallbacks.Where(match => match.ToolTipable == control);
    }
}
