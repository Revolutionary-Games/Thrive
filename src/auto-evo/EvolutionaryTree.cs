using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoEvo;
using Godot;

/// <summary>
///   An evolutionary tree showing species origins and mutations. Supports dragging and zooming.
/// </summary>
/// <remarks>
///   <para>
///     To use this, simply add this inside any controller and adjust its size.
///     Don't put this under a draggable controller.
///   </para>
/// </remarks>
public partial class EvolutionaryTree : Control
{
    [Export]
    public NodePath? TimelinePath;

    [Export]
    public NodePath TreePath = null!;

    private const float TIMELINE_HEIGHT = 50.0f;
    private const float TIMELINE_LINE_THICKNESS = 2.0f;
    private const float TIMELINE_AXIS_Y = 5.0f;
    private const float TIMELINE_MARK_LENGTH = 4.0f;
    private const float TREE_LINE_THICKNESS = 4.0f;

    /// <summary>
    ///   Vertical separation between species.
    /// </summary>
    private const float SPECIES_SEPARATION = 50.0f;

    /// <summary>
    ///   Horizontal separation between generations.
    /// </summary>
    private const float GENERATION_SEPARATION = 100.0f;

    /// <summary>
    ///   The horizontal offset where species name is drawn to the right of the button or line
    /// </summary>
    private const float SPECIES_NAME_OFFSET = 10.0f;

    /// <summary>
    ///   Each time the control is zoomed, sizeFactor gets multiplied (or divided) by this factor.
    ///   This allows for smooth zooming, that zoom will not seem too quick when small, nor too slow when big.
    /// </summary>
    private const float ZOOM_FACTOR = 0.9f;

    private const float SIZE_FACTOR_MIN = 0.2f;
    private const float SIZE_FACTOR_MAX = 1.0f;

    /// <summary>
    ///   Draws outside this margin will be omitted to improve performance.
    /// </summary>
    private const float DRAW_MARGIN = 30.0f;

    /// <summary>
    ///   Default size of <see cref="EvolutionaryTreeNode"/>.
    /// </summary>
    private static readonly Vector2 TreeNodeSize = new(30, 30);

    /// <summary>
    ///   Auxiliary vector for <see cref="DRAW_MARGIN"/>
    /// </summary>
    private static readonly Vector2 DrawMargin = new(DRAW_MARGIN, DRAW_MARGIN);

    /// <summary>
    ///   Stores the created nodes for species by the species ids
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     ReSharper disable RedundantNameQualifier
    ///   </para>
    /// </remarks>
    private readonly System.Collections.Generic.Dictionary<uint, List<EvolutionaryTreeNode>> speciesNodes = new();

    private readonly System.Collections.Generic.Dictionary<uint, string> speciesNames = new();

    private readonly System.Collections.Generic.Dictionary<uint, (uint ParentSpeciesID, int SplitGeneration)>
        speciesOrigin = new();

    private readonly System.Collections.Generic.Dictionary<int, double> generationTimes = new();

    // ReSharper enable RedundantNameQualifier

    /// <summary>
    ///   All EvolutionaryTreeNodes are in this group so that they work as radio buttons.
    /// </summary>
    private readonly ButtonGroup nodesGroup = new();

#pragma warning disable CA2213

    /// <summary>
    ///   Timeline part of <see cref="EvolutionaryTree"/>. Consists of an axis and multiple time marks.
    /// </summary>
    private Control timeline = null!;

    /// <summary>
    ///   Tree part of <see cref="EvolutionaryTree"/>. Consists of many buttons and connection lines.
    /// </summary>
    private Control tree = null!;

    // Local copy of fonts
    private LabelSettings bodyItalicFont = null!;
    private LabelSettings bodyFont = null!;

    private PackedScene treeNodeScene = null!;
#pragma warning restore CA2213

    /// <summary>
    ///   Drag offset relative to tree.
    /// </summary>
    private Vector2 dragOffset;

    private bool dragging;
    private Vector2 lastMousePosition;

    /// <summary>
    ///   Default size of lato-small font.
    /// </summary>
    private float smallFontSize;

