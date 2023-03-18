using Godot;

/// <summary>
///   Data for drag and drop of items. Has to be a Godot reference to work
/// </summary>
public class InventoryDragData : Reference
{
    public InventoryDragData(InventorySlot fromSlot, IInventoryItem item)
    {
        FromSlot = fromSlot;
        Item = item;
    }

    public InventorySlot FromSlot { get; }
    public IInventoryItem Item { get; }
}
