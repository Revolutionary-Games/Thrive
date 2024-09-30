using System;
using System.Collections.Generic;
using System.Threading;
using AutoEvo;
using Godot;
using GraphShape.Algorithms.Layout;
using QuikGraph;
using QuikGraph.Algorithms;

/// <summary>
///   Displays a food chain from auto-evo results in the GUI for the player to inspect. This node should be put inside
///   a <see cref="DraggableScrollContainer"/> to allow big food chains to be viewed
/// </summary>
public partial class FoodChainDisplay : Control
{
    /// <summary>
    ///   Used to run the graph layout algorithm. So this is a variant of this food chain in graph-library specific
    ///   data format.
    /// </summary>
    private readonly AdjacencyGraph<GraphNode, Edge<GraphNode>> layoutGraph = new();

    private readonly List<GraphNode> graphNodes = new();

    private readonly HashSet<(Control Start, Control End)> lines = new();

    private readonly List<Species> workMemory = new();

#pragma warning disable CA2213
    private PackedScene speciesResultButtonScene = null!;
    private PackedScene resourceScene = null!;
#pragma warning restore CA2213

    private RunResults? lastResults;
    private Patch? lastPatch;

    [Signal]
    public delegate void SpeciesSelectedEventHandler(uint id);

    [Export]
    public Vector2 Margin { get; set; } = new(10, 5);

    [Export]
    public Vector2 EstimatedSpeciesButtonSize { get; set; } = new(110, 92);

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
            var start = startControl.Position + startControl.Size * 0.5f;
            var end = endControl.Position + endControl.Size * 0.5f;

