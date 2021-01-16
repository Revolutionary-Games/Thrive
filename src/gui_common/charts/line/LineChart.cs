using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Array = Godot.Collections.Array;
using DataSetDictionary = System.Collections.Generic.Dictionary<string, LineChartData>;

/// <summary>
///   A custom widget for multi-line chart with hoverable data points tooltip. Uses <see cref="LineChartData"/>
///   as dataset; currently only support numerical datas.
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
    public NodePath DrawAreaPath;

    [Export]
    public NodePath LegendsContainerPath;

    /// <summary>
    ///   The name identifier for this chart. Each chart instance should have a unique name.
    /// </summary>
    [Export]
    public string ChartName;

    /// <summary>
    ///   Number of scales to represent x axis values
    /// </summary>
    [Export]
    public int XAxisTicks = 3;

    /// <summary>
    ///   Number of scales to represent y axis values
    /// </summary>
    [Export]
    public int YAxisTicks = 3;

    [Export]
    public LegendDisplayMode LegendMode = LegendDisplayMode.Icon;

    /// <summary>
    ///   Limits how many icon legend should be shown
    /// </summary>
    public int MaxIconLegend = 10;

    /// <summary>
    ///   Limits how many dataset lines should be allowed to be shown on the chart.
    /// </summary>
    public int MaxDisplayedDataSet = 3;

    /// <summary>
    ///   Fallback icon for the legend display mode using icons
    /// </summary>
    private Texture defaultIconLegendTexture;

    private Texture hLineTexture;

    // ReSharper disable once NotAccessedField.Local
    private Texture vLineTexture;

    private Label horizontalLabel;
    private Label verticalLabel;
    private VBoxContainer verticalLabelsContainer;
    private HBoxContainer horizontalLabelsContainer;
    private Control drawArea;
    private HBoxContainer legendContainer;

    private string xAxisName;
    private string yAxisName;

    private List<ToolTipCallbackData> toolTipCallbacks = new List<ToolTipCallbackData>();

    /// <summary>
    ///   Datasets to be plotted on the chart. Key is the dataset's name
    /// </summary>
    private DataSetDictionary dataSets = new DataSetDictionary();

    /// <summary>
    ///   Modes on how the chart legend should be displayed
    /// </summary>
    public enum LegendDisplayMode
    {
        /// <summary>
        ///   Legend will be displayed as rows of toggleable icons
        /// </summary>
        Icon,

        /// <summary>
        ///   Legend will be displayed as a dropdown button with a list of toggleable items
        /// </summary>
        DropDown,
    }

    /// <summary>
    ///   The lowest data point value from all the datasets.
    /// </summary>
    public Vector2 MinValues { get; private set; }

    /// <summary>
    ///   The highest data point value from all the datasets.
    /// </summary>
    public Vector2 MaxValues { get; private set; }

    public string YAxisName
    {
        get => yAxisName;
        private set
        {
            yAxisName = value;
            UpdateAxesName();
        }
    }

    public string XAxisName
    {
        get => xAxisName;
        private set
        {
            xAxisName = value;
            UpdateAxesName();
        }
    }

    /// <summary>
    ///   Returns true if the number of shown datasets is more than the maximum allowed.
    /// </summary>
    public bool VisibleDataSetLimitReached
    {
        get
        {
            var count = 0;

            foreach (var data in dataSets)
            {
                if (data.Value.Draw)
                    count++;
            }

            return count >= MaxDisplayedDataSet;
        }
    }

    public override void _Ready()
    {
        if (string.IsNullOrEmpty(ChartName))
            throw new Exception("Chart name must not be unset");

        horizontalLabel = GetNode<Label>(HorizontalLabelPath);
        verticalLabel = GetNode<Label>(VerticalLabelPath);
        verticalLabelsContainer = GetNode<VBoxContainer>(VerticalTicksContainerPath);
        horizontalLabelsContainer = GetNode<HBoxContainer>(HorizontalTicksContainerPath);
        drawArea = GetNode<Control>(DrawAreaPath);
        legendContainer = GetNode<HBoxContainer>(LegendsContainerPath);
        defaultIconLegendTexture = GD.Load<Texture>("res://assets/textures/gui/bevel/blankCircle.png");
        hLineTexture = GD.Load<Texture>("res://assets/textures/gui/bevel/hSeparatorCentered.png");
        vLineTexture = GD.Load<Texture>("res://assets/textures/gui/bevel/vSeparatorUp.png");

        UpdateAxesName();
    }

    /// <summary>
    ///   Add a dataset into this chart (overwrites existing one if the name already existed)
    /// </summary>
    public void AddDataSet(string name, LineChartData dataset)
    {
        dataSets[name] = dataset;
    }

    public LineChartData GetDataSet(string name)
    {
        if (!dataSets.ContainsKey(name))
        {
            GD.PrintErr("DataSet with name '" + name + "' not found");
            return null;
        }

        return dataSets[name];
    }

    public void ClearDataSets()
    {
        foreach (var dataset in dataSets)
        {
            dataset.Value.ClearPoints();
        }

        dataSets.Clear();
    }

    /// <summary>
    ///   Plots the chart from available datasets
    /// </summary>
    /// <param name="xAxisName">The horizontal axis label title</param>
    /// <param name="yAxisName">The vertical axis label title</param>
    /// <param name="legendTitle">Title for the chart legend. If null, the legend will not be created</param>
    public void Plot(string xAxisName, string yAxisName, string legendTitle = null)
    {
        ClearChart();

        if (dataSets == null || dataSets.Count <= 0)
        {
            GD.PrintErr(ChartName + " chart missing datasets, aborting plotting data");
            return;
        }

        if (XAxisTicks <= 0 || YAxisTicks <= 0)
        {
            GD.PrintErr(ChartName + " chart ticks has to be more than 0, aborting plotting data");
            return;
        }

        XAxisName = xAxisName;
        YAxisName = yAxisName;

        var dataSetCount = 0;

        // Used to find min/max value of the data points
        var totalDataPoints = new List<Vector2>();

        foreach (var data in dataSets)
        {
            dataSetCount++;

            // Hide the rest if number of shown dataset is more than maximum allowed.
            if (dataSetCount > MaxDisplayedDataSet)
                UpdateDataSetVisibility(data.Key, false);

            foreach (var point in data.Value.DataPoints)
            {
                totalDataPoints.Add(point.Value);

                // Create tooltip for the point markers
                var toolTip = ToolTipHelper.CreateDefaultToolTip();

                toolTip.DisplayName = data.Key + point.Value;
                toolTip.Description = $"{((double)point.Value.x).FormatNumber()} {XAxisName}\n" +
                    $"{((double)point.Value.y).FormatNumber()} {YAxisName}";
                toolTip.DisplayDelay = 0;
                toolTip.HideOnMousePress = false;
                toolTip.UseFadeIn = false;

                point.RegisterToolTipForControl(toolTip, toolTipCallbacks);
                ToolTipManager.Instance.AddToolTip(toolTip, "chartMarkers" + ChartName + data.Key);
            }
        }

        if (totalDataPoints.Count <= 0)
        {
            GD.PrintErr("Missing data points to plot");
            return;
        }

        // Find out value boundaries
        MaxValues = new Vector2(totalDataPoints.Max(point => point.x), totalDataPoints.Max(point => point.y));
        MinValues = new Vector2(totalDataPoints.Min(point => point.x), totalDataPoints.Min(point => point.y));

        // Can't have min/max values to be equal. Set a value to zero as the initial point
        if (MinValues.x == MaxValues.x)
        {
            if (MaxValues.x > 0)
            {
                MinValues = new Vector2(0, MinValues.y);
            }
            else if (MaxValues.x < 0)
            {
                MaxValues = new Vector2(0, MaxValues.y);
            }
        }

        if (MinValues.y == MaxValues.y)
        {
            if (MaxValues.y > 0)
            {
                MinValues = new Vector2(MinValues.x, 0);
            }
            else if (MaxValues.y < 0)
            {
                MaxValues = new Vector2(MaxValues.x, 0);
            }
        }

        // Populate the rows
        for (int i = 0; i < XAxisTicks; i++)
        {
            var label = new Label
            {
                SizeFlagsHorizontal = (int)SizeFlags.ExpandFill,
                Align = Label.AlignEnum.Center,
            };

            label.Text = Math.Round(
                i * (MaxValues.x - MinValues.x) / (XAxisTicks - 1) + MinValues.x, 1).FormatNumber();

            horizontalLabelsContainer.AddChild(label);
        }

        // Populate the columns (in reverse order)
        for (int i = YAxisTicks; i-- > 0;)
        {
            var label = new Label
            {
                SizeFlagsVertical = (int)SizeFlags.ExpandFill,
                Align = Label.AlignEnum.Center,
                Valign = Label.VAlign.Center,
            };

            label.Text = Math.Round(
                i * (MaxValues.y - MinValues.y) / (YAxisTicks - 1) + MinValues.y, 1).FormatNumber();

            verticalLabelsContainer.AddChild(label);
        }

        // Create chart legend
        if (!string.IsNullOrEmpty(legendTitle))
        {
            // Switch to dropdown if amount of dataset is more than maximum number of icon legends allowed
            if (dataSets.Count > MaxIconLegend && LegendMode == LegendDisplayMode.Icon)
                LegendMode = LegendDisplayMode.DropDown;

            switch (LegendMode)
            {
                case LegendDisplayMode.Icon:
                    CreateIconLegend(legendTitle);
                    break;
                case LegendDisplayMode.DropDown:
                    CreateDropDownLegend(legendTitle);
                    break;
                default:
                    throw new Exception("Invalid legend display mode");
            }
        }

        drawArea.Update();
    }

    public void ClearChart()
    {
        toolTipCallbacks.Clear();

        foreach (var data in dataSets)
        {
            ToolTipManager.Instance.ClearToolTips("chartMarkers" + ChartName + data.Key);
            ToolTipManager.Instance.ClearToolTips("chartLegend" + ChartName + data.Key);
        }

        // Clear points
        drawArea.QueueFreeChildren();

        // Clear legend
        legendContainer.QueueFreeChildren();

        // Clear abscissas
        horizontalLabelsContainer.QueueFreeChildren();

        // Clear ordinates
        verticalLabelsContainer.QueueFreeChildren();
    }

    public void UpdateDataSetVisibility(string name, bool visible)
    {
        dataSets[name].Draw = visible;
        drawArea.Update();
    }

    private void CreateIconLegend(string title)
    {
        _ = title;

        foreach (var data in dataSets)
        {
            var fallbackIconIsUsed = false;

            // Use the default icon as a fallback if the data icon texture hasn't been set already
            if (data.Value.IconTexture == null)
            {
                data.Value.IconTexture = defaultIconLegendTexture;
                fallbackIconIsUsed = true;
            }

            var icon = new TextureButton
            {
                Expand = true,
                RectMinSize = new Vector2(18, 18),
                EnabledFocusMode = FocusModeEnum.None,
                ToggleMode = true,
                Pressed = true,
                Name = data.Key,
                TextureNormal = data.Value.IconTexture,
                StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
            };

            // Set the default icon's color
            if (fallbackIconIsUsed)
                icon.Modulate = data.Value.DataColour;

            legendContainer.AddChild(icon);

            icon.Connect("mouse_entered", this, nameof(IconLegendMouseEnter), new Array
            {
                icon, fallbackIconIsUsed, data.Value.DataColour,
            });
            icon.Connect("mouse_exited", this, nameof(IconLegendMouseExit), new Array
            {
                icon, fallbackIconIsUsed, data.Value.DataColour,
            });
            icon.Connect("toggled", this, nameof(IconLegendToggled), new Array
            {
                icon, data.Key, fallbackIconIsUsed,
            });

            // Set initial icon toggle state
            if (!data.Value.Draw)
            {
                icon.Pressed = false;
                IconLegendToggled(false, icon, data.Key, fallbackIconIsUsed);
            }

            // Create tooltips
            var toolTip = ToolTipHelper.CreateDefaultToolTip();

            toolTip.DisplayName = data.Key;
            toolTip.Description = data.Key;

            icon.RegisterToolTipForControl(toolTip, toolTipCallbacks);
            ToolTipManager.Instance.AddToolTip(toolTip, "chartLegend" + ChartName + data.Key);
        }
    }

    private void CreateDropDownLegend(string title)
    {
        var dropDown = new CustomDropDown
        {
            Flat = false,
            Text = title,
            EnabledFocusMode = FocusModeEnum.None,
        };

        var itemId = 0;

        dropDown.Popup.HideOnCheckableItemSelection = false;

        foreach (var data in dataSets)
        {
            // Use the default icon as a fallback if the data icon texture hasn't been set already
            data.Value.IconTexture = data.Value.IconTexture == null ?
                defaultIconLegendTexture :
                data.Value.IconTexture;

            // Use the DataColor as the icon's color if using the default icon
            var colorToUse = data.Value.IconTexture == defaultIconLegendTexture ?
                data.Value.DataColour :
                new Color(1, 1, 1);

            dropDown.AddItem(data.Key, itemId, true, colorToUse, data.Value.IconTexture);

            // Set initial item check state
            if (data.Value.Draw)
                dropDown.Popup.SetItemChecked(dropDown.Popup.GetItemIndex(itemId), true);

            itemId++;
        }

        legendContainer.AddChild(dropDown);

        dropDown.Popup.Connect("index_pressed", this, nameof(DropDownLegendItemSelected),
            new Array { dropDown });
    }

    /// <summary>
    ///   Draw the chart visuals. Mainly used by the Drawer node to connect its 'draw()' signal here.
    /// </summary>
    private void RenderChart()
    {
        if (dataSets.Count <= 0)
            return;

        DrawOrdinateLines();
        DrawLineSegments();
    }

    /// <summary>
    ///   Draw columns of lines going horizontal
    /// </summary>
    private void DrawOrdinateLines()
    {
        foreach (Control tick in verticalLabelsContainer.GetChildren())
        {
            drawArea.DrawTextureRect(hLineTexture, new Rect2(new Vector2(
                    0, tick.RectPosition.y + (tick.RectSize.y / 2)), drawArea.RectSize.x, 1), false,
                new Color(1, 1, 1, 0.3f));
        }
    }

    /// <summary>
    ///   Connect the dataset points with line segments
    /// </summary>
    private void DrawLineSegments()
    {
        foreach (var data in dataSets)
        {
            var points = data.Value.DataPoints;

            if (points.Count <= 0)
                continue;

            // Setup the points
            foreach (var point in points)
            {
                // Skip if any of the min and max value is equal, otherwise
                // the data point marker kind of just glitch out.
                if (MinValues.x == MaxValues.x || MinValues.y == MaxValues.y)
                    continue;

                point.Coordinate = ConvertToCoordinate(point.Value);

                if (!point.IsInsideTree())
                    drawArea.AddChild(point);
            }

            var previousPoint = points.First();

            // Draw the lines
            foreach (var point in points)
            {
                if (data.Value.Draw)
                {
                    drawArea.DrawLine(previousPoint.Coordinate, point.Coordinate, data.Value.DataColour,
                        data.Value.LineWidth, true);
                }

                previousPoint = point;
            }
        }
    }

    /// <summary>
    ///   Helper method for converting a single point data value into a coordinate.
    /// </summary>
    /// <returns>Position of the given value on the chart</returns>
    private Vector2 ConvertToCoordinate(Vector2 value)
    {
        return new Vector2(ConvertToXCoordinate(value.x), ConvertToYCoordinate(value.y));
    }

    private float ConvertToXCoordinate(float value)
    {
        var lineRectX = drawArea.RectSize.x / XAxisTicks;
        var lineRectWidth = lineRectX * (XAxisTicks - 1);
        var dx = MaxValues.x - MinValues.x;

        return ((value - MinValues.x) * lineRectWidth / dx) + lineRectX / 2;
    }

    private float ConvertToYCoordinate(float value)
    {
        var lineRectY = drawArea.RectSize.y / YAxisTicks;
        var lineRectHeight = lineRectY * (YAxisTicks - 1);
        var dy = MaxValues.y - MinValues.y;

        return lineRectHeight - ((value - MinValues.y) * lineRectHeight / dy) + lineRectY / 2;
    }

    private void UpdateAxesName()
    {
        if (horizontalLabel == null || verticalLabel == null)
            return;

        horizontalLabel.Text = xAxisName;
        verticalLabel.Text = yAxisName;
    }

    /*
        GUI Callbacks
    */

    private void IconLegendMouseEnter(TextureButton icon, bool fallbackIconIsUsed, Color dataColor)
    {
        if (icon.Pressed)
        {
            // Adjust the icon color to be highlighted
            icon.Modulate = fallbackIconIsUsed ? dataColor.Lightened(0.5f) : new Color(0.7f, 0.7f, 0.7f);
        }
    }

    private void IconLegendMouseExit(TextureButton icon, bool fallbackIconIsUsed, Color dataColor)
    {
        if (icon.Pressed)
        {
            // Adjust the icon color back to normal
            icon.Modulate = fallbackIconIsUsed ? dataColor : Colors.White;
        }
    }

    private void IconLegendToggled(bool toggled, TextureButton icon, string name, bool fallbackIconIsUsed)
    {
        if (toggled && VisibleDataSetLimitReached)
        {
            icon.Pressed = false;
            return;
        }

        var data = dataSets[name];

        if (fallbackIconIsUsed)
        {
            icon.Modulate = toggled ? data.DataColour : data.DataColour.Darkened(0.5f);
        }
        else
        {
            icon.Modulate = toggled ? Colors.White : Colors.Gray;
        }

        UpdateDataSetVisibility(name, toggled);
    }

    private void DropDownLegendItemSelected(int index, CustomDropDown dropDown)
    {
        var name = dropDown.Popup.GetItemText(index);

        if (!dropDown.Popup.IsItemChecked(index) && VisibleDataSetLimitReached)
            return;

        dropDown.Popup.ToggleItemChecked(index);

        UpdateDataSetVisibility(name, dropDown.Popup.IsItemChecked(index));
    }
}
