﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoEvo;
using Godot;
using Array = Godot.Collections.Array;

/// <summary>
///   An evolutionary tree showing species origins and mutations. Supports dragging and zooming.
/// </summary>
/// <remarks>
///   <para>
///     To use this, simply add this inside any controller and adjust its size.
///     Don't put this under a draggable controller.
///   </para>
/// </remarks>
public class EvolutionaryTree : Control
{
    [Export]
    public NodePath TimelinePath = null!;

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

    // ReSharper disable RedundantNameQualifier
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

    /// <summary>
    ///   Timeline part of <see cref="EvolutionaryTree"/>. Consists of an axis and multiple time marks.
    /// </summary>
    private Control timeline = null!;

    /// <summary>
    ///   Tree part of <see cref="EvolutionaryTree"/>. Consists of many buttons and connection lines.
    /// </summary>
    private Control tree = null!;

    // Local copy of fonts
    private Font latoSmallItalic = null!;
    private Font latoSmallRegular = null!;

    private PackedScene treeNodeScene = null!;

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
    ///      <see cref="Control.RectScale"/> is not used here so that we know the drawn parts.
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
    public delegate void SpeciesSelected(int generation, uint id);

    private Vector2 TreeSize =>
        new(latestGeneration * GENERATION_SEPARATION + 200, maxSpeciesId * SPECIES_SEPARATION + 100);

    public override void _Ready()
    {
        base._Ready();

        timeline = GetNode<Control>(TimelinePath);
        tree = GetNode<Control>(TreePath);

        treeNodeScene = GD.Load<PackedScene>("res://src/auto-evo/EvolutionaryTreeNode.tscn");

        // Font size is adjusted dynamically so this needs to be a copy.
        latoSmallItalic = (Font)GD.Load("res://src/gui_common/fonts/Lato-Italic-Small.tres").Duplicate();
        smallFontSize = (int)latoSmallItalic.Get("size");

        // LatoRegular is used in the timeline part which has a fixed size, so we don't need to clone.
        latoSmallRegular = (Font)GD.Load("res://src/gui_common/fonts/Lato-Regular-Small.tres");
    }

    public void Init(Species luca, string? updatedLUCAName = null)
    {
        SetupTreeNode(luca, null, 0);

        speciesOrigin.Add(luca.ID, (uint.MaxValue, 0));
        speciesNames.Add(luca.ID, updatedLUCAName ?? luca.FormattedName);
        generationTimes.Add(0, 0);

        dirty = true;
    }

    public void Clear()
    {
        speciesOrigin.Clear();
        speciesNames.Clear();
        generationTimes.Clear();
        speciesNodes.Clear();

        tree.QueueFreeChildren();
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (dirty)
        {
            UpdateTreeNodeSizeAndPosition();

            // Inform them to update
            timeline.Update();
            tree.Update();

            dirty = false;
        }
    }

