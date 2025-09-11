namespace Systems;

using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;

/// <summary>
///   Fills out the component vectors like <see cref="OrganelleContainer.SlimeJets"/>
/// </summary>
[With(typeof(OrganelleContainer))]
[RunsAfter(typeof(MicrobeReproductionSystem))]
[RunsAfter(typeof(MulticellularGrowthSystem))]
[RunsBefore(typeof(MicrobeMovementSystem))]
[RunsBefore(typeof(OrganelleTickSystem))]
[RuntimeCost(0.25f)]
public partial class OrganelleComponentFetchSystem : BaseSystem<World, float>
{
    public OrganelleComponentFetchSystem(World world, IParallelRunner runner) : base(world, runner)
    {
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update([Data] in float delta, ref TODO components, in Entity entity)
    {
        ref var container = ref entity.Get<OrganelleContainer>();

        if (container.OrganelleComponentsCached)
            return;

        container.FetchLayoutOrganelleComponents();
    }
}