    /// <summary>
    ///   The tree's size factor.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     <see cref="Control.Scale"/> is not used here so that we know the drawn parts.
    ///   </para>
    /// </remarks>
    private float sizeFactor = 1.0f;

    /// <summary>
    ///   If redraw is needed.
    /// </summary>
    private bool dirty;

    private uint maxSpeciesId;

    private int latestGeneration;

    [Signal]
    public delegate void SpeciesSelectedEventHandler(int generation, uint id);

    /// <summary>
    ///   Allows access to tree-generated data. This helps with data exporting, for example.
    /// </summary>
    public IReadOnlyDictionary<uint, string> CurrentWorldSpecies => speciesNames;

    public IReadOnlyDictionary<uint, (uint ParentSpeciesId, int SplitGeneration)> SpeciesOrigin => speciesOrigin;

    private Vector2 TreeSize =>
        new(latestGeneration * GENERATION_SEPARATION + 200, maxSpeciesId * SPECIES_SEPARATION + 100);

    public override void _Ready()
    {
        base._Ready();

        timeline = GetNode<Control>(TimelinePath);
        tree = GetNode<Control>(TreePath);

        treeNodeScene = GD.Load<PackedScene>("res://src/auto-evo/EvolutionaryTreeNode.tscn");

        // Font size is adjusted dynamically so this needs to be a copy.
        bodyItalicFont = GD.Load<LabelSettings>("res://src/gui_common/fonts/Body-Italic-Small.tres");
        smallFontSize = bodyItalicFont.FontSize;

        bodyFont = GD.Load<LabelSettings>("res://src/gui_common/fonts/Body-Regular-Small.tres");
    }

    public void Init(IEnumerable<Species> initialSpecies, uint playerSpeciesId = 1,
        string? updatedPlayerSpeciesName = null)
    {
        foreach (var species in initialSpecies)
        {
            SetupTreeNode(species, null, 0);

            speciesOrigin.Add(species.ID, (uint.MaxValue, 0));
            speciesNames.Add(species.ID, species.FormattedName);
        }

        if (speciesNames.Count == 0)
            throw new ArgumentException("No initial species provided");

        if (updatedPlayerSpeciesName != null)
            speciesNames[playerSpeciesId] = updatedPlayerSpeciesName;

        generationTimes.Add(0, 0);
        dirty = true;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (dirty)
        {
            UpdateTreeNodeSizeAndPosition();

            // Inform them to update
            timeline.QueueRedraw();
            tree.QueueRedraw();

            dirty = false;
        }
    }

    public void Update(System.Collections.Generic.Dictionary<uint, SpeciesRecordFull> records, int generation,
        double time, uint playerSpeciesID)
    {
        foreach (var speciesRecordPair in records.OrderBy(r => r.Key))
        {
            var speciesID = speciesRecordPair.Key;
            var record = speciesRecordPair.Value;

            if (record.Population <= 0)
            {
                if (record.SplitFromID == null)
                {
                    SetupTreeNode(record.Species,
                        speciesNodes[speciesID].Last(), generation, true);
                }
            }
            else if (record.SplitFromID != null)
            {
                var splitFromID = (uint)record.SplitFromID;
                SetupTreeNode(record.Species, speciesNodes[splitFromID].Last(), generation);

                speciesOrigin.Add(speciesID, (splitFromID, generation));

                // If the player switches to a new species, that can cause that a species name already exists, so
                // names are only added if not already there to prevent player set names from being overwritten
                if (!speciesNames.ContainsKey(speciesID))
                    speciesNames.Add(speciesID, record.Species.FormattedName);
            }
            else if (record.MutatedPropertiesID != null)
            {
                SetupTreeNode(record.Species,
                    speciesNodes[speciesID].Last(), generation);
            }
            else if (speciesID == playerSpeciesID)
            {
                // Always add nodes for the player species
                SetupTreeNode(record.Species,
                    speciesNodes[speciesID].Last(), generation);
            }

            if (speciesID > maxSpeciesId)
                maxSpeciesId = speciesID;
        }

        latestGeneration = generation;
        generationTimes[generation] = time;

        BuildTree();

        dirty = true;
    }

