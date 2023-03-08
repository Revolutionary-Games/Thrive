using Godot;

public class InventorySlot : Button
{
#pragma warning disable CA2213
    private Control tooHeavyIndicator = null!;
#pragma warning restore CA2213

    private IInventoryItem? item;

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
            }
            else
            {
                Icon = null;
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

    public override void _Ready()
    {
        tooHeavyIndicator = GetNode<Control>("Overlays/TooHeavyToCarry");

        // TODO: tooltip for showing the name of the item / extra details if any exist
    }
}
