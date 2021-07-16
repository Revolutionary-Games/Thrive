using System.Collections.Generic;
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
    private static readonly List<ToolTipCallbackData> toolTipCallbacks = new List<ToolTipCallbackData>();

    /// <summary>
    ///   Instantiates a default tooltip scene
    /// </summary>
    public static DefaultToolTip CreateDefaultToolTip()
    {
        return (DefaultToolTip)DefaultTipScene.Instance();
    }

    /// <summary>
    ///   Registers a Control mouse enter/exit event to display a custom tooltip.
    /// </summary>
    /// <param name="control">The Control to register the tooltip to.</param>
    /// <param name="tooltip">The tooltip to register with.</param>
    public static void RegisterToolTipForControl(this Control control, ICustomToolTip tooltip)
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
        control.Connect("tree_exiting", toolTipCallbackData, nameof(ToolTipCallbackData.OnToolTipableExitingTree));

        toolTipCallbacks.Add(toolTipCallbackData);
    }

    /// <summary>
    ///   Deletes stored callback data for the given tooltip.
    /// </summary>
    public static void UnRegisterToolTipForControl(this Control control, ICustomToolTip tooltip)
    {
        if (!control.IsToolTipRegistered(tooltip))
            return;

        var data = GetToolTipCallbackData(control, tooltip);
        toolTipCallbacks.Remove(data);
        Invoke.Instance.Queue(data.Free);
    }

    public static bool IsToolTipRegistered(this Control control, ICustomToolTip tooltip)
    {
        return toolTipCallbacks.Contains(GetToolTipCallbackData(control, tooltip));
    }

    /// <summary>
    ///   Registers a Control mouse enter/exit event to display a custom tooltip from the given tooltip and group name.
    /// </summary>
    /// <param name="tooltip">The internal node name of the tooltip.</param>
    /// <param name="group">Name of the tooltip group the tooltip is part of.</param>
    public static void RegisterToolTipForControl(this Control control, string tooltip, string group =
        ToolTipManager.DEFAULT_GROUP_NAME)
    {
        control.RegisterToolTipForControl(ToolTipManager.Instance.GetToolTip(tooltip, group));
    }

    private static ToolTipCallbackData GetToolTipCallbackData(Control control, ICustomToolTip tooltip)
    {
        return toolTipCallbacks.Find(match => match.ToolTipable == control && match.ToolTip == tooltip);
    }
}
