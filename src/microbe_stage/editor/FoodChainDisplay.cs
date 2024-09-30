using System;
using System.Collections.Generic;
using AutoEvo;
using Godot;

/// <summary>
///   Displays a food chain from auto-evo results in the GUI for the player to inspect. This node should be put inside
///   a <see cref="DraggableScrollContainer"/> to allow big food chains to be viewed
/// </summary>
public partial class FoodChainDisplay : Control
{
    private readonly HashSet<(Control Start, Control End)> lines = new();

    private readonly List<Species> workMemory = new();

#pragma warning disable CA2213
    private PackedScene speciesResultButtonScene = null!;
    private PackedScene resourceScene = null!;
#pragma warning restore CA2213

    private RunResults? lastResults;
    private Patch? lastPatch;

    public override void _Ready()
    {
        speciesResultButtonScene = GD.Load<PackedScene>("res://src/microbe_stage/editor/SpeciesResultButton.tscn");
        resourceScene = GD.Load<PackedScene>("res://src/microbe_stage/editor/FoodChainResource.tscn");
    }

    public override void _Process(double delta)
    {
        // TODO: mouse hover on lines to show more info
    }

    public override void _Draw()
    {
        base._Draw();

        foreach (var (startControl, endControl) in lines)
        {
            var start = startControl.Position + startControl.Size / 2;
            var end = endControl.Position + endControl.Size / 2;

            DrawLine(start, end, Colors.Aquamarine, 2, true);
        }
    }

