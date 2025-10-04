namespace Systems;

using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Components;

/// <summary>
///   Evenly distributes compounds (except ones that can't be shared between cells like ATP) between cells in a
///   colony
/// </summary>
/// <remarks>
///   <para>
///     This runs before the engulfing system to allow newly spawned multicellular members to stay in engulf mode
///     when growing as otherwise the cell would have no compounds and get immediately kicked out of engulf mode
///     due to missing ATP.
///   </para>
/// </remarks>
[ReadsComponent(typeof(MicrobeColony))]
[WritesToComponent(typeof(CompoundStorage))]
[RunsBefore(typeof(EngulfingSystem))]
public partial class ColonyCompoundDistributionSystem : BaseSystem<World, float>
{
    public ColonyCompoundDistributionSystem(World world) : base(world)
    {
    }

    [Query]
    [None<AttachedToEntity>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref MicrobeColony colony)
    {
        colony.GetCompounds().DistributeCompoundSurplus();
    }
}
