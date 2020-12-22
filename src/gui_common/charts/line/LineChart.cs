using System;
using System.Collections.Generic;
using System.Globalization;
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
    ///   The name identifier for this chart. Each chart instance should have unique name.
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
    ///   Limits how many icon legend shown
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

    public Vector2 MinValues { get; private set; }

    public Vector2 MaxValues { get; private set; }

    [Export]
    public string YAxisName
    {
        get => yAxisName;
        set
        {
            yAxisName = value;
            UpdateAxesName();
        }
    }

    [Export]
    public string XAxisName
    {
        get => xAxisName;
        set
        {
            xAxisName = value;
            UpdateAxesName();
        }
    }

    /// <summary>
    ///   Returns true if the number of datasets is more than the allowed maximum amount displayed.
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

    /// <summary>
    ///   Plots the chart from available datasets
    /// </summary>
    /// <param name="legendTitle">Title for the chart legend. If null, the legend will not be created</param>
    public void Plot(string legendTitle = null)
    {
        if (dataSets == null || dataSets.Count <= 0)
        {
            GD.PrintErr("Missing data sets, aborting plotting data");
            return;
        }

        if (XAxisTicks <= 0 || YAxisTicks <= 0)
        {
            GD.PrintErr("Ticks has to be more than 0, aborting plotting data");
            return;
        }

        ClearChart();

        var dataSetCount = 0;

        foreach (var data in dataSets)
        {
            dataSetCount++;

            // Hide the rest of the dataset if the amount is more than max number to be displayed
            if (dataSetCount > MaxDisplayedDataSet)
                UpdateDataSetVisibility(data.Key, false);

            foreach (var point in data.Value.DataPoints)
            {
                // Find out value boundaries
                MaxValues = new Vector2(Mathf.Max(point.Value.x, MaxValues.x), Mathf.Max(point.Value.y, MaxValues.y));
                MinValues = new Vector2(Mathf.Min(point.Value.x, MinValues.x), Mathf.Min(point.Value.y, MinValues.y));

                // Create tooltip for the point markers
                var toolTip = ToolTipHelper.CreateDefaultToolTip();

                toolTip.DisplayName = data.Key + point.Value;
                toolTip.Description = $"{point.Value.x} {XAxisName}\n{point.Value.y} {YAxisName}";
                toolTip.DisplayDelay = 0;

                ToolTipHelper.RegisterToolTipForControl(point, toolTipCallbacks, toolTip);
                ToolTipManager.Instance.AddToolTip(toolTip, "chartMarkers" + ChartName + data.Key);
            }
        }

        // Populate the rows
        for (int i = 1; i < XAxisTicks; i++)
        {
            var label = new Label
            {
                SizeFlagsHorizontal = (int)SizeFlags.ExpandFill,
                Align = Label.AlignEnum.Right,
            };

            label.Text = Mathf.Round(i * (MaxValues.x - MinValues.x) /
                (XAxisTicks - 1) + MinValues.x).ToString(CultureInfo.CurrentCulture);

            horizontalLabelsContainer.AddChild(label);
        }

        // Populate the columns (in reverse order)
        for (int i = YAxisTicks; i-- > 0;)
        {
            var label = new Label
            {
                SizeFlagsVertical = (int)SizeFlags.ExpandFill,
                Align = Label.AlignEnum.Center,
                Valign = Label.VAlign.Bottom,
            };

            // Don't set the text for the 0 scale label, since there's already one on the x axis.
            // Add the label nevertheless so the vertical ticks spacings will remain consistent
            if (i > 0)
            {
                label.Text = Mathf.Round(i * (MaxValues.y - MinValues.y) /
                    (YAxisTicks - 1) + MinValues.y).ToString(CultureInfo.CurrentCulture);
            }

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
        }

        foreach (var data in dataSets)
        {
            ToolTipManager.Instance.ClearToolTips("chartLegend" + ChartName + data.Key);
        }

        // Clear points
        foreach (Node child in drawArea.GetChildren())
        {
            child.QueueFree();
        }

        // Clear legend
        foreach (Node child in legendContainer.GetChildren())
        {
            child.QueueFree();
        }

        // Clear abscissas
        foreach (Node child in horizontalLabelsContainer.GetChildren())
        {
            child.QueueFree();
        }

        // Clear ordinates
        foreach (Node child in verticalLabelsContainer.GetChildren())
        {
            child.QueueFree();
        }
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

            ToolTipHelper.RegisterToolTipForControl(icon, toolTipCallbacks, toolTip);
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
        for (int i = 0; i < YAxisTicks; i++)
        {
            var value = Mathf.Round(i * (MaxValues.y - MinValues.y) /
                (YAxisTicks - 1) + MinValues.y);

            drawArea.DrawTextureRect(hLineTexture, new Rect2(new Vector2(
                0, ConvertToYCoordinate(value)), RectSize.x, 1), false, new Color(1, 1, 1, 0.3f));
        }
    }

    /// <summary>
    ///   Connect the dataset points with line segments
    /// </summary>
    private void DrawLineSegments()
    {
        foreach (var data in dataSets)
        {
            // Setup the points
            data.Value.DataPoints.ForEach(point =>
            {
                point.Coordinate = ConvertToCoordinate(point.Value);

                if (!point.IsInsideTree())
                    drawArea.AddChild(point);

                var pointHasZeroValue = point.Value.x <= 0 || point.Value.y <= 0;

                point.Draw = pointHasZeroValue ? false : true;

                // Let the point at zero coordinates be drawn if this flag is set to true
                if (point.DrawAtZeroValue)
                {
                    point.Draw = true;

                    // Offset the marker position so it doesn't overlap the axes line

                    if (point.Value.x <= 0)
                        point.Coordinate += new Vector2(point.Size, 0);

                    if (point.Value.y <= 0)
                        point.Coordinate -= new Vector2(0, point.Size);
                }
            });

            var previousPoint = data.Value.DataPoints.First();

            // Draw the lines
            foreach (var point in data.Value.DataPoints)
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

        var lineRectWidth = lineRectX * XAxisTicks;

        var dx = MaxValues.x - MinValues.x;

        return (value - MinValues.x) * lineRectWidth / dx;
    }

    private float ConvertToYCoordinate(float value)
    {
        var lineRectY = drawArea.RectSize.y / YAxisTicks;

        var lineRectHeight = lineRectY * (YAxisTicks - 1);

        var dy = MaxValues.y - MinValues.y;

        return lineRectHeight - ((value - MinValues.y) * lineRectHeight / dy) + lineRectY;
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
            // Adjust the icon highlight color
            icon.Modulate = fallbackIconIsUsed ?
                new Color(
                    dataColor.r - 0.3f, dataColor.g - 0.3f, dataColor.b - 0.3f) :
                new Color(0.7f, 0.7f, 0.7f);
        }
    }

    private void IconLegendMouseExit(TextureButton icon, bool fallbackIconIsUsed, Color dataColor)
    {
        // Adjust the icon color to normal
        if (icon.Pressed)
            icon.Modulate = fallbackIconIsUsed ? dataColor : new Color(1, 1, 1);
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
            icon.Modulate = toggled ?
                data.DataColour :
                new Color(
                    data.DataColour.r - 0.5f, data.DataColour.g - 0.5f, data.DataColour.b - 0.5f);
        }
        else
        {
            icon.Modulate = toggled ? new Color(1, 1, 1) : new Color(0.5f, 0.5f, 0.5f);
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
