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

        var currentRotation = Unit.GlobalTransform.Basis.GetRotationQuaternion();
        var targetRotation = new Quaternion(new Vector3(0, 0, 1), toTarget.Normalized()).Normalized();

        if (currentRotation.AngleTo(targetRotation) >= 0.22f)
        {
            var smoothRotation = currentRotation.Slerp(targetRotation, 0.8f * delta);
            Unit.GlobalTransform = new Transform3D(new Basis(smoothRotation), unitPosition);

            // The particles kind of look bad when turning in place so we don't set the moving property here yet
            // Unit.Moving = true;
            return false;
        }
        else
        {
            var smoothRotation = currentRotation.Slerp(targetRotation, 0.5f * delta);

            bool finished = false;

            if (distanceToTarget < adjustedSpeed)
            {
                unitPosition = TargetPosition;
                finished = true;
                Unit.Moving = false;
            }
            else
            {
                unitPosition += direction * adjustedSpeed;
                Unit.Moving = true;
            }

            Unit.GlobalTransform = new Transform3D(new Basis(smoothRotation), unitPosition);
            return finished;
        }
    }
}
