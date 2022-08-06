using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoEvo;
using Godot;
using Godot.Collections;

public class EvolutionaryTree : Control
{
    [Export]
    public NodePath TimelinePath = null!;

    [Export]
    public NodePath TreePath = null!;

    private const float LEFT_MARGIN = 5.0f;
    private const float TIMELINE_LINE_THICKNESS = 2.0f;
    private const float TIMELINE_LINE_Y = 5.0f;
    private const float TIMELINE_MARK_SIZE = 4.0f;
    private const float SPECIES_SEPARATION = 50.0f;
    private const float GENERATION_SEPARATION = 100.0f;
    private const float SPECIES_NAME_OFFSET = 10.0f;

    private readonly System.Collections.Generic.Dictionary<uint, List<EvolutionaryTreeNode>> speciesNodes = new();

    private readonly System.Collections.Generic.Dictionary<uint, string> speciesNames = new();

    private readonly System.Collections.Generic.Dictionary<uint, (uint ParentSpeciesID, int SplitGeneration)>
        speciesOrigin = new();

    private readonly System.Collections.Generic.Dictionary<int, double> generationTimes = new();

    private readonly ButtonGroup nodesGroup = new();

    private Control timeline = null!;
    private Control tree = null!;

    private Font latoSmallItalic = null!;
    private Font latoSmallRegular = null!;

    private PackedScene treeNodeScene = null!;

    private Vector2 treeNodeSize;

    private uint maxSpeciesId;

    private int latestGeneration;

    [Signal]
    public delegate void SpeciesSelected(int generation, uint id);

    public override void _Ready()
    {
        base._Ready();

        timeline = GetNode<Control>(TimelinePath);
        timeline.Connect("draw", this, nameof(TimelineDraw));

        tree = GetNode<Control>(TreePath);
        tree.Connect("draw", this, nameof(TreeDraw));

        treeNodeScene = GD.Load<PackedScene>("res://src/auto-evo/EvolutionaryTreeNode.tscn");

        latoSmallItalic = GD.Load<Font>("res://src/gui_common/fonts/Lato-Italic-Small.tres");
        latoSmallRegular = GD.Load<Font>("res://src/gui_common/fonts/Lato-Regular-Small.tres");
    }

    public void Init(Species luca)
    {
        SetupTreeNode(luca, null, 0);
        treeNodeSize = speciesNodes[luca.ID][0].RectSize;

        speciesOrigin.Add(luca.ID, (uint.MaxValue, 0));
        speciesNames.Add(luca.ID, luca.FormattedName);
        generationTimes.Add(0, 0);
    }

