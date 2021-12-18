using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Array = Godot.Collections.Array;
using DataSetDictionary = System.Collections.Generic.Dictionary<string, LineChartData>;

/// <summary>
///   A custom widget for multi-line chart with hoverable data points tooltip. Uses <see cref="LineChartData"/>
///   as dataset; currently only support numerical data.
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

    [Export]
    public NodePath ExtraLegendContainerPath;

    [Export]
    public NodePath InspectButtonPath;

    /// <summary>
    ///   The translatable name identifier for this chart. Each chart instance should have a unique name.
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
    ///   Limits how many icon legends should be shown
    /// </summary>
    public int MaxIconLegend = 10;

    /// <summary>
    ///   Limits how many dataset lines should be allowed to be shown on the chart.
    /// </summary>
    public int MaxDisplayedDataSet = 10;

    /// <summary>
    ///   Limits how many dataset lines should be hidden.
    /// </summary>
    public int MinDisplayedDataSet;

    /// <summary>
    ///   Specifies how the X axis value display should be formatted on the datapoint tooltip.
    ///   Leave this null/empty to use the default.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Format should have maximum of one format item (e.g "{0}%") where it will be inserted with the actual value.
    ///     This will only be applied after calling Plot().
    ///   </para>
    /// </remarks>
    public string TooltipXAxisFormat;

    /// <summary>
    ///   Specifies how the Y axis value display should be formatted on the datapoint tooltip.
    ///   Leave this null/empty to use the default.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Format should have maximum of one format item (e.g "{0}%") where it will be inserted with the actual value.
    ///     This will only be applied after calling Plot().
    ///   </para>
    /// </remarks>
    public string TooltipYAxisFormat;

    /// <summary>
    ///   Datasets to be plotted on the chart. Key is the dataset's name
    /// </summary>
    private readonly DataSetDictionary dataSets = new();

    /// <summary>
    ///   Lines for each of the plotted datasets
    /// </summary>
    private readonly Dictionary<string, DataLine> dataLines = new();

    private readonly Dictionary<string, Dictionary<DataPoint, ICustomToolTip>> dataPointToolTips = new();

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
    private GridContainer extraLegendContainer;
    private TextureButton inspectButton;
    private CustomDialog chartPopup;
    private LineChart chartClone;

    private string xAxisName;
    private string yAxisName;

    private bool isClone;

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
    ///   The update result that is returned after calling UpdateDataSetVisibility().
    /// </summary>
    public enum DataSetVisibilityUpdateResult
    {
        /// <summary>
        ///   The dataset is not present in the collection.
        /// </summary>
        NotFound,

        /// <summary>
        ///   Number of shown dataset reaches the maximum limit.
        /// </summary>
        MaxVisibleLimitReached,

        /// <summary>
        ///   Number of shown dataset reaches the minimum limit.
        /// </summary>
        MinVisibleLimitReached,

        /// <summary>
        ///   The dataset visibility is successfully changed.
        /// </summary>
        Success,
    }

    /// <summary>
    ///   The lowest data point value from all the datasets.
    /// </summary>
    public (double X, double Y) MinValues { get; private set; }

    /// <summary>
    ///   The highest data point value from all the datasets.
    /// </summary>
    public (double X, double Y) MaxValues { get; private set; }

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
    ///   Returns the number of shown datasets.
    /// </summary>
    public int VisibleDataSets
    {
        get
        {
            var count = 0;

            foreach (var data in dataSets)
            {
                if (data.Value.Draw)
                    count++;
            }

            return count;
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
        extraLegendContainer = GetNode<GridContainer>(ExtraLegendContainerPath);
        inspectButton = GetNode<TextureButton>(InspectButtonPath);
        chartPopup = GetNode<CustomDialog>("ChartPopup");
        defaultIconLegendTexture = GD.Load<Texture>("res://assets/textures/gui/bevel/blankCircle.png");
        hLineTexture = GD.Load<Texture>("res://assets/textures/gui/bevel/hSeparatorCentered.png");
        vLineTexture = GD.Load<Texture>("res://assets/textures/gui/bevel/vSeparatorUp.png");

        SetupChartClone();
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

        if (chartClone != null)
        {
            foreach (var dataset in chartClone.dataSets)
            {
                dataset.Value.ClearPoints();
            }

            chartClone.dataSets.Clear();
        }

        dataPointToolTips.Clear();
    }

    /// <summary>
    ///   Plots and constructs the chart from available datasets
    /// </summary>
    /// <param name="xAxisName">Overrides the horizontal axis label title</param>
    /// <param name="yAxisName">Overrides the vertical axis label title</param>
    /// <param name="initialVisibleDataSets">How many datasets should be visible initially after plotting</param>
    /// <param name="legendTitle">Title for the chart legend. If null, the legend will not be created</param>
    /// <param name="defaultDataSet">The name of dataset that'll always be shown after plotting</param>
    /// <param name="expandedXTicks">
    ///   Overrides number of x scales in the expanded graph, leave this as zero to use the default
    /// </param>
    /// <param name="expandedYTicks">
    ///   Overrides number of y scales in the expanded graph, leave this as zero to use the default
    /// </param>
    public void Plot(string xAxisName, string yAxisName, int initialVisibleDataSets,
        string legendTitle = null, string defaultDataSet = null, int expandedXTicks = 0, int expandedYTicks = 0)
    {
        ClearChart();

        // These are before the parameter checks to apply any possible translation to the axes labels
        XAxisName = string.IsNullOrEmpty(xAxisName) ? XAxisName : xAxisName;
        YAxisName = string.IsNullOrEmpty(yAxisName) ? YAxisName : yAxisName;

        if (expandedXTicks > 0 && isClone)
            XAxisTicks = expandedXTicks;

        if (expandedYTicks > 0 && isClone)
            YAxisTicks = expandedYTicks;

        if (dataSets == null || dataSets.Count <= 0)
        {
            GD.PrintErr($"{ChartName} chart missing datasets, aborting plotting data");
            return;
        }

        if (XAxisTicks <= 0 || YAxisTicks <= 0)
        {
            GD.PrintErr($"{ChartName} chart ticks has to be more than 0, aborting plotting data");
            return;
        }

        MinDisplayedDataSet = Mathf.Clamp(MinDisplayedDataSet, 0, MaxDisplayedDataSet);

        initialVisibleDataSets = Mathf.Clamp(initialVisibleDataSets, 0, MaxDisplayedDataSet);

        // Start from 1 if defaultDataSet is specified as it's always visible
        var visibleDataSetCount = string.IsNullOrEmpty(defaultDataSet) ? 0 : 1;

        UpdateMinimumAndMaximumValues();

        foreach (var data in dataSets)
        {
            chartClone?.dataSets.Add(data.Key, (LineChartData)data.Value.Clone());

            // Null check to suppress ReSharper's warning
            if (string.IsNullOrEmpty(data.Key))
                throw new Exception("Dataset dictionary key is null");

            // Hide the rest, if number of shown dataset exceeds initialVisibleDataSets
            if (visibleDataSetCount >= initialVisibleDataSets && data.Key != defaultDataSet)
            {
                UpdateDataSetVisibility(data.Key, false);
            }
            else if (visibleDataSetCount < initialVisibleDataSets && data.Key != defaultDataSet)
            {
                visibleDataSetCount++;
            }

            // Initialize line
            var dataLine = new DataLine(data.Value, data.Key == defaultDataSet);
            dataLines[data.Key] = dataLine;
            drawArea.AddChild(dataLine);

            if (!dataPointToolTips.ContainsKey(data.Key))
                dataPointToolTips.Add(data.Key, new Dictionary<DataPoint, ICustomToolTip>());

            foreach (var point in data.Value.DataPoints)
            {
                // Create tooltip for the point markers
                var toolTip = ToolTipHelper.CreateDefaultToolTip();

                var xValueForm = string.IsNullOrEmpty(TooltipXAxisFormat) ?
                    $"{point.x.FormatNumber()} {XAxisName}" :
                    string.Format(CultureInfo.CurrentCulture,
                        TooltipXAxisFormat, point.x);

                var yValueForm = string.IsNullOrEmpty(TooltipYAxisFormat) ?
                    $"{point.y.FormatNumber()} {YAxisName}" :
                    string.Format(CultureInfo.CurrentCulture,
                        TooltipYAxisFormat, point.y);

                toolTip.DisplayName = data.Key + point;
                toolTip.Description = $"{data.Key}\n{xValueForm}\n{yValueForm}";

                toolTip.DisplayDelay = 0;
                toolTip.HideOnMousePress = false;
                toolTip.TransitionType = ToolTipTransitioning.Immediate;
                toolTip.Positioning = ToolTipPositioning.ControlBottomRightCorner;

                point.RegisterToolTipForControl(toolTip);
                ToolTipManager.Instance.AddToolTip(toolTip, "chartMarkers" + ChartName + data.Key);

                var key = dataPointToolTips[data.Key];
                key[point] = toolTip;

                drawArea.AddChild(point);
            }
        }

        // Create chart legend
        if (!string.IsNullOrEmpty(legendTitle))
        {
            legendContainer.Show();

            // Switch to dropdown if number of datasets exceeds the max amount of icon legends
            if (dataSets.Count > MaxIconLegend && LegendMode == LegendDisplayMode.Icon)
                LegendMode = LegendDisplayMode.DropDown;

            switch (LegendMode)
            {
                case LegendDisplayMode.Icon:
                    CreateDatasetIconLegend(legendTitle);
                    break;
                case LegendDisplayMode.DropDown:
                    CreateDatasetDropDownLegend(legendTitle);
                    break;
                default:
                    throw new Exception("Invalid legend display mode");
            }
        }

        drawArea.Update();

        foreach (var data in dataSets.Keys)
        {
            FlattenLines(data);
        }

        chartPopup.WindowTitle = TranslationServer.Translate(ChartName);
        chartClone?.Plot(xAxisName, yAxisName, initialVisibleDataSets, legendTitle, defaultDataSet, expandedXTicks, expandedYTicks);
    }

    /// <summary>
    ///   Wipes clean all node elements on this chart
    /// </summary>
    public void ClearChart()
    {
        foreach (var data in dataSets)
        {
            ToolTipManager.Instance.ClearToolTips("chartMarkers" + ChartName + data.Key);
        }

        ToolTipManager.Instance.ClearToolTips("chartLegend" + ChartName);

        // Clear lines
        foreach (var data in dataSets)
        {
            if (!dataLines.ContainsKey(data.Key))
                continue;

            var dataLine = dataLines[data.Key];

            dataLine.DetachAndQueueFree();

            foreach (var rect in dataLine.CollisionBoxes)
            {
                rect.Value.DetachAndQueueFree();
            }
        }

        dataLines.Clear();

        // Clear points
        drawArea.QueueFreeChildren();

        // Clear legend
        legendContainer.QueueFreeChildren();

        // Clear abscissas
        horizontalLabelsContainer.QueueFreeChildren();

        // Clear ordinates
        verticalLabelsContainer.QueueFreeChildren();

        extraLegendContainer.QueueFreeChildren();
    }

    public DataSetVisibilityUpdateResult UpdateDataSetVisibility(string name, bool visible)
    {
        chartClone?.UpdateDataSetVisibility(name, visible);

        if (!dataSets.ContainsKey(name))
            return DataSetVisibilityUpdateResult.NotFound;

        var data = dataSets[name];

        if (visible && VisibleDataSets >= MaxDisplayedDataSet)
        {
            return DataSetVisibilityUpdateResult.MaxVisibleLimitReached;
        }

        if (!visible && VisibleDataSets - 1 < MinDisplayedDataSet)
        {
            return DataSetVisibilityUpdateResult.MinVisibleLimitReached;
        }

        var initiallyVisible = data.Draw;

        data.Draw = visible;
        UpdateMinimumAndMaximumValues();
        drawArea.Update();

        if (dataLines.ContainsKey(name) && !initiallyVisible)
            FlattenLines(name);

        // Update the legend
        switch (LegendMode)
        {
            case LegendDisplayMode.Icon:
                UpdateIconLegend(visible, name);
                break;
            case LegendDisplayMode.DropDown:
                UpdateDropDownLegendItem(visible, name);
                break;
            default:
                throw new Exception("Invalid legend mode");
        }

        return DataSetVisibilityUpdateResult.Success;
    }

    /// <summary>
    ///   Adds additional legend in icon form at the bottom of the graph's inspection panel.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     NOTE: Must be called after Plot() as this will got cleared (removed) if called beforehand.
    ///   </para>
    /// </remarks>
    public void AddIconLegend(Texture icon, string name, float size = 15)
    {
        if (isClone)
            return;

        var parent = new HBoxContainer();
        parent.AddConstantOverride("separation", 7);

        var rect = new TextureRect
        {
            Texture = icon,
            Expand = true,
            RectMinSize = new Vector2(size, size),
            SizeFlagsVertical = (int)Control.SizeFlags.ShrinkCenter,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
        };

        var label = new Label { Text = name };

        label.AddFontOverride("font", GD.Load<Font>("res://src/gui_common/fonts/Lato-Regular-Small.tres"));

        parent.AddChild(rect);
        parent.AddChild(label);

        extraLegendContainer.AddChild(parent);
    }

    public void OverrideDataPointToolTipDescription(string dataset, DataPoint datapoint, string description)
    {
        if (dataPointToolTips.ContainsKey(dataset) && dataPointToolTips[dataset].TryGetValue(
            datapoint, out var tooltip))
        {
            tooltip.Description = description;
        }

        if (chartClone == null)
            return;

        if (chartClone.dataPointToolTips.ContainsKey(dataset) && chartClone.dataPointToolTips[dataset].TryGetValue(
            datapoint, out var clonedTooltip))
        {
            clonedTooltip.Description = description;
        }
    }

    private void CreateDatasetIconLegend(string title)
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

            var icon = new DatasetIconLegend(data.Key, this, data.Value, fallbackIconIsUsed);

            legendContainer.AddChild(icon);

            icon.Connect("toggled", this, nameof(OnIconLegendToggled), new Array { icon });

            // Set initial icon toggle state
            if (!data.Value.Draw)
            {
                icon.Pressed = false;
                UpdateIconLegend(false, data.Key);
            }

            // Create tooltips
            var toolTip = ToolTipHelper.CreateDefaultToolTip();

            toolTip.DisplayName = data.Key;
            toolTip.Description = data.Key;

            icon.RegisterToolTipForControl(toolTip);
            ToolTipManager.Instance.AddToolTip(toolTip, "chartLegend" + ChartName);
        }
    }

    private void CreateDatasetDropDownLegend(string title)
    {
        var dropDown = new CustomDropDown
        {
            Flat = false,
            Text = title,
            FocusMode = FocusModeEnum.None,
        };

        var itemId = 0;

        dropDown.Popup.HideOnCheckableItemSelection = false;
        dropDown.Popup.HideOnItemSelection = false;

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

            dropDown.AddItem(data.Key, itemId, !dataLines[data.Key].Default, colorToUse, data.Value.IconTexture);

            // Set initial item check state
            dropDown.Popup.SetItemChecked(
                dropDown.Popup.GetItemIndex(itemId), dataLines[data.Key].Default ? true : data.Value.Draw);

            itemId++;
        }

        legendContainer.AddChild(dropDown);

        dropDown.Popup.Connect("index_pressed", this, nameof(OnDropDownLegendItemSelected),
            new Array { dropDown });
    }

    /// <summary>
    ///   Draws the chart visuals. The Drawer node connect its 'draw()' signal to here.
    /// </summary>
    private void RenderChart()
    {
        // Handle errors
        if (dataSets == null || VisibleDataSets <= 0)
        {
            DrawErrorText(TranslationServer.Translate("NO_DATA_TO_SHOW"));
        }
        else if (!IsMinMaxValid())
        {
            DrawErrorText(TranslationServer.Translate("INVALID_DATA_TO_PLOT"));
        }

        DrawOrdinateLines();
        ApplyCoordinatesToDataPoints();
        UpdateLineSegments();
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
    ///   Connects data points with line segments.
    /// </summary>
    private void UpdateLineSegments()
    {
        foreach (var data in dataSets)
        {
            // Create into List so we can use IndexOf
            var points = data.Value.DataPoints.ToList();

            if (points.Count <= 0 || !dataLines.ContainsKey(data.Key))
                continue;

            // This is actually the first point (left-most)
            var previousPoint = points.Last();

            var dataLine = dataLines[data.Key];

            // Setup lines
            foreach (var point in points)
            {
                if (!point.IsInsideTree())
                    continue;

                var index = points.IndexOf(point);

                if (index < dataLine.Points.Length)
                {
                    dataLine.InterpolatePointPosition(
                        index, point.RectPosition + (point.RectSize / 2), point.Coordinate);
                }
                else
                {
                    dataLine.AddPoint(point.Coordinate, index);
                }

                // "First" is the last point on the chart (right-most one)
                if (point != points.First())
                    UpdateLineColliders(data.Key, previousPoint, point);

                previousPoint = point;
            }

            dataLine.Visible = data.Value.Draw;
        }
    }

    /// <summary>
    ///   Generates and updates line "colliders" to detect mouse enter/exit
    /// </summary>
    private void UpdateLineColliders(string datasetName, DataPoint firstPoint, DataPoint secondPoint)
    {
        var dataLine = dataLines[datasetName];

        // Create a new collision rect if it hasn't been created yet
        if (!dataLine.CollisionBoxes.ContainsKey(firstPoint))
        {
            var newCollisionRect = new Control { RectSize = Vector2.One };

            newCollisionRect.Connect("mouse_entered", dataLine, nameof(dataLine.OnMouseEnter));
            newCollisionRect.Connect("mouse_exited", dataLine, nameof(dataLine.OnMouseExit));

            // Create tooltip
            var tooltip = ToolTipHelper.CreateDefaultToolTip();

            tooltip.DisplayName = datasetName + "line" + firstPoint.Coordinate;
            tooltip.Description = datasetName;
            tooltip.DisplayDelay = 0.5f;

            newCollisionRect.RegisterToolTipForControl(tooltip);
            ToolTipManager.Instance.AddToolTip(tooltip, "chartMarkers");

            dataLine.CollisionBoxes[firstPoint] = newCollisionRect;

            drawArea.AddChild(newCollisionRect);
        }

        // Update collider rect scaling and positioning

        var mouseCollider = dataLine.CollisionBoxes[firstPoint];

        // Position the collider at a middle point between two data point coordinates
        mouseCollider.RectPosition = firstPoint.Coordinate.LinearInterpolate(secondPoint.Coordinate, 0.5f);

        // Set pivot at the center of the rect
        mouseCollider.RectPivotOffset = mouseCollider.RectSize / 2;

        // Use the distance between two coordinates as the length of the collider
        mouseCollider.RectScale = new Vector2(
            firstPoint.Coordinate.DistanceTo(secondPoint.Coordinate) - firstPoint.RectSize.x,
            dataSets[datasetName].LineWidth + 10);

        mouseCollider.RectRotation = Mathf.Rad2Deg(firstPoint.Coordinate.AngleToPoint(secondPoint.Coordinate));

        mouseCollider.Visible = dataSets[datasetName].Draw;
    }

    /// <summary>
    ///   Draws an error text on the center of the chart.
    /// </summary>
    private void DrawErrorText(string error)
    {
        var font = GetFont("jura_small", "Label");

        // Values are rounded to make the font not be blurry
        var position = new Vector2(
            Mathf.Round((drawArea.RectSize.x - font.GetStringSize(error).x) / 2),
            Mathf.Round(drawArea.RectSize.y / 2));

        drawArea.DrawString(font, position, error);
    }

    /// <summary>
    ///   Sets the y coordinate for all of the given dataset's points at the bottom of the chart.
    ///   This is used to animate the lines rising from the bottom.
    /// </summary>
    private void FlattenLines(string datasetName)
    {
        var data = dataSets[datasetName];

        foreach (var point in data.DataPoints)
        {
            if (!IsInstanceValid(point))
                continue;

            // First we move the point marker to the bottom of the chart
            point.SetCoordinate(new Vector2(ConvertToXCoordinate(point.x), drawArea.RectSize.y), false);

            // Next start interpolating it into its assigned position
            point.SetCoordinate(ConvertToCoordinate(point));
        }

        UpdateLineSegments();
    }

    /// <summary>
    ///   Calculates the min/max values of this chart based on shown datasets and generates the axes scale ticks.
    /// </summary>
    private void UpdateMinimumAndMaximumValues()
    {
        // Default to zeros
        MaxValues = (0, 0);
        MinValues = (0, 0);

        // Find the max values of all the data points first to make finding min values possible
        foreach (var data in dataSets)
        {
            if (!data.Value.Draw)
                continue;

            foreach (var point in data.Value.DataPoints)
            {
                MaxValues = (Math.Max(point.x, MaxValues.X), Math.Max(point.y, MaxValues.Y));
            }
        }

        MinValues = MaxValues;

        // Find the minimum values of all the data points
        foreach (var data in dataSets)
        {
            if (!data.Value.Draw)
                continue;

            foreach (var point in data.Value.DataPoints)
            {
                MinValues = (Math.Min(point.x, MinValues.X), Math.Min(point.y, MinValues.Y));
            }
        }

        // If min/max turns out to be equal, set one of their point to zero as the initial value

        if (MinValues.X == MaxValues.X)
        {
            if (MaxValues.X > 0)
            {
                MinValues = (0, MinValues.Y);
            }
            else if (MaxValues.X < 0)
            {
                MaxValues = (0, MaxValues.Y);
            }
        }

        if (MinValues.Y == MaxValues.Y)
        {
            if (MaxValues.Y > 0)
            {
                MinValues = (MinValues.X, 0);
            }
            else if (MaxValues.Y < 0)
            {
                MaxValues = (MaxValues.X, 0);
            }
        }

        horizontalLabelsContainer.QueueFreeChildren();
        verticalLabelsContainer.QueueFreeChildren();

        // If no data is visible, don't create the labels as it will just have zero values
        // and be potentially confusing to look at, likewise if min max is invalid.
        if (VisibleDataSets <= 0 || !IsMinMaxValid())
            return;

        // Populate the rows
        for (int i = 0; i < XAxisTicks; i++)
        {
            var label = new Label
            {
                SizeFlagsHorizontal = (int)SizeFlags.ExpandFill,
                Align = Label.AlignEnum.Center,
            };

            label.Text = Math.Round(
                i * (MaxValues.X - MinValues.X) / (XAxisTicks - 1) + MinValues.X, 1).FormatNumber();

            horizontalLabelsContainer.AddChild(label);
        }

        // Populate the columns (in reverse order)
        for (int i = YAxisTicks - 1; i >= 0; i--)
        {
            var label = new Label
            {
                SizeFlagsVertical = (int)SizeFlags.ExpandFill,
                Align = Label.AlignEnum.Right,
                Valign = Label.VAlign.Center,
            };

            label.Text = Math.Round(
                i * (MaxValues.Y - MinValues.Y) / (YAxisTicks - 1) + MinValues.Y, 1).FormatNumber();

            verticalLabelsContainer.AddChild(label);
        }
    }

    private void ApplyCoordinatesToDataPoints()
    {
        foreach (var data in dataSets)
        {
            foreach (var point in data.Value.DataPoints)
            {
                if (IsMinMaxValid())
                {
                    point.SetCoordinate(ConvertToCoordinate(point));
                }
                else
                {
                    // Plotting failed, here we place the marker at the bottom left corner, we can't hide it at the
                    // same time because that will break its position in a later coordinate update for some reason.
                    point.SetCoordinate(new Vector2(0, drawArea.RectSize.y), false);
                }
            }
        }
    }

    /// <summary>
    ///   Helper method for converting a single point data value into a coordinate.
    /// </summary>
    /// <returns>Position of the given value on the chart</returns>
    private Vector2 ConvertToCoordinate(DataPoint value)
    {
        return new Vector2(ConvertToXCoordinate(value.x), ConvertToYCoordinate(value.y));
    }

    private float ConvertToXCoordinate(double value)
    {
        var lineRectX = drawArea.RectSize.x / XAxisTicks;
        var lineRectWidth = lineRectX * (XAxisTicks - 1);
        var dx = MaxValues.X - MinValues.X;

        return (float)((value - MinValues.X) * lineRectWidth / dx) + lineRectX / 2;
    }

    private float ConvertToYCoordinate(double value)
    {
        var lineRectY = drawArea.RectSize.y / YAxisTicks;
        var lineRectHeight = lineRectY * (YAxisTicks - 1);
        var dy = MaxValues.Y - MinValues.Y;

        return (float)(lineRectHeight - ((value - MinValues.Y) * lineRectHeight / dy) + lineRectY / 2);
    }

    /// <summary>
    ///   If false the coordinate calculations will break, this is used to guard against that.
    /// </summary>
    private bool IsMinMaxValid()
    {
        return !(MinValues.X == MaxValues.X || MinValues.Y == MaxValues.Y);
    }

    private void UpdateAxesName()
    {
        if (horizontalLabel == null || verticalLabel == null)
            return;

        horizontalLabel.Text = xAxisName;
        verticalLabel.Text = yAxisName;
    }

    private void UpdateIconLegend(bool toggled, string name)
    {
        if (LegendMode != LegendDisplayMode.Icon)
            return;

        chartClone?.UpdateIconLegend(toggled, name);

        // To really make sure we aren't accessing empty child
        if (legendContainer.GetChildCount() <= 0)
            return;

        var icon = legendContainer.GetChildren()
            .Cast<DatasetIconLegend>()
            .ToList()
            .Find(i => i.DataName == name);

        if (icon == null)
            return;

        icon.Pressed = toggled;

        var data = dataSets[name];

        if (icon.IsUsingFallbackIcon)
        {
            icon.Modulate = toggled ? data.DataColour : data.DataColour.Darkened(0.5f);
        }
        else
        {
            icon.Modulate = toggled ? Colors.White : Colors.Gray;
        }
    }

    private void UpdateDropDownLegendItem(bool toggled, string item)
    {
        if (LegendMode != LegendDisplayMode.DropDown)
            return;

        chartClone?.UpdateDropDownLegendItem(toggled, item);

        // To really make sure we aren't accessing empty child
        if (legendContainer.GetChildCount() <= 0)
            return;

        // It's assumed the child is a dropdown node
        var dropDown = legendContainer.GetChildOrNull<CustomDropDown>(0);

        var index = dropDown.GetItemIndex(item);
        if (index == -1)
            return;

        dropDown?.Popup.SetItemChecked(index, toggled);
    }

    private void SetupChartClone()
    {
        if (chartClone != null || isClone)
            return;

        var scene = GD.Load<PackedScene>("res://src/gui_common/charts/line/LineChart.tscn");
        chartClone = scene.Instance<LineChart>();

        chartClone.isClone = true;
        chartClone.ChartName = ChartName + "clone";
        chartClone.XAxisTicks = XAxisTicks;
        chartClone.XAxisName = XAxisName;
        chartClone.YAxisTicks = YAxisTicks;
        chartClone.YAxisName = YAxisName;
        chartClone.LegendMode = LegendMode;
        chartClone.MaxIconLegend = MaxIconLegend;
        chartClone.MaxDisplayedDataSet = MaxDisplayedDataSet;
        chartClone.MinDisplayedDataSet = MinDisplayedDataSet;
        chartClone.TooltipXAxisFormat = TooltipXAxisFormat;
        chartClone.TooltipYAxisFormat = TooltipYAxisFormat;

        var parent = extraLegendContainer.GetParent();
        parent.AddChild(chartClone);
        parent.MoveChild(chartClone, 0);

        chartClone.inspectButton.Hide();
    }

    /*
        GUI Callbacks
    */

    private void OnIconLegendToggled(bool toggled, DatasetIconLegend icon)
    {
        var result = UpdateDataSetVisibility(icon.DataName, toggled);

        switch (result)
        {
            case DataSetVisibilityUpdateResult.MaxVisibleLimitReached:
            {
                icon.Pressed = false;
                ToolTipManager.Instance.ShowPopup(string.Format(
                    CultureInfo.CurrentCulture, TranslationServer.Translate(
                        "MAX_VISIBLE_DATASET_WARNING"), MaxDisplayedDataSet), 1f);
                break;
            }

            case DataSetVisibilityUpdateResult.MinVisibleLimitReached:
            {
                icon.Pressed = true;
                ToolTipManager.Instance.ShowPopup(string.Format(
                    CultureInfo.CurrentCulture, TranslationServer.Translate(
                        "MIN_VISIBLE_DATASET_WARNING"), MinDisplayedDataSet), 1f);
                break;
            }
        }
    }

    private void OnDropDownLegendItemSelected(int index, CustomDropDown dropDown)
    {
        if (!dropDown.Popup.IsItemCheckable(index))
            return;

        var result = UpdateDataSetVisibility(
            dropDown.Popup.GetItemText(index), !dropDown.Popup.IsItemChecked(index));

        switch (result)
        {
            case DataSetVisibilityUpdateResult.MaxVisibleLimitReached:
            {
                ToolTipManager.Instance.ShowPopup(string.Format(
                    CultureInfo.CurrentCulture, TranslationServer.Translate(
                        "MAX_VISIBLE_DATASET_WARNING"), MaxDisplayedDataSet), 1f);
                break;
            }

            case DataSetVisibilityUpdateResult.MinVisibleLimitReached:
            {
                ToolTipManager.Instance.ShowPopup(string.Format(
                    CultureInfo.CurrentCulture, TranslationServer.Translate(
                        "MIN_VISIBLE_DATASET_WARNING"), MinDisplayedDataSet), 1f);
                break;
            }
        }
    }

    private void OnInspectButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        chartPopup.PopupCenteredShrink();
    }

    /*
        Subclasses
    */

    /// <summary>
    ///   Used as the chart's dataset line segments. Contains mouse collision boxes and
    ///   mouse enter/exit callback to make the line interactable.
    /// </summary>
    private class DataLine : Line2D
    {
        /// <summary>
        ///   The dataset lines will always be visible and can't be made hidden.
        /// </summary>
        public readonly bool Default;

        public Dictionary<DataPoint, Control> CollisionBoxes = new Dictionary<DataPoint, Control>();

        private LineChartData data;
        private Tween tween;

        private Color dataColour;

        public DataLine(LineChartData data, bool isDefault)
        {
            this.data = data;
            Default = isDefault;

            Width = data.LineWidth;
            DefaultColor = data.DataColour;
            dataColour = data.DataColour;

            tween = new Tween();
            AddChild(tween);

            // Antialiasing is turned off as it's a bit unreliable currently
        }

        public void InterpolatePointPosition(int i, Vector2 initialPos, Vector2 targetPos)
        {
            tween.InterpolateMethod(this, nameof(ChangePointPos), new Vector3(i, initialPos.x, initialPos.y),
                new Vector3(i, targetPos.x, targetPos.y), 0.5f, Tween.TransitionType.Expo, Tween.EaseType.Out);
            tween.Start();
        }

        public void OnMouseEnter()
        {
            var highlightColour = dataColour.IsLuminuous() ?
                dataColour.Darkened(0.5f) :
                dataColour.Lightened(0.5f);

            DefaultColor = highlightColour;
            data.DataColour = highlightColour;
        }

        public void OnMouseExit()
        {
            DefaultColor = dataColour;
            data.DataColour = dataColour;
        }

        /// <summary>
        ///   This is a really hacky way to get Godot to tween methods with multiple parameters.
        ///   Got extremely lucky that all parameters can fit into a single Godot primitive type here...
        /// </summary>
        private void ChangePointPos(Vector3 arguments)
        {
            SetPointPosition((int)arguments.x, new Vector2(arguments.y, arguments.z));
        }
    }

    private class DatasetIconLegend : TextureButton
    {
        public readonly string DataName;
        public readonly bool IsUsingFallbackIcon;

        private LineChart chart;
        private LineChartData data;
        private Tween tween;

        public DatasetIconLegend(string name, LineChart chart, LineChartData data, bool isUsingFallbackIcon)
        {
            DataName = name;
            this.chart = chart;
            this.data = data;
            IsUsingFallbackIcon = isUsingFallbackIcon;
            Expand = true;
            RectMinSize = new Vector2(18, 18);
            FocusMode = FocusModeEnum.None;
            ToggleMode = true;
            Pressed = true;
            TextureNormal = data.IconTexture;
            StretchMode = StretchModeEnum.KeepAspectCentered;
            RectPivotOffset = RectMinSize / 2;

            // Set the default icon's color
            if (isUsingFallbackIcon)
                Modulate = data.DataColour;

            Connect("mouse_entered", this, nameof(IconLegendMouseEnter));
            Connect("mouse_exited", this, nameof(IconLegendMouseExit));

            tween = new Tween();
            AddChild(tween);
        }

        private void IconLegendMouseEnter()
        {
            tween.InterpolateProperty(this, "rect_scale", Vector2.One, new Vector2(1.1f, 1.1f), 0.1f);
            tween.Start();

            // Highlight icon
            Modulate = IsUsingFallbackIcon ? data.DataColour.Lightened(0.5f) : Colors.LightGray;

            chart.dataLines[DataName].OnMouseEnter();
        }

        private void IconLegendMouseExit()
        {
            tween.InterpolateProperty(this, "rect_scale", new Vector2(1.1f, 1.1f), Vector2.One, 0.1f);
            tween.Start();

            if (Pressed)
            {
                // Reset icon color
                Modulate = IsUsingFallbackIcon ? data.DataColour : Colors.White;
            }
            else
            {
                Modulate = Colors.DarkGray;
            }

            chart.dataLines[DataName].OnMouseExit();
        }
    }
}
