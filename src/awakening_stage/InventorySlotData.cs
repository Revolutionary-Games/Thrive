using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   The backing data for an item slot, <see cref="InventorySlot"/> is the GUI component for displaying a slot.
/// </summary>
/// <remarks>
///   <para>
///     TODO: check if making this into a struct makes sense, ref variables can be used to access raw arrays without
///     copies
///   </para>
/// </remarks>
public class InventorySlotData
{
    public readonly int Id;
    public EquipmentSlotType SlotType;

    /// <summary>
    ///   Specifies position for this if this is an equipment slot, used to draw at the right position on the
    ///   creature diagram. Coordinates are in the range [0, 1]
    /// </summary>
    public Vector2? EquipmentPosition;

    public IInteractableEntity? ContainedItem;

    /// <summary>
    ///   Initializes an empty slot
    /// </summary>
    /// <param name="id">The ID which must be unique within the containing creature</param>
    [JsonConstructor]
    public InventorySlotData(int id)
    {
        Id = id;
        ContainedItem = null;
        SlotType = EquipmentSlotType.None;
        EquipmentPosition = null;
    }

    public InventorySlotData(int id, EquipmentSlotType equipmentType, Vector2 guiPosition)
    {
        if (equipmentType == EquipmentSlotType.None)
            throw new ArgumentException("This constructor is for equipment slots");

        Id = id;
        ContainedItem = null;
        SlotType = equipmentType;
        EquipmentPosition = guiPosition;
    }
}
