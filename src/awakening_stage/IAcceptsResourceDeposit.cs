using System.Collections.Generic;

/// <summary>
///   Things that allow putting resources in them
/// </summary>
public interface IAcceptsResourceDeposit
{
    public bool AutoTakesResources { get; }

    public IEnumerable<InventorySlotData>? GetWantedItems(IInventory availableItems);

    public void DepositItems(IEnumerable<IInventoryItem> items);
}
