using Godot;
using Newtonsoft.Json;

/// <summary>
///   Space equivalent of <see cref="SocietyStructureSystem"/>
/// </summary>
public class SpaceStructureSystem
{
    private readonly Node worldRoot;

    [JsonProperty]
    private float elapsed = 1;

    public SpaceStructureSystem(Node root)
    {
        worldRoot = root;
    }

    public void Process(float delta, ISocietyStructureDataAccess societyData)
    {
        elapsed += delta;

        if (elapsed < Constants.SPACE_STAGE_STRUCTURE_PROCESS_INTERVAL)
            return;

        foreach (var structure in worldRoot.GetChildrenToProcess<PlacedSpaceStructure>(Constants
                     .SPACE_STRUCTURE_ENTITY_GROUP))
        {
            if (!structure.Completed)
            {
                continue;
            }

            structure.ProcessSpace(elapsed, societyData);
        }

        elapsed = 0;
    }

    /// <summary>
    ///   Immediately calculates derived values
    /// </summary>
    public void CalculateDerivedStats()
    {
        // TODO: something
        // foreach (var structure in worldRoot.GetChildrenToProcess<PlacedSpaceStructure>(Constants
        //              .SPACE_STRUCTURE_ENTITY_GROUP))
        // {
        //     if (!structure.Completed)
        //         continue;
        // }
    }
}
