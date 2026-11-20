using System;
using System.Collections.Generic;
using AutoEvo;
using Godot;

/// <summary>
///   A GUI miche tree for debugging the miche system. Supports dragging and zooming.
/// </summary>
/// <remarks>
///   <para>
///     To use this, simply add this inside any controller and adjust its size.
///     Don't put this under a draggable controller.
///   </para>
/// </remarks>
public partial class MicheTree : Control
{
    // TODO: See if this class and EvolutionaryTree can be combined into a parent class
    // https://github.com/Revolutionary-Games/Thrive/issues/5373

    private const float TREE_LINE_THICKNESS = 4.0f;

    /// <summary>
    ///   Vertical separation between depths.
    /// </summary>
    private const float DEPTH_SEPARATION = 50.0f;

    /// <summary>
    ///   Horizontal separation between leafs.
    /// </summary>
    private const float LEAF_SEPARATION = 50.0f;

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
    ///   Default size of <see cref="MicheTreeNode"/>.
    /// </summary>
    private static readonly Vector2 TreeNodeSize = new(30, 30);

    private readonly List<MicheTreeNode> nodes = new();

    /// <summary>
    ///   All MicheTreeNodes are in this group so that they work as radio buttons.
    /// </summary>
    private readonly ButtonGroup nodesGroup = new();

#pragma warning disable CA2213
    private Miche? rootMiche;

    /// <summary>
    ///   Contains the buttons and draws their connecting lines.
    /// </summary>
    [Export]
    private Control tree = null!;

    private PackedScene treeNodeScene = null!;
#pragma warning restore CA2213

    private int treeDepth;
    private float treeWidth;

    /// <summary>
    ///   Drag offset relative to tree.
    /// </summary>
    private Vector2 dragOffset;

    private bool dragging;
    private Vector2 lastMousePosition;

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

    [Signal]
    public delegate void MicheSelectedEventHandler(MicheTreeNode node);

    private Vector2 TreeSize => new(treeWidth + 200, treeDepth * DEPTH_SEPARATION + 100);

    public override void _Ready()
    {
        base._Ready();

        treeNodeScene = GD.Load<PackedScene>("res://src/auto-evo/MicheTreeNode.tscn");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (dirty)
        {
            UpdateTreeNodeSizeAndPosition();

            // Inform them to update
            tree.QueueRedraw();

            dirty = false;
        }
    }

    public void SetMiche(Miche miche)
    {
        if (ReferenceEquals(rootMiche, miche))
            return;

        Clear();

        rootMiche = miche;
        treeWidth = AddMiche(miche, null, 0, 0, false);
    }

    public void Clear()
    {
        rootMiche = null;
        treeDepth = 0;
        treeWidth = 0;
        nodes.Clear();
        dragOffset = Vector2.Zero;
        dragging = false;
        tree.QueueFreeChildren();
        dirty = true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            nodesGroup.Dispose();
        }

        base.Dispose(disposing);
    }

    private float AddMiche(Miche miche, MicheTreeNode? parent, int depth, float left, bool showsOccupant)
    {
        var node = treeNodeScene.Instantiate<MicheTreeNode>();
        node.Miche = miche;

        // Older saves can contain an occupant row written into the miche tree by the viewer.
        node.ShowsOccupant = showsOccupant || miche.Pressure is NoOpPressure;
        node.ParentNode = parent;
        node.ButtonGroup = nodesGroup;
        node.Pressed += () => EmitSignal(SignalName.MicheSelected, node);

        nodes.Add(node);
        tree.AddChild(node);
        treeDepth = Math.Max(treeDepth, depth);

        float width;
        if (node.ShowsOccupant)
        {
            width = LEAF_SEPARATION;
        }
        else if (miche.IsLeafNode())
        {
            width = AddMiche(miche, node, depth + 1, left, true);
        }
        else
        {
            width = 0;
            foreach (var child in miche.Children)
            {
                width += AddMiche(child, node, depth + 1, left + width, false);
            }
        }

        node.LogicalPosition = new Vector2(left + width / 2, depth * DEPTH_SEPARATION);
        return width;
    }

    private void UpdateTreeNodeSizeAndPosition()
    {
        var treeNodeSize = sizeFactor * TreeNodeSize;

        foreach (var node in nodes)
        {
            node.CustomMinimumSize = treeNodeSize;

            // RectSize needs to be adjusted explicitly to force the size to change
            node.Size = treeNodeSize;

            node.Position = sizeFactor * (node.LogicalPosition + dragOffset);
        }
    }

    /// <summary>
    ///   _GUIInput for <see cref="tree"/>.
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
                    Math.Clamp(sizeFactor * (float)Math.Pow(ZOOM_FACTOR, buttonEvent.Factor * (zoomIn ? -1 : 1)),
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
    ///   Stop dragging when mouse exit <see cref="tree"/>
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
        var drawRegion = new Rect2(Vector2.Zero, tree.Size).Grow(DRAW_MARGIN);
        foreach (var node in nodes)
        {
            if (node.ParentNode == null)
                continue;

            var from = node.ParentNode.Center;
            var to = node.Center;
            if (to.Y < drawRegion.Position.Y || from.Y > drawRegion.End.Y ||
                Math.Max(from.X, to.X) < drawRegion.Position.X || Math.Min(from.X, to.X) > drawRegion.End.X)
            {
                continue;
            }

            TreeDrawLine(from, to);
        }
    }

    /// <summary>
    ///   Draw a line in <see cref="tree"/>. Separate a normal line into 2 vertical ones and a horizontal one.
    /// </summary>
    private void TreeDrawLine(Vector2 from, Vector2 to)
    {
        var lineWidth = sizeFactor * TREE_LINE_THICKNESS;
        var halfLineWidth = lineWidth / 2;

        if (MathF.Abs(to.X - from.X) < MathUtils.EPSILON)
        {
            tree.DrawLine(from, to, Colors.DarkCyan, lineWidth);
        }
        else
        {
            var mid = from + new Vector2(0, sizeFactor * DEPTH_SEPARATION / 2.0f);
            tree.DrawLine(from, new Vector2(mid.X, mid.Y), Colors.DarkCyan, lineWidth);

            if (mid.X > to.X)
                halfLineWidth = -halfLineWidth;

            // We draw horizontal line a little longer so the turning point looks better.
            tree.DrawLine(new Vector2(mid.X - halfLineWidth, mid.Y), new Vector2(to.X + halfLineWidth, mid.Y),
                Colors.DarkCyan, lineWidth);

            tree.DrawLine(new Vector2(to.X, mid.Y), to, Colors.DarkCyan, lineWidth);
        }
    }
}