    public void UpdateEvolutionaryTreeWithRunResults(RunResults results, int generation, double time)
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
                        speciesNodes[species.ID].Last(), generation - 1, true);
                }
            }
            else if (result.SplitFrom != null)
            {
                SetupTreeNode((Species)species.Clone(),
                    speciesNodes[result.SplitFrom.ID].Last(), generation);

                speciesOrigin.Add(species.ID, (result.SplitFrom.ID, generation));
                speciesNames.Add(species.ID, species.FormattedName);
            }
            else if (result.MutatedProperties != null)
            {
                SetupTreeNode((Species)result.MutatedProperties.Clone(),
                    speciesNodes[species.ID].Last(), generation);
            }

            if (species.ID > maxSpeciesId)
                maxSpeciesId = species.ID;
        }

        latestGeneration = generation;
        generationTimes[generation] = time;

        BuildTree();
    }

    private void SetupTreeNode(Species species, EvolutionaryTreeNode? parent, int generation,
        bool isLastGeneration = false)
    {
        var node = treeNodeScene.Instance<EvolutionaryTreeNode>();
        node.Generation = generation;
        node.SpeciesID = species.ID;
        node.LastGeneration = false;
        node.ParentNode = parent;
        node.RectPosition = new Vector2(LEFT_MARGIN + generation * GENERATION_SEPARATION, 0);
        node.LastGeneration = isLastGeneration;
        node.Group = nodesGroup;
        node.Connect("pressed", this, nameof(OnTreeNodeSelected), new Array { node });

        if (!speciesNodes.ContainsKey(species.ID))
            speciesNodes.Add(species.ID, new List<EvolutionaryTreeNode>());

        speciesNodes[species.ID].Add(node);
        tree.AddChild(node);
    }

    private void BuildTree()
    {
        uint index = 0;
        BuildTree(speciesNodes.First().Key, ref index);

        tree.Update();
    }

    private void BuildTree(uint id, ref uint index)
    {
        // Adjust nodes of this species' vertical position based on index
        foreach (var treeNode in speciesNodes[id])
        {
            var position = treeNode.RectPosition;
            position.y = index * SPECIES_SEPARATION;
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

    private void TimelineDraw()
    {
        // Draw timeline
        timeline.DrawLine(new Vector2(0, TIMELINE_LINE_Y), new Vector2(RectSize.x, TIMELINE_LINE_Y), Colors.Cyan,
            TIMELINE_LINE_THICKNESS);

        for (int i = 0; i <= latestGeneration; i++)
        {
            timeline.DrawLine(
                new Vector2(LEFT_MARGIN + i * GENERATION_SEPARATION + treeNodeSize.x / 2, TIMELINE_LINE_Y),
                new Vector2(LEFT_MARGIN + i * GENERATION_SEPARATION + treeNodeSize.x / 2,
                    TIMELINE_LINE_Y + TIMELINE_MARK_SIZE),
                Colors.Cyan, TIMELINE_LINE_THICKNESS);

            var localizedText = string.Format(CultureInfo.CurrentCulture, "{0:#,##0,,}", generationTimes[i]) + " "
                + TranslationServer.Translate("MEGA_YEARS");
            var size = latoSmallItalic.GetStringSize(localizedText);
            timeline.DrawString(latoSmallRegular, new Vector2(
                    LEFT_MARGIN + i * GENERATION_SEPARATION + treeNodeSize.x / 2 - size.x / 2,
                    TIMELINE_LINE_Y + TIMELINE_MARK_SIZE * 2 + size.y),
                localizedText, Colors.Cyan);
        }
    }

    private void TreeDraw()
    {
        // Draw new species connection lines
        foreach (var node in speciesNodes.Values.Select(l => l.First()))
        {
            if (node.ParentNode == null)
                continue;

            TreeDrawLine(node.ParentNode.Center, node.Center);
        }

        float treeRightPosition = LEFT_MARGIN + GENERATION_SEPARATION * latestGeneration + treeNodeSize.x;

        // Draw horizontal lines
        foreach (var nodeList in speciesNodes.Values)
        {
            var lineStart = nodeList.First().Center;

            var lastNode = nodeList.Last();

            // If species extinct, line ends at the last node; else it ends at the right end of the tree
            var lineEnd = lastNode.LastGeneration ?
                lastNode.Center :
                new Vector2(treeRightPosition, lineStart.y);

            TreeDrawLine(lineStart, lineEnd);
        }

        // Draw species name
        foreach (var pair in speciesNodes)
        {
            var lastNode = pair.Value.Last();
            if (lastNode.LastGeneration)
            {
                tree.DrawString(latoSmallItalic, new Vector2(
                    lastNode.RectPosition.x + lastNode.RectSize.x + SPECIES_NAME_OFFSET,
                    lastNode.Center.y), speciesNames[pair.Key], Colors.DarkRed);
            }
            else
            {
                tree.DrawString(latoSmallItalic,
                    new Vector2(treeRightPosition + SPECIES_NAME_OFFSET, pair.Value.First().Center.y),
                    speciesNames[pair.Key]);
            }
        }
    }

    private void TreeDrawLine(Vector2 from, Vector2 to)
    {
        if (to.y - from.y < MathUtils.EPSILON)
        {
            tree.DrawLine(from, to, Colors.DarkCyan, 4.0f);
        }
        else
        {
            var mid = to - new Vector2(GENERATION_SEPARATION / 2.0f, 0);
            tree.DrawLine(from, new Vector2(mid.x, from.y), Colors.DarkCyan, 4.0f);
            tree.DrawLine(new Vector2(mid.x, from.y), new Vector2(mid.x, to.y), Colors.DarkCyan, 4.0f);
            tree.DrawLine(new Vector2(mid.x, to.y), to, Colors.DarkCyan, 4.0f);
        }
    }

    private void OnTreeNodeSelected(EvolutionaryTreeNode node)
    {
        EmitSignal(nameof(SpeciesSelected), node.Generation, node.SpeciesID);
    }
}
