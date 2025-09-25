namespace Systems;

using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Components;

/// <summary>
///   Fills out the component vectors like <see cref="OrganelleContainer.SlimeJets"/>
/// </summary>
[RunsAfter(typeof(MicrobeReproductionSystem))]
[RunsAfter(typeof(MulticellularGrowthSystem))]
[RunsBefore(typeof(MicrobeMovementSystem))]
[RunsBefore(typeof(OrganelleTickSystem))]
[RuntimeCost(0.25f)]
public partial class OrganelleComponentFetchSystem : BaseSystem<World, float>
{
    public OrganelleComponentFetchSystem(World world) : base(world)
    {
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(ref OrganelleContainer container)
    {
        if (container.OrganelleComponentsCached)
            return;

        container.FetchLayoutOrganelleComponents();
    }
}
