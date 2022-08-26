using System.Collections.Generic;
using Godot;

/// <summary>
///   Listens for any controller axis inputs and creates <see cref="ControllerAxisVisualizer"/>s to show them
/// </summary>
public class ControllerInputAxisVisualizationContainer : HFlowContainer
{
    /// <summary>
    ///   Automatically creates even axes below odd axes to make the horizontal and vertical controller axis mappings
    ///   show up in a more natural presentation.
    /// </summary>
    [Export]
    public bool AutoCreateEvenAxes = true;

    /// <summary>
    ///   If true this automatically calls <see cref="Start"/> when this becomes visible. Set to false when used in
    ///   scenes that are loaded during gameplay to save performance.
    /// </summary>
    [Export]
    public bool AutoDetectBecomesVisible = true;

    /// <summary>
    ///   Created axis visualizers
    /// </summary>
    private readonly Dictionary<int, ControllerAxisVisualizer> axisVisualizers = new();

    /// <summary>
    ///   As each visualizer can show two axes, this is a secondary map that also points to the axes in the primary
    ///   map. This is done this way to make cleanup easier.
    /// </summary>
    private readonly Dictionary<int, ControllerAxisVisualizer> secondaryVisualizerMapping = new();

    private PackedScene visualizerScene = null!;

    private bool wasLastVisible;

    public override void _Ready()
    {
        visualizerScene = ResourceLoader.Load<PackedScene>("res://src/engine/input/ControllerAxisVisualizer.tscn");

        SetProcessInput(false);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (wasLastVisible && !IsVisibleInTree())
        {
            Stop();
        }
        else if (!wasLastVisible && AutoDetectBecomesVisible && IsVisibleInTree())
        {
            Start();
        }
    }

    /// <summary>
    ///   Starts showing input (until this becomes hidden)
    /// </summary>
    public void Start()
    {
        wasLastVisible = true;
        SetProcessInput(true);
    }

    /// <summary>
    ///   Stops updating and frees child elements
    /// </summary>
    public void Stop()
    {
        wasLastVisible = false;

        foreach (var axisVisualizer in axisVisualizers)
        {
            axisVisualizer.Value.Free();
        }

        axisVisualizers.Clear();
        secondaryVisualizerMapping.Clear();

        SetProcessInput(false);
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        if (@event is InputEventJoypadMotion joypadMotion)
        {
            HandleController(joypadMotion.Axis, joypadMotion.AxisValue);
        }

        InputManager.ForwardInput(@event);
    }

    private void HandleController(int axis, float motionValue)
    {
        // If we axis value is odd, make sure the previous axis value exists, to make the horizontal and vertical
        // axes work better
        if (AutoCreateEvenAxes && axis > 0 && axis % 2 == 1)
        {
            GetOrCreateVisualizer(axis - 1);
        }

        var axisVisualizer = GetOrCreateVisualizer(axis);

        axisVisualizer.SetAxisValue(axis, motionValue);
    }

    private ControllerAxisVisualizer GetOrCreateVisualizer(int axis)
    {
        var axisVisualizer = GetVisualizer(axis);

        if (axisVisualizer != null)
            return axisVisualizer;

        // Find a visualizer we can piggyback off
        // For now an axis value that is 1 lower or higher can be used
        axisVisualizer = FindAndAddToVisualizer(axis - 1, axis);

        if (axisVisualizer != null)
            return axisVisualizer;

        axisVisualizer = FindAndAddToVisualizer(axis + 1, axis);

        if (axisVisualizer != null)
            return axisVisualizer;

        // Need to create a new visualizer
        axisVisualizer = (ControllerAxisVisualizer)visualizerScene.Instance();

        AddChild(axisVisualizer);
        axisVisualizers.Add(axis, axisVisualizer);

        axisVisualizer.AddAxis(axis);
        return axisVisualizer;
    }

    private ControllerAxisVisualizer? GetVisualizer(int axis)
    {
        if (axisVisualizers.TryGetValue(axis, out var axisVisualizer))
            return axisVisualizer;

        if (secondaryVisualizerMapping.TryGetValue(axis, out axisVisualizer))
            return axisVisualizer;

        return null;
    }

    private ControllerAxisVisualizer? FindAndAddToVisualizer(int axisToLookFor, int axisToAdd)
    {
        var visualizer = GetVisualizer(axisToLookFor);

        if (visualizer == null)
            return null;

        if (!visualizer.HasSecondAxis)
        {
            visualizer.AddAxis(axisToAdd);
            secondaryVisualizerMapping.Add(axisToAdd, visualizer);
            return visualizer;
        }

        return null;
    }
}
