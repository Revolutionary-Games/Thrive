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
    ///   Handles applying <see cref="MicrobeControl"/> to a microbe
    /// </summary>
    [With(typeof(MicrobeControl))]
    [With(typeof(OrganelleContainer))]
    [With(typeof(CellProperties))]
    [With(typeof(CompoundStorage))]
    [With(typeof(Physics))]
    [With(typeof(WorldPosition))]
    [With(typeof(Health))]
    [ReadsComponent(typeof(AttachedToEntity))]
    [ReadsComponent(typeof(MicrobeColony))]
    [ReadsComponent(typeof(Health))]
    public sealed class MicrobeMovementSystem : AEntitySetSystem<float>
    {
        private readonly PhysicalWorld physicalWorld;
        private readonly Compound atp;

        public MicrobeMovementSystem(PhysicalWorld physicalWorld, World world, IParallelRunner runner) : base(world,
            runner, Constants.SYSTEM_HIGHER_ENTITIES_PER_THREAD)
        {
            this.physicalWorld = physicalWorld;

            atp = SimulationParameters.Instance.GetCompound("atp");
        }

        protected override void Update(float delta, in Entity entity)
        {
            ref var physics = ref entity.Get<Physics>();

            if (physics.BodyDisabled || physics.Body == null)
                return;

            // Skip dead microbes being allowed to move, this is now needed as the death system keeps the physics body
            // alive so velocity still moves microbes for a bit even after death
            if (entity.Get<Health>().Dead)
            {
                // Disable control to not have the dead microbes maintain rotation or anything like that
                physicalWorld.DisableMicrobeBodyControl(physics.Body);
                return;
            }

            ref var organelles = ref entity.Get<OrganelleContainer>();
            ref var control = ref entity.Get<MicrobeControl>();

            // Position is used to calculate the look direction
            ref var position = ref entity.Get<WorldPosition>();

            var lookVector = control.LookAtPoint - position.Position;
            lookVector.y = 0;

            var length = lookVector.Length();

            if (length > MathUtils.EPSILON)
            {
                // Normalize vector when it has a length
                lookVector /= length;
            }
            else
            {
                // Without any difference with the look at point compared to the current position, default to looking
                // forward
                lookVector = Vector3.Forward;
            }

#if DEBUG
            if (!lookVector.IsNormalized())
                throw new Exception("Look vector not normalized");
#endif

            var up = Vector3.Up;

            // Math loaned from Godot.Transform.SetLookAt adapted to fit here and removed one extra operation
            // For some reason this results in an inverse quaternion, so for simplicity this is just flipped
            lookVector *= -1;
            var column0 = up.Cross(lookVector);
            var column1 = lookVector.Cross(column0);
            var wantedRotation = new Basis(column0.Normalized(), column1.Normalized(), lookVector).Quat();

#if DEBUG
            if (!wantedRotation.IsNormalized())
                throw new Exception("Created target microbe rotation is not normalized");
#endif

            var compounds = entity.Get<CompoundStorage>().Compounds;
            ref var cellProperties = ref entity.Get<CellProperties>();

            var rotationSpeed = CalculateRotationSpeed(entity, ref organelles);

            var movementImpulse =
                CalculateMovementForce(entity, ref control, ref cellProperties, ref position, ref organelles, compounds,
                    delta);

            physicalWorld.ApplyBodyMicrobeControl(physics.Body, movementImpulse, wantedRotation, rotationSpeed);
        }

        private static float CalculateRotationSpeed(in Entity entity, ref OrganelleContainer organelles)
        {
            float rotationSpeed = organelles.RotationSpeed;

            // Note that cilia taking ATP is actually calculated later, this is the max speed rotation calculation
            // only

            // Lower value is faster rotation
            if (CheatManager.Speed > 1 && entity.Has<PlayerMarker>())
                rotationSpeed /= CheatManager.Speed;

            if (entity.Has<MicrobeColony>())
            {
                rotationSpeed = Mathf.Clamp(rotationSpeed * entity.Get<MicrobeColony>().ColonyRotationMultiplier,
                    Math.Max(rotationSpeed * Constants.CELL_COLONY_MAX_ROTATION_HELP, Constants.CELL_MIN_ROTATION),
                    Constants.CELL_MAX_ROTATION);
            }

            return rotationSpeed;
        }

        private Vector3 CalculateMovementForce(in Entity entity, ref MicrobeControl control,
            ref CellProperties cellProperties, ref WorldPosition position,
            ref OrganelleContainer organelles, CompoundBag compounds, float delta)
        {
            if (control.MovementDirection == Vector3.Zero)
            {
                // Slime jets work even when not holding down any movement keys
                var jetMovement = CalculateMovementFromSlimeJets(ref organelles);

                if (jetMovement == Vector3.Zero)
                    return Vector3.Zero;

                return position.Rotation.Xform(jetMovement);
            }

            // Ensure no cells attempt to move on the y-axis
            control.MovementDirection.y = 0;

            // Normalize if length is over 1 to not allow diagonal movement to be very fast
            var length = control.MovementDirection.Length();

            // Movement direction should not be normalized *always* to allow different speeds
            if (length > 1)
            {
                control.MovementDirection /= length;
                length = 1;
            }

            // Base movement force
            float force = MicrobeInternalCalculations.CalculateBaseMovement(cellProperties.MembraneType,
                cellProperties.MembraneRigidity, organelles.HexCount);

            // Length is multiplied here so that cells that set very slow movement speed don't need to pay the entire
            // movement cost
            var cost = Constants.BASE_MOVEMENT_ATP_COST * organelles.HexCount * length * delta;

            var got = compounds.TakeCompound(atp, cost);

            // Halve base movement speed if out of ATP
            if (got < cost)
            {
                // Not enough ATP to move at full speed
                force *= 0.5f;
            }

            // Speed from flagella (these also take ATP otherwise they won't work)
            if (organelles.ThrustComponents != null && control.MovementDirection != Vector3.Zero)
            {
                foreach (var flagellum in organelles.ThrustComponents)
                {
                    force += flagellum.UseForMovement(control.MovementDirection, compounds, Quat.Identity, delta);
                }
            }

            if (control.MovementDirection != Vector3.Zero && entity.Has<MicrobeColony>())
            {
                CalculateColonyImpactOnMovementForce(ref entity.Get<MicrobeColony>(), control.MovementDirection, delta,
                    ref force);
            }

            if (control.SlowedBySlime)
                force /= Constants.MUCILAGE_IMPEDE_FACTOR;

            // Movement modifier from engulf (this used to be handled in the engulfing code, now it's here)
            if (control.State == MicrobeState.Engulf)
                force *= Constants.ENGULFING_MOVEMENT_MULTIPLIER;

            force *= cellProperties.MembraneType.MovementFactor -
                (cellProperties.MembraneRigidity * Constants.MEMBRANE_RIGIDITY_BASE_MOBILITY_MODIFIER);

            if (CheatManager.Speed > 1 && entity.Has<PlayerMarker>())
            {
                float mass = 1000;

                if (entity.Has<PhysicsShapeHolder>())
                {
                    entity.Get<PhysicsShapeHolder>().TryGetShapeMass(out mass);
                }

                // There's an additional divisor here to make the speed cheat reasonable
                force *= mass / 1000.0f * CheatManager.Speed / 4;
            }

            var movementVector = control.MovementDirection * force;

            // Speed from jets (these are related to a non-rotated state of the cell so this is done before rotating
            // by the transform)
            movementVector += CalculateMovementFromSlimeJets(ref organelles);

            // MovementDirection is proportional to the current cell rotation, so we need to rotate the movement
            // vector to work correctly
            return position.Rotation.Xform(movementVector);
        }

        private Vector3 CalculateMovementFromSlimeJets(ref OrganelleContainer organelles)
        {
            var movementVector = Vector3.Zero;

            if (organelles.SlimeJets is { Count: > 0 })
            {
                foreach (var jet in organelles.SlimeJets)
                {
                    if (!jet.Active)
                        continue;

                    // It might be better to consume the queued force always but, this probably results at most in just
                    // one extra frame of thrust whenever the jets are engaged
                    jet.ConsumeMovementForce(out var jetForce);
                    movementVector += jetForce;
                }
            }

            return movementVector;
        }

        private void CalculateColonyImpactOnMovementForce(ref MicrobeColony microbeColony, Vector3 movementDirection,
            float delta, ref float force)
        {
            // Multiplies the movement factor as if the colony has the normal microbe speed
            // Then it subtracts movement speed from 100% up to 75%(soft cap),
            // using a series that converges to 1 , value = (1/2 + 1/4 + 1/8 +.....) = 1 - 1/2^n
            // when specialized cells become a reality the cap could be lowered to encourage cell specialization
            // int memberCount;
            force *= microbeColony.ColonyMembers.Length;
            var seriesValue = 1 - 1 / (float)Math.Pow(2, microbeColony.ColonyMembers.Length - 1);
            force -= (force * 0.15f) * seriesValue;

            // Colony members have their movement update before organelle update, so that the movement organelles
            // see the direction
            // The colony master should be already updated as the movement direction is either set by the
            // player input or microbe AI, neither of which will happen concurrently, so this should always get the
            // up to date value

            foreach (var colonyMember in microbeColony.ColonyMembers)
            {
                // Colony leader processes the normal movement logic so it isn't taken into account here
                if (colonyMember == microbeColony.Leader)
                    continue;

                // Flagella in colony members
                ref var organelles = ref colonyMember.Get<OrganelleContainer>();

                if (organelles.ThrustComponents != null)
                {
                    var compounds = colonyMember.Get<CompoundStorage>().Compounds;
                    var relativeRotation = colonyMember.Get<AttachedToEntity>().RelativeRotation;

                    foreach (var flagellum in organelles.ThrustComponents)
                    {
                        force += flagellum.UseForMovement(movementDirection, compounds,
                            relativeRotation, delta);
                    }
                }
            }
        }
    }
}
