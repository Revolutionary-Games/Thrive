using System.Collections.Generic;
using System.Globalization;
using Godot;

/// <summary>
///   Custom widget for plotting data points on a line
/// </summary>
public class LineChart : VBoxContainer
{
    [Export]
    public NodePath HorizontalLabelPath;

    [Export]
    public NodePath VerticalLabelPath;

    [Export]
    public NodePath VerticalTicksContainerPath;

    [Export]
    public NodePath HorizontalTicksContainerPath;

    [Export]
    public NodePath LineContainerPath;

    /// <summary>
    ///   Number of scales to represent x axis values
    /// </summary>
    [Export]
    public int XAxisTicks;

    /// <summary>
    ///   Number of scales to represent y axis values
    /// </summary>
    [Export]
    public int YAxisTicks;

    private readonly PackedScene
        toolTipScene = GD.Load<PackedScene>("res://src/gui_common/tooltip/DefaultToolTip.tscn");

    private Label horizontalLabel;
    private Label verticalLabel;
    private VBoxContainer verticalLabelsContainer;
    private HBoxContainer horizontalLabelsContainer;
    private LineChartDrawer drawer;

    private string xAxisName;
    private string yAxisName;

    /// <summary>
    ///   To easily remove the marker tooltips, we store them here immediately after creating them
    /// </summary>
    private List<DefaultToolTip> cachedMarkerToolTips = new List<DefaultToolTip>();

    private List<ToolTipCallbackData> toolTipCallbacks = new List<ToolTipCallbackData>();

    public Vector2 MinValues { get; private set; }

    public Vector2 MaxValues { get; private set; }

    [Export]
    public string YAxisName
    {
        get => yAxisName;
        set
        {
            yAxisName = value;
            UpdateAxisNames();
        }
    }

    [Export]
    public string XAxisName
    {
        get => xAxisName;
        set
        {
            xAxisName = value;
            UpdateAxisNames();
        }
    }

    /// <summary>
    ///   Datas to be plotted
    /// </summary>
    public List<LineChartData> DataSets { get; set; } = new List<LineChartData>();

    public override void _Ready()
    {
        horizontalLabel = GetNode<Label>(HorizontalLabelPath);
        verticalLabel = GetNode<Label>(VerticalLabelPath);
        verticalLabelsContainer = GetNode<VBoxContainer>(VerticalTicksContainerPath);
        horizontalLabelsContainer = GetNode<HBoxContainer>(HorizontalTicksContainerPath);
        drawer = GetNode<LineChartDrawer>(LineContainerPath);

        drawer.Init(this);

        UpdateAxisNames();
    }

    /// <summary>
    ///   Plots the chart from available data sets
    /// </summary>
    public void Plot()
    {
        if (XAxisTicks <= 0 || YAxisTicks <= 0)
        {
            GD.PrintErr("Ticks has to be more than 0, aborting plotting data");
            return;
        }

        ClearTicksLabels();
        drawer.ClearPoints();

        // Clear tooltips created from previous plot
        cachedMarkerToolTips.ForEach(tip => ToolTipManager.Instance.RemoveToolTip(tip.Name, "chartMarkers"));

        foreach (var data in DataSets)
        {
            foreach (var point in data.PointDatas)
            {
                // Find out the min/max values
                if (point.Value.x < MinValues.x)
                {
                    MinValues = new Vector2(point.Value.x, MinValues.y);
                }

                if (point.Value.x > MaxValues.x)
                {
                    MaxValues = new Vector2(point.Value.x, MaxValues.y);
                }

                if (point.Value.y < MinValues.y)
                {
                    MinValues = new Vector2(MinValues.x, point.Value.y);
                }

                if (point.Value.y > MaxValues.y)
                {
                    MaxValues = new Vector2(MaxValues.x, point.Value.y);
                }

                // Create tooltip for the point markers
                var toolTip = (DefaultToolTip)toolTipScene.Instance();

                toolTip.DisplayName = data.Name + point.Value;
                toolTip.DisplayDelay = 0;
                toolTip.Description = $"{point.Value.x} {XAxisName}\n{point.Value.y} {YAxisName}";

                ToolTipManager.Instance.AddToolTip(toolTip, "chartMarkers");
                cachedMarkerToolTips.Add(toolTip);

                ToolTipHelper.RegisterToolTipForControl(point, toolTipCallbacks, toolTip);
            }
        }

        // Populate the rows
        for (int i = 0; i < XAxisTicks; i++)
        {
            var label = new Label();

            label.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            label.Align = Label.AlignEnum.Center;

            label.Text = Mathf.Round(i * (MaxValues.x - MinValues.x) /
                (XAxisTicks - 1) + MinValues.x).ToString(CultureInfo.CurrentCulture);

            horizontalLabelsContainer.AddChild(label);
        }

        // Populate the columns
        for (int i = YAxisTicks; i-- > 0;)
        {
            var label = new Label();

            label.SizeFlagsVertical = (int)SizeFlags.ExpandFill;
            label.Align = Label.AlignEnum.Center;
            label.Valign = Label.VAlign.Center;

            label.Text = Mathf.Round(i * (MaxValues.y - MinValues.y) /
                (YAxisTicks - 1) + MinValues.y).ToString(CultureInfo.CurrentCulture);

            verticalLabelsContainer.AddChild(label);
        }

        drawer.Update();
    }

    private void ClearTicksLabels()
    {
        // Abscissa
        foreach (Node child in horizontalLabelsContainer.GetChildren())
        {
            child.QueueFree();
        }

        // Ordinate
        foreach (Node child in verticalLabelsContainer.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void UpdateAxisNames()
    {
        if (horizontalLabel == null || verticalLabel == null)
            return;

        horizontalLabel.Text = xAxisName;
        verticalLabel.Text = yAxisName;
    }
}
