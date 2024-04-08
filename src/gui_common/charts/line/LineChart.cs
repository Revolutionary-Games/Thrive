using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using DataSetDictionary = System.Collections.Generic.Dictionary<string, ChartDataSet>;

/// <summary>
///   A custom widget for multi-line chart with hoverable data points tooltip. Uses <see cref="LineChartData"/>
///   as dataset; currently only support numerical data.
/// </summary>
public partial class LineChart : VBoxContainer
{
    [Export]
    public NodePath? HorizontalLabelPath;

    [Export]
    public NodePath VerticalLabelPath = null!;

    [Export]
    public NodePath VerticalTicksContainerPath = null!;

    [Export]
    public NodePath HorizontalTicksContainerPath = null!;

    [Export]
    public NodePath DrawAreaPath = null!;

    [Export]
    public NodePath LegendsContainerPath = null!;

    [Export]
    public NodePath ExtraLegendContainerPath = null!;

    [Export]
    public NodePath InspectButtonPath = null!;

    /// <summary>
    ///   The translatable name identifier for this chart. Each chart instance should have a unique name.
    /// </summary>
    [Export]
    public string ChartName = null!;

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
    public string? TooltipXAxisFormat;

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
    public string? TooltipYAxisFormat;

    private const string TOOLTIP_GROUP_BASE_NAME = "chartDatasets";

    private static readonly Dictionary<string, Dictionary<string, bool>> StoredDatasetsVisibilityStatus = new();

    /// <summary>
    ///   Datasets to be plotted on the chart. Key is the dataset's name
    /// </summary>
    private readonly DataSetDictionary dataSets = new();

    /// <summary>
    ///   Lines for each of the plotted datasets
    /// </summary>
    private readonly Dictionary<string, DataLine> dataLines = new();

    private readonly Dictionary<DataLine, DataLineToolTipData> dataLineTooltips = new();

    private readonly Dictionary<string, Dictionary<DataPoint, DefaultToolTip>> dataPointToolTips = new();

    private readonly Dictionary<Control, DefaultToolTip> legendToolTips = new();

#pragma warning disable CA2213

    /// <summary>
    ///   Fallback icon for the legend display mode using icons
    /// </summary>
    private Texture2D defaultIconLegendTexture = null!;

    private Texture2D hLineTexture = null!;

    // ReSharper disable once NotAccessedField.Local
    private Texture2D vLineTexture = null!;

    private Label? horizontalLabel;
    private Label? verticalLabel;
    private VBoxContainer verticalLabelsContainer = null!;
    private HBoxContainer horizontalLabelsContainer = null!;
    private Control drawArea = null!;
    private HBoxContainer legendContainer = null!;
    private GridContainer extraLegendContainer = null!;
    private TextureButton inspectButton = null!;
    private CustomWindow chartPopup = null!;
    private LineChart? childChart;

    private LabelSettings legendLabelSettings = null!;

    /// <summary>
    ///   Useful for any operations in the child chart involving the parent chart.
    /// </summary>
    private LineChart? parentChart;
#pragma warning restore CA2213

    private string xAxisName = string.Empty;
    private string yAxisName = string.Empty;

    /// <summary>
    ///   If true this means that this chart is part of another parent chart.
    /// </summary>
    private bool isChild;

    private bool toolTipsDetached;

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

        /// <summary>
        ///   Legend will be displayed as created by the user or none if the user didn't create any.
        /// </summary>
        CustomOrNone,
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
        ///   Dataset visibility is already at the state the user requests.
        /// </summary>
        UnchangedValue,

