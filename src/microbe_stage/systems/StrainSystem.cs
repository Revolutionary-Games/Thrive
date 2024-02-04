namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    [With(typeof(CompoundStorage))]
    [With(typeof(MicrobeControl))]
    [RunsAfter(typeof(PhysicsBodyDisablingSystem))]
    public sealed class StrainSystem : AEntitySetSystem<float>
    {
        private readonly Compound atp;

        public StrainSystem(World world, IParallelRunner runner) : base(world, runner)
        {
            atp = SimulationParameters.Instance.GetCompound("atp");
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var control = ref entity.Get<MicrobeControl>();

            if (control.Sprinting && control.MovementDirection != Vector3.Zero)
            {
                control.CurrentStrain += Constants.SPRINTING_STRAIN_INCREASE_PER_UPDATE;

                if (control.CurrentStrain > Constants.MAX_STRAIN_PER_CELL)
                    control.CurrentStrain = Constants.MAX_STRAIN_PER_CELL;

                control.StrainDecreaseCooldown = Constants.STRAIN_DECREASE_COOLDOWN_SECONDS;
            }
            else
            {
                if (control.StrainDecreaseCooldown <= Mathf.Epsilon)
                {
                    control.CurrentStrain -= Constants.PASSIVE_STRAIN_DECREASE_PER_UPDATE;
                }
                else
                {
                    control.StrainDecreaseCooldown -= delta;
                    control.CurrentStrain -= Constants.PASSIVE_STRAIN_DECREASE_PER_UPDATE / 3.0f;
                }

                if (control.CurrentStrain < 0)
                    control.CurrentStrain = 0;
            }

            // If the entity is not moving, anyway remove some ATP due to strain
            if (control.MovementDirection == Vector3.Zero)
            {
                var compounds = entity.Get<CompoundStorage>().Compounds;
                var strainFraction = control.CalculateStrainFraction();
                compounds.TakeCompound(atp, Constants.PASSIVE_STRAIN_TO_ATP_USAGE * strainFraction * Constants.STRAIN_TO_ATP_USAGE_COEFFICIENT * delta);
            }
        }
    }
}
