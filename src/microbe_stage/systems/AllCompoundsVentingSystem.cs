namespace Systems;

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;

/// <summary>
///   Vents all compounds until empty from a <see cref="CompoundStorage"/> that has a <see cref="CompoundVenter"/>.
///   Requires a <see cref="WorldPosition"/>
/// </summary>
[WritesToComponent(typeof(Physics))]
[WritesToComponent(typeof(MicrobeShaderParameters))]
[ReadsComponent(typeof(WorldPosition))]
[RunsAfter(typeof(PhysicsUpdateAndPositionSystem))]
[RuntimeCost(9)]
public partial class AllCompoundsVentingSystem : BaseSystem<World, float>
{
    private readonly CompoundCloudSystem compoundCloudSystem;
    private readonly WorldSimulation worldSimulation;

    // This list makes this not able to be run in parallel (would need a thread local list or something like that)
    private readonly List<Compound> processedCompoundKeys = new();

    public AllCompoundsVentingSystem(CompoundCloudSystem compoundClouds, WorldSimulation worldSimulation,
        World world) : base(world)
    {
        compoundCloudSystem = compoundClouds;
        this.worldSimulation = worldSimulation;
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update([Data] in float delta, ref CompoundVenter venter, ref CompoundStorage compoundStorage,
        ref WorldPosition position, in Entity entity)
    {
        // TODO: rate limit updates if needed for performance?

        if (venter.VentingPrevented)
            return;

        var compounds = compoundStorage.Compounds;

        if (compounds.Compounds.Count < 1)
        {
            // Empty, perform defined actions for when this venter runs out
            OnOutOfCompounds(in entity, ref venter);
            return;
        }

        processedCompoundKeys.Clear();
        processedCompoundKeys.AddRange(compounds.Compounds.Keys);

        // Loop through all the compounds in the storage bag and eject them
        bool vented = false;
        foreach (var compound in processedCompoundKeys)
        {
            if (compoundStorage.VentChunkCompound(compound, delta * venter.VentEachCompoundPerSecond, position.Position,
                    compoundCloudSystem))
            {
                vented = true;
            }
        }

        if (!vented)
        {
            OnOutOfCompounds(in entity, ref venter);
        }
    }

    private void OnOutOfCompounds(in Entity entity, ref CompoundVenter venter)
    {
        if (venter.RanOutOfVentableCompounds)
            return;

        // Stop venting
        venter.VentingPrevented = true;
        venter.RanOutOfVentableCompounds = true;

        if (venter.UsesMicrobialDissolveEffect)
        {
            // Disable physics to stop collisions
            if (entity.Has<Physics>())
            {
                ref var physics = ref entity.Get<Physics>();
                physics.BodyDisabled = true;
            }

            entity.StartDissolveAnimation(worldSimulation, true, true);

            // This entity is no longer important to save
            worldSimulation.ReportEntityDyingSoon(entity);
        }
        else if (venter.DestroyOnEmpty)
        {
            worldSimulation.DestroyEntity(entity);
        }
    }
}
