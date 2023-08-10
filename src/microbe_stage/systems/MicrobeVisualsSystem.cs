namespace Systems
{
    using System;
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;

    /// <summary>
    ///   Generates the visuals needed for microbes
    /// </summary>
    [With(typeof(MicrobeSpeciesMember))]
    public sealed class MicrobeVisualsSystem : AEntitySetSystem<float>
    {
        public MicrobeVisualsSystem(World world, IParallelRunner runner) : base(world, runner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var speciesMember = ref entity.Get<MicrobeSpeciesMember>();

            // TODO: membrane and other visuals generation
            // var membrane = new Membrane();

            throw new NotImplementedException();
        }
    }
}
