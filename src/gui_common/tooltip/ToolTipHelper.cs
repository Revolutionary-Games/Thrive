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
    ///   Instantiates a default tooltip scene
    /// </summary>
    public static DefaultToolTip CreateDefaultToolTip()
    {
        return (DefaultToolTip)DefaultTipScene.Instance();
    }

    /// <summary>
    ///   Registers a Control mouse enter/exit event to display a tooltip
    /// </summary>
    /// <param name="control">The Control to register the tooltip to</param>
    /// <param name="tooltip">The tooltip to register with</param>
    /// <param name="callbackDatas">List to store the callbacks to keep them from unloading</param>
    public static void RegisterToolTipForControl(this Control control, ICustomToolTip tooltip,
        List<ToolTipCallbackData> callbackDatas)
    {
        // Skip if already registered
        if (callbackDatas.Find(match => match.ToolTip == tooltip) != null)
            return;

        var toolTipCallbackData = new ToolTipCallbackData(tooltip);

        control.Connect("mouse_entered", toolTipCallbackData, nameof(ToolTipCallbackData.OnMouseEnter));
        control.Connect("mouse_exited", toolTipCallbackData, nameof(ToolTipCallbackData.OnMouseExit));
        control.Connect("hide", toolTipCallbackData, nameof(ToolTipCallbackData.OnMouseExit));
        control.Connect("tree_exiting", toolTipCallbackData, nameof(ToolTipCallbackData.OnMouseExit));

        callbackDatas.Add(toolTipCallbackData);
    }
}
