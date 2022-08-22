using System;
using System.Globalization;
using Godot;

public class FilterArgumentSlider : HBoxContainer
{
    [Export]
    public NodePath SliderPath = null!;

    [Export]
    public NodePath LabelPath = null!;

    private HSlider slider = null!;
    private Label label = null!;

    /// <summary>
    ///   If redraw is needed.
    /// </summary>
    private bool dirty = true;

    private float value;
    private float valueSnapshot;

    private Filter.NumberFilterArgument filterArgument = null!;

    public void Initialize(Filter.NumberFilterArgument filterArgument)
    {
        this.filterArgument = filterArgument;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        if (filterArgument == null)
            throw new InvalidOperationException("Node was not initialized!");

        slider = GetNode<HSlider>(SliderPath);

        slider.MinValue = filterArgument.MinValue;
        slider.MaxValue = filterArgument.MaxValue;

        label = GetNode<Label>(LabelPath);
        label.Text = filterArgument.Value.ToString(CultureInfo.CurrentCulture);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (dirty)
        {
            label.Update();
            dirty = false;
        }
    }

    public void MakeSnapshot()
    {
        valueSnapshot = value;

        GD.Print("FAS making snapshot ", valueSnapshot);
    }

    public void RestoreLastSnapshot()
    {
        GD.Print("FAS restoring value from snapshot: ", valueSnapshot);
        slider.Value = valueSnapshot;
        OnNewValueSelected(valueSnapshot);
    }

    private void OnNewValueSelected(float value)
    {
        this.value = value;
        label.Text = value.ToString(CultureInfo.CurrentCulture);
        filterArgument.Value = value;
        dirty = true;
    }
}
