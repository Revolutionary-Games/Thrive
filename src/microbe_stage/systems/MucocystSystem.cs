namespace Systems;

using Components;
using DefaultEcs;
using DefaultEcs.System;
using Newtonsoft.Json;

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

    [JsonProperty]
    private readonly MarkedItemList<Entity> activeMucocysts = new();

    public MucocystSystem(World world) : base(world)
    {
    }

    protected override void PreUpdate(float state)
    {
        base.PreUpdate(state);

        activeMucocysts.UnMarkAll();
    }

    protected override void Update(float delta, in Entity entity)
    {
        // Handles invulnerability from mucocyst. Other buffs/debuffs from mucocyst are in related systems to what they
        // affect

        ref var control = ref entity.Get<MicrobeControl>();

        if (control.State != MicrobeState.MucocystShield)
            return;

        ref var storage = ref entity.Get<CompoundStorage>();

        // Take mucilage as cost for keeping the mucocyst active
        var requiredMucilage = Constants.MUCOCYST_MUCILAGE_DRAIN * delta;

        if (storage.Compounds.TakeCompound(mucilageCompound, requiredMucilage) < requiredMucilage)
        {
            control.State = MicrobeState.Normal;

            // Will disable mucocyst effect in the post update
        }
        else
        {
            if (activeMucocysts.AddOrMark(entity))
            {
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

    protected override void PostUpdate(float state)
    {
        base.PostUpdate(state);

        var itemsToRemove = activeMucocysts.GetUnMarkedItems();

        var count = itemsToRemove.Count;
        if (count > 0)
        {
            // Avoid enumerator allocations
            for (int i = 0; i < count; ++i)
            {
                OnMucocystDisabled(itemsToRemove[i].Item);
            }

            activeMucocysts.RemoveUnmarked();
        }
    }

    private void OnMucocystDisabled(in Entity entity)
    {
        // This method checks existing components for everything in case the entity is deleted (so it doesn't have any
        // components anymore)

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
