using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Base dataset class for charts.
/// </summary>
public class ChartDataSet : ICloneable
{
    private List<DataPoint> dataPoints = new List<DataPoint>();
    private Color dataColour;
    private bool draw = true;

    public IReadOnlyCollection<DataPoint> DataPoints => dataPoints;

    public Texture Icon { get; set; }

    public Color Colour
    {
        get => dataColour;
        set
        {
            dataColour = value;

            foreach (var point in DataPoints)
            {
                point.MarkerColour = dataColour;
                point.Update();
            }
        }
    }

    public bool Draw
    {
        get => draw;
        set
        {
            draw = value;

            foreach (var point in DataPoints)
                point.Visible = value;
        }
    }

    public void AddPoint(DataPoint point)
    {
        dataPoints.Add(point);
    }

    public void ClearPoints()
    {
        foreach (var point in dataPoints)
            point.Free();

        dataPoints.Clear();
    }

    public virtual object Clone()
    {
        var result = new ChartDataSet
        {
            Icon = Icon,
            Colour = Colour,
            Draw = Draw,
        };

        foreach (var point in dataPoints)
        {
            result.AddPoint((DataPoint)point.Clone());
        }

        return result;
    }

    protected void ClonePropertiesTo(ChartDataSet dataset)
    {
        dataset.Icon = Icon;
        dataset.Colour = Colour;
        dataset.Draw = Draw;

        foreach (var point in dataPoints)
            dataset.AddPoint((DataPoint)point.Clone());
    }
}
