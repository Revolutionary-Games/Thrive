namespace Systems;

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;

/// <summary>
///   Handles absorbing compounds from <see cref="CompoundCloudSystem"/> into <see cref="CompoundStorage"/>
/// </summary>
/// <remarks>
///   <para>
///     Marked as being on the main thread as that's a limitation of Arch ECS parallel processing.
///   </para>
/// </remarks>
[ReadsComponent(typeof(WorldPosition))]
[RunsOnMainThread]
[RuntimeCost(38)]
public partial class CompoundAbsorptionSystem : BaseSystem<World, float>
{
    private readonly CompoundCloudSystem compoundCloudSystem;

    public CompoundAbsorptionSystem(CompoundCloudSystem compoundCloudSystem, World world) : base(world)
    {
        this.compoundCloudSystem = compoundCloudSystem;
    }

    [Query(Parallel = true)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update([Data] in float delta, ref CompoundAbsorber absorber, ref CompoundStorage storage,
        ref WorldPosition position, in Entity entity)
    {
        if (absorber.AbsorbRadius <= 0 || absorber.AbsorbSpeed < 0)
            return;

        if (absorber.AbsorbSpeed != 0)
        {
            // Rate-limited absorbing is not implemented
            throw new NotImplementedException();
        }

        if (absorber.OnlyAbsorbUseful && !storage.Compounds.HasAnyBeenSetUseful())
        {
            // Skip processing until something is set useful
            // TODO: maybe there is a conceivable scenario where only generally useful compounds should be absorbed
            // in which case this check fails even though the generally useful stuff should be absorbed
            return;
        }

        if (!absorber.OnlyAbsorbUseful)
        {
            // The clouds by default check that the bag has a compound set useful before absorbing it, so if this
            // flag is set to false, we would need to communicate that to the clouds someway
            throw new NotImplementedException();
        }

        compoundCloudSystem.AbsorbCompounds(position.Position, absorber.AbsorbRadius, storage.Compounds,
            absorber.TotalAbsorbedCompounds, delta, absorber.AbsorptionRatio);

        // Player infinite compounds cheat, doesn't *really* belong here, but this is probably the best place to put
        // this instead of creating a dedicated cheat handling system
        if (CheatManager.InfiniteCompounds && entity.Has<PlayerMarker>())
        {
            var usefulCompounds =
                SimulationParameters.Instance.GetCloudCompounds().Where(storage.Compounds.IsUseful);
            foreach (var usefulCompound in usefulCompounds)
            {
                storage.Compounds.AddCompound(usefulCompound.ID,
                    storage.Compounds.GetFreeSpaceForCompound(usefulCompound.ID));
            }
        }
    }
}
