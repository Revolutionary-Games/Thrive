namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using MicrobeColony = Components.MicrobeColony;

    /// <summary>
    ///   Handles updating the statistics values (and applying the ones that apply to other components, for example
    ///   entity weight) for microbe colonies
    /// </summary>
    [With(typeof(MicrobeColony))]
    [WritesToComponent(typeof(Spawned))]
    [RunsAfter(typeof(SpawnSystem))]
    [RunsAfter(typeof(MulticellularGrowthSystem))]
    public sealed class ColonyStatsUpdateSystem : AEntitySetSystem<float>
    {
        public ColonyStatsUpdateSystem(World world, IParallelRunner parallelRunner) : base(world,
            parallelRunner)
        {
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var colony = ref entity.Get<MicrobeColony>();

            colony.CanEngulf();

            if (!colony.ColonyRotationMultiplierCalculated)
                colony.CalculateRotationMultiplier();

            if (!colony.EntityWeightApplied)
            {
                if (entity.Has<Spawned>())
                {
                    ref var spawned = ref entity.Get<Spawned>();

                    // Weight calculation may not be ready immediately so this can fail (in which case this is retried)
                    if (colony.CalculateEntityWeight(entity, out var weight))
                    {
                        spawned.EntityWeight = weight;
                        colony.EntityWeightApplied = true;
                    }
                }
                else
                {
                    colony.EntityWeightApplied = true;
                }
            }
        }
    }
}
