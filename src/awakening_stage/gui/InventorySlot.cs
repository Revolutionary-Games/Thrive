using Godot;

/// <summary>
///   GUI control showing an inventory slot that can be interacted with
/// </summary>
public partial class InventorySlot : Button
{
#pragma warning disable CA2213
    private Control tooHeavyIndicator = null!;
#pragma warning restore CA2213

    private IInventoryItem? item;
    private IInventoryItem? ghostItem;

    private bool takeOnly;

    [Signal]
    public delegate void OnSelectedEventHandler(InventorySlot slot);

    [Signal]
    public delegate void OnPressedEventHandler(InventorySlot slot);

    [Signal]
    public delegate void OnDragStartedEventHandler();

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
            ApplyIcon();
        }
    }

    /// <summary>
    ///   When working with transient slots, the originals need to have ghost item placeholders in them to give some
    ///   kind of chance we can get the inventory logic working.
    /// </summary>
    public IInventoryItem? GhostItem
    {
        get => ghostItem;
        set
        {
            if (ghostItem == value)
                return;

            ghostItem = value;

            // TODO: should we have checks like this:
            // if (ghostItem != null && item != null)
            //     throw new InvalidOperationException("Can't set ghost item when normal item exists");

            ApplyIcon();
        }
    }

    /// <summary>
    ///   If true this inventory slot can't be interacted with
    /// </summary>
    public bool Locked
    {
        get => Disabled;
        set => Disabled = value;
    }

    public bool ShowTooHeavyIcon
    {
        get => tooHeavyIndicator.Visible;
        set => tooHeavyIndicator.Visible = value;
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
    ///   The category this slot is in, higher level controls than this use this information
    /// </summary>
    public InventorySlotCategory Category { get; set; }

    /// <summary>
    ///   Slot ID metadata to tie this to the inventory slot IDs in the "backend" data
    /// </summary>
    public int SlotId { get; set; } = -1;

    /// <summary>
    ///   Game logic needs to know about transient slots that don't "really" contain the items they have (for example
    ///   crafting slots)
    /// </summary>
    public bool Transient { get; set; }

    public static Control CreateDragPreviewForItem(IInventoryItem item)
    {
        // TODO: when dragging to a different popup, this causes flickering, and in general this seems to flicker with
        // the cursor, so find a way around that or report a Godot bug
        return new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            Texture = item.Icon,
            CustomMinimumSize = new Vector2(32, 32),
        };
    }

    public override void _Ready()
    {
        tooHeavyIndicator = GetNode<Control>("Overlays/TooHeavyToCarry");

        // TODO: tooltip for showing the name of the item / extra details if any exist
    }

    public override Variant _GetDragData(Vector2 position)
    {
        if (Item == null || Locked)
            return default(Variant);

        SetDragPreview(CreateDragPreviewForItem(Item));

        EmitSignal(SignalName.OnDragStarted);

        return new InventoryDragData(this, Item);
    }

    public override bool _CanDropData(Vector2 position, Variant data)
    {
        if (Locked)
            return false;

        var inventoryDragData = (InventoryDragData?)data;
        if (inventoryDragData == null)
            return false;

        if (AllowDropHandler != null)
        {
            return AllowDropHandler.Invoke(this, inventoryDragData);
        }

        // Fall back to the default behaviour of just allowing
        return true;
    }

    public override void _DropData(Vector2 position, Variant data)
    {
        var inventoryDragData = (InventoryDragData)data;

        DragResult result;
        if (PerformDropHandler != null)
        {
            result = PerformDropHandler.Invoke(this, inventoryDragData);

            if (result == DragResult.AlreadyHandled)
                return;
        }
        else
        {
            // Default handling
            result = DragResult.Success;
        }

        if (result == DragResult.Failure)
            return;

        // If our item is not null, we need to swap with the other slot
        var oldItem = Item;

        Item = inventoryDragData.Item;

        if (oldItem != null)
        {
            inventoryDragData.FromSlot.Item = oldItem;
        }
        else if (inventoryDragData.FromSlot.Item == Item)
        {
            // Clear the old slot to make sure the data doesn't exist in multiple places
            inventoryDragData.FromSlot.Item = null;
        }
    }

    public override string ToString()
    {
        return $"{base.ToString()} {Category} (slot: {SlotId}){(Transient ? " Transient" : string.Empty)}";
    }

    private void ApplyIcon()
    {
        if (item != null)
        {
            Icon = item.Icon;
            SelfModulate = new Color(1, 1, 1, 1);

            if (TakeOnly)
                Locked = false;
        }
        else
        {
            if (ghostItem != null)
            {
                Icon = ghostItem.Icon;
                SelfModulate = new Color(1, 1, 1, 0.7f);
            }
            else
            {
                Icon = null;
                SelfModulate = new Color(1, 1, 1, 1);
            }

            if (TakeOnly)
                Locked = true;
        }
    }

    private void OnPress()
    {
        EmitSignal(SignalName.OnPressed, this);
    }

    private void OnToggle(bool pressed)
    {
        if (pressed)
            EmitSignal(SignalName.OnSelected, this);
    }
}
