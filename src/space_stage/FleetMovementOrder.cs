using Godot;
using Newtonsoft.Json;

/// <summary>
///   Orders a space fleet to move to the given position
/// </summary>
public class FleetMovementOrder : UnitOrderBase<SpaceFleet>
{
    [JsonConstructor]
    public FleetMovementOrder(SpaceFleet fleet, Vector3 targetPosition) : base(fleet)
    {
        TargetPosition = targetPosition;
    }

    [JsonProperty]
    public Vector3 TargetPosition { get; }

    protected override bool WorkOnOrder(float delta)
    {
        var unitPosition = Unit.GlobalPosition;

        var toTarget = TargetPosition - unitPosition;

        var distanceToTarget = toTarget.Length();

        var adjustedSpeed = delta * Unit.Speed;

        // The normalized vector from the ship to the target point
        var direction = toTarget / distanceToTarget;

        var currentRotation = new Quaternion(new Vector3(0, 0, 1), Unit.Transform.Basis.Z.Normalized()).Normalized();
        var targetRotation = new Quaternion(new Vector3(0, 0, 1), toTarget.Normalized()).Normalized();

        if (direction.Dot(Unit.Transform.Basis.Z) < 0.99f)
        {
            var smoothRotation = currentRotation.Slerp(targetRotation, 0.8f * delta);
            Unit.Transform = new Transform3D(new Basis(smoothRotation), unitPosition);
            return false;
        }
        else
        {
            var smoothRotation = currentRotation.Slerp(targetRotation, 0.1f * delta);
            Unit.Transform = new Transform3D(new Basis(smoothRotation), unitPosition);

            if (distanceToTarget < adjustedSpeed)
            {
                Unit.GlobalPosition = TargetPosition;

                // Unit._engineEmittor.Emitting = false;
                return true;
            }

            // Unit._engineEmittor.Emitting = true;

            Unit.GlobalPosition += direction * adjustedSpeed;
            return false;
        }
    }
}
