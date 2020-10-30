using System;
using System.Collections.Generic;
using System.Globalization;
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
    public NodePath DrawerPath;

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

    private Label horizontalLabel;
    private Label verticalLabel;
    private VBoxContainer verticalLabelsContainer;
    private HBoxContainer horizontalLabelsContainer;
    private LineChartDrawer drawer;
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

    // ReSharper disable once CollectionNeverUpdated.Global
    /// <summary>
    ///   Datasets to be plotted on the chart
    /// </summary>
    public Dictionary<string, LineChartData> DataSets { get; set; } = new Dictionary<string, LineChartData>();

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
        drawer = GetNode<LineChartDrawer>(DrawerPath);
        legendContainer = GetNode<HBoxContainer>(LegendsContainerPath);
        defaultIconLegendTexture = GD.Load<Texture>("res://assets/textures/gui/bevel/blankCircle.png");

        drawer.Init(this);

        UpdateAxesName();
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

        drawer.ClearPoints();

        ToolTipManager.Instance.ClearToolTip("chartMarkers");

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
                // Find out boundaries
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

        drawer.Update();
    }

    public void CreateLegend(string title)
    {
        foreach (Node child in legendContainer.GetChildren())
            child.QueueFree();

        ToolTipManager.Instance.ClearToolTip("chartLegends");

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

                    icon.Connect("mouse_entered", this, nameof(IconLegendMouseEnter), new Array {
                        icon, fallbackIconIsUsed, data.Value.DataColor });
                    icon.Connect("mouse_exited", this, nameof(IconLegendMouseExit), new Array {
                        icon, fallbackIconIsUsed, data.Value.DataColor });
                    icon.Connect("toggled", this, nameof(IconLegendToggled), new Array {
                        icon, data.Key, fallbackIconIsUsed });

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
                var button = new MenuButton
                {
                    Flat = false,
                    Text = title,
                    EnabledFocusMode = FocusModeEnum.None,
                };

                var popupMenu = button.GetPopup();
                var itemId = 0;

                popupMenu.HideOnCheckableItemSelection = false;

                foreach (var data in DataSets)
                {
                    popupMenu.AddCheckItem(data.Key, itemId);

                    // Set initial item check state
                    if (data.Value.Draw)
                        popupMenu.SetItemChecked(popupMenu.GetItemIndex(itemId), true);

                    itemId++;
                }

                legendContainer.AddChild(button);

                popupMenu.Connect("index_pressed", this, nameof(DropDownLegendItemSelected), new Array { button });

                break;
            }

            default:
                throw new Exception("Invalid legend display mode");
        }
    }

    public void UpdateDataSetVisibility(string name, bool visible)
    {
        DataSets[name].Draw = visible;
        drawer.Update();
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
            icon.Modulate = fallbackIconIsUsed ?
                new Color(
                    dataColor.r - 0.3f, dataColor.g - 0.3f, dataColor.b - 0.3f) :
                new Color(0.7f, 0.7f, 0.7f);
        }
    }

    private void IconLegendMouseExit(TextureButton icon, bool fallbackIconIsUsed, Color dataColor)
    {
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

    private void DropDownLegendItemSelected(int index, MenuButton button)
    {
        var name = button.GetPopup().GetItemText(index);
        var popupMenu = button.GetPopup();

        if (!popupMenu.IsItemChecked(index) && CheckMaxDataSetShown())
            return;

        popupMenu.ToggleItemChecked(index);

        UpdateDataSetVisibility(name, popupMenu.IsItemChecked(index));
    }
}
