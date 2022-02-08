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

    [Export]
    public Color ConnectionColour = new Color(1.0f, 1.0f, 1.0f, 1.0f);

    private readonly List<PatchMapNode> nodes = new();

    private PatchMap? map;
    private bool dirty = true;

    private PackedScene nodeScene = null!;

    private Patch? selectedPatch;

    private Patch? playerPatch;

    public PatchMap? Map
    {
        get => map;
        set
        {
            map = value ?? throw new ArgumentNullException();
            dirty = true;

            playerPatch ??= map.CurrentPatch;
        }
    }

    public Patch? PlayerPatch
    {
        get => playerPatch;
        set
        {
            if (playerPatch == value)
                return;

            playerPatch = value;
            UpdateNodeSelections();
            NotifySelectionChanged();
        }
    }

    public Patch? SelectedPatch
    {
        get => selectedPatch;
        set
        {
            if (selectedPatch == value)
                return;

            selectedPatch = value;
            UpdateNodeSelections();
            NotifySelectionChanged();
        }
    }

    /// <summary>
    ///   Called when the currently shown patch properties should be looked up again
    /// </summary>
    public Action<PatchMapDrawer>? OnSelectedPatchChanged { get; set; }

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
        DrawLine(center1, center2, ConnectionColour, ConnectionLineWidth, true);
    }

    private void RebuildMapNodes()
    {
        foreach (var node in nodes)
        {
            node.DetachAndFree();
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

            node.Patch = entry.Value;
            node.PatchIcon = entry.Value.BiomeTemplate.LoadedIcon;

            node.SelectCallback = clicked => { SelectedPatch = clicked.Patch; };

            AddChild(node);
            nodes.Add(node);
        }

        UpdateNodeSelections();
        NotifySelectionChanged();
    }

    private void UpdateNodeSelections()
    {
        foreach (var node in nodes)
        {
            node.Selected = node.Patch == selectedPatch;
            node.Marked = node.Patch == playerPatch;
        }
    }

    private void NotifySelectionChanged()
    {
        OnSelectedPatchChanged?.Invoke(this);
    }
}
