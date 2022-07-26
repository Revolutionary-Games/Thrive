using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using Godot;
using Godot.Collections;

public class EvolutionaryTree : Control
{
    private readonly List<EvolutionaryTreeNode> nodes = new();

    private readonly System.Collections.Generic.Dictionary<uint, EvolutionaryTreeNode> latest = new();

    private readonly System.Collections.Generic.Dictionary<uint, (uint ParentSpeciesID, int SplitGeneration)>
        speciesOrigin = new();

    private readonly ButtonGroup nodesGroup = new();

    private PackedScene treeNodeScene = null!;

    private uint maxSpeciesId;

    private int latestGeneration;

    [Signal]
    public delegate void SpeciesSelected(int generation, uint id);

    public override void _Ready()
    {
        base._Ready();

        treeNodeScene = GD.Load<PackedScene>("res://src/auto-evo/EvolutionaryTreeNode.tscn");
    }

    public override void _Draw()
    {
        base._Draw();

        foreach (var node in nodes)
        {
            if (node.ParentNode == null)
                continue;

            DrawLine(node.ParentNode.Center, node.Center);
        }

        foreach (var latestNode in latest.Values.Where(n => !n.LastGeneration))
        {
            var position = latestNode.Center;
            DrawLine(position, new Vector2(100 * latestGeneration + latestNode.RectSize.x, position.y));
        }
    }

    public void Init(Species player)
    {
        SetupTreeNode(player, null, 0);

        speciesOrigin.Add(player.ID, (uint.MaxValue, 0));
    }

    public void UpdateEvolutionaryTreeWithRunResults(RunResults results, int generation)
    {
        foreach (var speciesResultPair in results.OrderBy(r => r.Value.Species.ID))
        {
            var species = speciesResultPair.Key;
            var result = speciesResultPair.Value;

            if (result.SplitFrom != null)
            {
                SetupTreeNode(species, nodes.First(n => n.Species == result.SplitFrom), generation);

                speciesOrigin.Add(species.ID, (result.SplitFrom.ID, generation));
            }
            else if (result.MutatedProperties != null)
            {
                SetupTreeNode(result.MutatedProperties, nodes.First(n => n.Species == species), generation);
            }
            else if (result.Species.Population <= 0)
            {
                SetupTreeNode(species, nodes.First(n => n.Species == species), generation - 1, true);
            }

            if (species.ID > maxSpeciesId)
                maxSpeciesId = species.ID;
        }

        latestGeneration = generation;

        BuildTree();
    }

    private void SetupTreeNode(Species species, EvolutionaryTreeNode? parent, int generation,
        bool isLastGeneration = false)
    {
        var node = treeNodeScene.Instance<EvolutionaryTreeNode>();
        node.Generation = generation;
        node.Species = species;
        node.LastGeneration = false;
        node.ParentNode = parent;
        node.RectPosition = new Vector2(generation * 100, 0);
        node.LastGeneration = isLastGeneration;
        node.Group = nodesGroup;
        node.Connect("button_down", this, nameof(OnTreeNodeSelected), new Array { node });

        nodes.Add(node);
        AddChild(node);
        latest[species.ID] = node;
    }

    private void BuildTree()
    {
        uint index = 0;
        BuildTree(nodes[0].Species.ID, ref index);

        Update();
    }

    private void BuildTree(uint id, ref uint index)
    {
        // Adjust nodes of this species' vertical position based on index
        foreach (var treeNode in nodes.Where(n => n.Species.ID == id))
        {
            var position = treeNode.RectPosition;
            position.y = index * 50;
            treeNode.RectPosition = position;
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

    private void DrawLine(Vector2 from, Vector2 to)
    {
        if (to.y - from.y < MathUtils.EPSILON)
        {
            DrawLine(from, to, Colors.DarkCyan, 4.0f, true);
        }
        else
        {
            var mid = to - new Vector2(100 / 2.0f, 0);
            DrawLine(from, new Vector2(mid.x, from.y), Colors.DarkCyan, 4.0f, true);
            DrawLine(new Vector2(mid.x, from.y), new Vector2(mid.x, to.y), Colors.DarkCyan, 4.0f, true);
            DrawLine(new Vector2(mid.x, to.y), to, Colors.DarkCyan, 4.0f, true);
        }
    }

    private void OnTreeNodeSelected(EvolutionaryTreeNode node)
    {
        EmitSignal(nameof(SpeciesSelected), node.Generation, node.Species.ID);
    }
}
