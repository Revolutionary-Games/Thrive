using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Array = Godot.Collections.Array;

/// <summary>
///   Custom widget for plotting data points on a line.
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
    public LegendDisplayMode LegendMode = LegendDisplayMode.Icon;

    /// <summary>
    ///   Limits how many icon legend shown
    /// </summary>
    public int MaxIconLegend = 10;

    /// <summary>
    ///   Limits how many dataset should be allowed to be shown on the chart.
    /// </summary>
    public int MaxDisplayedDataSet = 3;

    /// <summary>
    ///   Fallback icon for the legend display mode using icons
    /// </summary>
    private Texture defaultIconLegendTexture;

    private Texture hLineTexture;

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

    // ReSharper disable once RedundantNameQualifier
    /// <summary>
    ///   Datasets to be plotted on the chart. Key is the dataset's name
    /// </summary>
    public System.Collections.Generic.Dictionary<string, LineChartData> DataSets { get; set; } =
        new System.Collections.Generic.Dictionary<string, LineChartData>();

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

    public override void _Ready()
    {
        horizontalLabel = GetNode<Label>(HorizontalLabelPath);
        verticalLabel = GetNode<Label>(VerticalLabelPath);
        verticalLabelsContainer = GetNode<VBoxContainer>(VerticalTicksContainerPath);
        horizontalLabelsContainer = GetNode<HBoxContainer>(HorizontalTicksContainerPath);
        drawArea = GetNode<Control>(DrawAreaPath);
        legendContainer = GetNode<HBoxContainer>(LegendsContainerPath);
        defaultIconLegendTexture = GD.Load<Texture>("res://assets/textures/gui/bevel/blankCircle.png");
        hLineTexture = GD.Load<Texture>("res://assets/textures/gui/bevel/hSeparatorCentered.png");

        UpdateAxesName();
    }

    public override void _Draw()
    {
        if (DataSets.Count <= 0)
            return;

        DrawSetTransform(new Vector2(drawArea.RectPosition.x, (RectSize.y - drawArea.RectSize.y) -
            verticalLabel.RectSize.y), drawArea.RectRotation, drawArea.RectScale);

        DrawOrdinateLines();
        DrawLineSegments();
    }

    /// <summary>
    ///   Plots the chart from available datasets
    /// </summary>
    public void Plot()
    {
        if (DataSets.Count <= 0)
        {
            GD.PrintErr("Missing data sets, aborting plotting data");
            return;
        }

        if (XAxisTicks <= 0 || YAxisTicks <= 0)
        {
            GD.PrintErr("Ticks has to be more than 0, aborting plotting data");
            return;
        }

        ClearPoints();

        ToolTipManager.Instance.ClearToolTips("chartMarkers");

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

        var dataSetCount = 0;

        foreach (var data in DataSets)
        {
            dataSetCount++;

            // Hide the rest of the dataset if the amount is more than max number to be displayed
            if (dataSetCount > MaxDisplayedDataSet)
                UpdateDataSetVisibility(data.Key, false);

            foreach (var point in data.Value.Points)
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
                ToolTipManager.Instance.AddToolTip(toolTip, "chartMarkers");
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

        // Populate the columns (in reverse order)
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

        RenderChart();
    }

    public void CreateLegend(string title)
    {
        foreach (Node child in legendContainer.GetChildren())
            child.QueueFree();

        ToolTipManager.Instance.ClearToolTips("chartLegends");

        // Switch to dropdown if amount of dataset is more than maximum number of icon legends allowed
        if (DataSets.Count > MaxIconLegend && LegendMode == LegendDisplayMode.Icon)
            LegendMode = LegendDisplayMode.DropDown;

        switch (LegendMode)
        {
            case LegendDisplayMode.Icon:
            {
                foreach (var data in DataSets)
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
                        icon.Modulate = data.Value.DataColor;

                    legendContainer.AddChild(icon);

                    icon.Connect("mouse_entered", this, nameof(IconLegendMouseEnter), new Array
                    {
                        icon, fallbackIconIsUsed, data.Value.DataColor,
                    });
                    icon.Connect("mouse_exited", this, nameof(IconLegendMouseExit), new Array
                    {
                        icon, fallbackIconIsUsed, data.Value.DataColor,
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
                    ToolTipManager.Instance.AddToolTip(toolTip, "chartLegends");
                }

                break;
            }

            case LegendDisplayMode.DropDown:
            {
                var dropDown = new CustomDropDown
                {
                    Flat = false,
                    Text = title,
                    EnabledFocusMode = FocusModeEnum.None,
                };

                var itemId = 0;

                dropDown.Popup.HideOnCheckableItemSelection = false;

                foreach (var data in DataSets)
                {
                    // Use the default icon as a fallback if the data icon texture hasn't been set already
                    data.Value.IconTexture = data.Value.IconTexture == null ?
                        defaultIconLegendTexture :
                        data.Value.IconTexture;

                    // Use the DataColor as the icon's color if using the default icon
                    var colorToUse = data.Value.IconTexture == defaultIconLegendTexture ?
                        data.Value.DataColor :
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

                break;
            }

            default:
                throw new Exception("Invalid legend display mode");
        }
    }

    public void UpdateDataSetVisibility(string name, bool visible)
    {
        DataSets[name].Draw = visible;
        RenderChart();
    }

    /// <summary>
    ///   Redraws the chart. This method is mainly used by the draw area node to connect its "draw()"
    ///   signal to this for requesting a redraw (since it can't be connected directly with "Update()")
    /// </summary>
    private void RenderChart()
    {
        Update();
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

            DrawTextureRect(hLineTexture, new Rect2(new Vector2(
                0, ConvertToYCoordinate(value)), RectSize.x, 1), false, new Color(1, 1, 1, 0.5f));
        }
    }

    /// <summary>
    ///   Connect the dataset points
    /// </summary>
    private void DrawLineSegments()
    {
        foreach (var data in DataSets)
        {
            // Assign coordinate of the dataset points
            data.Value.Points.ForEach(point =>
            {
                point.Coordinate = ConvertToCoordinate(point.Value);

                if (!point.IsInsideTree())
                    drawArea.AddChild(point);
            });

            var previousPoint = data.Value.Points.First();

            // Draw the lines
            foreach (var point in data.Value.Points)
            {
                if (data.Value.Draw)
                {
                    DrawLine(previousPoint.Coordinate, point.Coordinate, data.Value.DataColor,
                        data.Value.LineWidth, true);
                }

                previousPoint = point;
            }
        }
    }

    /// <summary>
    ///   Helper method for converting a single point data value into a coordinate.
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

    /// <summary>
    ///   Returns true if the number of datasets is more than the allowed maximum amount displayed.
    /// </summary>
    private bool CheckMaxDataSetShown()
    {
        var count = 0;

        foreach (var data in DataSets)
        {
            if (data.Value.Draw)
                count++;
        }

        return count >= MaxDisplayedDataSet;
    }

    /// <summary>
    ///   Deletes all point markers
    /// </summary>
    private void ClearPoints()
    {
        foreach (Node child in drawArea.GetChildren())
        {
            child.QueueFree();
        }
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
        if (toggled && CheckMaxDataSetShown())
        {
            icon.Pressed = false;
            return;
        }

        var data = DataSets[name];

        if (fallbackIconIsUsed)
        {
            icon.Modulate = toggled ?
                data.DataColor :
                new Color(
                    data.DataColor.r - 0.5f, data.DataColor.g - 0.5f, data.DataColor.b - 0.5f);
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

        if (!dropDown.Popup.IsItemChecked(index) && CheckMaxDataSetShown())
            return;

        dropDown.Popup.ToggleItemChecked(index);

        UpdateDataSetVisibility(name, dropDown.Popup.IsItemChecked(index));
    }
}
