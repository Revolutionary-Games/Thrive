using System.Collections.Generic;

/// <summary>
///   Things that allow putting resources in them
/// </summary>
public interface IAcceptsResourceDeposit
{
    /// <summary>
    ///   Some deposit receivers will not always allow depositing
    /// </summary>
    public bool DepositActionAllowed { get; }

    public bool AutoTakesResources { get; }

    public IEnumerable<InventorySlotData>? GetWantedItems(IInventory availableItems);

    public void DepositItems(IEnumerable<IInventoryItem> items);
}
