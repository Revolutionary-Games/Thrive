namespace Systems;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.System;
using Components;
using Godot;
using World = Arch.Core.World;

/// <summary>
///   Handles venting unneeded compounds or compounds that exceed storage capacity from microbes
/// </summary>
/// <remarks>
///   <para>
///     Marked as being on the main thread as that's a limitation of Arch ECS parallel processing.
///   </para>
/// </remarks>
[ReadsComponent(typeof(UnneededCompoundVenter))]
[ReadsComponent(typeof(CellProperties))]
[ReadsComponent(typeof(WorldPosition))]
[RunsAfter(typeof(ProcessSystem))]
[RunsOnMainThread]
[RuntimeCost(10)]
public partial class UnneededCompoundVentingSystem : BaseSystem<World, float>
{
    private readonly CompoundCloudSystem compoundCloudSystem;
    private readonly IReadOnlyList<CompoundDefinition> ventableCompounds;

    public UnneededCompoundVentingSystem(CompoundCloudSystem compoundCloudSystem, World world) : base(world)
    {
        this.compoundCloudSystem = compoundCloudSystem;

        // Cloud types are ones that can be vented
        ventableCompounds = SimulationParameters.Instance.GetCloudCompounds();
    }

    [Query(Parallel = true)]
    [None<AttachedToEntity>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update([Data] in float delta, ref UnneededCompoundVenter venter, ref CompoundStorage storage,
        ref WorldPosition position, ref CellProperties cellProperties)
    {
        if (venter.VentThreshold >= float.MaxValue)
            return;

        var compounds = storage.Compounds;

        // Skip until something is marked as useful (set by the bioprocess system)
        if (!compounds.HasAnyBeenSetUseful())
            return;

        float amountToVent = Constants.COMPOUNDS_TO_VENT_PER_SECOND * delta;

        int count = ventableCompounds.Count;

        // Manual loop here to avoid enumerator allocations
        for (int i = 0; i < count; ++i)
        {
            var type = ventableCompounds[i].ID;

            var capacity = compounds.GetCapacityForCompound(type);

            // Vent if not useful, or if overflowed the capacity
            // The multiply by threshold is here to be more kind to cells that have just divided and make it
            // much less likely the player often sees their cell venting away their precious compounds
            if (!compounds.IsUseful(type) && !SimulationParameters.GetCompound(type).AlwaysAbsorbable)
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
