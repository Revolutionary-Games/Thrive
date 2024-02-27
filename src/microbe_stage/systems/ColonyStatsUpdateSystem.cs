namespace Systems
{
    using Components;
    using DefaultEcs;
    using DefaultEcs.System;
    using DefaultEcs.Threading;

    /// <summary>
    ///   Handles updating the statistics values (and applying the ones that apply to other components, for example
    ///   entity weight) for microbe colonies
    /// </summary>
    [With(typeof(MicrobeColony))]
    [WritesToComponent(typeof(Spawned))]
    [ReadsComponent(typeof(MicrobeColonyMember))]
    [RunsAfter(typeof(SpawnSystem))]
    [RunsAfter(typeof(MulticellularGrowthSystem))]
    [RuntimeCost(0.5f, false)]
    public sealed class ColonyStatsUpdateSystem : AEntitySetSystem<float>
    {
        private readonly IWorldSimulation worldSimulation;

        public ColonyStatsUpdateSystem(IWorldSimulation worldSimulation, World world, IParallelRunner parallelRunner) :
            base(world, parallelRunner)
        {
            this.worldSimulation = worldSimulation;
        }

        /// <summary>
        ///   Destroys colonies or colony membership information on deleted entities
        /// </summary>
        public void OnEntityDestroyed(in Entity entity)
        {
            if (entity.Has<MicrobeColony>())
            {
                // Disbanding a colony. As despawn system can despawn the colony leaders, this simply just destroys all
                // the other entities in the colony
                ref var colony = ref entity.Get<MicrobeColony>();

                foreach (var colonyMember in colony.ColonyMembers)
                {
                    if (colonyMember != entity && colonyMember.IsAlive)
                        worldSimulation.DestroyEntity(colonyMember);
                }

                return;
            }

            if (!entity.Has<MicrobeColonyMember>())
                return;

            // Handle removing a member's data from a colony
            ref var memberInfo = ref entity.Get<MicrobeColonyMember>();

            if (!memberInfo.ColonyLeader.Has<MicrobeColony>())
            {
                // This entity is part of a destroyed colony, which is fine as the entity is being deleted
                return;
            }

            ref var parentColony = ref memberInfo.ColonyLeader.Get<MicrobeColony>();

            var recorder = worldSimulation.StartRecordingEntityCommands();

            lock (AttachedToEntityHelpers.EntityAttachRelationshipModifyLock)
            {
                parentColony.RemoveFromColony(memberInfo.ColonyLeader, entity, recorder);
            }

            // As this is called by the destroy callback, the world can't be doing anything else so we can safely
            // apply the operations immediately
            recorder.Execute();

            worldSimulation.FinishRecordingEntityCommands(recorder);
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var colony = ref entity.Get<MicrobeColony>();

            colony.CanEngulf();

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
