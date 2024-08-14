﻿namespace Systems;

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
/// <remarks>
///   <para>
///     The only write this does to <see cref="MicrobeControl"/> is ensuring the movement direction is normalized.
///   </para>
/// </remarks>
/// <remarks>
///   <para>
///     Once save compatibility is broken after 0.6.7 add with temporary effects amd strain affected
///   </para>
/// </remarks>
[With(typeof(MicrobeControl))]
[With(typeof(OrganelleContainer))]
[With(typeof(CellProperties))]
[With(typeof(CompoundStorage))]
[With(typeof(Physics))]
[With(typeof(WorldPosition))]
[With(typeof(Health))]

// [With(typeof(MicrobeTemporaryEffects))]
// [With(typeof(StrainAffected))]
[ReadsComponent(typeof(CellProperties))]
[ReadsComponent(typeof(WorldPosition))]
[ReadsComponent(typeof(AttachedToEntity))]
[ReadsComponent(typeof(MicrobeColony))]
[ReadsComponent(typeof(MicrobeTemporaryEffects))]
[WritesToComponent(typeof(StrainAffected))]
[RunsAfter(typeof(PhysicsBodyCreationSystem))]
[RunsAfter(typeof(PhysicsBodyDisablingSystem))]
[RunsBefore(typeof(PhysicsBodyControlSystem))]
[RuntimeCost(14)]
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

        if (!physics.IsBodyEffectivelyEnabled())
            return;

        // Skip dead microbes being allowed to move, this is now needed as the death system keeps the physics body
        // alive so velocity still moves microbes for a bit even after death
        if (entity.Get<Health>().Dead)
        {
            // Disable control to not have the dead microbes maintain rotation or anything like that
            physicalWorld.DisableMicrobeBodyControl(physics.Body!);
            return;
        }

        if (entity.Has<MicrobeColonyMember>())
        {
            GD.PrintErr("Colony members shouldn't run movement system");
            return;
        }

        ref var organelles = ref entity.Get<OrganelleContainer>();
        ref var control = ref entity.Get<MicrobeControl>();

        // Position is used to calculate the look direction
        ref var position = ref entity.Get<WorldPosition>();

        var lookVector = control.LookAtPoint - position.Position;
        lookVector.Y = 0;
        var lookVectorLength = lookVector.Length();

        var turnAngle = (position.Rotation * Vector3.Forward).SignedAngleTo(lookVector, Vector3.Up);
        var unsignedTurnAngle = Math.Abs(turnAngle);

        // Linear function reaching 1 at CELL_TURN_INFLECTION_RADIANS
        var blendFactor = Mathf.Pi / 4.0f * Mathf.Min(unsignedTurnAngle *
            (1.0f / Constants.CELL_TURN_INFLECTION_RADIANS), 1.0f);

        // Simplify turns to 90 degrees to keep consistent turning speed
        if (turnAngle > 0.0f)
        {
            lookVector = (position.Rotation
                * new Vector3(-Mathf.Sin(blendFactor), 0, -Mathf.Cos(blendFactor))).Normalized();
        }
        else if (turnAngle < 0.0f)
        {
            lookVector = (position.Rotation
                * new Vector3(Mathf.Sin(blendFactor), 0, -Mathf.Cos(blendFactor))).Normalized();
        }
        else if (lookVectorLength > MathUtils.EPSILON)
        {
            // Normalize vector when it has a length
            lookVector /= lookVectorLength;
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
        var wantedRotation =
            new Basis(column0.Normalized(), column1.Normalized(), lookVector).GetRotationQuaternion();

#if DEBUG
        if (!wantedRotation.IsNormalized())
            throw new Exception("Created target microbe rotation is not normalized");

        if (physics.Body!.IsDetached)
            throw new Exception("Trying to run microbe control on detached body");
#endif

        var compounds = entity.Get<CompoundStorage>().Compounds;
        ref var cellProperties = ref entity.Get<CellProperties>();

        var rotationSpeed = CalculateRotationSpeed(entity, ref organelles);

        var movementImpulse =
            CalculateMovementForce(entity, ref control, ref cellProperties, ref position, ref organelles, compounds,
                delta);

        if (control.State == MicrobeState.MucocystShield)
        {
            rotationSpeed /= Constants.MUCOCYST_SPEED_MULTIPLIER;
            movementImpulse *= Constants.MUCOCYST_SPEED_MULTIPLIER;
        }

        physicalWorld.ApplyBodyMicrobeControl(physics.Body!, movementImpulse, wantedRotation, rotationSpeed);
    }

    private static float CalculateRotationSpeed(in Entity entity, ref OrganelleContainer organelles)
    {
        float rotationSpeed = organelles.RotationSpeed;

        // Note that cilia taking ATP is actually calculated later, this is the max speed rotation calculation
        // only

        if (entity.Has<MicrobeColony>())
        {
            rotationSpeed = entity.Get<MicrobeColony>().ColonyRotationSpeed;
        }

        // Lower value is faster rotation
        if (CheatManager.Speed > 1 && entity.Has<PlayerMarker>())
            rotationSpeed /= CheatManager.Speed * 2;

        return rotationSpeed;
    }

    private Vector3 CalculateMovementForce(in Entity entity, ref MicrobeControl control,
        ref CellProperties cellProperties, ref WorldPosition position,
        ref OrganelleContainer organelles, CompoundBag compounds, float delta)
    {
        // TODO: switch to always reading strain affected once old save compatibility is removed
        // ref var strain = ref entity.Get<StrainAffected>();

        float strainMultiplier = 1;

        if (control.MovementDirection == Vector3.Zero)
        {
            if (entity.Has<StrainAffected>())
            {
                // TODO: move this variable up in the future
                ref var strain = ref entity.Get<StrainAffected>();
                strainMultiplier = GetStrainAtpMultiplier(ref strain);
                strain.IsUnderStrain = false;
            }

            // Remove ATP due to strain even if not moving
            // This is calculated similarly to the regular movement cost for consistency
            var strainCost = Constants.BASE_MOVEMENT_ATP_COST * organelles.HexCount * delta * strainMultiplier;
            compounds.TakeCompound(atp, strainCost);

            // Slime jets work even when not holding down any movement keys
            var jetMovement = CalculateMovementFromSlimeJets(ref organelles);

            if (jetMovement == Vector3.Zero)
                return Vector3.Zero;

            return position.Rotation * jetMovement;
        }

        // Ensure no cells attempt to move on the y-axis
        control.MovementDirection.Y = 0;

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
            cellProperties.MembraneRigidity, organelles.HexCount, cellProperties.IsBacteria);

        bool usesSprintingForce = false;

        if (entity.Has<StrainAffected>())
        {
            // TODO: move this variable up in the future
            ref var strain = ref entity.Get<StrainAffected>();
            strainMultiplier = GetStrainAtpMultiplier(ref strain);

            // TODO: move this if down in the future (once save compatibility is broken next time)
            if (control.Sprinting)
            {
                strain.IsUnderStrain = true;
                usesSprintingForce = true;
            }
            else
            {
                strain.IsUnderStrain = false;
            }
        }

        // Length is multiplied here so that cells that set very slow movement speed don't need to pay the entire
        // movement cost
        var cost = Constants.BASE_MOVEMENT_ATP_COST * organelles.HexCount * length * delta * strainMultiplier;

        var got = compounds.TakeCompound(atp, cost);

        // Halve base movement speed if out of ATP
        if (got < cost)
        {
            // Not enough ATP to move at full speed
            force *= 0.5f;

            // Force out of sprint if not enough ATP
            if (usesSprintingForce)
            {
                control.Sprinting = false;

                // Under strain will reset on the next update to false
            }
        }

        // TODO: this if check can be removed (can be assumed to be present) once save compatibility is next broken
        if (entity.Has<MicrobeTemporaryEffects>())
        {
            ref var temporaryEffects = ref entity.Get<MicrobeTemporaryEffects>();

            // Apply base movement debuff if cell is currently affected by one
            if (temporaryEffects.SpeedDebuffDuration > 0)
            {
                force *= 1 - Constants.MACROLIDE_BASE_MOVEMENT_DEBUFF;
            }
        }

        // Speed from flagella (these also take ATP otherwise they won't work)
        if (organelles.ThrustComponents != null && control.MovementDirection != Vector3.Zero)
        {
            foreach (var flagellum in organelles.ThrustComponents)
            {
                force += flagellum.UseForMovement(control.MovementDirection, compounds, Quaternion.Identity,
                    cellProperties.IsBacteria, delta);
            }
        }

        force *= cellProperties.MembraneType.MovementFactor -
            cellProperties.MembraneRigidity * Constants.MEMBRANE_RIGIDITY_BASE_MOBILITY_MODIFIER;

        if (usesSprintingForce)
        {
            force *= Constants.SPRINTING_FORCE_MULTIPLIER;

            // TODO: put this code back once strain is guaranteed
            // strain.IsUnderStrain = true;
        }

        /*else
        {
            strain.IsUnderStrain = false;
        }*/

        bool hasColony = entity.Has<MicrobeColony>();

        if (control.MovementDirection != Vector3.Zero && hasColony)
        {
            CalculateColonyImpactOnMovementForce(ref entity.Get<MicrobeColony>(), control.MovementDirection,
                cellProperties.IsBacteria, delta, ref force);
        }

        if (control.SlowedBySlime)
            force /= Constants.MUCILAGE_IMPEDE_FACTOR;

        // Movement modifier from engulf (this used to be handled in the engulfing code, now it's here)
        // TODO: should colony member engulf states be separately calculated for movement? Right now this makes it
        // very powerful to not have the primary cell type able to engulf but having other engulfing cells.
        if (control.State == MicrobeState.Engulf)
            force *= Constants.ENGULFING_MOVEMENT_MULTIPLIER;

        if (CheatManager.Speed > 1 && entity.Has<PlayerMarker>())
        {
            force *= CheatManager.Speed;
        }

        var movementVector = control.MovementDirection * force;

        // Speed from jets (these are related to a non-rotated state of the cell so this is done before rotating
        // by the transform)
        movementVector += CalculateMovementFromSlimeJets(ref organelles);

        // Handle colony jets
        if (hasColony)
        {
            // This is a duplicate fetch of this component, but this method would get pretty ugly / would need to
            // be split into many methods to allow sharing the variable
            ref var colony = ref entity.Get<MicrobeColony>();

            foreach (var colonyMember in colony.ColonyMembers)
            {
                // This doesn't really hurt as the slime jets were consumed above but for consistency with
                // basically all other places code like this is needed we skip the leader here
                if (colonyMember == entity)
                    continue;

                ref var memberOrganelles = ref colonyMember.Get<OrganelleContainer>();

                movementVector += CalculateMovementFromSlimeJets(ref memberOrganelles);
            }
        }

        // MovementDirection is proportional to the current cell rotation, so we need to rotate the movement
        // vector to work correctly
        return position.Rotation * movementVector;
    }

    private float GetStrainAtpMultiplier(ref StrainAffected strain)
    {
        var strainFraction = strain.CalculateStrainFraction();
        return strainFraction * Constants.STRAIN_TO_ATP_USAGE_COEFFICIENT + 1.0f;
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
        bool isBacteria, float delta, ref float force)
    {
        // Multiplies the movement factor as if the colony has the normal microbe speed
        // Then it subtracts movement speed from 100% up to 75%(soft cap),
        // using a series that converges to 1 , value = (1/2 + 1/4 + 1/8 +.....) = 1 - 1/2^n
        // when specialized cells become a reality the cap could be lowered to encourage cell specialization
        // Note that the multiplier below was added as a workaround for colonies being faster than individual cells
        // TODO: a proper rebalance of the algorithm would be excellent to do
        force *= microbeColony.ColonyMembers.Length * Constants.CELL_COLONY_MOVEMENT_FORCE_MULTIPLIER;
        var seriesValue = 1 - 1 / (float)Math.Pow(2, microbeColony.ColonyMembers.Length - 1);
        force -= force * 0.15f * seriesValue;

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
                        relativeRotation, isBacteria, delta) * Constants.CELL_COLONY_MOVEMENT_FORCE_MULTIPLIER;
                }
            }
        }
    }
}