    public void DisplayFoodChainIfRequired(RunResults autoEvoResults, Patch forPatch)
    {
        // Only update if data has changed
        if (autoEvoResults == lastResults && forPatch == lastPatch)
            return;

        lastResults = autoEvoResults;
        lastPatch = forPatch;

        // TODO: reuse nodes that can be to make this faster
        this.QueueFreeChildren();
        lines.Clear();

        var micheTree = autoEvoResults.GetMicheForPatch(forPatch);

        var seenSpecies = new HashSet<Species>();

        var graphNodes = new List<GraphNode>();

        // Build relationships based on the miche tree as that's the source of truth for what energy is available
        micheTree.GetOccupants(seenSpecies);

        // To not show disappeared species (according to the report screen), prune ones from the miche tree that
        // don't have any population (and didn't have any previous population)
        workMemory.Clear();

        foreach (var species in seenSpecies)
        {
            // Species that weren't part of auto-evo always
            // TODO: maybe a bug in auto-evo: https://github.com/Revolutionary-Games/Thrive/issues/5549
            if (!autoEvoResults.SpeciesHasResults(species))
            {
                workMemory.Add(species);
                continue;
            }

            var speciesResult = autoEvoResults.GetSpeciesResultForInternalUse(species);
            if (speciesResult.OldPopulationInPatches.TryGetValue(forPatch, out var oldPopulation) && oldPopulation > 0)
            {
                continue;
            }

            if (speciesResult.NewPopulationInPatches.TryGetValue(forPatch, out var newPopulation) && newPopulation > 0)
            {
                continue;
            }

            workMemory.Add(species);
        }

        foreach (var species in workMemory)
        {
            seenSpecies.Remove(species);
        }

        // Species that didn't get a miche and are going extinct aren't seen above, but they will be handled in
        // BuildMicheEnergyNodes

        // Create tree nodes for all the species
        foreach (var species in seenSpecies)
        {
            graphNodes.Add(new GraphNode(species, false));
        }

        // Then generate relationships from the species to the other nodes
        foreach (var species in seenSpecies)
        {
            // This doesn't use GetSpeciesResultForInternalUse as this doesn't just care about the localized names of
            // energy sources, but also the types for more smart display in a graph like format

            BuildMicheEnergyNodes(micheTree, species, graphNodes, forPatch);
        }

        // TODO: laying out the graph nicely

        // Generate the final controls for the graph
        float x = 10;
        float y = 20;

        float y2 = 70;
        float y3 = 60;
        float y4 = 70;

        foreach (var graphNode in graphNodes)
        {
            switch (graphNode.Type)
            {
                case GraphNode.NodeType.Species:
                case GraphNode.NodeType.ExtinctSpecies:
                {
                    var resultDisplay = speciesResultButtonScene.Instantiate<SpeciesResultButton>();

                    resultDisplay.DisplaySpecies(autoEvoResults.GetSpeciesResultForInternalUse(graphNode.Species ??
                        throw new Exception("Invalid state of graph node")), false);

                    var speciesResult = autoEvoResults.GetSpeciesResultForInternalUse(graphNode.Species);
                    speciesResult.OldPopulationInPatches.TryGetValue(forPatch, out var oldPopulation);

                    resultDisplay.DisplayPopulation(
                        autoEvoResults.GetNewSpeciesPopulationInPatch(speciesResult, forPatch), oldPopulation, true);
                    resultDisplay.HideGlobalPopulation();

                    // Ensure the control size doesn't blow up
                    resultDisplay.SizeFlagsVertical = SizeFlags.ShrinkBegin;
                    resultDisplay.AnchorLeft = 0;
                    resultDisplay.AnchorRight = 0;
                    resultDisplay.AnchorTop = 0;
                    resultDisplay.AnchorBottom = 0;

                    resultDisplay.Size = resultDisplay.CustomMinimumSize;

                    // Position the node
                    resultDisplay.Position = new Vector2(x, y);

                    if (graphNode.Type == GraphNode.NodeType.ExtinctSpecies)
                    {
                        resultDisplay.Disabled = true;
                    }

                    // TODO: signals
                    AddChild(resultDisplay);
                    graphNode.CreatedControl = resultDisplay;

                    y += 10 + resultDisplay.Size.Y;
                    break;
                }

                case GraphNode.NodeType.EnvironmentalCompound:
                    CreateResourceNode(graphNode, 100, ref y2);
                    break;
                case GraphNode.NodeType.CompoundChunk:
                    CreateResourceNode(graphNode, 200, ref y3);
                    break;

                case GraphNode.NodeType.CompoundCloud:
                    CreateResourceNode(graphNode, 300, ref y4);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // Make sure this control is big enough to contain all the child nodes and to make the scroll container work
        CustomMinimumSize = new Vector2(Size.X, y);

        // Generate lines list
        foreach (var graphNode in graphNodes)
        {
            foreach (var nodeLink in graphNode.Links)
            {
                if (graphNode.CreatedControl == null || nodeLink.CreatedControl == null)
                {
                    GD.PrintErr("Invalid state of graph node (missing created Control)");
                    continue;
                }

                var line = (graphNode.CreatedControl, nodeLink.CreatedControl);

                lines.Add(line);
            }
        }

        // Queue a redraw to draw all the connection lines again
        QueueRedraw();
    }

    private void CreateResourceNode(GraphNode graphNode, float x, ref float y)
    {
        var resource = resourceScene.Instantiate<FoodChainResource>();

        resource.CompoundIcon = graphNode.Compound;

        resource.Position = new Vector2(x, y);

        AddChild(resource);
        graphNode.CreatedControl = resource;

        y += 60 + resource.Size.Y;
    }

    private void BuildMicheEnergyNodes(Miche miche, Species species, List<GraphNode> graphNodes, Patch patch)
    {
        if (miche.Occupant == species)
        {
            var energy = miche.Pressure.GetEnergy(patch);

            GraphNode? ourNode = null;

            foreach (var node in graphNodes)
            {
                if (node.Species == species)
                {
                    ourNode = node;
                    break;
                }
            }

            if (ourNode == null)
                throw new InvalidOperationException("Species graph node not found");

            // Process this miche for the species
            switch (miche.Pressure)
            {
                case ChunkCompoundPressure chunkCompoundPressure:
                    LinkToCompoundNode(graphNodes, ourNode, chunkCompoundPressure.GetUsedCompoundType(),
                        GraphNode.NodeType.CompoundChunk);
                    break;

                case CompoundCloudPressure compoundCloudPressure:
                    LinkToCompoundNode(graphNodes, ourNode, compoundCloudPressure.GetUsedCompoundType(),
                        GraphNode.NodeType.CompoundChunk);
                    break;
                case EnvironmentalCompoundPressure environmentalCompoundPressure:
                    LinkToCompoundNode(graphNodes, ourNode, environmentalCompoundPressure.GetUsedCompoundType(),
                        GraphNode.NodeType.CompoundChunk);
                    break;

                case PredationEffectivenessPressure predationEffectivenessPressure:
                {
                    bool found = false;

                    foreach (var node in graphNodes)
                    {
                        if (node.Species == predationEffectivenessPressure.Prey)
                        {
                            ourNode.Links.Add(node);
                            found = true;
                        }
                    }

                    if (!found)
                    {
                        // Can predate on an extinct species that didn't get a miche for itself
                        var node = new GraphNode(predationEffectivenessPressure.Prey, true);
                        graphNodes.Add(node);

                        ourNode.Links.Add(node);
                    }

                    break;
                }

                // Pressures that aren't really food sources so can just be skipped
                case AvoidPredationSelectionPressure:
                case CompoundConversionEfficiencyPressure:
                case MaintainCompoundPressure:
                case MetabolicStabilityPressure:
                case NoOpPressure:
                case PredatorRoot:
                case RootPressure:
                    break;

                default:
                    // Pressures that don't contribute energy are not critical to show if this code hasn't been
                    // updated to know about them
                    if (energy > 0)
                    {
                        GD.PrintErr($"Unknown miche selection pressure ({miche.Pressure.GetType().Name}) to show " +
                            $"in {nameof(FoodChainDisplay)}");
                    }

                    break;
            }
        }

        // Look for more relevant miches in the children
        foreach (var child in miche.Children)
        {
            BuildMicheEnergyNodes(child, species, graphNodes, patch);
        }
    }

    private void LinkToCompoundNode(List<GraphNode> graphNodes, GraphNode nodeToLinkFrom, Compound compoundType,
        GraphNode.NodeType nodeTypeToLinkTo)
    {
        GraphNode? targetNode = null;

        foreach (var node in graphNodes)
        {
            if (node.Type == nodeTypeToLinkTo && node.Compound == compoundType)
            {
                targetNode = node;
                break;
            }
        }

        if (targetNode != null)
        {
            nodeToLinkFrom.Links.Add(targetNode);
        }
        else
        {
            // Need a new node
            var node = new GraphNode(compoundType, nodeTypeToLinkTo);
            graphNodes.Add(node);

            nodeToLinkFrom.Links.Add(node);
        }
    }

    private class GraphNode
    {
        public readonly NodeType Type;
        public readonly Species? Species;
        public readonly Compound Compound = Compound.Invalid;

        public readonly HashSet<GraphNode> Links = new();

        public Control? CreatedControl;

        public GraphNode(Species species, bool extinct)
        {
            Species = species;
            Type = extinct ? NodeType.ExtinctSpecies : NodeType.Species;
        }

        public GraphNode(Compound compound, NodeType nodeType)
        {
            Compound = compound;
            Type = nodeType;

            if (Type != NodeType.CompoundCloud && Type != NodeType.EnvironmentalCompound &&
                Type != NodeType.CompoundChunk)
            {
                throw new ArgumentException("Type must be a compound using type", nameof(nodeType));
            }
        }

        public enum NodeType
        {
            Species,
            ExtinctSpecies,
            CompoundCloud,
            CompoundChunk,
            EnvironmentalCompound,
        }
    }
}
