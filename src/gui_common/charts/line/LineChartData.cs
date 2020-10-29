using System.Collections.Generic;
using Godot;

/// <summary>
///   Dataset to be visualized on the line chart
/// </summary>
public class LineChartData
{
    private Color lineColor;
    private bool draw = true;

    // ReSharper disable once CollectionNeverUpdated.Global
    public List<ChartPoint> Points { get; set; } = new List<ChartPoint>();

    /// <summary>
    ///   The icon on the chart legend
    /// </summary>
    public Texture IconTexture { get; set; }

    public float LineWidth { get; set; } = 1.5f;

    /// <summary>
    ///   Used to differentiate the data set's visual by color
    /// </summary>
    public Color DataColor
    {
        get => lineColor;
        set
        {
            lineColor = value;

            if (Points.Count <= 0)
                return;

            foreach (var point in Points)
                point.MarkerColor = lineColor;
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

            if (Points.Count <= 0)
                return;

            foreach (var point in Points)
                point.Visible = value;
        }
    }
}
