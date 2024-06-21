using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using Godot;

/// <summary>
///   A miche tree for debugging the miche system. Supports dragging and zooming.
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

    [Export]
    public NodePath TreePath = null!;

    /// <summary>
    ///   Stores the created nodes for miches by the miche hashes
    /// </summary>
    public Dictionary<int, MicheTreeNode> MicheNodes = new();

    public Dictionary<int, Miche> MicheByHash = new();

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

    /// <summary>
    ///   Auxiliary vector for <see cref="DRAW_MARGIN"/>
    /// </summary>
    private static readonly Vector2 DrawMargin = new(DRAW_MARGIN, DRAW_MARGIN);

    /// <summary>
    ///   All MicheTreeNodes are in this group so that they work as radio buttons.
    /// </summary>
    private readonly ButtonGroup nodesGroup = new();

#pragma warning disable CA2213
    private Miche rootMiche = null!;
    private MicheTreeNode rootMicheNode = null!;

    /// <summary>
    ///   Tree part of <see cref="EvolutionaryTree"/>. Consists of many buttons and connection lines.
    /// </summary>
    private Control tree = null!;

    // Local copy of fonts
    private LabelSettings bodyItalicFont = null!;
    private LabelSettings bodyFont = null!;

    private PackedScene treeNodeScene = null!;
