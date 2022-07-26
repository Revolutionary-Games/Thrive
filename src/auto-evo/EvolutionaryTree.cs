using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using Godot;

public class EvolutionaryTree : Control
{
    private readonly List<EvolutionaryTreeNode> nodes = new();

    private readonly Dictionary<uint, (uint ParentSpeciesID, int SplitGeneration)> speciesOrigin = new();

    private PackedScene treeNodeScene = null!;

    private uint maxSpeciesId;

    public override void _Ready()
    {
        treeNodeScene = GD.Load<PackedScene>("res://src/auto-evo/EvolutionaryTreeNode.tscn");
    }

    public void Init(Species player)
    {
        var node = treeNodeScene.Instance<EvolutionaryTreeNode>();
        node.Generation = -1;
        node.Species = player;
        node.LastGeneration = false;
        node.ParentNode = null;

        nodes.Add(node);
        AddChild(node);

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
                var node = treeNodeScene.Instance<EvolutionaryTreeNode>();
                node.Generation = generation;
                node.Species = species;
                node.LastGeneration = false;
                node.ParentNode = nodes.First(n => n.Species == result.SplitFrom);
                node.RectPosition = new Vector2(generation * 50, 0);

                nodes.Add(node);
                AddChild(node);

                speciesOrigin.Add(species.ID, (node.ParentNode.Species.ID, generation));
            }
            else if (result.MutatedProperties != null)
            {
                var node = treeNodeScene.Instance<EvolutionaryTreeNode>();
                node.Generation = generation;
                node.Species = result.MutatedProperties;
                node.LastGeneration = false;
                node.ParentNode = nodes.First(n => n.Species == species);
                node.RectPosition = new Vector2(generation * 100, 0);

                nodes.Add(node);
                AddChild(node);
            }

            if (species.ID > maxSpeciesId)
                maxSpeciesId = species.ID;
        }

        AdjustTree();
    }

    private void AdjustTree()
    {
        var root = new TreeNode { ID = nodes[0].Species.ID };
        CreateTree(root);

        int index = 0;
        AdjustTree(root, ref index);
    }

    private void CreateTree(TreeNode node)
    {
        var id = node.ID;

        node.Children.AddRange(speciesOrigin.Where(p => p.Value.ParentSpeciesID == id)
            .OrderBy(p => p.Value.SplitGeneration)
            .Select(p => new TreeNode { ID = p.Key }));

        foreach (var child in node.Children)
        {
            CreateTree(child);
        }
    }

    private void AdjustTree(TreeNode node, ref int index)
    {
        foreach (var treeNode in nodes.Where(n => n.Species.ID == node.ID))
        {
            var position = treeNode.RectPosition;
            position.y = index * 50;
            treeNode.RectPosition = position;
        }

        ++index;

        foreach (var child in node.Children)
        {
            AdjustTree(child, ref index);
        }
    }

    private class TreeNode
    {
        public readonly List<TreeNode> Children = new();
        public uint ID;
    }
}
