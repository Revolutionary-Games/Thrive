using Godot;

/// <summary>
///   Data for drag and drop of items
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
