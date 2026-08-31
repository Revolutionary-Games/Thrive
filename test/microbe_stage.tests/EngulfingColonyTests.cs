using System;
using Arch.Core;
using Arch.Core.Extensions;
using Components;
using GdUnit4;
using Godot;
using Systems;
using Test.Utils;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class EngulfingColonyTests
{
    [TestCase]
    public void EngulfingAColonyMemberReplacesItsColonyAttachment()
    {
        using var setup = CreateSetup(0);

        AssertThat(setup.EngulfingSystem.CheckStartEngulfingOnCandidate(ref setup.Engulfer.Get<CellProperties>(),
            ref setup.Engulfer.Get<Engulfer>(), ref setup.Engulfer.Get<SpeciesMember>(), setup.Engulfer,
            setup.Target)).IsTrue();

        setup.World.ProcessAll(0.1f);

        AssertThat(setup.Target.Has<MicrobeColonyMember>()).IsFalse();
        AssertThat(setup.Target.Has<AttachedToEntity>()).IsTrue();
        AssertThat(setup.Target.Get<AttachedToEntity>().AttachedTo).IsEqual(setup.Engulfer);
        AssertThat(setup.Target.Get<Engulfable>().PhagocytosisStep).IsEqual(PhagocytosisPhase.Ingestion);
    }

    [TestCase]
    public void FullEngulfmentStorageDoesNotDetachColonyMember()
    {
        using var setup = CreateSetup(9.5f);
        bool storageFullReported = false;

        setup.Engulfer.Get<MicrobeEventCallbacks>().OnEngulfmentStorageFull = _ => storageFullReported = true;

        AssertThat(setup.EngulfingSystem.CheckStartEngulfingOnCandidate(ref setup.Engulfer.Get<CellProperties>(),
            ref setup.Engulfer.Get<Engulfer>(), ref setup.Engulfer.Get<SpeciesMember>(), setup.Engulfer,
            setup.Target)).IsFalse();

        setup.World.ProcessAll(0.1f);

        AssertThat(storageFullReported).IsTrue();
        AssertThat(setup.Target.Has<MicrobeColonyMember>()).IsTrue();
        AssertThat(setup.Target.Has<AttachedToEntity>()).IsTrue();
        AssertThat(setup.Target.Get<AttachedToEntity>().AttachedTo).IsEqual(setup.ColonyLeader);
        AssertThat(setup.Target.Get<Engulfable>().PhagocytosisStep).IsEqual(PhagocytosisPhase.None);
    }

    private static Setup CreateSetup(float usedEngulfingCapacity)
    {
        var world = new TestWorldSimulation();
        var membraneType = SimulationParameters.Instance.GetMembrane("single");
        var species = new MicrobeSpecies(1001, "Engulfer", "Test")
        {
            MembraneType = membraneType,
        };

        var engulfer = world.EntitySystem.Create(
            new CellProperties
            {
                MembraneType = membraneType,
                CreatedMembrane = CreateDummyMembrane(membraneType),
                ShapeCreated = true,
            },
            new Engulfer
            {
                EngulfingSize = 10,
                UsedEngulfingCapacity = usedEngulfingCapacity,
                EngulfStorageSize = 10,
            },
            new SpeciesMember(species),
            new WorldPosition(Vector3.Zero),
            default(RenderPriorityOverride),
            default(MicrobeEventCallbacks));

        var colonyLeader = world.EntitySystem.Create(
            default(CellProperties),
            new CompoundStorage { Compounds = new CompoundBag(10) },
            default(MicrobeControl),
            default(Physics),
            new WorldPosition(Vector3.Zero));

        var target = world.EntitySystem.Create(
            new EntityRadiusInfo(0.5f),
            default(SpatialInstance),
            new WorldPosition(Vector3.Zero),
            default(Physics),
            default(MicrobeControl),
            new Engulfable(PhagocytosisPhase.None, Entity.Null)
            {
                BaseEngulfSize = 1,
            },
            new CompoundStorage { Compounds = new CompoundBag(10) },
            new AttachedToEntity(colonyLeader, Vector3.One, Quaternion.Identity));

        var colony = new MicrobeColony(colonyLeader, MicrobeState.Normal, colonyLeader, target);
        colonyLeader.Add(colony);
        target.Add(new MicrobeColonyMember(colonyLeader));

        var engulfingSystem = new EngulfingSystem(world, new DummySpawnSystem(), world.EntitySystem);

        return new Setup(world, engulfingSystem, engulfer, colonyLeader, target,
            engulfer.Get<CellProperties>().CreatedMembrane!);
    }

    private static Membrane CreateDummyMembrane(MembraneType membraneType)
    {
        var points = new[]
        {
            new Vector2(-1, -1),
            new Vector2(-1, 1),
            new Vector2(1, 1),
            new Vector2(1, -1),
        };

        var membrane = new Membrane();
        membrane.MembraneData = new MembranePointData([Vector2.Zero], 1, membraneType, points);
        return membrane;
    }

    private sealed record Setup(TestWorldSimulation World, EngulfingSystem EngulfingSystem, Entity Engulfer,
        Entity ColonyLeader, Entity Target, Membrane Membrane) : IDisposable
    {
        public void Dispose()
        {
            World.Dispose();
            Membrane.Free();
        }
    }
}
