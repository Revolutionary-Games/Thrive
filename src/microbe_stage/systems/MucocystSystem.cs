namespace Systems;

using Components;
using DefaultEcs;
using DefaultEcs.System;

/// <summary>
///   Handles gaining immortality from mucocyst ability
/// </summary>
[With(typeof(MicrobeControl))]
[With(typeof(Health))]
[With(typeof(CompoundStorage))]
[With(typeof(CellProperties))]
[With(typeof(CompoundAbsorber))]
[Without(typeof(AttachedToEntity))]
[ReadsComponent(typeof(CellProperties))]
[RunsBefore(typeof(MicrobeMovementSystem))]
[RunsOnMainThread]
public sealed class MucocystSystem : AEntitySetSystem<float>
{
    private readonly Compound mucilageCompound = SimulationParameters.Instance.GetCompound("mucilage");

    public MucocystSystem(World world) : base(world)
    {
    }

    protected override void Update(float delta, in Entity entity)
    {
        // Handles invulnerability from mucocyst. Other buffs/debuffs from mucocyst are in related systems to what they
        // affect

        ref var control = ref entity.Get<MicrobeControl>();

        bool wantsMucocyst = control.State == MicrobeState.MucocystShield;

        // Skip processing if already in correct state (and not currently using mucocyst which has an upkeep cost)
        if (control.MucocystEffectsApplied == wantsMucocyst && !control.MucocystEffectsApplied)
            return;

        if (wantsMucocyst)
        {
            ref var storage = ref entity.Get<CompoundStorage>();

            // Take mucilage as cost for keeping the mucocyst active
            var requiredMucilage = Constants.MUCOCYST_MUCILAGE_DRAIN * delta;

            if (storage.Compounds.TakeCompound(mucilageCompound, requiredMucilage) < requiredMucilage)
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

                // Make sure membrane is playing the mucocyst effect animation
                var membrane = entity.Get<CellProperties>().CreatedMembrane;

                membrane?.SetMucocystEffectVisible(true);
            }
        }

        // If cell doesn't want to keep mucocyst on or cannot afford, then disable the effects
        if (!wantsMucocyst)
        {
            OnMucocystDisabled(entity);
            control.MucocystEffectsApplied = false;
        }
    }

    // This system could be refactored to use a post update to apply visual effects to be able to run with multiple
    // threads processing all the entities

    // protected override void PostUpdate(float state)

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
        if (entity.Has<Health>())
        {
            entity.Get<Health>().Invulnerable =
                HealthHelpers.GetMicrobeInvulnerabilityState(entity.Has<PlayerMarker>(), false);
        }
    }
}
