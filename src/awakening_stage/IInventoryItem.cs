using System;
using Godot;

public interface IInventoryItem
{
    public Texture Icon { get; }

    /// <summary>
    ///   Required when moving from a transient slot (the crafting screen) to somewhere else to know where the item
    ///   is from originally.
    /// </summary>
    public WeakReference<InventorySlot>? ShownAsGhostIn { get; set; }
}
