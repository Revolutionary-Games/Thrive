﻿using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using Godot;
using Godot.Collections;

public class EvolutionaryTree : Control
{
    private readonly List<EvolutionaryTreeNode> nodes = new();

    private readonly System.Collections.Generic.Dictionary<uint, string> speciesNames = new();

    private readonly System.Collections.Generic.Dictionary<uint, EvolutionaryTreeNode> latestNodes = new();

    private readonly System.Collections.Generic.Dictionary<uint, (uint ParentSpeciesID, int SplitGeneration)>
        speciesOrigin = new();

    private readonly ButtonGroup nodesGroup = new();

    private Font latoSmall = null!;

    private PackedScene treeNodeScene = null!;

    private uint maxSpeciesId;

    private int latestGeneration;

    [Signal]
    public delegate void SpeciesSelected(int generation, uint id);

    public override void _Ready()
    {
        base._Ready();

        treeNodeScene = GD.Load<PackedScene>("res://src/auto-evo/EvolutionaryTreeNode.tscn");
        latoSmall = GD.Load<Font>("res://src/gui_common/fonts/Lato-Italic-Small.tres");
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

        foreach (var latestNode in latestNodes.Values.Where(n => !n.LastGeneration))
        {
            var lineStart = latestNode.Center;
            var lineEnd = new Vector2(100 * latestGeneration + latestNode.RectSize.x, lineStart.y);
            DrawLine(lineStart, lineEnd);
            DrawString(latoSmall, lineEnd + new Vector2(10, 0), speciesNames[latestNode.SpeciesID]);
        }

        foreach (var extinctedSpecies in latestNodes.Values.Where(n => n.LastGeneration))
        {
            DrawString(latoSmall, new Vector2(extinctedSpecies.RectPosition.x + extinctedSpecies.RectSize.x + 10,
                extinctedSpecies.Center.y), speciesNames[extinctedSpecies.SpeciesID], Colors.DarkRed);
        }
    }

    public void Init(Species player)
    {
        SetupTreeNode(player, null, 0);

        speciesOrigin.Add(player.ID, (uint.MaxValue, 0));
        speciesNames.Add(player.ID, player.FormattedName);
    }

    public void UpdateEvolutionaryTreeWithRunResults(RunResults results, int generation)
    {
        foreach (var speciesResultPair in results.OrderBy(r => r.Value.Species.ID))
        {
            var species = speciesResultPair.Key;
            var result = speciesResultPair.Value;

            if (result.Species.Population <= 0)
            {
                if (result.SplitFrom == null)
                {
                    SetupTreeNode((Species)species.Clone(),
                        nodes.FindLast(n => n.SpeciesID == species.ID), generation - 1, true);
                }
            }
            else if (result.SplitFrom != null)
            {
                SetupTreeNode((Species)species.Clone(),
                    nodes.FindLast(n => n.SpeciesID == result.SplitFrom.ID), generation);

                speciesOrigin.Add(species.ID, (result.SplitFrom.ID, generation));
                speciesNames.Add(species.ID, species.FormattedName);
            }
            else if (result.MutatedProperties != null)
            {
                SetupTreeNode((Species)result.MutatedProperties.Clone(),
                    nodes.FindLast(n => n.SpeciesID == species.ID), generation);
            }

            if (species.ID > maxSpeciesId)
                maxSpeciesId = species.ID;
        }

        latestGeneration = generation;

        BuildTree();
        AdjustSize();
    }

    private void AdjustSize()
    {
        RectMinSize = new Vector2(100 * latestGeneration + 200, 50 * maxSpeciesId + 200);
    }

    private void SetupTreeNode(Species species, EvolutionaryTreeNode? parent, int generation,
        bool isLastGeneration = false)
    {
        var node = treeNodeScene.Instance<EvolutionaryTreeNode>();
        node.Generation = generation;
        node.SpeciesID = species.ID;
        node.LastGeneration = false;
        node.ParentNode = parent;
        node.RectPosition = new Vector2(generation * 100, 0);
        node.LastGeneration = isLastGeneration;
        node.Group = nodesGroup;
        node.Connect("button_down", this, nameof(OnTreeNodeSelected), new Array { node });

        nodes.Add(node);
        AddChild(node);
        latestNodes[species.ID] = node;
    }

    private void BuildTree()
    {
        uint index = 0;
        BuildTree(nodes[0].SpeciesID, ref index);

        Update();
    }

    private void BuildTree(uint id, ref uint index)
    {
        // Adjust nodes of this species' vertical position based on index
        foreach (var treeNode in nodes.Where(n => n.SpeciesID == id))
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
        EmitSignal(nameof(SpeciesSelected), node.Generation, node.SpeciesID);
    }
}
