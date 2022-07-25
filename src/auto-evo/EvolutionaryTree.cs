using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using Godot;

public class EvolutionaryTree : Control
{
    private PackedScene treeNodeScene = null!;

    private List<EvolutionaryTreeNode> nodes = new();

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

                nodes.Add(node);
                AddChild(node);
            }
            else if (result.MutatedProperties != null)
            {
                var node = treeNodeScene.Instance<EvolutionaryTreeNode>();
                node.Generation = generation;
                node.Species = result.MutatedProperties;
                node.LastGeneration = false;
                node.ParentNode = nodes.First(n => n.Species == species);

                nodes.Add(node);
                AddChild(node);
            }
        }
    }
}
