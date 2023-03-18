using System.Collections.Generic;

public interface IHarvestAction
{
    /// <summary>
    ///   Checks whether this entity can be harvested with the available tools
    /// </summary>
    /// <param name="availableTools">
    ///   Tools the potential harvester has. Note that this is an <c>ICollection</c> so that <c>Contains</c>
    ///   can be called.
    /// </param>
    /// <returns>Null if can harvest, if not null then the first missing tool</returns>
    public EquipmentCategory? CheckRequiredTool(ICollection<EquipmentCategory> availableTools);

    public List<IInteractableEntity> PerformHarvest();
}
