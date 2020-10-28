using System.Collections.Generic;
using Godot;

/// <summary>
///   Data set to be visualized on the line chart
/// </summary>
public class LineChartData
{
    private Color lineColor;
    private bool draw = true;

    /// <summary>
    ///   Name of the data set
    /// </summary>
    public string Name { get; set; }

    public List<ChartPoint> PointDatas { get; set; }

    public Texture LegendIcon { get; set; }

    public float LineWidth { get; set; } = 1.5f;

    public Color LineColor
    {
        get => lineColor;
        set
        {
            lineColor = value;

            if (PointDatas.Count <= 0)
                return;

            foreach (var point in PointDatas)
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

            if (PointDatas.Count <= 0)
                return;

            foreach (var point in PointDatas)
                point.Visible = value;
        }
    }

    /// <summary>
    ///   Helper for collecting coordinates from all data points
    /// </summary>
    public List<Vector2> GetCoordinates()
    {
        var result = new List<Vector2>();

        foreach (var point in PointDatas)
        {
            result.Add(point.Coordinate);
        }

        return result;
    }
}