    public void UpdateEvolutionaryTreeWithRunResults(
        System.Collections.Generic.Dictionary<uint, SpeciesRecordFull> records, int generation, double time,
        uint playerSpeciesID)
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
                        speciesNodes[speciesID].Last(), generation - 1, true);
                }
            }
            else if (record.SplitFromID != null)
            {
                var splitFromID = (uint)record.SplitFromID;
                SetupTreeNode(record.Species, speciesNodes[splitFromID].Last(), generation);

                speciesOrigin.Add(speciesID, (splitFromID, generation));
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

    private void SetupTreeNode(Species species, EvolutionaryTreeNode? parent, int generation,
        bool isLastGeneration = false)
    {
        var node = treeNodeScene.Instance<EvolutionaryTreeNode>();
        node.Generation = generation;
        node.SpeciesID = species.ID;
        node.LastGeneration = false;
        node.ParentNode = parent;
        node.Position = new Vector2(generation * GENERATION_SEPARATION, 0);
        node.LastGeneration = isLastGeneration;
        node.Group = nodesGroup;
        node.Connect("pressed", this, nameof(OnTreeNodeSelected), new Array { node });

        if (!speciesNodes.ContainsKey(species.ID))
        {
            speciesNodes.Add(species.ID, new List<EvolutionaryTreeNode>());
        }
        else if (speciesNodes[species.ID].Any(n => (n.Position - node.Position).Length() < MathUtils.EPSILON))
        {
            // Remove the existing node in this position so we can replace it (e.g. with an extinct node)
            var existingNode = speciesNodes[species.ID].Where(n => n.Position == node.Position).First();
            speciesNodes[species.ID].Remove(existingNode);
            existingNode.DetachAndQueueFree();
        }

        speciesNodes[species.ID].Add(node);
        tree.AddChild(node);
    }

    private void UpdateTreeNodeSizeAndPosition()
    {
        var treeNodeSize = sizeFactor * TreeNodeSize;

        foreach (var node in speciesNodes.Values.SelectMany(speciesNodeList => speciesNodeList))
        {
            node.RectMinSize = treeNodeSize;

            // RectSize needs to be adjusted explicitly even when SizeFlag set to ShrinkEnd.
            node.RectSize = treeNodeSize;

            node.RectPosition = sizeFactor * (node.Position + dragOffset);
        }
    }

    private void BuildTree()
    {
        uint index = 0;
        BuildTree(speciesNodes.First().Key, ref index);
    }

    private void BuildTree(uint id, ref uint index)
    {
        // Adjust nodes of this species' vertical position based on index
        foreach (var treeNode in speciesNodes[id])
        {
            var position = treeNode.Position;
            position.y = index * SPECIES_SEPARATION;
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
            new Vector2(RectSize.x, TIMELINE_AXIS_Y), Colors.Cyan, TIMELINE_LINE_THICKNESS);

        int increment = (int)Math.Ceiling(1 / sizeFactor);

        // Draw time marks
        int firstDrawnGeneration =
            (int)Math.Ceiling((-dragOffset.x - TreeNodeSize.x / 2) / GENERATION_SEPARATION / increment) * increment;

        int lastDrawnGeneration =
            Math.Min((int)Math.Floor((RectSize.x / sizeFactor - dragOffset.x - TreeNodeSize.x / 2) /
                GENERATION_SEPARATION), latestGeneration);

        for (int i = firstDrawnGeneration; i <= lastDrawnGeneration; i += increment)
        {
            var x = sizeFactor * (dragOffset.x + i * GENERATION_SEPARATION + TreeNodeSize.x / 2);

            timeline.DrawLine(new Vector2(x, TIMELINE_AXIS_Y), new Vector2(x, TIMELINE_AXIS_Y + TIMELINE_MARK_LENGTH),
                Colors.Cyan, TIMELINE_LINE_THICKNESS);

            var localizedText = string.Empty;
            if (generationTimes.TryGetValue(i, out var generationTime))
            {
                localizedText = string.Format(CultureInfo.CurrentCulture, "{0:#,##0,,}", generationTime) + " "
                    + TranslationServer.Translate("MEGA_YEARS");
            }

            var size = latoSmallRegular.GetStringSize(localizedText);

            timeline.DrawString(latoSmallRegular, new Vector2(Mathf.Clamp(x - size.x / 2, 0, RectSize.x - size.x),
                TIMELINE_AXIS_Y + TIMELINE_MARK_LENGTH * 2 + size.y), localizedText, Colors.Cyan);
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
            dragging = (buttonEvent.ButtonMask & ((int)ButtonList.Left | (int)ButtonList.Right)) != 0;
            if (dragging)
                lastMousePosition = buttonEvent.Position;

            if (buttonEvent.Pressed &&
                (ButtonList)buttonEvent.ButtonIndex is ButtonList.WheelDown or ButtonList.WheelUp)
            {
                bool zoomIn = (ButtonList)buttonEvent.ButtonIndex == ButtonList.WheelUp;

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
                dragOffset += horizontalOnly ? new Vector2(delta.x, 0) : delta;
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
        var start = RectSize / sizeFactor - TreeSize;

        float x = dragOffset.x;
        x = Math.Max(x, start.x);
        x = Math.Min(x, 0);

        float y = dragOffset.y;
        y = Math.Max(y, start.y);
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
        var drawRegion = new Rect2(RectPosition - DrawMargin, RectSize + DrawMargin);

        // Draw new species connection lines
        foreach (var node in speciesNodes.Values.Select(l => l.First()))
        {
            if (node.ParentNode == null)
                continue;

            // If the vertical position is outside draw region, skip.
            if (node.Center.y < drawRegion.Position.y || node.ParentNode.Center.y > drawRegion.End.y)
                continue;

            // Horizontal too.
            if (node.Center.x < drawRegion.Position.x || node.ParentNode.Center.x > drawRegion.End.x)
                continue;

            TreeDrawLine(node.ParentNode.Center, node.Center);
        }

        float treeRightPosition =
            sizeFactor * (dragOffset.x + GENERATION_SEPARATION * latestGeneration + TreeNodeSize.x);

        // Draw horizontal lines
        foreach (var nodeList in speciesNodes.Values)
        {
            var lineStart = nodeList.First().Center;

            // If the line is outside draw region, skip it.
            if (lineStart.y < drawRegion.Position.y || lineStart.y > drawRegion.End.y)
                continue;

            var lastNode = nodeList.Last();

            // If species extinct, line ends at the last node; else it ends at the right end of the tree
            var lineEnd = lastNode.LastGeneration ?
                lastNode.Center :
                new Vector2(treeRightPosition, lineStart.y);

            TreeDrawLine(lineStart, lineEnd);
        }

        latoSmallItalic.Set("size", sizeFactor * smallFontSize);

        float speciesNameOffset = sizeFactor * SPECIES_NAME_OFFSET;

        // Draw species name
        foreach (var pair in speciesNodes)
        {
            var lastNode = pair.Value.Last();

            // If the string is outside draw region, skip it.
            if (lastNode.Center.y < drawRegion.Position.y || lastNode.Center.x > drawRegion.End.x)
                continue;

            if (lastNode.LastGeneration)
            {
                tree.DrawString(latoSmallItalic,
                    new Vector2(lastNode.RectPosition.x + sizeFactor * TreeNodeSize.x + speciesNameOffset,
                        lastNode.Center.y), speciesNames[pair.Key], Colors.DarkRed);
            }
            else
            {
                tree.DrawString(latoSmallItalic,
                    new Vector2(treeRightPosition + speciesNameOffset, pair.Value.First().Center.y),
                    speciesNames[pair.Key]);
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

        if (to.y - from.y < MathUtils.EPSILON)
        {
            tree.DrawLine(from, to, Colors.DarkCyan, lineWidth);
        }
        else
        {
            var mid = to - new Vector2(sizeFactor * GENERATION_SEPARATION / 2.0f, 0);
            tree.DrawLine(from, new Vector2(mid.x, from.y), Colors.DarkCyan, lineWidth);

            // We draw vertical line a little longer so the turning point looks better.
            tree.DrawLine(new Vector2(mid.x, from.y - halfLineWidth), new Vector2(mid.x, to.y + halfLineWidth),
                Colors.DarkCyan, lineWidth);

            tree.DrawLine(new Vector2(mid.x, to.y), to, Colors.DarkCyan, lineWidth);
        }
    }

    private void OnTreeNodeSelected(EvolutionaryTreeNode node)
    {
        EmitSignal(nameof(SpeciesSelected), node.Generation, node.SpeciesID);
    }
}
