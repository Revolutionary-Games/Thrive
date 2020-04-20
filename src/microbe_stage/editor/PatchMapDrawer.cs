using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Draws a PatchMap inside a control
/// </summary>
public class PatchMapDrawer : Control
{
    [Export]
    public bool DrawDefaultMapIfEmpty = false;

    [Export]
    public float ConnectionLineWidth = 2.0f;

    [Export]
    public float PatchNodeWidth = 64.0f;

    [Export]
    public float PatchNodeHeight = 64.0f;

    public PatchMap map;
    private bool dirty = true;

    private PackedScene nodeScene;

    private List<PatchMapNode> nodes = new List<PatchMapNode>();

    public PatchMap Map
    {
        get
        {
            return map;
        }
        set
        {
            map = value;
            dirty = true;
        }
    }

    public override void _Ready()
    {
        nodeScene = GD.Load<PackedScene>("res://src/microbe_stage/editor/PatchMapNode.tscn");

        if (DrawDefaultMapIfEmpty && Map == null)
        {
            GD.Print("Generating and showing a new patch map for testing in PatchMapDrawer");
            Map = new GameWorld(new WorldGenerationSettings()).Map;
        }
    }

    public override void _Process(float delta)
    {
        if (dirty)
        {
            RebuildMapNodes();
            Update();
            dirty = false;
        }
    }

    /// <summary>
    ///   Custom drawing, draws the lines between map nodes
    /// </summary>
    public override void _Draw()
    {
        if (Map == null)
            return;

        // This ends up drawing duplicates but that doesn't seem problematic ATM
        foreach (var entry in Map.Patches)
        {
            foreach (var adjacent in entry.Value.Adjacent)
            {
                var start = Center(entry.Value.ScreenCoordinates);
                var end = Center(adjacent.ScreenCoordinates);

                DrawNodeLink(start, end);
            }
        }
    }

    private Vector2 Center(Vector2 pos)
    {
        return new Vector2(pos.x + PatchNodeWidth / 2, pos.y + PatchNodeHeight / 2);
    }

    private void DrawNodeLink(Vector2 center1, Vector2 center2)
    {
        DrawLine(center1, center2, new Color(0.05f, 0.05f, 0.05f, 1), 2, true);
    }

    private void RebuildMapNodes()
    {
        foreach (var node in nodes)
        {
            node.Free();
        }

        nodes.Clear();

        if (Map == null)
            return;

        foreach (var entry in Map.Patches)
        {
            var node = (PatchMapNode)nodeScene.Instance();
            node.MarginLeft = entry.Value.ScreenCoordinates.x;
            node.MarginTop = entry.Value.ScreenCoordinates.y;
            node.RectSize = new Vector2(PatchNodeWidth, PatchNodeHeight);

            AddChild(node);
        }
    }
}
