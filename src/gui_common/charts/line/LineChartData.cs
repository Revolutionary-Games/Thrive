using System.Collections.Generic;
using Godot;

/// <summary>
///   Dataset to be visualized on the line chart. Contains series of data points.
/// </summary>
public class LineChartData
{
    private List<DataPoint> dataPoints = new List<DataPoint>();
    private Color dataColour;
    private bool draw = true;

    public IReadOnlyCollection<DataPoint> DataPoints => dataPoints;

    /// <summary>
    ///   The icon on the chart legend
    /// </summary>
    public Texture IconTexture { get; set; }

    public float LineWidth { get; set; } = 1.13f;

    /// <summary>
    ///   Used to differentiate the data set's visual by color
    /// </summary>
    public Color DataColour
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
            point.Free();

        dataPoints.Clear();
    }
}
