/// <summary>
///   Dataset to be visualized on <see cref="LineChart"/>. Contains series of data points.
/// </summary>
public class LineChartData : ChartDataSet
{
    public float LineWidth { get; set; } = 1.13f;

    public override object Clone()
    {
        var result = new LineChartData();

        ClonePropertiesTo(result);

        result.LineWidth = LineWidth;

        return result;
    }
}
