using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Allows picking the growth order of things in a GUI
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
public partial class GrowthOrderPicker : Control
{
    private readonly List<DraggableItem> itemControls = new();

    /// <summary>
    ///   Memory to return currently set order with
    /// </summary>
    private readonly List<IPlayerReadableName> readItemOrder = new();

    private readonly Callable downPress;
    private readonly Callable upPress;
    private readonly Callable dragSwitch;

    private readonly Comparer itemComparer;

#pragma warning disable CA2213
    [Export]
    private Container buttonContainer = null!;

    private PackedScene draggableItemScene = null!;
#pragma warning restore CA2213

    private List<IPlayerReadableName?>? currentSavedOrder;

    private SaveComparer? savedItemComparer;

    public GrowthOrderPicker()
    {
        downPress = new Callable(this, nameof(MoveDown));
        upPress = new Callable(this, nameof(MoveUp));
        dragSwitch = new Callable(this, nameof(MoveToFront));

        itemComparer = new Comparer(itemControls);
    }

    [Signal]
    public delegate void OrderResetEventHandler();

    /// <summary>
    ///   A special property to handle saving and loading of state. Can technically be used to read the data but this
    ///   causes more memory allocations than necessary.
    /// </summary>
    [JsonProperty]
    public List<IPlayerReadableName?> CurrentSavedOrder
    {
        get
        {
            if (itemControls.Count < 1)
            {
                currentSavedOrder ??= new List<IPlayerReadableName?>();
                return currentSavedOrder;
            }

            return itemControls.Select(c => (IPlayerReadableName?)c.UserData).ToList();
        }
        private set => currentSavedOrder = value;
    }

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
        // We are getting real items, so let go of any saved data
        currentSavedOrder = null;

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

            var existingItem = itemControls[index++];
            var wantedItem = enumerator.Current;

            if (!ReferenceEquals(existingItem.UserData, wantedItem))
            {
                // Need to update this item to repurpose it for this
                existingItem.UserData = enumerator.Current;
                existingItem.SetLabelText(enumerator.Current.ReadableName);
            }

            // Always enable down button, it will be disabled later for the last item
            existingItem.CanMoveDown = true;

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

