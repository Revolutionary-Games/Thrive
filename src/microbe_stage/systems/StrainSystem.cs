namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    [With(typeof(CompoundStorage))]
    [With(typeof(OrganelleContainer))]
    [With(typeof(MicrobeControl))]
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
            ref var control = ref entity.Get<MicrobeControl>();
            ref var strain = ref entity.Get<StrainAffected>();
            ref var organelles = ref entity.Get<OrganelleContainer>();

            if (strain.IsUnderStrain)
            {
                var strainIncrease = Constants.SPRINTING_STRAIN_INCREASE_PER_UPDATE;
                strainIncrease += organelles.HexCount * Constants.SPRINTING_STRAIN_INCREASE_PER_HEX;

                if (strain.CurrentStrain > Constants.MAX_STRAIN_PER_CELL)
                {
                    var difference = strain.CurrentStrain - Constants.MAX_STRAIN_PER_CELL;
                    strain.ExcessStrain += difference;

                    strain.CurrentStrain = Constants.MAX_STRAIN_PER_CELL;
                }

                strain.CurrentStrain += strainIncrease;

                strain.StrainDecreaseCooldown = Constants.STRAIN_DECREASE_COOLDOWN_SECONDS;
            }
            else
            {
                if (strain.StrainDecreaseCooldown <= Mathf.Epsilon)
                {
                    ReduceStrain(ref strain);
                }
                else
                {
                    strain.StrainDecreaseCooldown -= delta;
                    ReduceStrain(ref strain, Constants.PASSIVE_STRAIN_DECREASE_PRE_COOLDOWN_DIVISOR);
                }
            }
        }

        private void ReduceStrain(ref StrainAffected strain, float divisor = 1.0f)
        {
            if (strain.ExcessStrain <= Mathf.Epsilon)
            {
                strain.CurrentStrain -= Constants.PASSIVE_STRAIN_DECREASE_PER_UPDATE / divisor;
            }
            else
            {
                strain.ExcessStrain -= Constants.PASSIVE_STRAIN_DECREASE_PER_UPDATE / divisor;
            }

            if (strain.CurrentStrain < 0)
                strain.CurrentStrain = 0;
        }
    }
}
