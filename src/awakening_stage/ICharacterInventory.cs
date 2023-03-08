using System.Collections.Generic;
using System.Linq;

public interface ICharacterInventory
{
    public bool PickUpItem(IInteractableEntity item, int slotId);
    public bool DropItem(IInteractableEntity item);

    public IEnumerable<InventorySlotData> ListInventoryContents();

    public IEnumerable<InventorySlotData> ListHandContents();
}

public static class CharacterInventoryHelpers
{
    public static IEnumerable<InventorySlotData> ListAllItems(this ICharacterInventory inventory)
    {
        foreach (var content in inventory.ListHandContents())
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
}
