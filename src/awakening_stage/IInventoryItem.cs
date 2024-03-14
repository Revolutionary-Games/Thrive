using System;
using Godot;

public interface IInventoryItem
{
    public Texture2D Icon { get; }

    /// <summary>
    ///   Required when moving from a transient slot (the crafting screen) to somewhere else to know where the item
    ///   is from originally.
    /// </summary>
    public WeakReference<InventorySlot>? ShownAsGhostIn { get; set; }
}

public static class InventoryItemHelpers
{
    public static WorldResource? ResourceFromItem(this IInventoryItem item)
    {
        if (item is ResourceEntity resourceEntity)
        {
            return resourceEntity.ResourceType ?? throw new Exception("World resource with no type set");
        }

        return null;
    }
}
