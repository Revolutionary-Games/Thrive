using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
///   Anything that can hold items
/// </summary>
public interface IInventory
{
    /// <summary>
    ///   Consumes (i.e. destroys) an item in a slot
    /// </summary>
    /// <param name="slotId">The slot id of the item to consume</param>
    /// <returns>True on success, false if the slot was empty</returns>
    public bool DeleteItem(int slotId);

    public IEnumerable<InventorySlotData> ListInventoryContents();

    public IEnumerable<InventorySlotData> ListEquipmentContents();
}

public static class InventoryHelpers
{
    public static IEnumerable<InventorySlotData> ListAllItems(this IInventory inventory)
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

    public static IEnumerable<Equipment> ListAllEquipment(this IInventory inventory)
    {
        foreach (var content in inventory.ListEquipmentContents())
        {
            if (content.ContainedItem is Equipment equipment)
                yield return equipment;
        }
    }

    public static Dictionary<IInventoryItem, int> CalculateAvailableItems(this IInventory inventory)
    {
        var result = new Dictionary<IInventoryItem, int>();

        foreach (var slot in inventory.ListAllItems())
        {
            var item = slot.ContainedItem;

            if (item == null)
                continue;

            result.TryGetValue(item, out var count);
            result[item] = count + 1;
        }

        return result;
    }

    public static Dictionary<WorldResource, int> CalculateAvailableResources(this IInventory inventory)
    {
        var result = new Dictionary<WorldResource, int>();

        foreach (var slot in inventory.ListAllItems())
        {
            var resource = slot.ContainedItem?.ResourceFromItem();

            if (resource != null)
            {
                result.TryGetValue(resource, out var count);
                result[resource] = count + 1;
            }
        }

        return result;
    }

    public static bool HasEmptySlot(this IInventory inventory)
    {
        return inventory.ListAllItems().Any(s => s.ContainedItem == null);
    }

    public static InventorySlotData? SlotWithItem(this IInventory inventory, IInteractableEntity item)
    {
        return inventory.ListAllItems().FirstOrDefault(s => s.ContainedItem == item);
    }

    public static InventorySlotData? SlotWithId(this IInventory inventory, int id)
    {
        if (id < 0)
            throw new ArgumentException("Invalid slot ID given", nameof(id));

        return inventory.ListAllItems().FirstOrDefault(s => s.Id == id);
    }

    public static HashSet<EquipmentCategory> GetAllCategoriesOfEquippedItems(this IInventory inventory)
    {
        var result = new HashSet<EquipmentCategory>();

        foreach (var equipment in inventory.ListAllEquipment())
        {
            result.Add(equipment.Definition.Category);
        }

        return result;
    }

    public static List<InventorySlotData>? FindRequiredResources(this IInventory inventory,
        IReadOnlyDictionary<WorldResource, int> requiredResources)
    {
        var availableItems = inventory.ListAllItems().ToList();

        var usedSlots = new List<InventorySlotData>();

        foreach (var requiredResource in requiredResources)
        {
            var amountLeft = requiredResource.Value;

            // Find items to satisfy the amount left
            while (amountLeft > 0)
            {
                bool foundSomething = false;

                foreach (var availableItem in availableItems)
                {
                    if (availableItem.ContainedItem == null)
                        continue;

                    var resource = availableItem.ContainedItem.ResourceFromItem();

                    if (resource != requiredResource.Key)
                        continue;

                    // Don't allow taking from the same slot twice
                    if (usedSlots.Contains(availableItem))
                        continue;

                    // Found an item to use
                    --amountLeft;
                    usedSlots.Add(availableItem);
                    foundSomething = true;

                    if (amountLeft <= 0)
                        break;
                }

                if (!foundSomething)
                {
                    // Not enough of this resource type
                    return null;
                }
            }
        }

        return usedSlots;
    }

    /// <summary>
    ///   Finds any resources that are available given in the required list, as compared to
    ///   <see cref="FindRequiredResources"/> which only finds things when every required item can be found
    /// </summary>
    /// <param name="inventory">Where to look items</param>
    /// <param name="requiredResources">The needed items</param>
    /// <returns>The found resources that were asked for by <see cref="requiredResources"/></returns>
    public static List<InventorySlotData>? FindAvailableResources(this IInventory inventory,
        Dictionary<WorldResource, int> requiredResources)
    {
        var availableItems = inventory.ListAllItems().ToList();

        var usedSlots = new List<InventorySlotData>();

        foreach (var requiredResource in requiredResources)
        {
            var amountLeft = requiredResource.Value;

            // Find items to satisfy the amount left
            while (amountLeft > 0)
            {
                bool foundSomething = false;

                foreach (var availableItem in availableItems)
                {
                    if (availableItem.ContainedItem == null)
                        continue;

                    var resource = availableItem.ContainedItem.ResourceFromItem();

                    if (resource != requiredResource.Key)
                        continue;

                    // Don't allow taking from the same slot twice
                    if (usedSlots.Contains(availableItem))
                        continue;

                    // Found an item to use
                    --amountLeft;
                    usedSlots.Add(availableItem);
                    foundSomething = true;

                    if (amountLeft <= 0)
                        break;
                }

                if (!foundSomething)
                {
                    // Not enough of this resource type
                    break;
                }
            }
        }

        if (usedSlots.Count < 1)
            return null;

        return usedSlots;
    }
}