            DrawLine(start, end, Colors.Aquamarine, 2, true);
        }
    }

    public void DisplayFoodChainIfRequired(RunResults autoEvoResults, Patch forPatch, Species playerSpecies)
    {
        // Only update if data has changed
        if (autoEvoResults == lastResults && forPatch == lastPatch)
            return;

        lastResults = autoEvoResults;
        lastPatch = forPatch;

        // TODO: reuse possible nodes
        graphNodes.Clear();

        var micheTree = autoEvoResults.GetMicheForPatch(forPatch);

        var seenSpecies = new HashSet<Species>();

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

        HandleAddingPlayer(autoEvoResults, forPatch, playerSpecies, seenSpecies);

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

            BuildMicheEnergyNodes(micheTree, species, forPatch);
        }

        GenerateGraphGraphics(autoEvoResults, forPatch);

        LayoutGraph();

        ApplyGraphPositions();

        CreateLines();
    }

    private static void HandleAddingPlayer(RunResults autoEvoResults, Patch forPatch, Species playerSpecies,
        HashSet<Species> seenSpecies)
    {
        // Player cannot be considered to be extinct due to missing from miches, so add player always if missing

        if (autoEvoResults.SpeciesHasResults(playerSpecies))
        {
            var speciesResult = autoEvoResults.GetSpeciesResultForInternalUse(playerSpecies);
            if ((speciesResult.OldPopulationInPatches.TryGetValue(forPatch, out var oldPopulation) &&
                    oldPopulation > 0) ||
                (speciesResult.NewPopulationInPatches.TryGetValue(forPatch, out var newPopulation) &&
                    newPopulation > 0))
            {
                seenSpecies.Add(playerSpecies);
            }
        }
        else
        {
            GD.PrintErr("No results for player species");
        }
    }

    private void LayoutGraph()
    {
        layoutGraph.Clear();

        // Create the graph data object with the nodes and connections
        foreach (var node in graphNodes)
        {
            layoutGraph.AddVertex(node);
        }

        foreach (var source in graphNodes)
        {
            foreach (var targetNode in source.Links)
            {
                layoutGraph.AddEdge(new Edge<GraphNode>(source, targetNode));
            }
        }

        // Then calculate a layout for them
        // Set an absolute deadline of 15 seconds to not totally freeze the game (could switch to a background layout)
        var cancellationSource = new CancellationTokenSource();
        cancellationSource.CancelAfter(TimeSpan.FromSeconds(15));

        // FRLayoutAlgorithm seems to fail often so it is not usable with our data

        var layoutAlgorithm =
            new SugiyamaLayoutAlgorithm<GraphNode, Edge<GraphNode>, AdjacencyGraph<GraphNode, Edge<GraphNode>>>(
                layoutGraph, new SugiyamaLayoutParameters
                {
                    Direction = GraphShape.Algorithms.Layout.LayoutDirection.BottomToTop,
                    LayerGap = 150,
                    SliceGap = 140,
                    MinimizeEdgeLength = false,
                    OptimizeWidth = false,
                    EdgeRouting = SugiyamaEdgeRouting.Traditional,
                });

        layoutAlgorithm.Compute();
        cancellationSource.Token.Register(() => layoutAlgorithm.Abort());

        if (layoutAlgorithm.State != ComputationState.Finished)
            GD.PrintErr("Graph layout algorithm didn't finish");

        // Convert graph coordinates so they are all on-screen
        double xOffset = 0;
        double yOffset = 0;

        foreach (var point in layoutAlgorithm.VerticesPositions.Values)
        {
            if (point.X < xOffset)
                xOffset = point.X;

            if (point.Y < yOffset)
                yOffset = point.Y;
        }

        // Convert the offsets to positive to make them cancel out then the most negative positions below
        xOffset = Math.Abs(xOffset);
        yOffset = Math.Abs(yOffset);

        // And add base margin from this
        // As well as size from the species buttons to be able to center align the controls on the generated points
        // without anything getting cutoff
        xOffset += Margin.X + EstimatedSpeciesButtonSize.X;
        yOffset += Margin.Y + EstimatedSpeciesButtonSize.Y;

        // Apply the calculated positions to the nodes
        // TODO: convert from horizontal to vertical graph layout
        foreach (var node in graphNodes)
        {
            if (!layoutAlgorithm.VerticesPositions.TryGetValue(node, out var position))
                continue;

            // Round the positions to get integer coordinates which are less blurry for text
            node.ReportComputedGraphPosition(new Vector2((float)Math.Round(position.X + xOffset),
                (float)Math.Round(position.Y + yOffset)));
        }

        // Mirror the vertical axis to make the graph more similar to a food chain which his usually seen top down
    }

    private void ApplyGraphPositions()
    {
        float width = 100;
        float height = 100;

        foreach (var graphNode in graphNodes)
        {
            graphNode.SetPositionFromGraph(ref width, ref height);
        }

        // Make sure this control is big enough to contain all the child nodes and to make the scroll container work
        CustomMinimumSize = new Vector2((int)Math.Ceiling(width), (int)Math.Ceiling(height));
    }

    private void CreateLines()
    {
        lines.Clear();

        // TODO: get this from the graph to follow the wanted contours
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

    private void GenerateGraphGraphics(RunResults autoEvoResults, Patch forPatch)
    {
        // TODO: reuse nodes that can be to make this faster
        this.QueueFreeChildren();

        // Generate the controls for the graph which are positioned later once the layout is calculated

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

                    if (graphNode.Type == GraphNode.NodeType.ExtinctSpecies)
                    {
                        resultDisplay.Disabled = true;
                    }
                    else
                    {
                        resultDisplay.Connect(SpeciesResultButton.SignalName.SpeciesSelected,
                            new Callable(this, nameof(OnSpeciesClicked)));
                    }

                    AddChild(resultDisplay);
                    graphNode.CreatedControl = resultDisplay;

                    break;
                }

                case GraphNode.NodeType.EnvironmentalCompound:
                    CreateResourceNode(graphNode);
                    break;

                case GraphNode.NodeType.CompoundChunk:
                    CreateResourceNode(graphNode);
                    break;

                case GraphNode.NodeType.CompoundCloud:
                    CreateResourceNode(graphNode);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void CreateResourceNode(GraphNode graphNode)
    {
        var resource = resourceScene.Instantiate<FoodChainResource>();

        resource.CompoundIcon = graphNode.Compound;

        AddChild(resource);
        graphNode.CreatedControl = resource;
    }

    private void BuildMicheEnergyNodes(Miche miche, Species species, Patch patch)
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
                    LinkToCompoundNode(ourNode, chunkCompoundPressure.GetUsedCompoundType(),
                        GraphNode.NodeType.CompoundChunk);
                    break;

                case CompoundCloudPressure compoundCloudPressure:
                    LinkToCompoundNode(ourNode, compoundCloudPressure.GetUsedCompoundType(),
                        GraphNode.NodeType.CompoundChunk);
                    break;
                case EnvironmentalCompoundPressure environmentalCompoundPressure:
                    LinkToCompoundNode(ourNode, environmentalCompoundPressure.GetUsedCompoundType(),
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
            BuildMicheEnergyNodes(child, species, patch);
        }
    }

    private void LinkToCompoundNode(GraphNode nodeToLinkFrom, Compound compoundType,
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

    private void OnSpeciesClicked(uint id)
    {
        EmitSignal(SignalName.SpeciesSelected, id);
    }

    private class GraphNode
    {
        public readonly NodeType Type;
        public readonly Species? Species;
        public readonly Compound Compound = Compound.Invalid;

        public readonly HashSet<GraphNode> Links = new();

        public Control? CreatedControl;

        private Vector2 graphPosition;

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

        // TODO: try to hook this up to GraphShape
        public Vector2 GetControlSize()
        {
            if (CreatedControl == null)
                throw new InvalidOperationException("No control created");

            return CreatedControl.Size;
        }

        public void ReportComputedGraphPosition(Vector2 position)
        {
#if DEBUG
            if (float.IsNaN(position.X) || float.IsNaN(position.Y))
            {
                GD.PrintErr("Graph node position is NaN");
                position = Vector2.Zero;
            }
#endif

            graphPosition = position;
        }

        public void SetPositionFromGraph(ref float maximumWidth, ref float maximumHeight)
        {
            if (CreatedControl == null)
                throw new InvalidOperationException("No control created");

            var halfSize = CreatedControl.Size * 0.5f;

            // Center on the graph point so that different sized controls look good
            CreatedControl.Position = graphPosition - halfSize;

            float right = graphPosition.X + halfSize.X;
            float bottom = graphPosition.Y + halfSize.Y;

            if (right > maximumWidth)
                maximumWidth = right;

            if (bottom > maximumHeight)
                maximumHeight = bottom;
        }

        public override string ToString()
        {
            if (Species != null)
                return $"({Type}, {Species})";

            return $"({Type}, {Compound})";
        }
    }
}
