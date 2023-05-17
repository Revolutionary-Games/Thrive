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
        TargetStructure = targetStructure;
    }

    [JsonProperty]
    public PlacedSpaceStructure TargetStructure { get; }

    protected override bool WorkOnOrder(float delta)
    {
        if (!resourcesDeposited)
        {
            // TODO: maybe gradually take resources?
            if (TargetStructure.DepositBulkResources(availableResources))
                resourcesDeposited = true;

            return false;
        }

        // TODO: make the construction take time / animate it
        TargetStructure.OnFinishConstruction();

        return true;
    }
}
