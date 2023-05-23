using Godot;
using Newtonsoft.Json;

/// <summary>
///   Orders a space flee to build a structure (doesn't check distance, separate move order needs to be created first)
/// </summary>
public class FleetBuildOrder : UnitOrderBase<SpaceFleet>
{
    [JsonProperty]
    private readonly IResourceContainer availableResources;

    [JsonProperty]
    private bool resourcesDeposited;

    [JsonConstructor]
    public FleetBuildOrder(SpaceFleet fleet, PlacedSpaceStructure targetStructure,
        IResourceContainer availableResources) : base(fleet)
    {
        this.availableResources = availableResources;
        TargetStructure = new EntityReference<PlacedSpaceStructure>(targetStructure);
    }

    [JsonProperty]
    public EntityReference<PlacedSpaceStructure> TargetStructure { get; }

    protected override bool WorkOnOrder(float delta)
    {
        var target = TargetStructure.Value;

        if (target == null)
        {
            GD.Print("Canceling build order as structure is gone");
            return true;
        }

        if (!resourcesDeposited)
        {
            // TODO: maybe gradually take resources?
            if (target.DepositBulkResources(availableResources))
                resourcesDeposited = true;

            return false;
        }

        // TODO: make the construction take time / animate it
        target.OnFinishConstruction();

        return true;
    }
}
