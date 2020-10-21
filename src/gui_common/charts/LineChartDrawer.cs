using Godot;

/// <summary>
///   Draws visuals for the line chart like the line segments and markers
/// </summary>
/// <para>
///   This is separated from LineChart class because it seems the renderings behave better
///   if they are done directly from this node
/// </para>
public class LineChartDrawer : Control
{
    private readonly Texture hLineTexture = GD.Load<Texture>("res://assets/textures/gui/bevel/hSeparatorCentered.png");

    /// <summary>
    ///   Reference to the parent chart to access datas
    /// </summary>
    private LineChart chart;

    public void Init(LineChart chart)
    {
        this.chart = chart;
    }

    public override void _Draw()
    {
        if (chart == null || chart.DataSets.Count <= 0)
            return;

        DrawOrdinateLines();
        DrawLineSegments();
    }

    /// <summary>
    ///   Removes all point markers from scene tree
    /// </summary>
    public void ClearPoints()
    {
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
        }
    }

    private void DrawOrdinateLines()
    {
        for (int i = 0; i < chart.YAxisTicks; i++)
        {
            var value = Mathf.Round(i * (chart.MaxValues.y - chart.MinValues.y) /
                (chart.YAxisTicks - 1) + chart.MinValues.y);

            DrawTextureRect(hLineTexture, new Rect2(new Vector2(
                0, ConvertToYCoordinate(value)), RectSize.x, 1), false, new Color(1, 1, 1, 0.5f));
        }
    }

    /// <summary>
    ///   Connect the points
    /// </summary>
    private void DrawLineSegments()
    {
        foreach (var data in chart.DataSets)
        {
            foreach (var point in data.PointDatas)
            {
                point.Coordinate = ConvertToCoordinate(point.Value);

                if (!point.IsInsideTree())
                    AddChild(point);

                point.Update();
            }

            if (data.Draw)
                DrawPolyline(data.GetCoordinates().ToArray(), data.LineColor, data.LineWidth, true);
        }
    }

    /// <summary>
    ///   Helper method for converting a chart value into a coordinate.
    /// </summary>
    /// <para>
    ///   (for purely aesthetic reasons) find out if the origin could be at 0,0.
    ///   Currently it's offset a bit from the bottom left
    /// </para>
    /// <returns>Position of the given value on the chart</returns>
    private Vector2 ConvertToCoordinate(Vector2 value)
    {
        return new Vector2(ConvertToXCoordinate(value.x), ConvertToYCoordinate(value.y));
    }

    private float ConvertToXCoordinate(float value)
    {
        var lineRectX = RectSize.x / chart.XAxisTicks;

        var lineRectWidth = lineRectX * (chart.XAxisTicks - 1);

        var dx = chart.MaxValues.x - chart.MinValues.x;

        return ((value - chart.MinValues.x) * lineRectWidth / dx) + lineRectX / 2;
    }

    private float ConvertToYCoordinate(float value)
    {
        var lineRectY = RectSize.y / chart.YAxisTicks;

        var lineRectHeight = lineRectY * (chart.YAxisTicks - 1);

        var dy = chart.MaxValues.y - chart.MinValues.y;

        return lineRectHeight - ((value - chart.MinValues.y) * lineRectHeight / dy) + lineRectY / 2;
    }
}
