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
    ///   Limits how many icon legends should be shown
    /// </summary>
    public int MaxIconLegend = 10;

    /// <summary>
    ///   Limits how many dataset lines should be allowed to be shown on the chart.
    /// </summary>
    public int MaxDisplayedDataSet = 10;

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
    ///   Lines for each of the plotted datasets
    /// </summary>
    private Dictionary<string, DataLine> dataLines = new Dictionary<string, DataLine>();

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
        VisibleLimitReached,

        /// <summary>
        ///   The dataset visibility is succesfully changed.
        /// </summary>
        Success,
    }

    /// <summary>
    ///   The lowest data point value from all the datasets.
    /// </summary>
    public Vector2 MinValues { get; private set; }

    /// <summary>
    ///   The highest data point value from all the datasets.
    /// </summary>
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

    public override void _Process(float delta)
    {
        // https://github.com/Revolutionary-Games/Thrive/issues/1976
        if (delta <= 0)
            return;

        // Apply coordinates (with interpolation for smooth animations)
        foreach (var data in dataSets)
        {
            foreach (var point in data.Value.DataPoints)
            {
                if (!IsInstanceValid(point))
                    continue;

                var coordinate = ConvertToCoordinate(point.Value);

                if (point.Coordinate == coordinate)
                    continue;

                // TODO: Handle overlapping data point markers, will be difficult.
                // As a workaround, players could instead manually hide the dataset
                // of one of the overlapping points.

                point.Coordinate = point.Coordinate.LinearInterpolate(coordinate, 3.0f * delta);
                UpdateLineSegments();
            }
        }
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
    /// <param name="xAxisName">Overrides the horizontal axis label title</param>
    /// <param name="yAxisName">Overrides the vertical axis label title</param>
    /// <param name="initialVisibleDataSets">How many datasets should be visible initially after plotting</param>
    /// <param name="legendTitle">Title for the chart legend. If null, the legend will not be created</param>
    /// <param name="defaultDataSet">The name of dataset that'll always be shown after plotting</param>
    public void Plot(string xAxisName, string yAxisName, int initialVisibleDataSets,
        string legendTitle = null, string defaultDataSet = null)
    {
        ClearChart();

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

        XAxisName = string.IsNullOrEmpty(xAxisName) ? XAxisName : xAxisName;
        YAxisName = string.IsNullOrEmpty(yAxisName) ? YAxisName : yAxisName;

        initialVisibleDataSets = Mathf.Clamp(initialVisibleDataSets, 0, MaxDisplayedDataSet);

        // Start from 1 if defaultDataSet is specified as it's always visible
        var visibleDataSetCount = string.IsNullOrEmpty(defaultDataSet) ? 0 : 1;

        foreach (var data in dataSets)
        {
            // Hide the rest, if number of shown dataset exceeds initialVisibleDataSets
            if (visibleDataSetCount >= initialVisibleDataSets && data.Key != defaultDataSet)
            {
                UpdateDataSetVisibility(data.Key, false);
            }
            else if (visibleDataSetCount < initialVisibleDataSets && data.Key != defaultDataSet)
            {
                visibleDataSetCount++;
            }

            foreach (var point in data.Value.DataPoints)
            {
                // Find the max values of all the data points first to make finding min values possible
                MaxValues = new Vector2(Mathf.Max(point.Value.x, MaxValues.x), Mathf.Max(point.Value.y, MaxValues.y));

                // Create tooltip for the point markers
                var toolTip = ToolTipHelper.CreateDefaultToolTip();

                toolTip.DisplayName = data.Key + point.Value;
                toolTip.Description = $"{data.Key}\n{((double)point.Value.x).FormatNumber()} {XAxisName}\n" +
                    $"{((double)point.Value.y).FormatNumber()} {YAxisName}";
                toolTip.DisplayDelay = 0;
                toolTip.HideOnMousePress = false;
                toolTip.UseFadeIn = false;

                point.RegisterToolTipForControl(toolTip, toolTipCallbacks);
                ToolTipManager.Instance.AddToolTip(toolTip, "chartMarkers" + ChartName + data.Key);
            }

            // Initialize line
            var dataLine = new DataLine(data.Value, data.Key == defaultDataSet);
            dataLines[data.Key] = dataLine;
            drawArea.AddChild(dataLine);
        }

        MinValues = MaxValues;

        // Find the minimum values of all the data points
        foreach (var data in dataSets)
        {
            foreach (var point in data.Value.DataPoints)
            {
                MinValues = new Vector2(Mathf.Min(point.Value.x, MinValues.x), Mathf.Min(point.Value.y, MinValues.y));
            }
        }

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
            // Switch to dropdown if number of datasets exceeds the max amount of icon legends
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

        foreach (var data in dataSets.Keys)
        {
            FlattenLines(data);
        }
    }

    public void ClearChart()
    {
        toolTipCallbacks.Clear();

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
    }

    public DataSetVisibilityUpdateResult UpdateDataSetVisibility(string name, bool visible)
    {
        if (!dataSets.ContainsKey(name))
            return DataSetVisibilityUpdateResult.NotFound;

        var data = dataSets[name];

        if (visible && VisibleDataSetLimitReached)
            return DataSetVisibilityUpdateResult.VisibleLimitReached;

        if (dataLines.ContainsKey(name) && !data.Draw)
            FlattenLines(name);

        data.Draw = visible;
        drawArea.Update();

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

            var icon = new IconLegend(data.Key, data.Value, fallbackIconIsUsed);

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

            icon.RegisterToolTipForControl(toolTip, toolTipCallbacks);
            ToolTipManager.Instance.AddToolTip(toolTip, "chartLegend" + ChartName);
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
    ///   Draw the chart visuals. Mainly used by the Drawer node to connect its 'draw()' signal here.
    /// </summary>
    private void RenderChart()
    {
        if (dataSets.Count <= 0)
            return;

        DrawOrdinateLines();
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
    ///   Connects the points with line segments
    /// </summary>
    private void UpdateLineSegments()
    {
        foreach (var data in dataSets)
        {
            // Create into List so we can use IndexOf
            var points = data.Value.DataPoints.ToList();

            if (points.Count <= 0)
                continue;

            // Setup the points (applying coordinate)
            foreach (var point in points)
            {
                // Skip if any of the min and max value is equal, otherwise
                // the data point marker kind of just glitch out.
                if (MinValues.x == MaxValues.x || MinValues.y == MaxValues.y)
                    continue;

                // Add the marker if not yet, this is called here so the node will
                // be rendered on top of the line segments
                if (!point.IsInsideTree())
                    drawArea.AddChild(point);
            }

            if (!dataLines.ContainsKey(data.Key))
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
                    dataLine.SetPointPosition(index, point.Coordinate);
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

            newCollisionRect.RegisterToolTipForControl(tooltip, toolTipCallbacks);
            ToolTipManager.Instance.AddToolTip(tooltip, "chartMarkers");

            dataLine.CollisionBoxes[firstPoint] = newCollisionRect;

            drawArea.AddChild(newCollisionRect);
        }

        // Update collider rect scaling and positioning

        var mouseCollider = dataLine.CollisionBoxes[firstPoint];

        // Position the collider at a middle point between two data point coordinates
        mouseCollider.RectPosition = new Vector2(
            (firstPoint.Coordinate.x + secondPoint.Coordinate.x) / 2,
            (firstPoint.Coordinate.y + secondPoint.Coordinate.y) / 2);

        // Set pivot at the center of the rect
        mouseCollider.RectPivotOffset = mouseCollider.RectSize / 2;

        // Use the distance between two coordinates as the collider's length
        mouseCollider.RectScale = new Vector2(
            firstPoint.Coordinate.DistanceTo(secondPoint.Coordinate) - firstPoint.RectSize.x,
            dataSets[datasetName].LineWidth + 10);

        mouseCollider.RectRotation = Mathf.Rad2Deg(firstPoint.Coordinate.AngleToPoint(secondPoint.Coordinate));

        mouseCollider.Visible = dataSets[datasetName].Draw;
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
            // Had to apply the coordinate on the next frame to compensate with Godot's UI update delay,
            // so the coordinates could be correctly calculated (since it depends on the Control container
            // rect sizes) just after Plot() call. This may result to a subtle glitchy look in the first
            // few frame where all the points were postioned at the top-left. But this works just fine for now.
            Invoke.Instance.Queue(() =>
            {
                if (!IsInstanceValid(point))
                    return;

                point.Coordinate = new Vector2(
                    ConvertToXCoordinate(point.Value.x), drawArea.RectSize.y);
            });
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

    private void UpdateIconLegend(bool toggled, string name)
    {
        if (LegendMode != LegendDisplayMode.Icon)
            return;

        var icon = legendContainer.GetChildren()
            .Cast<IconLegend>()
            .ToList()
            .Find(i => i.DataName == name);

        if (icon == null)
            return;

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

        // It's assumed the child is a dropdown node
        var dropDown = legendContainer.GetChildOrNull<CustomDropDown>(0);

        dropDown?.Popup.SetItemChecked(dropDown.GetItemIndex(item), toggled);
    }

    /*
        GUI Callbacks
    */

    private void OnVisibilityChanged()
    {
        if (!Visible)
            return;

        foreach (var data in dataSets.Keys)
        {
            FlattenLines(data);
        }
    }

    private void OnIconLegendToggled(bool toggled, IconLegend icon)
    {
        var result = UpdateDataSetVisibility(icon.DataName, toggled);

        if (result == DataSetVisibilityUpdateResult.VisibleLimitReached)
        {
            icon.Pressed = false;
            ToolTipManager.Instance.ShowPopup($"Not allowed to show more than {MaxDisplayedDataSet} datasets!", 1f);
        }
    }

    private void OnDropDownLegendItemSelected(int index, CustomDropDown dropDown)
    {
        if (!dropDown.Popup.IsItemCheckable(index))
            return;

        var result = UpdateDataSetVisibility(
            dropDown.Popup.GetItemText(index), !dropDown.Popup.IsItemChecked(index));

        if (result == DataSetVisibilityUpdateResult.VisibleLimitReached)
        {
            ToolTipManager.Instance.ShowPopup($"Not allowed to show more than {MaxDisplayedDataSet} datasets!", 1f);
        }
    }

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

        public DataLine(LineChartData data, bool isDefault)
        {
            this.data = data;
            Default = isDefault;

            Width = data.LineWidth;
            DefaultColor = data.DataColour;

            // Antialiasing is turned off as it's a bit unreliable currently
        }

        public void OnMouseEnter()
        {
            DefaultColor = data.DataColour.IsLuminuous() ?
                data.DataColour.Darkened(0.5f) :
                data.DataColour.Lightened(0.5f);
        }

        public void OnMouseExit()
        {
            DefaultColor = data.DataColour;
        }
    }

    private class IconLegend : TextureButton
    {
        public readonly string DataName;
        public readonly bool IsUsingFallbackIcon;

        private LineChartData data;
        private Tween tween;

        public IconLegend(string name, LineChartData data, bool isUsingFallbackIcon)
        {
            DataName = name;
            this.data = data;
            IsUsingFallbackIcon = isUsingFallbackIcon;
            Expand = true;
            RectMinSize = new Vector2(18, 18);
            EnabledFocusMode = FocusModeEnum.None;
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
            Connect("pressed", this, nameof(IconLegendPressed));

            tween = new Tween();
            AddChild(tween);
        }

        private void IconLegendMouseEnter()
        {
            tween.InterpolateProperty(this, "rect_scale", Vector2.One, new Vector2(1.1f, 1.1f), 0.1f);
            tween.Start();

            if (Pressed)
            {
                // Adjust the icon color to be highlighted
                Modulate = IsUsingFallbackIcon ? data.DataColour.Lightened(0.5f) : new Color(0.7f, 0.7f, 0.7f);
            }
        }

        private void IconLegendMouseExit()
        {
            tween.InterpolateProperty(this, "rect_scale", new Vector2(1.1f, 1.1f), Vector2.One, 0.1f);
            tween.Start();

            if (Pressed)
            {
                // Adjust the icon color back to normal
                Modulate = IsUsingFallbackIcon ? data.DataColour : Colors.White;
            }
        }

        private void IconLegendPressed()
        {
            tween.InterpolateProperty(this, "rect_scale", new Vector2(0.8f, 0.8f), Vector2.One, 0.1f);
            tween.Start();
        }
    }
}
