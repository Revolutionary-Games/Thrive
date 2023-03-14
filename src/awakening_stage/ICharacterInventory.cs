using System;
using System.Collections.Generic;
using System.Linq;

public interface ICharacterInventory
{
    public bool PickUpItem(IInteractableEntity item, int slotId);
    public bool DropItem(IInteractableEntity item);

    public IEnumerable<InventorySlotData> ListInventoryContents();

    public IEnumerable<InventorySlotData> ListEquipmentContents();

    /// <summary>
    ///   Checks whether moving item (or nothing) from slot to to slot is allowed (and if target has an item it is
    ///   swapped back to the original slot)
    /// </summary>
    /// <param name="fromSlotId">The id of the slot to move from</param>
    /// <param name="toSlotId">The id of the target slot</param>
    /// <returns>True when move is allowed</returns>
    public bool IsItemSlotMoveAllowed(int fromSlotId, int toSlotId);

    /// <summary>
    ///   Swaps the contents of item slots. <see cref="IsItemSlotMoveAllowed"/> should be called first to verify the
    ///   move is valid
    /// </summary>
    public void MoveItemSlots(int fromSlotId, int toSlotId);
}

public static class CharacterInventoryHelpers
{
    public static IEnumerable<InventorySlotData> ListAllItems(this ICharacterInventory inventory)
    {
        foreach (var content in inventory.ListEquipmentContents())
        {
            yield return content;
        }

        foreach (var content in inventory.ListInventoryContents())
        {
            yield return content;
        }
    }

    public static bool HasEmptySlot(this ICharacterInventory inventory)
    {
        return inventory.ListAllItems().Any(s => s.ContainedItem == null);
    }

    public static InventorySlotData? SlotWithItem(this ICharacterInventory inventory, IInteractableEntity item)
    {
        return inventory.ListAllItems().FirstOrDefault(s => s.ContainedItem == item);
    }

    public static InventorySlotData? SlotWithId(this ICharacterInventory inventory, int id)
    {
        if (id < 0)
            throw new ArgumentException("Invalid slot ID given", nameof(id));

        return inventory.ListAllItems().FirstOrDefault(s => s.Id == id);
    }
}
