namespace Systems;

using System;
using System.Collections.Generic;
using System.IO.Pipes;
using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;

/// <summary>
///   Handles gaining immortality from mucocyst ability
/// </summary>
[With(typeof(MicrobeControl))]
[With(typeof(Health))]
[With(typeof(CompoundStorage))]
[With(typeof(CellProperties))]
[Without(typeof(AttachedToEntity))]
[ReadsComponent(typeof(CellProperties))]
[RunsAfter(typeof(OrganelleComponentFetchSystem))]
[RunsBefore(typeof(MicrobeMovementSystem))]
public sealed class MucocystSystem : AEntitySetSystem<float>
{
    private Compound mucilageCompound = SimulationParameters.Instance.GetCompound("mucilage");

    private List<Entity> activeMucocysts = new();

    public MucocystSystem(World world) :
        base(world)
    {
    }

    protected override void Update(float delta, in Entity entity)
    {
        // Handles invulnerability from mucocyst. Other buffs/debuffs from mucocyst are in related systems

        ref var control = ref entity.Get<MicrobeControl>();

        if (control.State == MicrobeState.MucocystShield)
        {
            ref var storage = ref entity.Get<CompoundStorage>();

            storage.Compounds.
                Compounds[mucilageCompound] -= Math.Min(
                Constants.MUCOCYST_MUCILAGE_DRAIN * delta,
                storage.Compounds.Compounds[mucilageCompound]);

            if (storage.Compounds.Compounds[mucilageCompound] <= 0)
                control.State = MicrobeState.Normal;

            entity.Get<Health>().Invulnerable = true;

            var membrane = entity.Get<CellProperties>().CreatedMembrane;

            membrane?.SetMucocystEffectVisible(true);
        }
        else
        {
            entity.Get<Health>().Invulnerable = entity.Has<PlayerMarker>() ? CheatManager.GodMode : false;

            var membrane = entity.Get<CellProperties>().CreatedMembrane;

            membrane?.SetMucocystEffectVisible(false);
        }
    }
}