#pragma warning restore CA2213

    private int treeDepth = 0;

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

    [Signal]
    public delegate void MicheSelectedEventHandler(int micheHash);

    private Vector2 TreeSize => new(rootMicheNode.Width + 200, treeDepth * DEPTH_SEPARATION + 100);

    public override void _Ready()
    {
        base._Ready();

        tree = GetNode<Control>(TreePath);

        treeNodeScene = GD.Load<PackedScene>("res://src/auto-evo/MicheTreeNode.tscn");

        // Font size is adjusted dynamically so this needs to be a copy.
        bodyItalicFont = GD.Load<LabelSettings>("res://src/gui_common/fonts/Body-Italic-Small.tres");
        smallFontSize = bodyItalicFont.FontSize;

        bodyFont = GD.Load<LabelSettings>("res://src/gui_common/fonts/Body-Regular-Small.tres");
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

    /// <summary>
    ///   A miche tree for debugging the miche system. Supports dragging and zooming.
    /// </summary>
    public void SetMiche(Miche miche)
    {
        if (rootMiche == miche)
            return;

        Clear();

        rootMiche = miche;
        rootMicheNode = SetupTreeNode(rootMiche, null, 0);

        var micheHash = rootMiche.GetHashCode();

        var totalWidth = 0f;
        foreach (var child in rootMiche.Children)
        {
            totalWidth += GenerateMicheData(child, rootMicheNode, 1);
        }

        rootMicheNode.Width = totalWidth;

        MicheByHash.Add(micheHash, rootMiche);

        BuildTree(miche, rootMicheNode, 0);

        dirty = true;
    }

    public void Clear()
    {
        treeDepth = 0;

        MicheNodes.Clear();
        MicheByHash.Clear();

        dragOffset = Vector2.Zero;

        tree.QueueFreeChildren();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (TreePath != null)
            {
                TreePath.Dispose();
                nodesGroup.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private float GenerateMicheData(Miche miche, MicheTreeNode parentNode, int depth)
    {
        var micheHash = miche.GetHashCode();

        if (depth > treeDepth)
            treeDepth = depth;

        if (miche.Pressure.GetType() != typeof(NoOpPressure) && miche.IsLeafNode())
        {
            var dummyMiche = new Miche(new NoOpPressure())
            {
                Occupant = miche.Occupant,
            };

            miche.AddChild(dummyMiche);
        }

        var micheNode = SetupTreeNode(miche, parentNode, depth);

        var totalWidth = 0f;
        foreach (var child in miche.Children)
        {
            totalWidth += GenerateMicheData(child, micheNode, depth + 1);
        }

        if (totalWidth == 0)
            totalWidth = LEAF_SEPARATION;

        micheNode.Width = totalWidth;

        MicheByHash.Add(micheHash, miche);

        return totalWidth;
    }

    private MicheTreeNode SetupTreeNode(Miche miche, MicheTreeNode? parent, int depth)
    {
        var position = new Vector2(0, depth * DEPTH_SEPARATION);
        var micheHash = miche.GetHashCode();

        MicheTreeNode node = treeNodeScene.Instantiate<MicheTreeNode>();

        node.ParentNode = parent;
        node.LogicalPosition = position;
        node.Unoccupied = miche.Pressure.GetType() == typeof(NoOpPressure) && miche.Occupant == null;
        node.Depth = depth;
        node.MicheHash = micheHash;

        node.ButtonGroup = nodesGroup;
        node.Connect(BaseButton.SignalName.Pressed, Callable.From(() => OnTreeNodeSelected(node)));

        MicheNodes.Add(micheHash, node);
        tree.AddChild(node);

        return node;
    }

    private void UpdateTreeNodeSizeAndPosition()
    {
        var treeNodeSize = sizeFactor * TreeNodeSize;

        foreach (var node in MicheNodes.Values)
        {
            node.CustomMinimumSize = treeNodeSize;

            // RectSize needs to be adjusted explicitly to force the size to change
            node.Size = treeNodeSize;

            node.Position = sizeFactor * (node.LogicalPosition + dragOffset);
        }
    }

    private void BuildTree(Miche miche, MicheTreeNode micheNode, float offset)
    {
        var position = micheNode.LogicalPosition;
        position.X = offset + micheNode.Width / 2;
        micheNode.LogicalPosition = position;
        micheNode.Position = position;

        foreach (var childMiche in miche.Children)
        {
            if (!MicheNodes.TryGetValue(childMiche.GetHashCode(), out var childNode))
            {
                throw new Exception("Missing node in miche nodes");
            }

            BuildTree(childMiche, childNode, offset);

            offset += childNode.Width;
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
        if (rootMiche == null)
            return;

        TreeDraw(rootMiche, rootMicheNode);
    }

    private void TreeDraw(Miche miche, MicheTreeNode micheNode)
    {
        // TODO: isn't this slightly outside the bounds of the tree control?
        var drawRegion = new Rect2(Position - DrawMargin, Size + DrawMargin);

        // Draw new species connection lines
        foreach (var childMiche in miche.Children)
        {
            if (!MicheNodes.TryGetValue(miche.GetHashCode(), out var childNode))
            {
                throw new Exception("Missing node in miche nodes");
            }

            TreeDraw(childMiche, childNode);

            // If the vertical position is outside draw region, skip.
            if (childNode.Center.Y < drawRegion.Position.Y || micheNode.Center.Y > drawRegion.End.Y)
                continue;

            // Horizontal too.
            if (childNode.Center.X < drawRegion.Position.X || micheNode.Center.X > drawRegion.End.X)
                continue;

            TreeDrawLine(micheNode.Center, childNode.Center);
        }
    }

    /// <summary>
    ///   Draw a line in <see cref="tree"/>. Separate a normal line into 2 vertical ones and a horizontal one.
    /// </summary>
    private void TreeDrawLine(Vector2 from, Vector2 to)
    {
        var lineWidth = sizeFactor * TREE_LINE_THICKNESS;
        var halfLineWidth = lineWidth / 2;

        if (Mathf.Abs(to.X - from.X) < MathUtils.EPSILON)
        {
            tree.DrawLine(from, to, Colors.DarkCyan, lineWidth);
        }
        else
        {
            var mid = from + new Vector2(0, sizeFactor * DEPTH_SEPARATION / 2.0f);
            tree.DrawLine(from, new Vector2(mid.X, mid.Y), Colors.DarkCyan, lineWidth);

            // We draw horizontal line a little longer so the turning point looks better.
            tree.DrawLine(new Vector2(mid.X - halfLineWidth, mid.Y), new Vector2(to.X + halfLineWidth, mid.Y),
                Colors.DarkCyan, lineWidth);

            tree.DrawLine(new Vector2(to.X, mid.Y), to, Colors.DarkCyan, lineWidth);
        }
    }

    private void OnTreeNodeSelected(MicheTreeNode node)
    {
        EmitSignal(SignalName.MicheSelected, node.MicheHash);
    }
}
