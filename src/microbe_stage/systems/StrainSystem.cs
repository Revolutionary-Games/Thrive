using Components;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using Godot;
using World = DefaultEcs.World;

[With(typeof(OrganelleContainer))]
[With(typeof(StrainAffected))]
[ReadsComponent(typeof(OrganelleContainer))]
[ReadsComponent(typeof(MicrobeControl))]
public sealed class StrainSystem : AEntitySetSystem<float>
{
    public StrainSystem(World world, IParallelRunner runner) : base(world, runner)
    {
    }

    protected override void Update(float delta, in Entity entity)
    {
        ref var strain = ref entity.Get<StrainAffected>();
        ref var organelles = ref entity.Get<OrganelleContainer>();

        if (strain.IsUnderStrain)
        {
            var strainIncrease = Constants.SPRINTING_STRAIN_INCREASE_PER_SECOND * delta;
            strainIncrease += organelles.HexCount * Constants.SPRINTING_STRAIN_INCREASE_PER_HEX;

            strain.CurrentStrain += strainIncrease;

            if (strain.CurrentStrain > Constants.MAX_STRAIN_PER_ENTITY)
                strain.CurrentStrain = Constants.MAX_STRAIN_PER_ENTITY;

            strain.StrainDecreaseCooldown = Constants.STRAIN_DECREASE_COOLDOWN_SECONDS;
        }
        else
        {
            if (strain.StrainDecreaseCooldown <= Mathf.Epsilon)
            {
                ReduceStrain(ref strain, delta);
            }
            else
            {
                strain.StrainDecreaseCooldown -= delta;
                ReduceStrain(ref strain, delta, Constants.PASSIVE_STRAIN_DECREASE_PRE_COOLDOWN_DIVISOR);
            }
        }
    }

    private void ReduceStrain(ref StrainAffected strain, float delta, float divisor = 1.0f)
    {
        strain.CurrentStrain -= Constants.PASSIVE_STRAIN_DECREASE_PER_SECOND * delta / divisor;

        if (strain.CurrentStrain < 0)
            strain.CurrentStrain = 0;
    }
}
