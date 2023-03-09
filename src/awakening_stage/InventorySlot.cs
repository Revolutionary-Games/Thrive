using System;
using Godot;

/// <summary>
///   GUI control showing an inventory slot that can be interacted with
/// </summary>
public class InventorySlot : Button
{
#pragma warning disable CA2213
    private Control tooHeavyIndicator = null!;
#pragma warning restore CA2213

    private IInventoryItem? item;

    private bool takeOnly;

    [Signal]
    public delegate void OnSelected();

    [Signal]
    public delegate void OnPressed();

    [Signal]
    public delegate void OnDragStarted();

    // Customization callbacks to override default behaviour for more control on item moving and knowing when it
    // happens
    public delegate bool AllowDrop(InventorySlot toSlot, InventoryDragData dragData);

    public delegate DragResult PerformDrop(InventorySlot toSlot, InventoryDragData dragData);

    public AllowDrop? AllowDropHandler { get; set; }
    public PerformDrop? PerformDropHandler { get; set; }

    public IInventoryItem? Item
    {
        get => item;
        set
        {
            if (item == value)
                return;

            item = value;

            if (item != null)
            {
                Icon = item.Icon;

                if (TakeOnly)
                    Locked = false;
            }
            else
            {
                Icon = null;

                if (TakeOnly)
                    Locked = true;
            }
        }
    }

    /// <summary>
    ///   If true this inventory slot can't be interacted with
    /// </summary>
    public bool Locked
    {
        get => Disabled;
        set
        {
            Disabled = value;
        }
    }

    public bool ShowTooHeavyIcon
    {
        get => tooHeavyIndicator.Visible;
        set
        {
            tooHeavyIndicator.Visible = value;
        }
    }

    /// <summary>
    ///   Only allow taking items from this
    /// </summary>
    public bool TakeOnly
    {
        get => takeOnly;
        set
        {
            takeOnly = value;

            // Lock when this is take only and empty to disallow putting anything here
            if (takeOnly && Item == null)
                Locked = true;
        }
    }

    /// <summary>
    ///   Slot ID metadata to tie this to the inventory slot IDs in the "backend" data
    /// </summary>
    public int SlotId { get; set; }

    public static Control CreateDragPreviewForItem(IInventoryItem item)
    {
        // TODO: when dragging to a different popup, this causes flickering, and in general this seems to flicker with
        // the cursor, so find a way around that or report a Godot bug
        return new TextureRect
        {
            Expand = true,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            Texture = item.Icon,
            RectMinSize = new Vector2(32, 32),
        };
    }

    public override void _Ready()
    {
        tooHeavyIndicator = GetNode<Control>("Overlays/TooHeavyToCarry");

        // TODO: tooltip for showing the name of the item / extra details if any exist
    }

    public override object? GetDragData(Vector2 position)
    {
        if (Item == null || Locked)
            return null;

        SetDragPreview(CreateDragPreviewForItem(Item));

        EmitSignal(nameof(OnDragStarted));

        return new InventoryDragData(this, Item);
    }

    public override bool CanDropData(Vector2 position, object data)
    {
        if (Locked)
            return false;

        if (data is not InventoryDragData inventoryDragData)
            return false;

        if (AllowDropHandler != null)
        {
            return AllowDropHandler.Invoke(this, inventoryDragData);
        }

        // Fall back to the default behaviour of just allowing
        return true;
    }

    public override void DropData(Vector2 position, object data)
    {
        if (data is not InventoryDragData inventoryDragData)
            throw new InvalidCastException($"Can't accept drop data of type: {data.GetType()}");

        DragResult result;
        if (PerformDropHandler != null)
        {
            result = PerformDropHandler.Invoke(this, inventoryDragData);
        }
        else
        {
            // Default handling
            result = DragResult.Success;

            if (Item != null)
                result = DragResult.Replaced;
        }

        if (result == DragResult.Failure)
            return;

        InventoryDragData? restartDragWith = null;

        if (result == DragResult.Replaced)
        {
            if (Item != null)
            {
                restartDragWith = new InventoryDragData(this, Item);
            }
            else
            {
                GD.PrintErr($"Can't replace drag without having an item in {nameof(InventorySlot)}");
            }
        }

        Item = inventoryDragData.Item;

        // Unset from the original slot
        if (inventoryDragData.FromSlot.Item == Item)
            inventoryDragData.FromSlot.Item = null;

        if (restartDragWith != null)
        {
            // TODO: how do we ensure the drag data is not lost if it cannot be dropped on anything?

            ForceDrag(restartDragWith, CreateDragPreviewForItem(restartDragWith.Item));
        }
    }

    private void OnPress()
    {
        EmitSignal(nameof(OnPressed));
    }

    private void OnToggle(bool pressed)
    {
        if (pressed)
            EmitSignal(nameof(OnSelected));
    }
}
