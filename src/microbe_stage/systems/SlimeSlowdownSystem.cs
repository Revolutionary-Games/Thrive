namespace Systems;

using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Components;

/// <summary>
///   Handles slowing down cells that are currently moving through slime (and don't have slime jets themselves)
/// </summary>
[ReadsComponent(typeof(OrganelleContainer))]
[ReadsComponent(typeof(WorldPosition))]
[RunsAfter(typeof(OrganelleComponentFetchSystem))]
[RunsBefore(typeof(MicrobeMovementSystem))]
[RuntimeCost(6)]
public partial class SlimeSlowdownSystem : BaseSystem<World, float>
{
    private readonly IReadonlyCompoundClouds compoundCloudSystem;

    public SlimeSlowdownSystem(IReadonlyCompoundClouds compoundCloudSystem, World world) : base(world)
    {
        this.compoundCloudSystem = compoundCloudSystem;
    }

    [Query]
    [None<AttachedToEntity>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref MicrobeControl control, ref OrganelleContainer organelles, ref WorldPosition position)
    {
        // Mucilage doesn't affect cells with jets
        if (organelles.SlimeJets is { Count: > 0 })
        {
            control.SlowedBySlime = false;
            return;
        }

        control.SlowedBySlime = compoundCloudSystem.AmountAvailable(Compound.Mucilage, position.Position, 1.0f) >
            Constants.COMPOUND_DENSITY_CATEGORY_FAIR_AMOUNT;
    }
}
