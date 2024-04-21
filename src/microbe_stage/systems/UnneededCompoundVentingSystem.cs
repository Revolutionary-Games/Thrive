namespace Systems;

using System;
using System.Collections.Generic;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using Godot;
using World = DefaultEcs.World;

/// <summary>
///   Handles venting unneeded compounds or compounds that exceed storage capacity from microbes
/// </summary>
[With(typeof(UnneededCompoundVenter))]
[With(typeof(CellProperties))]
[With(typeof(CompoundStorage))]
[With(typeof(WorldPosition))]
[ReadsComponent(typeof(UnneededCompoundVenter))]
[ReadsComponent(typeof(CellProperties))]
[ReadsComponent(typeof(WorldPosition))]
[Without(typeof(AttachedToEntity))]
[RunsAfter(typeof(ProcessSystem))]
[RuntimeCost(9)]
public sealed class UnneededCompoundVentingSystem : AEntitySetSystem<float>
{
    private readonly CompoundCloudSystem compoundCloudSystem;
    private readonly IReadOnlyList<Compound> ventableCompounds;

    public UnneededCompoundVentingSystem(CompoundCloudSystem compoundCloudSystem, World world,
        IParallelRunner parallelRunner) : base(world, parallelRunner, Constants.SYSTEM_HIGHER_ENTITIES_PER_THREAD)
    {
        this.compoundCloudSystem = compoundCloudSystem;

        // Cloud types are ones that can be vented
        ventableCompounds = SimulationParameters.Instance.GetCloudCompounds();
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var venter = ref entity.Get<UnneededCompoundVenter>();

        if (venter.VentThreshold >= float.MaxValue)
            return;

        var compounds = entity.Get<CompoundStorage>().Compounds;

        // Skip until something is marked as useful (set by bio process system)
        if (!compounds.HasAnyBeenSetUseful())
            return;

        ref var position = ref entity.Get<WorldPosition>();
        ref var cellProperties = ref entity.Get<CellProperties>();

        float amountToVent = Constants.COMPOUNDS_TO_VENT_PER_SECOND * delta;

        int count = ventableCompounds.Count;

        // Manual loop here to avoid enumerator allocations
        for (int i = 0; i < count; ++i)
        {
            var type = ventableCompounds[i];

            var capacity = compounds.GetCapacityForCompound(type);

            // Vent if not useful, or if overflowed the capacity
            // The multiply by threshold is here to be more kind to cells that have just divided and make it
            // much less likely the player often sees their cell venting away their precious compounds
            if (!compounds.IsUseful(type))
            {
                amountToVent -=
                    cellProperties.EjectCompound(ref position, compounds, compoundCloudSystem, type, amountToVent,
                        Vector3.Back);
            }
            else if (compounds.GetCompoundAmount(type) > venter.VentThreshold * capacity)
            {
                // Vent the part that went over
                float toVent = compounds.GetCompoundAmount(type) - venter.VentThreshold * capacity;

                amountToVent -= cellProperties.EjectCompound(ref position, compounds, compoundCloudSystem, type,
                    Math.Min(toVent, amountToVent), Vector3.Back);
            }

            if (amountToVent <= 0)
                break;
        }
    }
}
