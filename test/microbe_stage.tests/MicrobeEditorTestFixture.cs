using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using static GdUnit4.Assertions;

/// <summary>
///   Runs cell editor actions without entering the scene tree or starting predictions. The real scene supplies the
///   controls used by move callbacks. Owner queries and mutation-point reset are substituted; cell-type selection
///   models cloning and context routing without running the multicellular tab UI.
/// </summary>
public sealed class MicrobeEditorTestFixture : IDisposable
{
#pragma warning disable CA2213 // Godot nodes are freed in Dispose rather than disposed.
    private readonly Node3D visuals = new();
    private readonly CellEditorComponent editor;
#pragma warning restore CA2213

    private readonly bool reopenBetweenActions;
    private readonly Patch patch;
    private readonly Dictionary<string, CellType> cellTypes = new();

    private readonly Dictionary<CellType, OrganelleLayout<OrganelleTemplate>> cellLayouts =
        new(ReferenceEqualityComparer.Instance);

    private CellType? selectedCellType;

    public MicrobeEditorTestFixture(bool multicellular = true, bool reopenBetweenActions = false)
    {
        this.reopenBetweenActions = reopenBetweenActions;
        var biome = SimulationParameters.Instance.GetBiome("aavolcanic_vent");
        patch = new Patch(new LocalizedString("Editor test"), 1, biome, BiomeType.Vents,
            new PatchSnapshot((BiomeConditions)biome.Conditions.Clone(), biome.Background), 1);
        editor = GD.Load<PackedScene>("res://src/microbe_stage/editor/CellEditorComponent.tscn")
            .Instantiate<CellEditorComponent>();
        editor.IsMulticellularEditor = multicellular;

        var owner = DispatchProxy.Create<ICellEditorData, EditorData>();

        // ReSharper disable once SuspiciousTypeConversion.Global
        ((EditorData)owner).Fixture = this;

        typeof(EditorComponentBase<ICellEditorData>).GetField("editor",
            BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, owner);
        typeof(CellEditorComponent).GetProperty(nameof(CellEditorComponent.Membrane))!
            .SetValue(editor, SimulationParameters.Instance.GetMembrane("single"));

        editor.Colour = Colors.White;
        Invoke("LoadScenes");
        Invoke("LoadHexMaterials");
        SetLayout(new OrganelleLayout<OrganelleTemplate>());
        if (multicellular)
            SelectCellType("First");
    }

    public EditorActionHistory<EditorAction> History { get; } = new();

    public OrganelleLayout<OrganelleTemplate> Layout { get; private set; } = null!;

    public string? SelectedCellTypeName => selectedCellType?.CellTypeName;

    public OrganelleTemplate Template(string type, int q, int r = 0, int rotation = 0)
    {
        return new OrganelleTemplate(SimulationParameters.Instance.GetOrganelleType(type), new Hex(q, r), rotation);
    }

    public OrganelleTemplate At(int q, int r = 0)
    {
        return Layout.First(o => o.Position == new Hex(q, r));
    }

    public void Seed(params OrganelleTemplate[] organelles)
    {
        foreach (var organelle in organelles)
            Layout.AddFast(organelle, [], []);
    }

    public EditorAction Placement(string type, int q, int r = 0, int rotation = 0)
    {
        return (EditorAction)Invoke("CreateOrganellePlacementAction", Template(type, q, r, rotation),
            new Hex(q, r), rotation)!;
    }

    public EditorAction Movement(int fromQ, int toQ, int rotation = 0, int fromR = 0, int toR = 0)
    {
        return (EditorAction)Invoke("CreateOrganelleMoveAction", At(fromQ, fromR), new Hex(toQ, toR), rotation)!;
    }

    public EditorAction Removal(int q, int r = 0)
    {
        // The callback allows null tutorial state, so this also exercises ordinary (non-replacement) removals.
        var data = new OrganelleRemoveActionData(At(q, r));
        return new SingleEditorAction<OrganelleRemoveActionData>(d => Invoke("DoOrganelleRemoveAction", d),
            d => Invoke("UndoOrganelleRemoveAction", d), data);
    }

    public EditorAction NewCell()
    {
        var oldLayout = new OrganelleLayout<OrganelleTemplate>();
        foreach (var organelle in Layout)
            oldLayout.AddFast(organelle, [], []);
        var data = new NewMicrobeActionData(oldLayout, editor.Membrane, editor.Rigidity, editor.Colour, null, null);
        return new SingleEditorAction<NewMicrobeActionData>(d => Invoke("DoNewMicrobeAction", d),
            d => Invoke("UndoNewMicrobeAction", d), data);
    }

