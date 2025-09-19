namespace Systems;

using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Components;

/// <summary>
///   Handles gaining immortality from mucocyst ability
/// </summary>
[ReadsComponent(typeof(CellProperties))]
[RunsBefore(typeof(MicrobeMovementSystem))]
[RunsOnMainThread]
public partial class MucocystSystem : BaseSystem<World, float>
{
    public MucocystSystem(World world) : base(world)
    {
    }

    [Query]
    [All<Health, CompoundStorage, CellProperties, CompoundAbsorber>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update([Data] in float delta, ref MicrobeControl control, ref CompoundStorage storage,
        in Entity entity)
    {
        // Handles invulnerability from mucocyst. Other buffs/debuffs from mucocyst are in related systems to what they
        // affect

        bool wantsMucocyst = control.State == MicrobeState.MucocystShield;

        // Skip processing if already in the correct state (and not currently using mucocyst, which has an upkeep cost)
        if (control.MucocystEffectsApplied == wantsMucocyst && !control.MucocystEffectsApplied)
            return;

        if (wantsMucocyst)
        {
            // Take mucilage as a cost for keeping the mucocyst active
            var requiredMucilage = Constants.MUCOCYST_MUCILAGE_DRAIN * delta;

            if (storage.Compounds.TakeCompound(Compound.Mucilage, requiredMucilage) < requiredMucilage)
            {
                // Not enough to keep using mucocyst
                control.State = MicrobeState.Normal;
                wantsMucocyst = false;
            }
            else
            {
                if (!control.MucocystEffectsApplied)
                {
                    control.MucocystEffectsApplied = true;

                    // Started mucocyst mode on this entity
                    // Apply mucocyst effects
                    entity.Get<Health>().Invulnerable = true;

                    // Disable compound absorbing
                    entity.Get<CompoundAbsorber>().AbsorbSpeed = -1;
                }

                // Make sure the membrane is playing the mucocyst effect animation
                var membrane = entity.Get<CellProperties>().CreatedMembrane;

                membrane?.SetMucocystEffectVisible(true);
            }
        }

        // If the cell doesn't want to keep mucocyst on or cannot afford, then disable the effects
        if (!wantsMucocyst)
        {
            OnMucocystDisabled(entity);
            control.MucocystEffectsApplied = false;
        }
    }

    // This system could be refactored to use a post-update to apply visual effects to be able to run with multiple
    // threads processing all the entities

    // public override void AfterUpdate(in float delta)

    private void OnMucocystDisabled(in Entity entity)
    {
        // This used to be able to trigger after destroying an entity, which is why this checks all components existing

        // Disable membrane effect
        if (entity.Has<CellProperties>())
        {
            var membrane = entity.Get<CellProperties>().CreatedMembrane;

            membrane?.SetMucocystEffectVisible(false);
        }

        // Restore absorb speed
        if (entity.Has<CompoundAbsorber>())
        {
            entity.Get<CompoundAbsorber>().AbsorbSpeed = 0;
        }

        // Restore invulnerability state
        entity.Get<Health>().Invulnerable =
            HealthHelpers.GetMicrobeInvulnerabilityState(entity.Has<PlayerMarker>(), false);
    }
}
