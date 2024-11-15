using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Allows picking the growth order of things in a GUI
/// </summary>
public partial class GrowthOrderPicker : Control
{
    private readonly List<DraggableItem> itemControls = new();

    /// <summary>
    ///   Memory to return currently set order with
    /// </summary>
    private readonly List<IPlayerReadableName> readItemOrder = new();

#pragma warning disable CA2213
    [Export]
    private Container buttonContainer = null!;

    private PackedScene draggableItemScene = null!;
#pragma warning restore CA2213

    [Signal]
    public delegate void OrderResetEventHandler();

    public override void _Ready()
    {
        draggableItemScene = GD.Load<PackedScene>("res://src/microbe_stage/editor/DraggableItem.tscn");
    }

    public override void _Process(double delta)
    {
    }

    /// <summary>
    ///   Creates or updates the sequence of reorderable items. Note that item equality is checked with ReferenceEquals
    ///   and not value equality.
    /// </summary>
    /// <param name="items">Sequence of items to ensure are shown in the given order</param>
    public void UpdateItems(IEnumerable<IPlayerReadableName> items)
    {
        using var enumerator = items.GetEnumerator();

        DraggableItem? lastItem = null;

        int index = 0;
        bool ranOutOfDestinations = false;

        // Check / update existing items
        while (enumerator.MoveNext())
        {
            // Stop looping here once out of controls
            if (index >= itemControls.Count)
            {
                ranOutOfDestinations = true;
                break;
            }

            var existingItem = itemControls[index];
            var wantedItem = enumerator.Current;

            if (!ReferenceEquals(existingItem.UserData, wantedItem))
            {
                // Need to update this item to repurpose it for this
                existingItem.UserData = enumerator.Current;
                existingItem.SetLabelText(enumerator.Current.ReadableName);
            }

            lastItem = existingItem;
        }

        if (!ranOutOfDestinations)
        {
            // Fewer items than there are controls so delete the excess
            for (; index < itemControls.Count; ++index)
            {
                var item = itemControls[index];
                if (!itemControls.Remove(item))
                    GD.PrintErr("Failed to remove item from GrowthOrderPicker");

                item.QueueFree();
            }
        }
        else
        {
            // Then create new items at the end
            do
            {
                var item = draggableItemScene.Instantiate<DraggableItem>();

                item.UserData = enumerator.Current;

                item.CanMoveUp = itemControls.Count > 0;
                item.SetLabelText(enumerator.Current.ReadableName);

                itemControls.Add(item);
                item.PositionNumber = itemControls.Count;

                buttonContainer.AddChild(item);
                lastItem = item;
            }
            while (enumerator.MoveNext());
        }

        // Last item cannot move down in the list
        if (lastItem != null)
            lastItem.CanMoveDown = false;
    }

    /// <summary>
    ///   Applies current item ordering to a sequence. Allows for example calling <see cref="UpdateItems"/> without
    ///   reordering existing items and only adding new ones to the end.
    /// </summary>
    /// <param name="rawItems">Items to apply ordering to</param>
    /// <returns>Items with current GUI order state applied to them</returns>
    public IEnumerable<IPlayerReadableName> ApplyOrderingToItems(IEnumerable<IPlayerReadableName> rawItems)
    {
        // Might be some other way to do this, but the easiest way seems to just create a new list
        var result = rawItems.ToList();

        if (itemControls.Count > 0)
        {
            // TODO: Order based on index found for the item in itemControls
            throw new NotImplementedException();
        }

        return result;
    }

    /// <summary>
    ///   Gets the current order. Note that the returned object can be modified afterwards with new data if this method
    ///   is called again (so no new list is allocated on each call)
    /// </summary>
    /// <returns>List giving the current wanted order</returns>
    public IReadOnlyList<IPlayerReadableName> GetCurrentOrder()
    {
        readItemOrder.Clear();

        foreach (var itemControl in itemControls)
        {
            if (itemControl.UserData == null)
            {
                GD.PrintErr("A item control with null user data is in growth order list");
                continue;
            }

            readItemOrder.Add((IPlayerReadableName)itemControl.UserData);
        }

        return readItemOrder;
    }

    private void OnResetButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(SignalName.OrderReset);
    }
}