    public void Perform(params EditorAction[] actions)
    {
        foreach (var data in actions.SelectMany(action => action.Data).OfType<EditorCombinableActionData<CellType>>())
            data.Context = selectedCellType;
        RunWithVisualCleanup(() =>
            History.AddAction(actions.Length == 1 ? actions[0] : new CombinedEditorAction(actions)));
        if (reopenBetweenActions)
            ReopenCellType();
    }

    public void Undo(int count = 1)
    {
        for (int i = 0; i < count; ++i)
        {
            if (History.GetUndoContext<CellType>() is { } context)
                SelectCellType(context);
            RunWithVisualCleanup(() => AssertThat(History.Undo()).IsTrue());
            if (reopenBetweenActions)
                ReopenCellType();
        }
    }

    public void Redo(int count = 1)
    {
        for (int i = 0; i < count; ++i)
        {
            if (History.GetRedoContext<CellType>() is { } context)
                SelectCellType(context);
            RunWithVisualCleanup(() => AssertThat(History.Redo()).IsTrue());
            if (reopenBetweenActions)
                ReopenCellType();
        }
    }

    /// <summary>
    ///   Models the layout cloning done when leaving and reopening a cell type, retaining the action history.
    ///   This deliberately bypasses tab UI, name changes, and body-plan state.
    /// </summary>
    public void ReopenCellType()
    {
        var copy = new OrganelleLayout<OrganelleTemplate>();
        foreach (var organelle in Layout)
            copy.AddFast(organelle.Clone(), [], []);
        SetLayout(copy);
    }

    public void SelectCellType(string name)
    {
        if (!cellTypes.TryGetValue(name, out var type))
        {
            type = new CellType(SimulationParameters.Instance.GetMembrane("single")) { CellTypeName = name };
            cellTypes.Add(name, type);
            cellLayouts.Add(type, new OrganelleLayout<OrganelleTemplate>());
        }

        SelectCellType(type);
    }

    public void SetLayout(OrganelleLayout<OrganelleTemplate> layout)
    {
        Layout = layout;
        typeof(CellEditorComponent).GetField("editedMicrobeOrganelles",
            BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, layout);
    }

    /// <summary>
    ///   Checks the complete layout, including definition, origin, rotation, and occupied hexes.
    /// </summary>
    public void AssertLayout(params OrganelleTemplate[] expected)
    {
        AssertThat(Layout.Count).IsEqual(expected.Length);
        foreach (var organelle in expected)
        {
            AssertThat(Layout.Count(o => ReferenceEquals(o.Definition, organelle.Definition) &&
                o.Position == organelle.Position &&
                o.Orientation == organelle.Orientation)).IsEqual(1);
            foreach (var hex in organelle.RotatedHexes)
            {
                var occupying = Layout.GetElementAt(organelle.Position + hex, []);
                AssertThat(occupying != null && ReferenceEquals(occupying.Definition, organelle.Definition) &&
                    occupying.Position == organelle.Position).IsTrue();
            }
        }
    }

    public object? Invoke(string method, params object[] arguments)
    {
        return typeof(CellEditorComponent).GetMethod(method,
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, arguments);
    }

    public void Dispose()
    {
        editor.Free();
        visuals.Free();
    }

    private void SelectCellType(CellType type)
    {
        if (ReferenceEquals(selectedCellType, type))
            return;

        if (selectedCellType != null)
            cellLayouts[selectedCellType] = Layout;

        selectedCellType = type;
        SetLayout(cellLayouts[type]);
        ReopenCellType();
    }

    private void RunWithVisualCleanup(Action action)
    {
        // SceneDisplayer queues old models for deletion when switching organelle types. These tests run outside
        // the scene tree, so free those detached nodes immediately rather than waiting for another frame.
        var oldVisuals = visuals.FindChildren("*", owned: false);
        try
        {
            action();
        }
        finally
        {
            foreach (var node in oldVisuals)
            {
                if (GodotObject.IsInstanceValid(node) && node.IsQueuedForDeletion())
                    node.Free();
            }
        }
    }

    /// <summary>
    ///   Strict substitute for the few owner queries reached by organelle callbacks. Unexpected calls fail the test.
    /// </summary>
    public class EditorData : DispatchProxy
    {
        public MicrobeEditorTestFixture Fixture { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod!.Name switch
            {
                "get_RootOfDynamicallySpawned" => Fixture.visuals,
                "get_CurrentPatch" => Fixture.patch,
                "HexPlacedThisSession" => Fixture.History.HexPlacedThisSession<OrganelleTemplate, CellType>(
                    (OrganelleTemplate)args![0]!),
                "CalculateRawTolerances" => new ToleranceResult { OverallScore = 1 },
                "set_MutationPoints" => null,
                _ => throw new NotSupportedException($"Unexpected editor query: {targetMethod.Name}"),
            };
        }
    }
}
