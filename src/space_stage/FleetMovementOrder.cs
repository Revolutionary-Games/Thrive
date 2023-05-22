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
        var unitPosition = Unit.GlobalTranslation;
        var toTarget = TargetPosition - unitPosition;

        var distanceToTarget = toTarget.Length();

        var adjustedSpeed = delta * Unit.Speed;

        if (distanceToTarget < adjustedSpeed)
        {
            Unit.GlobalTranslation = TargetPosition;
            return true;
        }

        var direction = toTarget / distanceToTarget;
        Unit.GlobalTranslation += direction * adjustedSpeed;
        return false;
    }
}