                // Register signals to allow the items to reorder things
                item.Connect(DraggableItem.SignalName.OnDownPressed, downPress);
                item.Connect(DraggableItem.SignalName.OnUpPressed, upPress);
                item.Connect(DraggableItem.SignalName.OnDraggedToNewPosition, dragSwitch);

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
    public IEnumerable<T> ApplyOrderingToItems<T>(IEnumerable<T> rawItems)
        where T : IPlayerReadableName
    {
        if (itemControls.Count <= 0)
        {
            // If no existing items, can just return the raw list

            // Except if this is loaded from a save and no real order was created yet, then use that
            if (currentSavedOrder != null)
            {
                // It shouldn't be possible for the list to change so hopefully the save comparer never needs to be
                // recreated
                savedItemComparer ??= new SaveComparer(currentSavedOrder);
                return rawItems.OrderBy(i => i, savedItemComparer);
            }

            return rawItems;
        }

        // Need to use LINQ sort here as it is a stable sort and our sorter only does a partial ordering
        // Apparently `Order` might not be a stable sort so for safety `OrderBy` is used as that is guaranteed
        // according to the documentation to be stable
        return rawItems.OrderBy(i => i, itemComparer);
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

    private void MoveDown(DraggableItem item)
    {
        var itemCount = itemControls.Count;
        for (int i = 0; i < itemCount; ++i)
        {
            var currentControl = itemControls[i];

            if (currentControl == item)
            {
                if (i + 1 >= itemControls.Count)
                {
                    GD.PrintErr("Item is already at the bottom");
                    return;
                }

                // Swap this item with the below one to perform the down move
                SwapControls(currentControl, itemControls[i + 1]);
                return;
            }
        }

        GD.PrintErr("Couldn't find item control to reorder to move down");
    }

    private void MoveUp(DraggableItem item)
    {
        var itemCount = itemControls.Count;
        for (int i = 0; i < itemCount; ++i)
        {
            var currentControl = itemControls[i];

            if (currentControl == item)
            {
                if (i < 1)
                {
                    GD.PrintErr("Item is already at the top");
                    return;
                }

                SwapControls(currentControl, itemControls[i - 1]);
                return;
            }
        }

        GD.PrintErr("Couldn't find item control to reorder to move up");
    }

    private void MoveToFront(DraggableItem item, DraggableItem toFrontOf)
    {
        var itemIndex = itemControls.IndexOf(item);
        var targetIndex = itemControls.IndexOf(toFrontOf);

        if (itemIndex == targetIndex || itemIndex == -1 || targetIndex == -1)
        {
            GD.PrintErr("Couldn't find proper indexes for dragged item move");
            return;
        }

        int itemCount = itemControls.Count;

        if (itemIndex > targetIndex)
        {
            // Item is after target

            // First put the item there, and then "push" the remaining items until they reach the target and everything
            // is settled correctly

            object? wantedItem = item.UserData;

            for (int i = itemIndex; i > targetIndex; --i)
            {
                itemControls[i].UserData = itemControls[i - 1].UserData;
            }

            itemControls[targetIndex].UserData = wantedItem;
        }
        else
        {
            // Opposite of the above
            // Note that slightly differently than the name, when dragged from an earlier index, the item wants to be
            // placed at the given target index and *not before* it
            object? wantedItem = item.UserData;

            for (int i = itemIndex; i < targetIndex; ++i)
            {
                itemControls[i].UserData = itemControls[i + 1].UserData;
            }

            itemControls[targetIndex].UserData = wantedItem;
        }

        // Update all control labels (easier to bulk do it here at the end instead of interleaving with the other
        // algorithm)
        var firstIndex = Math.Min(itemIndex, targetIndex);
        for (int i = firstIndex; i < itemCount; ++i)
        {
            var itemControl = itemControls[i];

            if (itemControl.UserData == null)
            {
                GD.PrintErr("Dragged item has no user data, not updating label");
                continue;
            }

            itemControl.SetLabelText(((IPlayerReadableName)itemControl.UserData).ReadableName);
        }
    }

    private void SwapControls(DraggableItem from, DraggableItem to)
    {
        if (from == to)
            throw new ArgumentException("Can't swap an item with itself");

        var temp = from.UserData;

        if (temp == null || to.UserData == null)
            throw new InvalidOperationException("User data to move shouldn't be null");

        from.UserData = to.UserData;
        from.SetLabelText(((IPlayerReadableName)from.UserData).ReadableName);

        to.UserData = temp;
        to.SetLabelText(((IPlayerReadableName)to.UserData).ReadableName);
    }

    private class Comparer(List<DraggableItem> existingItems) : IComparer<IPlayerReadableName>
    {
        public int Compare(IPlayerReadableName? x, IPlayerReadableName? y)
        {
            if (ReferenceEquals(x, y))
                return 0;

            if (y is null)
                return 1;

            if (x is null)
                return -1;

            return FindIndex(x).CompareTo(FindIndex(y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindIndex(IPlayerReadableName item)
        {
            var count = existingItems.Count;
            for (var i = 0; i < count; ++i)
            {
                if (ReferenceEquals(item, existingItems[i].UserData))
                    return i;
            }

            // We want not-found items to be last, so return max int for them rather than the default -1
            return int.MaxValue;
        }
    }

    /// <summary>
    ///   Variant of the custom comparer that uses saved data list instead of a control data list
    /// </summary>
    private class SaveComparer(List<IPlayerReadableName?> existingItems) : IComparer<IPlayerReadableName>
    {
        public int Compare(IPlayerReadableName? x, IPlayerReadableName? y)
        {
            if (ReferenceEquals(x, y))
                return 0;

            if (y is null)
                return 1;

            if (x is null)
                return -1;

            return FindIndex(x).CompareTo(FindIndex(y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindIndex(IPlayerReadableName item)
        {
            var count = existingItems.Count;
            for (var i = 0; i < count; ++i)
            {
                if (ReferenceEquals(item, existingItems[i]))
                    return i;
            }

            return int.MaxValue;
        }
    }
}