    public void Clear()
    {
        speciesOrigin.Clear();
        speciesNames.Clear();
        generationTimes.Clear();
        speciesNodes.Clear();
        maxSpeciesId = 0;
        latestGeneration = 0;
        dragOffset = Vector2.Zero;

        tree.QueueFreeChildren();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (TimelinePath != null)
            {
                TimelinePath.Dispose();
                TreePath.Dispose();
                nodesGroup.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void SetupTreeNode(Species species, EvolutionaryTreeNode? parent, int generation,
        bool isLastGeneration = false)
    {
        var position = new Vector2(generation * GENERATION_SEPARATION, 0);

        if (!speciesNodes.TryGetValue(species.ID, out var speciesNodeList))
        {
            speciesNodeList = new List<EvolutionaryTreeNode>();
            speciesNodes.Add(species.ID, speciesNodeList);
        }

        // If there is already one, update it; otherwise, add a new one.
        var existing =
            speciesNodeList.FirstOrDefault(n => Math.Abs(n.LogicalPosition.X - position.X) < MathUtils.EPSILON);
        var node = existing ?? treeNodeScene.Instantiate<EvolutionaryTreeNode>();

        node.Generation = generation;
        node.SpeciesID = species.ID;
        node.ParentNode = parent;
        node.LogicalPosition = position;
        node.LastGeneration = isLastGeneration;

        // The remaining part only needs to be done when it is a new node.
        if (existing != null)
            return;

        node.ButtonGroup = nodesGroup;
        node.Connect(BaseButton.SignalName.Pressed, Callable.From(() => OnTreeNodeSelected(node)));

        speciesNodeList.Add(node);
        tree.AddChild(node);
    }

    private void UpdateTreeNodeSizeAndPosition()
    {
        var treeNodeSize = sizeFactor * TreeNodeSize;

        foreach (var node in speciesNodes.Values.SelectMany(l => l))
        {
            node.CustomMinimumSize = treeNodeSize;

            // RectSize needs to be adjusted explicitly to force the size to change
            node.Size = treeNodeSize;

            node.Position = sizeFactor * (node.LogicalPosition + dragOffset);
        }
    }

    private void BuildTree()
    {
        uint index = 0;

        foreach (var root in speciesOrigin.Where(o => o.Value.ParentSpeciesID == uint.MaxValue))
        {
            BuildTree(root.Key, ref index);
        }
    }

    private void BuildTree(uint id, ref uint index)
    {
        // Adjust nodes of this species' vertical position based on index
        foreach (var treeNode in speciesNodes[id])
        {
            var position = treeNode.LogicalPosition;
            position.Y = index * SPECIES_SEPARATION;
            treeNode.LogicalPosition = position;
            treeNode.Position = position;
        }

        ++index;

        // Search for derived species and do this recursively.
        // The later a species derived, the closer it is to its parent. This avoids any crossings in the tree.
        foreach (var child in speciesOrigin.Where(p => p.Value.ParentSpeciesID == id)
                     .OrderByDescending(p => p.Value.SplitGeneration)
                     .Select(p => p.Key))
        {
            BuildTree(child, ref index);
        }
    }

    /// <summary>
    ///   <see cref="Control._Draw"/> of <see cref="timeline"/>
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Timeline only responds to horizontal drag.
    ///   </para>
    /// </remarks>
    private void TimelineDraw()
    {
        // Draw timeline axis, which is static.
        timeline.DrawLine(new Vector2(0, TIMELINE_AXIS_Y),
            new Vector2(Size.X, TIMELINE_AXIS_Y), Colors.Cyan, TIMELINE_LINE_THICKNESS);

        int increment = (int)Math.Ceiling(1 / sizeFactor);

        // Draw time marks
        int firstDrawnGeneration =
            (int)Math.Ceiling((-dragOffset.X - TreeNodeSize.X / 2) / GENERATION_SEPARATION / increment) * increment;

        int lastDrawnGeneration =
            Math.Min((int)Math.Floor((Size.X / sizeFactor - dragOffset.X - TreeNodeSize.X / 2) /
                GENERATION_SEPARATION), latestGeneration);

        for (int i = firstDrawnGeneration; i <= lastDrawnGeneration; i += increment)
        {
            var x = sizeFactor * (dragOffset.X + i * GENERATION_SEPARATION + TreeNodeSize.X / 2);

            timeline.DrawLine(new Vector2(x, TIMELINE_AXIS_Y), new Vector2(x, TIMELINE_AXIS_Y + TIMELINE_MARK_LENGTH),
                Colors.Cyan, TIMELINE_LINE_THICKNESS);

            var localizedText = string.Empty;
            if (generationTimes.TryGetValue(i, out var generationTime))
            {
                localizedText = string.Format(CultureInfo.CurrentCulture, "{0:#,##0,,}", generationTime) + " "
                    + Localization.Translate("MEGA_YEARS");
            }

            timeline.DrawString(bodyFont.Font,
                new Vector2(x, TIMELINE_AXIS_Y + TIMELINE_MARK_LENGTH * 2 + bodyFont.FontSize), localizedText,
                HorizontalAlignment.Center, -1,
                bodyFont.FontSize, Colors.Cyan);
        }
    }

    /// <summary>
    ///   _GUIInput for <see cref="timeline"/> and <see cref="tree"/>.
    /// </summary>
    /// <param name="event">Godot input event, see <see cref="Control._GuiInput"/></param>
    /// <param name="horizontalOnly">
    ///   Binding parameter. If set to true, only horizontal offset will be considered.
    /// </param>
    private void GUIInput(InputEvent @event, bool horizontalOnly)
    {
        if (@event is not InputEventMouse)
            return;

        if (@event is InputEventMouseButton buttonEvent)
        {
            dragging = (buttonEvent.ButtonMask & (MouseButtonMask.Left | MouseButtonMask.Right)) != 0;
            if (dragging)
                lastMousePosition = buttonEvent.Position;

            if (buttonEvent.Pressed && buttonEvent.ButtonIndex is MouseButton.WheelDown or MouseButton.WheelUp)
            {
                bool zoomIn = buttonEvent.ButtonIndex == MouseButton.WheelUp;

                // The current mouse position relative to tree
                var mouseTreePosition = buttonEvent.Position / sizeFactor - dragOffset;

                // Update size factor
                sizeFactor =
                    Mathf.Clamp(sizeFactor * (float)Math.Pow(ZOOM_FACTOR, buttonEvent.Factor * (zoomIn ? -1 : 1)),
                        SIZE_FACTOR_MIN, SIZE_FACTOR_MAX);

                // Update drag offset so that the mouseTreePosition stays the same
                dragOffset = buttonEvent.Position / sizeFactor - mouseTreePosition;

                BindOffsetToTreeSize();
                dirty = true;
            }
        }
        else if (@event is InputEventMouseMotion motionEvent)
        {
            if (dragging)
            {
                var delta = (motionEvent.Position - lastMousePosition) / sizeFactor;
                dragOffset += horizontalOnly ? new Vector2(delta.X, 0) : delta;
                lastMousePosition = motionEvent.Position;
                BindOffsetToTreeSize();
                dirty = true;
            }
        }
    }

    private void BindOffsetToTreeSize()
    {
        // TreeSize may be less than RectSize, so the later Min and Max is not merged into Clamp.
        // Note that dragOffset's x and y should both be negative.
        var start = tree.Size / sizeFactor - TreeSize;

        float x = dragOffset.X;
        x = Math.Max(x, start.X);
        x = Math.Min(x, 0);

        float y = dragOffset.Y;
        y = Math.Max(y, start.Y);
        y = Math.Min(y, 0);

        dragOffset = new Vector2(x, y);
    }

    /// <summary>
    ///   Stop dragging when mouse exit <see cref="timeline"/> or <see cref="tree"/>
    /// </summary>
    private void MouseExit()
    {
        dragging = false;
    }

    /// <summary>
    ///   <see cref="Control._Draw"/> of <see cref="tree"/>
    /// </summary>
    private void TreeDraw()
    {
        // TODO: isn't this slightly outside the bounds of the tree control?
        var drawRegion = new Rect2(Position - DrawMargin, Size + DrawMargin);

        // Draw new species connection lines
        foreach (var node in speciesNodes.Values.Select(l => l.First()))
        {
            if (node.ParentNode == null)
                continue;

            // If the vertical position is outside draw region, skip.
            if (node.Center.Y < drawRegion.Position.Y || node.ParentNode.Center.Y > drawRegion.End.Y)
                continue;

            // Horizontal too.
            if (node.Center.X < drawRegion.Position.X || node.ParentNode.Center.X > drawRegion.End.X)
                continue;

            TreeDrawLine(node.ParentNode.Center, node.Center);
        }

        float treeRightPosition =
            sizeFactor * (dragOffset.X + GENERATION_SEPARATION * latestGeneration + TreeNodeSize.X);

        // Draw horizontal lines
        foreach (var nodeList in speciesNodes.Values)
        {
            var lineStart = nodeList.First().Center;

            // If the line is outside draw region, skip it.
            if (lineStart.Y < drawRegion.Position.Y || lineStart.Y > drawRegion.End.Y)
                continue;

            var lastNode = nodeList.Last();

            // If species extinct, line ends at the last node; else it ends at the right end of the tree
            var lineEnd = lastNode.LastGeneration ?
                lastNode.Center :
                new Vector2(treeRightPosition, lineStart.Y);

            TreeDrawLine(lineStart, lineEnd);
        }

        var size = sizeFactor * smallFontSize;

        float speciesNameOffset = sizeFactor * SPECIES_NAME_OFFSET;

        // Draw species name
        foreach (var pair in speciesNodes)
        {
            var lastNode = pair.Value.Last();

            // If the string is outside draw region, skip it.
            if (lastNode.Center.Y < drawRegion.Position.Y || lastNode.Center.X > drawRegion.End.X)
                continue;

            if (lastNode.LastGeneration)
            {
                tree.DrawString(bodyItalicFont.Font,
                    new Vector2(lastNode.Position.X + sizeFactor * TreeNodeSize.X + speciesNameOffset,
                        lastNode.Center.Y), speciesNames[pair.Key], HorizontalAlignment.Left, -1,
                    Mathf.RoundToInt(size), Colors.DarkRed);
            }
            else
            {
                tree.DrawString(bodyItalicFont.Font,
                    new Vector2(treeRightPosition + speciesNameOffset, pair.Value.First().Center.Y),
                    speciesNames[pair.Key], HorizontalAlignment.Left, -1, Mathf.RoundToInt(size));
            }
        }
    }

    /// <summary>
    ///   Draw a line in <see cref="tree"/>. Separate a normal line into 2 horizontal ones and a vertical one.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     <see cref="from"/> should be the left point and <see cref="to"/> the right point.
    ///   </para>
    /// </remarks>
    private void TreeDrawLine(Vector2 from, Vector2 to)
    {
        var lineWidth = sizeFactor * TREE_LINE_THICKNESS;
        var halfLineWidth = lineWidth / 2;

        if (to.Y - from.Y < MathUtils.EPSILON)
        {
            tree.DrawLine(from, to, Colors.DarkCyan, lineWidth);
        }
        else
        {
            var mid = to - new Vector2(sizeFactor * GENERATION_SEPARATION / 2.0f, 0);
            tree.DrawLine(from, new Vector2(mid.X, from.Y), Colors.DarkCyan, lineWidth);

            // We draw vertical line a little longer so the turning point looks better.
            tree.DrawLine(new Vector2(mid.X, from.Y - halfLineWidth), new Vector2(mid.X, to.Y + halfLineWidth),
                Colors.DarkCyan, lineWidth);

            tree.DrawLine(new Vector2(mid.X, to.Y), to, Colors.DarkCyan, lineWidth);
        }
    }

    private void OnTreeNodeSelected(EvolutionaryTreeNode node)
    {
        EmitSignal(SignalName.SpeciesSelected, node.Generation, node.SpeciesID);
    }
}
