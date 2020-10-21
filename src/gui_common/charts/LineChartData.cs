using System.Collections.Generic;
using Godot;

/// <summary>
///   Data set to be represented on the line chart
/// </summary>
public class LineChartData
{
    /// <summary>
    ///   Name for display in legends
    /// </summary>
    public string Name;

    public List<ChartPoint> PointDatas;
    public float LineWidth;
    public Color LineColor;

    private bool draw;

    public LineChartData(string name, List<ChartPoint> pointDatas, float lineWidth,
        Color lineColor, bool draw = true)
    {
        Name = name;
        PointDatas = pointDatas;
        LineWidth = lineWidth;
        LineColor = lineColor;
        Draw = draw;
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
