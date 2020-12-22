using System.Collections.Generic;
using Godot;

/// <summary>
///   Dataset to be visualized on the line chart
/// </summary>
public class LineChartData
{
    private Color lineColour;
    private bool draw = true;

    // ReSharper disable once CollectionNeverUpdated.Global
    public List<DataPoint> DataPoints { get; set; } = new List<DataPoint>();

    /// <summary>
    ///   The icon on the chart legend
    /// </summary>
    public Texture IconTexture { get; set; }

    public float LineWidth { get; set; } = 1.3f;

    /// <summary>
    ///   Used to differentiate the data set's visual by color
    /// </summary>
    public Color DataColour
    {
        get => lineColour;
        set
        {
            lineColour = value;

            foreach (var point in DataPoints)
                point.MarkerColour = lineColour;
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
}
