namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using World = DefaultEcs.World;

    [With(typeof(MicrobeControl))]
    [RunsAfter(typeof(PhysicsBodyDisablingSystem))]
    public sealed class StrainSystem : AEntitySetSystem<float>
    {
        public StrainSystem(World world, IParallelRunner runner) : base(world, runner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var control = ref entity.Get<MicrobeControl>();

            if (control.Sprinting)
            {
                control.CurrentStrain += Constants.SPRINTING_STRAIN_INCREASE_PER_UPDATE;

                if (control.CurrentStrain > Constants.MAX_STRAIN_PER_CELL)
                    control.CurrentStrain = Constants.MAX_STRAIN_PER_CELL;
            }
            else
            {
                control.CurrentStrain -= Constants.PASSIVE_STRAIN_DECREASE_PER_UPDATE;

                if (control.CurrentStrain < 0)
                    control.CurrentStrain = 0;
            }
        }
    }
}