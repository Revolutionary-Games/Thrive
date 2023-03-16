using System;
using System.Collections.Generic;
using System.Linq;

public interface ICharacterInventory
{
    /// <summary>
    ///   Pick up an item to the inventory.
    /// </summary>
    /// <param name="item">
    ///   The item to pick up. Note that the item may not be part of the world currently if this was just crafted.
    /// </param>
    /// <param name="slotId">Target slot ID (ids are found with the list methods) to put the item in</param>
    /// <returns>True on success</returns>
    public bool PickUpItem(IInteractableEntity item, int slotId);

    /// <summary>
    ///   Called to drop an item from this inventory
    /// </summary>
    /// <param name="item">The item to drop</param>
    /// <returns>True on success, false if the item doesn't exist or something else prevents dropping it</returns>
    public bool DropItem(IInteractableEntity item);

    /// <summary>
    ///   Consumes (i.e. destroys) an item in a slot
    /// </summary>
    /// <param name="slotId">The slot id of the item to consume</param>
    /// <returns>True on success, false if the slot was empty</returns>
    public bool DeleteItem(int slotId);

    /// <summary>
    ///   <see cref="DeleteItem"/> equivalent for a nearby world item to be consumed. This is needed to allow crafting
    ///   with items that are on the ground near the player.
    /// </summary>
    /// <param name="entity">The entity to consume</param>
    /// <returns>True on success, false otherwise</returns>
    public bool DeleteWorldEntity(IInteractableEntity entity);

    /// <summary>
    ///   Called when the character needs to drop an item that has not existed before. For example a crafting result
    ///   that didn't fit in inventory and must be dropped.
    /// </summary>
    /// <param name="entity">The entity to place in the world this character is in</param>
    public void DirectlyDropEntity(IInteractableEntity entity);

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
