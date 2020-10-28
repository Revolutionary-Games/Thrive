using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Array = Godot.Collections.Array;

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

    [Export]
    public NodePath LegendsContainerPath;

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

    [Export]
    public LegendDisplayMode DisplayedLegendMode = LegendDisplayMode.Icon;

    private int maxLegendIcons = 5;

    private Label horizontalLabel;
    private Label verticalLabel;
    private VBoxContainer verticalLabelsContainer;
    private HBoxContainer horizontalLabelsContainer;
    private LineChartDrawer drawer;
    private HBoxContainer legendsContainer;

    private string xAxisName;
    private string yAxisName;

    /// <summary>
    ///   To easily delete the chart tooltips, we store them here immediately after creating them
    /// </summary>
    private List<DefaultToolTip> cachedToolTips = new List<DefaultToolTip>();

    private List<ToolTipCallbackData> toolTipCallbacks = new List<ToolTipCallbackData>();

    public enum LegendDisplayMode
    {
        Icon,
        DropDown,
    }

    /// <summary>
    ///   Datas to be plotted
    /// </summary>
    public List<LineChartData> DataSets { get; set; } = new List<LineChartData>();

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

    public override void _Ready()
    {
        horizontalLabel = GetNode<Label>(HorizontalLabelPath);
        verticalLabel = GetNode<Label>(VerticalLabelPath);
        verticalLabelsContainer = GetNode<VBoxContainer>(VerticalTicksContainerPath);
        horizontalLabelsContainer = GetNode<HBoxContainer>(HorizontalTicksContainerPath);
        drawer = GetNode<LineChartDrawer>(LineContainerPath);
        legendsContainer = GetNode<HBoxContainer>(LegendsContainerPath);

        drawer.Init(this);

        UpdateAxisNames();

        // For testing purposes
        var tempData = new LineChartData()
        {
            PointDatas = new List<ChartPoint>
            {
                new ChartPoint(0, 0),
                new ChartPoint(20, 50),
                new ChartPoint(50, 80),
                new ChartPoint(100, 0),
                new ChartPoint(200, 50),
                new ChartPoint(300, 80),
            },
            Name = "Glucose",
            LegendIcon = GUICommon.Instance.GetCompoundIcon("Glucose"),
            LineColor = new Color(1, 1, 1),
        };

        var lightData = new LineChartData()
        {
            PointDatas = new List<ChartPoint>
            {
                new ChartPoint(15, 0),
                new ChartPoint(40, 20),
                new ChartPoint(70, 17),
                new ChartPoint(180, 65),
                new ChartPoint(250, 43),
                new ChartPoint(300, 0, 12, ChartPoint.MarkerIcon.Cross),
            },
            Name = "Ammonia",
            LegendIcon = GUICommon.Instance.GetCompoundIcon("Ammonia"),
            LineColor = new Color(0.63f, 0.4f, 0.0f),
            Draw = false,
        };

        var nitrogenData = new LineChartData()
        {
            PointDatas = new List<ChartPoint>
            {
                new ChartPoint(23, 20),
                new ChartPoint(50, 50),
                new ChartPoint(100, 10),
                new ChartPoint(160, 5),
                new ChartPoint(260, 30),
                new ChartPoint(300, 70),
            },
            Name = "Phosphate",
            LegendIcon = GUICommon.Instance.GetCompoundIcon("Phosphate"),
            LineColor = new Color(1, 1, 1),
        };

        DataSets.Add(tempData);
        DataSets.Add(lightData);
        DataSets.Add(nitrogenData);
        Plot();
        CreateLegends("Compounds");
    }

    /// <summary>
    ///   Plots the chart from available data sets
    /// </summary>
    /// <param name="legends">Creates data legends if set true</param>
    public void Plot()
    {
        if (DataSets.Count <= 0)
        {
            GD.PrintErr("Missing data sets, aborting plotting");
            return;
        }

        if (XAxisTicks <= 0 || YAxisTicks <= 0)
        {
            GD.PrintErr("Ticks has to be more than 0, aborting plotting data");
            return;
        }

        ClearTicksLabels();
        drawer.ClearPoints();

        // Clear legends
        foreach (Node child in legendsContainer.GetChildren())
            child.QueueFree();

        // Clear tooltips created from previous plot
        cachedToolTips.ForEach(tip => ToolTipManager.Instance.RemoveToolTip(tip.Name, "chartMarkers"));

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
                var toolTip = ToolTipHelper.CreateDefaultToolTip();

                toolTip.DisplayName = data.Name + point.Value;
                toolTip.Description = $"{point.Value.x} {XAxisName}\n{point.Value.y} {YAxisName}";
                toolTip.DisplayDelay = 0;

                ToolTipHelper.RegisterToolTipForControl(point, toolTipCallbacks, toolTip);
                ToolTipManager.Instance.AddToolTip(toolTip, "chartMarkers");
                cachedToolTips.Add(toolTip);
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

    private void CreateLegends(string title)
    {
        if (DataSets.Count > maxLegendIcons && DisplayedLegendMode == LegendDisplayMode.Icon)
            DisplayedLegendMode = LegendDisplayMode.DropDown;

        switch (DisplayedLegendMode)
        {
            case LegendDisplayMode.Icon:
            {
                foreach (var data in DataSets)
                {
                    if (data.LegendIcon == null)
                        continue;

                    var icon = new TextureButton()
                    {
                        Expand = true,
                        RectMinSize = new Vector2(18, 18),
                        EnabledFocusMode = FocusModeEnum.None,
                        ToggleMode = true,
                        Pressed = false,
                        Name = data.Name,
                        TextureNormal = data.LegendIcon,
                    };

                    legendsContainer.AddChild(icon);

                    icon.Connect("mouse_entered", this, nameof(LegendIconMouseEnter), new Array { icon });
                    icon.Connect("mouse_exited", this, nameof(LegendIconMouseExit), new Array { icon });
                    icon.Connect("toggled", this, nameof(LegendIconToggled), new Array { icon, data.Name });

                    if (!data.Draw)
                    {
                        icon.Pressed = true;
                        LegendIconToggled(true, icon, data.Name);
                    }

                    // Create tooltips
                    var toolTip = ToolTipHelper.CreateDefaultToolTip();

                    toolTip.DisplayName = data.Name;
                    toolTip.Description = data.Name;

                    ToolTipHelper.RegisterToolTipForControl(icon, toolTipCallbacks, toolTip);
                    ToolTipManager.Instance.AddToolTip(toolTip, "chartLegends");
                    cachedToolTips.Add(toolTip);
                }

                break;
            }

            case LegendDisplayMode.DropDown:
            {
                var button = new MenuButton()
                {
                    Flat = false,
                    Text = title,
                    EnabledFocusMode = FocusModeEnum.None,
                    RectMinSize = new Vector2(150, 25),
                };

                var popupMenu = button.GetPopup();
                var itemId = 0;

                popupMenu.HideOnCheckableItemSelection = false;

                foreach (var data in DataSets)
                {
                    popupMenu.AddCheckItem(data.Name, itemId);

                    if (data.Draw)
                        popupMenu.SetItemChecked(popupMenu.GetItemIndex(itemId), true);

                    itemId++;
                }

                legendsContainer.AddChild(button);

                popupMenu.Connect("index_pressed", this, nameof(LegendDropDownSelected), new Array { button });

                break;
            }

            default:
                throw new Exception("Invalid legend display mode");
        }
    }

    private void LegendIconMouseEnter(TextureButton icon)
    {
        if (!icon.Pressed)
            icon.Modulate = new Color(0.7f, 0.7f, 0.7f);
    }

    private void LegendIconMouseExit(TextureButton icon)
    {
        if (!icon.Pressed)
            icon.Modulate = new Color(1, 1, 1);
    }

    private void LegendIconToggled(bool active, TextureButton icon, string name)
    {
        var dataSet = DataSets.Find(match => match.Name == name);

        if (!active)
        {
            icon.Modulate = new Color(1, 1, 1);

            dataSet.Draw = true;
            drawer.Update();
        }
        else
        {
            icon.Modulate = new Color(0.5f, 0.5f, 0.5f);

            dataSet.Draw = false;
            drawer.Update();
        }
    }

    private void LegendDropDownSelected(int index, MenuButton button)
    {
        var name = button.GetPopup().GetItemText(index);

        var dataSet = DataSets.Find(match => match.Name == name);

        button.GetPopup().ToggleItemChecked(index);

        dataSet.Draw = button.GetPopup().IsItemChecked(index);
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
