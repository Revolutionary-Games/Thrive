using System.Runtime.CompilerServices;
using Arch.System;
using Components;
using Systems;
using World = Arch.Core.World;

/// <summary>
///   Handles strain buildup, reduction, and preventing sprinting when under too much strain
/// </summary>
[ReadsComponent(typeof(OrganelleContainer))]
[RunsBefore(typeof(MicrobeMovementSystem))]
[RuntimeCost(0.5f)]
public partial class StrainSystem : BaseSystem<World, float>
{
    public StrainSystem(World world) : base(world)
    {
    }

    [Query]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update([Data] in float delta, ref StrainAffected strain, ref MicrobeControl control,
        ref OrganelleContainer organelles)
    {
        if (control.OutOfSprint && control.Sprinting)
            control.Sprinting = false;

        if (strain.IsUnderStrain)
        {
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