        /// <summary>
        ///   The dataset visibility is successfully changed.
        /// </summary>
        Success,
    }

    public IDataSetsLegend? DataSetsLegend { get; private set; }

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
    public int VisibleDataSets => dataSets.Count(d => d.Value.Draw);

    public override void _Ready()
    {
        if (string.IsNullOrEmpty(ChartName))
            throw new InvalidOperationException($"{nameof(ChartName)} must not be unset");

        horizontalLabel = GetNode<Label>(HorizontalLabelPath);
        verticalLabel = GetNode<Label>(VerticalLabelPath);
        verticalLabelsContainer = GetNode<VBoxContainer>(VerticalTicksContainerPath);
        horizontalLabelsContainer = GetNode<HBoxContainer>(HorizontalTicksContainerPath);
        drawArea = GetNode<Control>(DrawAreaPath);
        legendContainer = GetNode<HBoxContainer>(LegendsContainerPath);
        extraLegendContainer = GetNode<GridContainer>(ExtraLegendContainerPath);
        inspectButton = GetNode<TextureButton>(InspectButtonPath);
        chartPopup = GetNode<CustomWindow>("ChartPopup");
        defaultIconLegendTexture = GD.Load<Texture2D>("res://assets/textures/gui/bevel/blankCircle.png");
        hLineTexture = GD.Load<Texture2D>("res://assets/textures/gui/bevel/hSeparatorCentered.png");
        vLineTexture = GD.Load<Texture2D>("res://assets/textures/gui/bevel/vSeparatorUp.png");

        legendLabelSettings = GD.Load<LabelSettings>("res://src/gui_common/fonts/Body-Regular-Small.tres");

        SetupChartChild();
        UpdateAxesName();
    }

    public override void _EnterTree()
    {
        base._EnterTree();

        ReRegisterDetachedToolTips();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        // Due to disposes happening in a pretty weird order on shutdown (and to avoid disposed object tooltip
        // detaches) an approach is used to detach the tooltips here and potentially re-attach them if we happen to
        // re-enter the tree
        TemporarilyUnregisterToolTips();
    }

    /// <summary>
    ///   Add a dataset into this chart (overwrites existing one if the name already existed)
    /// </summary>
    public void AddDataSet(string name, ChartDataSet dataset)
    {
        dataSets[name] = dataset;
    }

    public ChartDataSet? GetDataSet(string name)
    {
        if (!dataSets.TryGetValue(name, out var dataSet))
        {
            GD.PrintErr("DataSet with name '" + name + "' not found");
            return null;
        }

        return dataSet;
    }

    public void ClearDataSets()
    {
        foreach (var dataset in dataSets)
        {
            dataset.Value.ClearPoints();
        }

        dataSets.Clear();

        if (childChart != null)
        {
            foreach (var dataset in childChart.dataSets)
            {
                dataset.Value.ClearPoints();
            }

            childChart.dataSets.Clear();
        }

        ProperlyRemovePointAndLineToolTips();
    }

    /// <summary>
    ///   Plots and constructs the chart from available datasets
    /// </summary>
    /// <param name="xAxisName">Overrides the horizontal axis label title</param>
    /// <param name="yAxisName">Overrides the vertical axis label title</param>
    /// <param name="initialVisibleDataSets">How many datasets should be visible initially after plotting</param>
    /// <param name="legendTitle">Title for the datasets legend element</param>
    /// <param name="datasetsLegend">
    ///   Custom datasets legend element. Leave this to null to use the default one according to
    ///   <see cref="LegendMode"/>. However, if you've set <see cref="LegendMode"/> to
    ///   <see cref="LegendDisplayMode.CustomOrNone"/> you should pass in a custom element, or if you
    ///   wish to not create any legend then this can be left null.
    /// </param>
    /// <param name="defaultDataSet">The name of dataset that'll always be shown after plotting</param>
    /// <param name="expandedXTicks">
    ///   Overrides number of x scales in the expanded graph, leave this to zero to use the default
    /// </param>
    /// <param name="expandedYTicks">
    ///   Overrides number of y scales in the expanded graph, leave this ro zero to use the default
    /// </param>
    public void Plot(string xAxisName, string yAxisName, int initialVisibleDataSets,
        string? legendTitle, IDataSetsLegend? datasetsLegend = null, string? defaultDataSet = null,
        int expandedXTicks = 0, int expandedYTicks = 0)
    {
        ClearChart();
        EnsureNoDetachedToolTipsExist();

        // These are before the parameter checks to apply any possible translation to the axes labels
        XAxisName = string.IsNullOrEmpty(xAxisName) ? XAxisName : xAxisName;
        YAxisName = string.IsNullOrEmpty(yAxisName) ? YAxisName : yAxisName;

        if (expandedXTicks > 0 && isChild)
            XAxisTicks = expandedXTicks;

        if (expandedYTicks > 0 && isChild)
            YAxisTicks = expandedYTicks;

        if (dataSets.Count <= 0)
        {
            GD.PrintErr($"{ChartName} chart missing datasets, aborting plotting data");
            childChart?.ClearChart();
            return;
        }

        if (XAxisTicks <= 0 || YAxisTicks <= 0)
        {
            GD.PrintErr($"{ChartName} chart ticks has to be more than 0, aborting plotting data");
            childChart?.ClearChart();
            return;
        }

        MinDisplayedDataSet = Mathf.Clamp(MinDisplayedDataSet, 0, MaxDisplayedDataSet);

        initialVisibleDataSets = Mathf.Clamp(initialVisibleDataSets, 0, MaxDisplayedDataSet);

        // Start from 1 if defaultDataSet is specified as it's always visible
        var visibleDataSetCount = string.IsNullOrEmpty(defaultDataSet) ? 0 : 1;

        UpdateMinimumAndMaximumValues();

        if (!StoredDatasetsVisibilityStatus.ContainsKey(ChartName))
            StoredDatasetsVisibilityStatus.Add(ChartName, new Dictionary<string, bool>());

        var stored = StoredDatasetsVisibilityStatus[ChartName];

        foreach (var data in dataSets)
        {
            childChart?.dataSets.Add(data.Key, (LineChartData)data.Value.Clone());

            // Null check to suppress ReSharper's warning
            if (string.IsNullOrEmpty(data.Key))
                throw new Exception("Dataset dictionary key is null");

            if (data.Key != defaultDataSet)
            {
                var visible = visibleDataSetCount < initialVisibleDataSets;

                // Override visible value if stored value exists
                if (stored.TryGetValue(data.Key, out bool value))
                    visible = value;

                UpdateDataSetVisibility(data.Key, visible);

                if (visible)
                    visibleDataSetCount++;
            }

            // Initialize line
            var dataLine = new DataLine((LineChartData)data.Value, data.Key == defaultDataSet, isChild ? 2 : 1);

            dataLines[data.Key] = dataLine;
            drawArea.AddChild(dataLine);

            if (!dataPointToolTips.TryGetValue(data.Key, out var key))
            {
                key = new Dictionary<DataPoint, DefaultToolTip>();
                dataPointToolTips.Add(data.Key, key);
            }

            foreach (var point in data.Value.DataPoints)
            {
                // Enlarge marker if this is an expanded chart
                point.Size *= isChild ? 1.5f : 1;

                if (!key.TryGetValue(point, out var toolTip))
                {
                    // Create a new tooltip for the point marker
                    toolTip = ToolTipHelper.GetDefaultToolTip();
                    key.Add(point, toolTip);

                    if (toolTip.GetParent() != null)
                        Debugger.Break();

                    ToolTipManager.Instance.AddToolTip(toolTip, TOOLTIP_GROUP_BASE_NAME + ChartName + data.Key);
                }

                var xValueForm = string.IsNullOrEmpty(TooltipXAxisFormat) ?
                    $"{point.X.FormatNumber()} {XAxisName}" :
                    TooltipXAxisFormat!.FormatSafe(point.X);

                var yValueForm = string.IsNullOrEmpty(TooltipYAxisFormat) ?
                    $"{point.Y.FormatNumber()} {YAxisName}" :
                    TooltipYAxisFormat!.FormatSafe(point.Y);

                toolTip.DisplayName = data.Key + point;
                toolTip.Description = $"{data.Key}\n{xValueForm}\n{yValueForm}";

                toolTip.DisplayDelay = 0;
                toolTip.HideOnMouseAction = false;
                toolTip.TransitionType = ToolTipTransitioning.Immediate;
                toolTip.Positioning = ToolTipPositioning.ControlBottomRightCorner;

                point.RegisterToolTipForControl(toolTip, false);

                drawArea.AddChild(point);
            }
        }

        // Create chart legend

        // Switch to dropdown if number of datasets exceeds the max amount of icon legends
        if (dataSets.Count > MaxIconLegend && LegendMode == LegendDisplayMode.Icon && LegendMode !=
            LegendDisplayMode.CustomOrNone)
        {
            LegendMode = LegendDisplayMode.DropDown;
        }

        // ReSharper disable once UseNullPropagationWhenPossible
        // Can't use null coalescing here due to the immediate cloning afterwards
        if (isChild && datasetsLegend != null)
            datasetsLegend = datasetsLegend.Clone() as IDataSetsLegend;

        // Host chart means which chart should the datasets legend element interact with.
        // Here we want it to always be a parent chart (that have its own child chart) so that when
        // something is changed in the child chart's legend instance, the parent chart can also be affected.
        // Ex: Changing dataset visibility in a child chart's legend also updates the visibility in the
        // parent chart
        var hostChart = parentChart ?? this;

        DataSetsLegend = LegendMode switch
        {
            LegendDisplayMode.Icon => new DatasetsIconLegend(hostChart),
            LegendDisplayMode.DropDown => new DataSetsDropdownLegend(hostChart),
            LegendDisplayMode.CustomOrNone => datasetsLegend,
            _ => throw new Exception("Invalid legend display mode"),
        };

        if (DataSetsLegend != null)
        {
            legendContainer.AddChild(DataSetsLegend.CreateLegend(dataSets, legendTitle, legendToolTips));
            legendContainer.Show();
        }

        // Wait until rect sizes settle down then we update visuals
        Invoke.Instance.Queue(() =>
        {
            drawArea.QueueRedraw();

            foreach (var data in dataSets.Keys)
            {
                FlattenLines(data);
            }
        });

        chartPopup.WindowTitle = Localization.Translate(ChartName);

        if (!isChild)
        {
            if (childChart == null)
                throw new Exception("Child chart is not initialized even though it should be");

            childChart.LegendMode = LegendMode;
            childChart.Plot(xAxisName, yAxisName, initialVisibleDataSets, legendTitle, datasetsLegend,
                defaultDataSet, expandedXTicks, expandedYTicks);
        }
    }

    /// <summary>
    ///   Wipes clean all node elements on this chart
    /// </summary>
    public void ClearChart()
    {
        ProperlyRemovePointAndLineToolTips();

        ProperlyRemoveLegendToolTips();

        // Clear lines
        foreach (var data in dataSets)
        {
            if (!dataLines.TryGetValue(data.Key, out var dataLine))
                continue;

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
        DataSetsLegend = null;

        // Clear abscissas
        horizontalLabelsContainer.QueueFreeChildren();

        // Clear ordinates
        verticalLabelsContainer.QueueFreeChildren();

        extraLegendContainer.QueueFreeChildren();
    }

    public DataSetVisibilityUpdateResult UpdateDataSetVisibility(string name, bool visible)
    {
        childChart?.UpdateDataSetVisibility(name, visible);

        if (!dataSets.TryGetValue(name, out var data))
            return DataSetVisibilityUpdateResult.NotFound;

        if (data.Draw == visible)
            return DataSetVisibilityUpdateResult.UnchangedValue;

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
        drawArea.QueueRedraw();

        if (dataLines.ContainsKey(name) && !initiallyVisible)
            FlattenLines(name);

        // Update the legend
        DataSetsLegend?.OnDataSetVisibilityChange(visible, name);

        if (StoredDatasetsVisibilityStatus.TryGetValue(ChartName, out var value))
        {
            value[name] = visible;
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
    public void AddIconLegend(Texture2D icon, string name, float size = 15)
    {
        if (isChild)
            return;

        var parent = new HBoxContainer();
        parent.AddThemeConstantOverride("separation", 7);

        var rect = new TextureRect
        {
            Texture = icon,
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            CustomMinimumSize = new Vector2(size, size),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
        };

        var label = new Label { Text = name };

        label.LabelSettings = legendLabelSettings;

        parent.AddChild(rect);
        parent.AddChild(label);

        extraLegendContainer.AddChild(parent);
    }

    public void OverrideDataPointToolTipDescription(string dataset, DataPoint datapoint, string description)
    {
        if (dataPointToolTips.ContainsKey(dataset) &&
            dataPointToolTips[dataset].TryGetValue(datapoint, out var tooltip))
        {
            tooltip.Description = description;
        }

        // Too much of a hassle than it benefits
        // ReSharper disable once MergeSequentialChecksWhenPossible
        if (childChart != null && childChart.dataPointToolTips.ContainsKey(dataset) &&
            childChart.dataPointToolTips[dataset].TryGetValue(datapoint, out var clonedTooltip))
        {
            clonedTooltip.Description = description;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (HorizontalLabelPath != null)
            {
                HorizontalLabelPath.Dispose();
                VerticalLabelPath.Dispose();
                VerticalTicksContainerPath.Dispose();
                HorizontalTicksContainerPath.Dispose();
                DrawAreaPath.Dispose();
                LegendsContainerPath.Dispose();
                ExtraLegendContainerPath.Dispose();
                InspectButtonPath.Dispose();
            }

            ProperlyRemovePointAndLineToolTips();
            ProperlyRemoveLegendToolTips();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///   Draws the chart visuals. The Drawer node connect its 'draw()' signal to here.
    /// </summary>
    private void RenderChart()
    {
        // Handle errors
        if (VisibleDataSets <= 0)
        {
            DrawErrorText(Localization.Translate("NO_DATA_TO_SHOW"));
        }
        else if (!IsMinMaxValid())
        {
            DrawErrorText(Localization.Translate("INVALID_DATA_TO_PLOT"));
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
        foreach (var tick in verticalLabelsContainer.GetChildren().OfType<Control>())
        {
            drawArea.DrawTextureRect(hLineTexture,
                new Rect2(new Vector2(0, tick.Position.Y + tick.Size.Y * 0.5f), drawArea.Size.X, 1), false,
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

            if (points.Count <= 0 || !dataLines.TryGetValue(data.Key, out var dataLine))
                continue;

            dataLine.Visible = data.Value.Draw;

            // Skip drawing if line isn't visible
            if (!dataLine.Visible)
                continue;

            // This is actually the first point (left-most)
            var previousPoint = points.Last();

            // Setup lines
            foreach (var point in points)
            {
                if (!point.IsInsideTree())
                    continue;

                var index = points.IndexOf(point);

                if (index < dataLine.Points.Length)
                {
                    dataLine.InterpolatePointPosition(index, point.Position + point.Size * 0.5f,
                        point.Coordinate);
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
            var newCollisionRect = new Control { Size = Vector2.One };

            newCollisionRect.Connect(Control.SignalName.MouseEntered,
                new Callable(dataLine, nameof(dataLine.OnMouseEnter)));
            newCollisionRect.Connect(Control.SignalName.MouseExited,
                new Callable(dataLine, nameof(dataLine.OnMouseExit)));

            if (!dataLineTooltips.TryGetValue(dataLine, out var currentDataLineToolTips))
            {
                currentDataLineToolTips = new DataLineToolTipData(TOOLTIP_GROUP_BASE_NAME + dataLine.GetInstanceId());
                dataLineTooltips.Add(dataLine, currentDataLineToolTips);
            }

            // TODO: can this also reuse tooltips like the data points?

            // Create tooltip
            var tooltip = ToolTipHelper.GetDefaultToolTip();

            tooltip.DisplayName = datasetName + "line" + firstPoint.Coordinate;
            tooltip.Description = datasetName;
            tooltip.DisplayDelay = 0.5f;

            newCollisionRect.RegisterToolTipForControl(tooltip, false);
            ToolTipManager.Instance.AddToolTip(tooltip, currentDataLineToolTips.GroupName);
            currentDataLineToolTips.ToolTips.Add((tooltip, newCollisionRect));

            dataLine.CollisionBoxes[firstPoint] = newCollisionRect;

            drawArea.AddChild(newCollisionRect);
        }

        // Update collider rect scaling and positioning

        var mouseCollider = dataLine.CollisionBoxes[firstPoint];

        // Position the collider at a middle point between two data point coordinates
        mouseCollider.Position = firstPoint.Coordinate.Lerp(secondPoint.Coordinate, 0.5f);

        // Set pivot at the center of the rect
        mouseCollider.PivotOffset = mouseCollider.Size / 2;

        // Use the distance between two coordinates as the length of the collider
        mouseCollider.Scale = new Vector2(firstPoint.Coordinate.DistanceTo(secondPoint.Coordinate) - firstPoint.Size.X,
            ((LineChartData)dataSets[datasetName]).LineWidth + 10);

        mouseCollider.Rotation = firstPoint.Coordinate.AngleToPoint(secondPoint.Coordinate);

        mouseCollider.Visible = dataSets[datasetName].Draw;
    }

    /// <summary>
    ///   Draws an error text on the center of the chart.
    /// </summary>
    private void DrawErrorText(string error)
    {
        var font = GetThemeFont("font", "Label");

        // Values are rounded to make the font not be blurry
        var position = new Vector2(Mathf.Round((drawArea.Size.X - font.GetStringSize(error).X) / 2),
            Mathf.Round(drawArea.Size.Y / 2));

        drawArea.DrawString(font, position, error);
    }

    /// <summary>
    ///   Sets the y coordinate for all of the given dataset's points at the bottom of the chart.
    ///   This is used to animate the lines rising from the bottom.
    /// </summary>
    private void FlattenLines(string datasetName)
    {
        if (!IsMinMaxValid())
            return;

        var data = dataSets[datasetName];

        foreach (var point in data.DataPoints)
        {
            if (!IsInstanceValid(point))
                continue;

            // First we move the point marker to the bottom of the chart
            point.SetCoordinate(new Vector2(ConvertToXCoordinate(point.X), drawArea.Size.Y), false);

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
                MaxValues = (Math.Max(point.X, MaxValues.X), Math.Max(point.Y, MaxValues.Y));
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
                MinValues = (Math.Min(point.X, MinValues.X), Math.Min(point.Y, MinValues.Y));
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
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            label.Text = Math.Round(i * (MaxValues.X - MinValues.X) / (XAxisTicks - 1) + MinValues.X, 1).FormatNumber();

            horizontalLabelsContainer.AddChild(label);
        }

        // Populate the columns (in reverse order)
        for (int i = YAxisTicks - 1; i >= 0; i--)
        {
            var label = new Label
            {
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };

            label.Text = Math.Round(i * (MaxValues.Y - MinValues.Y) / (YAxisTicks - 1) + MinValues.Y, 1).FormatNumber();

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
                    // same time because it can't be automatically made visible again later
                    point.SetCoordinate(new Vector2(0, drawArea.Size.Y), false);
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
        return new Vector2(ConvertToXCoordinate(value.X), ConvertToYCoordinate(value.Y));
    }

    private float ConvertToXCoordinate(double value)
    {
        var lineRectX = drawArea.Size.X / XAxisTicks;
        var lineRectWidth = lineRectX * (XAxisTicks - 1);
        var dx = MaxValues.X - MinValues.X;

        return (float)((value - MinValues.X) * lineRectWidth / dx) + lineRectX / 2;
    }

    private float ConvertToYCoordinate(double value)
    {
        var lineRectY = drawArea.Size.Y / YAxisTicks;
        var lineRectHeight = lineRectY * (YAxisTicks - 1);
        var dy = MaxValues.Y - MinValues.Y;

        return (float)(lineRectHeight - ((value - MinValues.Y) * lineRectHeight / dy) + lineRectY * 0.5f);
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

    private void SetupChartChild()
    {
        if (childChart != null || isChild)
            return;

        var scene = GD.Load<PackedScene>("res://src/gui_common/charts/line/LineChart.tscn");
        childChart = scene.Instantiate<LineChart>();

        childChart.parentChart = this;
        childChart.isChild = true;
        childChart.ChartName = ChartName + "clone";
        childChart.XAxisTicks = XAxisTicks;
        childChart.XAxisName = XAxisName;
        childChart.YAxisTicks = YAxisTicks;
        childChart.YAxisName = YAxisName;
        childChart.LegendMode = LegendMode;
        childChart.MaxIconLegend = MaxIconLegend;
        childChart.MaxDisplayedDataSet = MaxDisplayedDataSet;
        childChart.MinDisplayedDataSet = MinDisplayedDataSet;
        childChart.TooltipXAxisFormat = TooltipXAxisFormat;
        childChart.TooltipYAxisFormat = TooltipYAxisFormat;

        var parent = extraLegendContainer.GetParent().GetParent();
        parent.AddChild(childChart);
        parent.MoveChild(childChart, 0);

        childChart.inspectButton.Hide();
    }

    private void ProperlyRemovePointAndLineToolTips()
    {
        bool alreadyDetached = toolTipsDetached;

        // Remove tooltips correctly
        foreach (var entry in dataPointToolTips)
        {
            foreach (var tooltipEntry in entry.Value)
            {
                // Unset tooltip data on the control
                if (!alreadyDetached)
                    tooltipEntry.Key.UnRegisterToolTipForControl(tooltipEntry.Value);
            }

            // This automatically returns default tooltips to the cache
            ToolTipManager.Instance.ClearToolTips(TOOLTIP_GROUP_BASE_NAME + ChartName + entry.Key);
        }

        dataPointToolTips.Clear();

        // Remove tooltips from data lines as well
        foreach (var entry in dataLineTooltips)
        {
            foreach (var toolTipEntry in entry.Value.ToolTips)
            {
                if (!alreadyDetached)
                    toolTipEntry.Parent.UnRegisterToolTipForControl(toolTipEntry.ToolTip);
            }

            ToolTipManager.Instance.ClearToolTips(entry.Value.GroupName, true);
        }

        dataLineTooltips.Clear();
    }

    private void ProperlyRemoveLegendToolTips()
    {
        bool alreadyDetached = toolTipsDetached;

        foreach (var entry in legendToolTips)
        {
            if (!alreadyDetached)
                entry.Key.UnRegisterToolTipForControl(entry.Value);
        }

        legendToolTips.Clear();

        ToolTipManager.Instance.ClearToolTips("chartLegend" + ChartName);
    }

    private void TemporarilyUnregisterToolTips()
    {
        if (toolTipsDetached)
        {
            GD.PrintErr("Tooltips are already detached");
            return;
        }

        toolTipsDetached = true;

        foreach (var entry in dataPointToolTips)
        {
            foreach (var tooltipEntry in entry.Value)
            {
                tooltipEntry.Key.UnRegisterToolTipForControl(tooltipEntry.Value);
            }
        }

        foreach (var entry in dataLineTooltips)
        {
            foreach (var toolTipEntry in entry.Value.ToolTips)
            {
                toolTipEntry.Parent.UnRegisterToolTipForControl(toolTipEntry.ToolTip);
            }
        }

        foreach (var entry in legendToolTips)
        {
            entry.Key.UnRegisterToolTipForControl(entry.Value);
        }
    }

    private void ReRegisterDetachedToolTips()
    {
        if (!toolTipsDetached)
            return;

        foreach (var entry in dataPointToolTips)
        {
            foreach (var tooltipEntry in entry.Value)
            {
                tooltipEntry.Key.RegisterToolTipForControl(tooltipEntry.Value, false);
            }
        }

        foreach (var entry in dataLineTooltips)
        {
            foreach (var toolTipEntry in entry.Value.ToolTips)
            {
                toolTipEntry.Parent.RegisterToolTipForControl(toolTipEntry.ToolTip, false);
            }
        }

        foreach (var entry in legendToolTips)
        {
            entry.Key.RegisterToolTipForControl(entry.Value, false);
        }

        toolTipsDetached = false;
    }

    /// <summary>
    ///   Called to make sure there aren't pending tooltips to reattach when data is going to be recreated
    /// </summary>
    private void EnsureNoDetachedToolTipsExist()
    {
        ReRegisterDetachedToolTips();
    }

    // GUI Callbacks

    private void OnInspectButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        chartPopup.PopupCenteredShrink();
    }

    private void OnCloseButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        chartPopup.Hide();
    }

    // Subclasses

    /// <summary>
    ///   Shows an icon on a dataset legend
    /// </summary>
    public partial class DatasetsIconLegend : RefCounted, IDataSetsLegend
    {
        protected readonly LineChart chart;
        protected readonly List<DatasetIcon> icons = new();
        protected DataSetDictionary? datasets;

        public DatasetsIconLegend(LineChart chart)
        {
            this.chart = chart;
        }

        public virtual Control CreateLegend(DataSetDictionary datasets, string? title,
            Dictionary<Control, DefaultToolTip> createdToolTips)
        {
            _ = title;

            var hBox = new HBoxContainer { Alignment = AlignmentMode.End };
            hBox.AddThemeConstantOverride("separation", 0);

            this.datasets = datasets;

            foreach (var data in datasets)
            {
                var fallbackIconIsUsed = false;

                // Use the default icon as a fallback if the data icon texture hasn't been set already
                if (data.Value.Icon == null)
                {
                    data.Value.Icon = chart.defaultIconLegendTexture;
                    fallbackIconIsUsed = true;
                }

                var icon = new DatasetIcon(data.Key, chart, (LineChartData)data.Value, fallbackIconIsUsed);
                icons.Add(icon);

                hBox.AddChild(icon);

                icon.Connect(BaseButton.SignalName.Toggled,
                    Callable.From<bool>(toggled => OnIconLegendToggled(toggled, icon)));

                // Set initial icon toggle state
                if (!data.Value.Draw)
                {
                    icon.ButtonPressed = false;
                    OnDataSetVisibilityChange(false, data.Key);
                }

                // Create tooltips
                if (!createdToolTips.TryGetValue(icon, out var toolTip))
                {
                    toolTip = ToolTipHelper.GetDefaultToolTip();
                    createdToolTips.Add(icon, toolTip);
                    ToolTipManager.Instance.AddToolTip(toolTip, "chartLegend" + chart.ChartName);
                }

                toolTip.DisplayName = data.Key;
                toolTip.Description = data.Key;

                icon.RegisterToolTipForControl(toolTip, false);
            }

            return hBox;
        }

        public virtual void OnDataSetVisibilityChange(bool visible, string dataset)
        {
            if (datasets == null)
                throw new InvalidOperationException("Datasets are not loaded");

            var icon = icons.Find(i => i.DataName == dataset);

            if (icon == null)
                return;

            icon.ButtonPressed = visible;

            var data = datasets[dataset];

            if (icon.IsUsingFallbackIcon)
            {
                icon.Modulate = visible ? data.Colour : data.Colour.Darkened(0.5f);
            }
            else
            {
                icon.Modulate = visible ? Colors.White : Colors.Gray;
            }
        }

        public virtual object Clone()
        {
            return new DatasetsIconLegend(chart);
        }

        private void OnIconLegendToggled(bool toggled, DatasetIcon icon)
        {
            var result = chart.UpdateDataSetVisibility(icon.DataName, toggled);

            switch (result)
            {
                case DataSetVisibilityUpdateResult.MaxVisibleLimitReached:
                    icon.ButtonPressed = false;
                    ToolTipManager.Instance.ShowPopup(Localization.Translate("MAX_VISIBLE_DATASET_WARNING")
                        .FormatSafe(chart.MaxDisplayedDataSet), 1.0f);
                    break;
                case DataSetVisibilityUpdateResult.MinVisibleLimitReached:
                    icon.ButtonPressed = true;
                    ToolTipManager.Instance.ShowPopup(Localization.Translate("MIN_VISIBLE_DATASET_WARNING")
                        .FormatSafe(chart.MinDisplayedDataSet), 1.0f);
                    break;
            }
        }
    }

    /// <summary>
    ///   Shows a dropdown of selectable items to show in dataset
    /// </summary>
    public partial class DataSetsDropdownLegend : RefCounted, IDataSetsLegend
    {
#pragma warning disable CA2213
        protected LineChart chart;
#pragma warning restore CA2213

        public DataSetsDropdownLegend(LineChart chart)
        {
            this.chart = chart;
        }

        public CustomDropDown? Dropdown { get; protected set; }

        public virtual Control CreateLegend(DataSetDictionary datasets, string? title,
            Dictionary<Control, DefaultToolTip> createdToolTips)
        {
            Dropdown = new CustomDropDown
            {
                Flat = false,
                Text = title,
                FocusMode = FocusModeEnum.None,
            };

            Dropdown.Popup.HideOnCheckableItemSelection = false;
            Dropdown.Popup.HideOnItemSelection = false;

            foreach (var data in datasets)
                AddSpeciesToList(data);

            Dropdown.CreateElements();

            Dropdown.Popup.Connect(PopupMenu.SignalName.IndexPressed,
                new Callable(this, nameof(OnDropDownLegendItemSelected)));

            return Dropdown;
        }

        public virtual void OnDataSetVisibilityChange(bool visible, string dataset)
        {
            if (Dropdown == null)
                throw new InvalidOperationException("Legend is not created");

            var indices = Dropdown.GetItemIndex(dataset);

            foreach (var index in indices)
                Dropdown.Popup.SetItemChecked(index, visible);
        }

        public virtual object Clone()
        {
            return new DataSetsDropdownLegend(chart);
        }

        protected void AddSpeciesToList(KeyValuePair<string, ChartDataSet> data, string section = "default")
        {
            if (Dropdown == null)
                throw new InvalidOperationException("Legend is not created");

            // Use the default icon as a fallback if the data icon texture hasn't been set already
            data.Value.Icon ??= chart.defaultIconLegendTexture;

            // Use the DataColor as the icon's color if using the default icon
            var colorToUse = data.Value.Icon == chart.defaultIconLegendTexture ?
                data.Value.Colour :
                new Color(1, 1, 1);

            var item = Dropdown.AddItem(data.Key, !chart.dataLines[data.Key].Default, colorToUse, data.Value.Icon,
                section);
            item.Checked = data.Value.Draw;
        }

        private void OnDropDownLegendItemSelected(int index)
        {
            if (Dropdown?.Popup.IsItemCheckable(index) != true)
                return;

            var result = chart.UpdateDataSetVisibility(Dropdown.Popup.GetItemText(index),
                !Dropdown.Popup.IsItemChecked(index));

            switch (result)
            {
                case DataSetVisibilityUpdateResult.MaxVisibleLimitReached:
                    ToolTipManager.Instance.ShowPopup(Localization.Translate("MAX_VISIBLE_DATASET_WARNING")
                        .FormatSafe(chart.MaxDisplayedDataSet), 1.0f);
                    break;
                case DataSetVisibilityUpdateResult.MinVisibleLimitReached:
                    ToolTipManager.Instance.ShowPopup(Localization.Translate("MIN_VISIBLE_DATASET_WARNING")
                        .FormatSafe(chart.MinDisplayedDataSet), 1.0f);
                    break;
            }
        }
    }

    /// <summary>
    ///   Icon for a dataset
    /// </summary>
    public partial class DatasetIcon : TextureButton
    {
        public readonly string DataName;
        public readonly bool IsUsingFallbackIcon;

        private readonly LineChart chart;
        private readonly LineChartData data;

        private readonly NodePath scaleReference = new("scale");

        public DatasetIcon(string name, LineChart chart, LineChartData data, bool isUsingFallbackIcon)
        {
            DataName = name;
            this.chart = chart;
            this.data = data;
            IsUsingFallbackIcon = isUsingFallbackIcon;
            CustomMinimumSize = new Vector2(18, 18);
            FocusMode = FocusModeEnum.None;
            ToggleMode = true;
            ButtonPressed = true;
            TextureNormal = data.Icon;
            StretchMode = StretchModeEnum.KeepAspectCentered;
            IgnoreTextureSize = true;
            PivotOffset = CustomMinimumSize / 2;

            // Set the default icon's color
            if (isUsingFallbackIcon)
                Modulate = data.Colour;

            Connect(Control.SignalName.MouseEntered, new Callable(this, nameof(IconLegendMouseEnter)));
            Connect(Control.SignalName.MouseExited, new Callable(this, nameof(IconLegendMouseExit)));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                scaleReference.Dispose();
            }

            base.Dispose(disposing);
        }

        private void IconLegendMouseEnter()
        {
            var tween = CreateTween();
            tween.TweenProperty(this, scaleReference, new Vector2(1.1f, 1.1f), 0.1);

            // Highlight icon
            Modulate = IsUsingFallbackIcon ? data.Colour.Lightened(0.5f) : Colors.LightGray;

            chart.dataLines[DataName].OnMouseEnter();
        }

        private void IconLegendMouseExit()
        {
            var tween = CreateTween();
            tween.TweenProperty(this, scaleReference, Vector2.One, 0.1);

            if (ButtonPressed)
            {
                // Reset icon color
                Modulate = IsUsingFallbackIcon ? data.Colour : Colors.White;
            }
            else
            {
                Modulate = Colors.DarkGray;
            }

            chart.dataLines[DataName].OnMouseExit();
        }
    }

    /// <summary>
    ///   Used as the chart's dataset line segments. Contains mouse collision boxes and
    ///   mouse enter/exit callback to make the line interactable.
    /// </summary>
    private partial class DataLine : Line2D
    {
        /// <summary>
        ///   The dataset lines will always be visible and can't be made hidden.
        /// </summary>
        public readonly bool Default;

        public readonly Dictionary<DataPoint, Control> CollisionBoxes = new();

        private readonly LineChartData data;

        private readonly Callable positionSetCallable;

        private Color dataColour;

        public DataLine(LineChartData data, bool isDefault, float widthMultiplier = 1)
        {
            this.data = data;
            Default = isDefault;

            Width = data.LineWidth * widthMultiplier;
            DefaultColor = data.Colour;
            dataColour = data.Colour;
            Texture = GD.Load<Texture2D>("res://assets/textures/gui/bevel/line.png");
            TextureMode = LineTextureMode.Stretch;

            // Antialiasing is turned off as it's a bit unreliable currently.
            // In the meantime we use a workaround by assigning a texture with transparent 1-pixel border
            // on top and bottom to emulate some antialiasing.

            positionSetCallable = new Callable(this, nameof(ChangePointPos));
        }

        public void InterpolatePointPosition(int i, Vector2 initialPos, Vector2 targetPos)
        {
            var finalValue = new Vector3(i, targetPos.X, targetPos.Y);

            var tween = CreateTween();
            tween.SetTrans(Tween.TransitionType.Expo);
            tween.SetEase(Tween.EaseType.Out);

            tween.TweenMethod(positionSetCallable, new Vector3(i, initialPos.X, initialPos.Y), finalValue, 0.5);
        }

        public void OnMouseEnter()
        {
            var highlightColour = dataColour.IsLuminuous() ?
                dataColour.Darkened(0.5f) :
                dataColour.Lightened(0.5f);

            DefaultColor = highlightColour;
            data.Colour = highlightColour;
        }

        public void OnMouseExit()
        {
            DefaultColor = dataColour;
            data.Colour = dataColour;
        }

        /// <summary>
        ///   This is a really hacky way to get Godot to tween methods with multiple parameters.
        ///   Got extremely lucky that all parameters can fit into a single Godot primitive type here...
        /// </summary>
        private void ChangePointPos(Vector3 arguments)
        {
            SetPointPosition((int)arguments.X, new Vector2(arguments.Y, arguments.Z));
        }
    }

    /// <summary>
    ///   Holds the tooltip data for a data line, needed to be able to release the data even after the controls are
    ///   disposed
    /// </summary>
    private class DataLineToolTipData
    {
        public readonly List<(DefaultToolTip ToolTip, Control Parent)> ToolTips = new();
        public readonly string GroupName;

        public DataLineToolTipData(string groupName)
        {
            GroupName = groupName;
        }
    }
}
