using Godot;
using Newtonsoft.Json;

/// <summary>
///   Orders a space flee to move to the given position
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

        // TODO: adjust the fleet rotation towards the travel direction

        if (distanceToTarget < adjustedSpeed)
        {
            Unit.GlobalPosition = TargetPosition;
            return true;
        }

        var direction = toTarget / distanceToTarget;
        Unit.GlobalPosition += direction * adjustedSpeed;
        return false;
    }
}
