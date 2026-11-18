using System.Threading.Tasks;
using AutoEvo;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

/// <summary>
///   Tests that the viewer can display occupancy without changing the Auto-Evo competition tree.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class MicheViewerTests
{
    /// <summary>
    ///   Opening and rebuilding a tree with an occupied and an empty leaf must keep both leaves intact.
    ///   Each opening should create exactly one selection row for each leaf.
    /// </summary>
    [TestCase]
    public async Task ReopeningMicheTreePreservesItsLeaves()
    {
        var root = new Miche(new RootPressure());
        var occupied = new Miche(new RootPressure()) { Occupant = new MicrobeSpecies(1, "Test", "resident") };
        var empty = new Miche(new RootPressure());
        root.AddChild(occupied);
        root.AddChild(empty);

        var viewer = GD.Load<PackedScene>("res://src/auto-evo/MicheTree.tscn").Instantiate<MicheTree>();
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.Root.AddChild(viewer);
        try
        {
            viewer.SetMiche(root);
            AssertThat(occupied.Children).IsEmpty();
            AssertThat(empty.Children).IsEmpty();
            AssertThat(viewer.GetNode<Control>("Tree").GetChildCount()).IsEqual(5);

            viewer.Clear();
            root.SetReadOnly();
            viewer.SetMiche(root);
            AssertThat(viewer.GetNode<Control>("Tree").GetChildCount()).IsEqual(5);
            AssertThat(occupied.Parent).IsSame(root);
            AssertThat(empty.Parent).IsSame(root);
            AssertThat(occupied.Children).IsEmpty();
            AssertThat(empty.Children).IsEmpty();
        }
        finally
        {
            viewer.Free();

            // Clear queues the original five rows for release at the end of the frame.
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    /// <summary>
    ///   A prediction copy and its patch resident can share an image, ID and generation but have different details.
    ///   The panel must retain the most recent selection even before it enters the scene.
    /// </summary>
    [TestCase]
    public void VisuallyIdenticalOccupantsKeepTheirOwnSpeciesDetails()
    {
        var resident = new MicrobeSpecies(1, "Test", "resident")
        {
            MembraneType = SimulationParameters.Instance.GetMembrane("single"),
        };
        var candidate = (Species)resident.Clone();
        candidate.Epithet = "candidate";

        AssertThat(candidate.GetVisualHashCode()).IsEqual(resident.GetVisualHashCode());

        var details = new SpeciesDetailsPanel();
        try
        {
            details.PreviewSpecies = candidate;
            AssertThat(details.PreviewSpecies).IsSame(candidate);
            details.PreviewSpecies = resident;
            AssertThat(details.PreviewSpecies).IsSame(resident);
        }
        finally
        {
            details.Free();
        }
    }
}
