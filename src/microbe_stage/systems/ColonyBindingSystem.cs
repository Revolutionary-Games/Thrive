namespace Systems
{
    using System;
    using Components;
    using DefaultEcs;
    using DefaultEcs.Command;
    using DefaultEcs.System;
    using DefaultEcs.Threading;
    using Godot;
    using World = DefaultEcs.World;

    /// <summary>
    ///   Handles microbe binding mode for creating microbe colonies
    /// </summary>
    [With(typeof(MicrobeControl))]
    [With(typeof(CollisionManagement))]
    [With(typeof(MicrobeSpeciesMember))]
    [With(typeof(Health))]
    [With(typeof(SoundEffectPlayer))]
    [With(typeof(CompoundStorage))]
    [With(typeof(OrganelleContainer))]
    [With(typeof(MicrobePhysicsExtraData))]
    [With(typeof(CellProperties))]
    [With(typeof(WorldPosition))]
    [Without(typeof(AttachedToEntity))]
    [RunsBefore(typeof(MicrobeFlashingSystem))]
    [RunsAfter(typeof(MicrobeMovementSystem))]
    [ReadsComponent(typeof(WorldPosition))]
    [WritesToComponent(typeof(Spawned))]
    public sealed class ColonyBindingSystem : AEntitySetSystem<float>
    {
        private readonly IWorldSimulation worldSimulation;
        private readonly Compound atp;

        public ColonyBindingSystem(IWorldSimulation worldSimulation, World world, IParallelRunner parallelRunner) :
            base(world, parallelRunner)
        {
            this.worldSimulation = worldSimulation;
            atp = SimulationParameters.Instance.GetCompound("atp");
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var control = ref entity.Get<MicrobeControl>();

            if (control.State == MicrobeState.Unbinding)
            {
                throw new NotImplementedException();
            }

            /*TODO: else */
            if (control.State == MicrobeState.Binding)
            {
                HandleBindingMode(ref control, entity, delta);
            }
        }

        private void HandleBindingMode(ref MicrobeControl control, in Entity entity, float delta)
        {
            ref var health = ref entity.Get<Health>();

            // Disallow binding to happen when dead
            if (health.Dead)
                return;

            ref var organelles = ref entity.Get<OrganelleContainer>();
            ref var ourSpecies = ref entity.Get<MicrobeSpeciesMember>();

            if (!organelles.CanBind(ourSpecies.Species))
            {
                // Force exit binding mode if a cell that cannot bind has entered binding mode
                control.State = MicrobeState.Normal;
                return;
            }

            // Drain atp
            var cost = Constants.BINDING_ATP_COST_PER_SECOND * delta;

            var compounds = entity.Get<CompoundStorage>().Compounds;

            if (compounds.TakeCompound(atp, cost) < cost - 0.001f)
            {
                control.State = MicrobeState.Normal;
                return;
            }

            ref var soundPlayer = ref entity.Get<SoundEffectPlayer>();

            // To simplify the logic this audio is now played non-looping
            // TODO: if this sounds too bad with the sound volume no longer fading then this will need to change
            soundPlayer.PlaySoundEffectIfNotPlayingAlready(Constants.MICROBE_BINDING_MODE_SOUND, 0.6f);

            var count = entity.Get<CollisionManagement>().GetActiveCollisions(out var collisions);

            if (count <= 0)
                return;

            // Can't bind when membrane is not ready (note this doesn't manage to check colony members so this isn't
            // an exact check meaning the actual bind method can still fail later even if this check passes)
            ref var cellProperties = ref entity.Get<CellProperties>();

            // TODO: should this require an up to date membrane data?
            if (cellProperties.CreatedMembrane == null /*|| cellProperties.CreatedMembrane.IsChangingShape*/)
                return;

            ref var extraPhysicsData = ref entity.Get<MicrobePhysicsExtraData>();

            for (int i = 0; i < count; ++i)
            {
                ref var collision = ref collisions![i];

                if (!organelles.CanBindWith(ourSpecies.Species, collision.SecondEntity))
                    continue;

                // TODO: to ensure no engulf can start on the same frame as a bind, maybe we need a cache of touched
                // entities in AttachedToEntityHelpers that gets cleared each world update?
                // Can't bind with an attached entity (engulfed entity for example)
                // The above check already checks against binding to something that is in a colony
                if (collision.SecondEntity.Has<AttachedToEntity>())
                    continue;

                // Second entity is not a full microbe (this shouldn't happen but for safety this check is here)
                if (!collision.SecondEntity.Has<MicrobePhysicsExtraData>())
                    continue;

                // Skip if trying to bind through a pilus
                if (extraPhysicsData.IsSubShapePilus(collision.FirstSubShapeData))
                    continue;

                if (collision.SecondEntity.Get<MicrobePhysicsExtraData>()
                    .IsSubShapePilus(collision.SecondSubShapeData))
                {
                    continue;
                }

                if (!extraPhysicsData.MicrobeIndexFromSubShape(collision.FirstSubShapeData,
                        out var indexOfMemberToBindTo))
                {
                    GD.PrintErr("Couldn't get colony member index to bind to");
                    continue;
                }

                // Lock here to try to guarantee no entity is going to get attached to multiple colonies at the same
                // time
                lock (AttachedToEntityHelpers.EntityAttachRelationshipModifyLock)
                {
                    // Binding can proceed
                    if (BeginBind(ref control, entity, indexOfMemberToBindTo, collision.SecondEntity))
                    {
                        // Try to bind at most once per frame
                        break;
                    }
                }
            }
        }

        private bool BeginBind(ref MicrobeControl control, in Entity primaryEntity, int indexOfMemberToBindTo,
            in Entity other)
        {
            if (!other.IsAlive)
            {
                GD.PrintErr("Binding attempted on a dead entity");
                return false;
            }

            // A recorder is used to record the new components to ensure thread safety here
            var recorder = worldSimulation.StartRecordingEntityCommands();

            bool success;

            // Create a colony if there isn't one yet
            if (!primaryEntity.Has<MicrobeColony>())
            {
                if (!primaryEntity.Has<MicrobeColonyMember>())
                {
                    if (indexOfMemberToBindTo != 0)
                    {
                        // This should never happen as the colony is not yet created, the parent cell is by itself so
                        // the index should always be 0
                        GD.PrintErr("Initial colony creation doesn't have parent entity index in colony of 0");
                        indexOfMemberToBindTo = 0;
                    }

                    var colony = new MicrobeColony(primaryEntity, control.State, primaryEntity, other);

                    if (!colony.AddInitialColonyMember(primaryEntity, indexOfMemberToBindTo, other, recorder))
                    {
                        GD.PrintErr("Setting up data of initial colony member failed, canceling colony creation");
                        success = false;
                    }
                    else
                    {
                        // Add the colony component to the lead cell
                        recorder.Record(primaryEntity).Set(colony);

                        // Report not being able to reproduce by the lead cell
                        MicrobeColonyHelpers.ReportReproductionStatusOnAddToColony(primaryEntity);

                        success = true;
                    }
                }
                else
                {
                    // This shouldn't happen as colony members shouldn't be able to collide
                    GD.PrintErr("Entity that is part of another microbe colony can't become a colony leader");
                    success = false;
                }
            }
            else
            {
                ref var colony = ref primaryEntity.Get<MicrobeColony>();

                success = HandleAddToColony(ref colony, primaryEntity, indexOfMemberToBindTo, other, recorder);
            }

            if (!success)
            {
                GD.PrintErr("Failed to bind a new cell to a colony, rolling back entity commands");
                recorder.Clear();
                worldSimulation.FinishRecordingEntityCommands(recorder);
                return false;
            }

            // Move out of binding state before adding the colony member to avoid accidental collisions being able to
            // recursively trigger colony attachment
            control.State = MicrobeState.Normal;

            // Other cell control is set by MicrobeColonyHelpers.OnColonyMemberAdded

            worldSimulation.FinishRecordingEntityCommands(recorder);
            return true;
        }

        private bool HandleAddToColony(ref MicrobeColony colony, in Entity colonyEntity, int parentIndex,
            in Entity newCell, EntityCommandRecorder entityCommandRecorder)
        {
            return colony.AddToColony(colonyEntity, parentIndex, newCell, entityCommandRecorder);
        }
    }
}
