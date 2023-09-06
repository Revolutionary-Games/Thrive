namespace Systems
{
    using System;
    using Components;
    using DefaultEcs;
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
    [Without(typeof(AttachedToEntity))]
    [RunsBefore(typeof(MicrobeFlashingSystem))]
    public sealed class ColonyBindingSystem : AEntitySetSystem<float>
    {
        private readonly Compound atp;

        public ColonyBindingSystem(World world, IParallelRunner parallelRunner) : base(world, parallelRunner)
        {
            atp = SimulationParameters.Instance.GetCompound("atp");
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var control = ref entity.Get<MicrobeControl>();

            if (control.State == MicrobeState.Unbinding)
            {
                throw new NotImplementedException();
            }
            else if (control.State == MicrobeState.Binding)
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

            if (count > 0)
            {
                ref var ourSpecies = ref entity.Get<MicrobeSpeciesMember>();

                for (int i = 0; i < count; ++i)
                {
                    ref var collision = ref collisions![i];

                    // Can bind with only members of our own species
                    if (!collision.SecondEntity.Has<MicrobeSpeciesMember>())
                        continue;

                    if (ourSpecies.Species != collision.SecondEntity.Get<MicrobeSpeciesMember>().Species)
                        continue;

                    // Can't bind with an existing colony or otherwise attached entity
                    if (collision.SecondEntity.Has<MicrobeColony>() || collision.SecondEntity.Has<AttachedToEntity>())
                        continue;

                    ref var otherHealth = ref collision.SecondEntity.Get<Health>();

                    // Can't bind with dead things
                    if (otherHealth.Dead)
                        continue;

                    // Binding can proceed
                    throw new NotImplementedException();
                }
            }
        }

        /// <summary>
        ///   This method calculates the relative rotation and translation this microbe should have to its microbe parent.
        ///   <a href="https://randomthrivefiles.b-cdn.net/documentation/fixed_colony_rotation_explanation_image.png">
        ///     Visual explanation
        ///   </a>
        /// </summary>
        /// <returns>Returns relative translation and rotation</returns>
        private (Vector3 Translation, Vector3 Rotation) GetNewRelativeTransform(ref WorldPosition colonyParentPosition,
            ref CellProperties colonyParentProperties, ref WorldPosition cellPosition,
            ref CellProperties cellProperties)
        {
            // TODO: the result of this method needs to have the relative transform of the colony parent (if it isn't
            // the colony leader) applied on top to get the total position

            if (colonyParentProperties.CreatedMembrane == null)
                throw new InvalidOperationException("Colony parent cell has no membrane set");

            if (cellProperties.CreatedMembrane == null)
                throw new InvalidOperationException("Cell to add to colony has no membrane set");

            // Gets the global rotation of the parent
            var globalParentRotation = colonyParentPosition.Rotation.GetEuler();

            // A vector from the parent to me
            var vectorFromParent = cellPosition.Position - colonyParentPosition.Position;

            // A vector from me to the parent
            var vectorToParent = -vectorFromParent;

            // TODO: using quaternions here instead of assuming that rotating about the up/down axis is right would be nice
            // This vector represents the vectorToParent as if I had no rotation.
            // This works by rotating vectorToParent by the negative value (therefore Down) of my current rotation
            // This is important, because GetVectorTowardsNearestPointOfMembrane only works with non-rotated microbes
            var vectorToParentWithoutRotation =
                vectorToParent.Rotated(Vector3.Down, cellPosition.Rotation.GetEuler().y);

            // This vector represents the vectorFromParent as if the parent had no rotation.
            var vectorFromParentWithoutRotation = vectorFromParent.Rotated(Vector3.Down, globalParentRotation.y);

            // Calculates the vector from the center of the parent's membrane towards me with canceled out rotation.
            // This gets added to the vector calculated one call before.
            var correctedVectorFromParent = colonyParentProperties.CreatedMembrane
                .GetVectorTowardsNearestPointOfMembrane(vectorFromParentWithoutRotation.x,
                    vectorFromParentWithoutRotation.z).Rotated(Vector3.Up, globalParentRotation.y);

            // Calculates the vector from my center to my membrane towards the parent.
            // This vector gets rotated back to cancel out the rotation applied two calls above.
            // -= to negate the vector, so that the two membrane vectors amplify
            correctedVectorFromParent -= cellProperties.CreatedMembrane
                .GetVectorTowardsNearestPointOfMembrane(vectorToParentWithoutRotation.x,
                    vectorToParentWithoutRotation.z)
                .Rotated(Vector3.Up, cellPosition.Rotation.GetEuler().y);

            // Rotated because the rotational scope is different.
            var newTranslation = correctedVectorFromParent.Rotated(Vector3.Down, globalParentRotation.y);

            return (newTranslation, cellPosition.Rotation.GetEuler() - globalParentRotation);
        }
    }
}
