﻿using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using Systems;
using World = DefaultEcs.World;

[With(typeof(StrainAffected))]
[With(typeof(MicrobeControl))]
[With(typeof(OrganelleContainer))]
[ReadsComponent(typeof(OrganelleContainer))]
[RunsBefore(typeof(MicrobeMovementSystem))]
public sealed class StrainSystem : AEntitySetSystem<float>
{
    public StrainSystem(World world, IParallelRunner runner) : base(world, runner,
        Constants.SYSTEM_EXTREME_ENTITIES_PER_THREAD)
    {
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var strain = ref entity.Get<StrainAffected>();
        ref var control = ref entity.Get<MicrobeControl>();

        if (control.OutOfSprint && control.Sprinting)
            control.Sprinting = false;

        if (strain.IsUnderStrain)
        {
            ref var organelles = ref entity.Get<OrganelleContainer>();

            var strainIncrease = Constants.SPRINTING_STRAIN_INCREASE_PER_SECOND * delta;
            strainIncrease += organelles.HexCount * Constants.SPRINTING_STRAIN_INCREASE_PER_HEX;

            strain.CurrentStrain += strainIncrease;

            if (strain.CurrentStrain > Constants.MAX_STRAIN_PER_ENTITY)
            {
                strain.CurrentStrain = Constants.MAX_STRAIN_PER_ENTITY;

                control.Sprinting = false;
                control.OutOfSprint = true;
            }

            strain.StrainDecreaseCooldown = Constants.STRAIN_DECREASE_COOLDOWN_SECONDS;
        }
        else
        {
            if (strain.StrainDecreaseCooldown <= 0)
            {
                ReduceStrain(ref strain, delta);
            }
            else
            {
                strain.StrainDecreaseCooldown -= delta;
                ReduceStrain(ref strain, delta, Constants.PASSIVE_STRAIN_DECREASE_PRE_COOLDOWN_MULTIPLIER);
            }

            if (strain.CurrentStrain <= Constants.MIN_STRAIN_SPRINT_REGAIN)
            {
                control.OutOfSprint = false;
            }
        }
    }

    private void ReduceStrain(ref StrainAffected strain, float delta, float multiplier = 1.0f)
    {
        strain.CurrentStrain -= Constants.PASSIVE_STRAIN_DECREASE_PER_SECOND * delta * multiplier;

        if (strain.CurrentStrain < 0)
            strain.CurrentStrain = 0;
    }
}
