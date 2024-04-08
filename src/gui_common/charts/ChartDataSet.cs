using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Base dataset class for charts.
/// </summary>
public class ChartDataSet : ICloneable
{
    private readonly List<DataPoint> dataPoints = new();
    private Color dataColour;
    private bool draw = true;

    /// <summary>
    ///   The icon on the chart legend
    /// </summary>
    public IReadOnlyCollection<DataPoint> DataPoints => dataPoints;

    public Texture2D? Icon { get; set; }

    /// <summary>
    ///   Used to differentiate the data set's visual by color
    /// </summary>
    public Color Colour
    {
        get => dataColour;
        set
        {
            // No translucent color, that'll probably cause unexpected behavior
            dataColour = new Color(value, 1);

            foreach (var point in DataPoints)
            {
                point.MarkerColour = dataColour;
                point.QueueRedraw();
            }
        }
    }

    /// <summary>
    ///   If this is true, visuals will be drawn (e.g lines, markers)
    /// </summary>
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

    /// <summary>
    ///   Adds a data point to this dataset.
    /// </summary>
    public void AddPoint(DataPoint point)
    {
        dataPoints.Add(point);
    }

    /// <summary>
    ///   Frees and removes all data point from this dataset.
    /// </summary>
    public void ClearPoints()
    {
        foreach (var point in dataPoints)
        {
            point.Detach();
            DataPoint.ReturnDataPoint(point);
        }

        dataPoints.Clear();
    }

    public virtual object Clone()
    {
        var result = new ChartDataSet();
        ClonePropertiesTo(result);
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
