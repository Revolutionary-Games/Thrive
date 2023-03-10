using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Array = Godot.Collections.Array;

/// <summary>
///   Player inventory and crafting screen for the awakening stage
/// </summary>
public class InventoryScreen : ControlWithInput
{
    [Export]
    public NodePath? InventoryPopupPath;

    [Export]
    public NodePath InventorySlotContainerPath = null!;

    [Export]
    public NodePath EquipmentSlotParentPath = null!;

    [Export]
    public NodePath EquipmentBackgroundImagePath = null!;

    [Export]
    public NodePath CraftingPanelButtonPath = null!;

    [Export]
    public NodePath GroundPanelButtonPath = null!;

    [Export]
    public NodePath CraftingPanelPopupPath = null!;

    [Export]
    public NodePath CraftingSlotsContainerPath = null!;

    [Export]
    public NodePath CraftingResultSlotsContainerPath = null!;

    [Export]
    public NodePath CraftingErrorStatusLabelPath = null!;

    [Export]
    public NodePath CraftingAnimationPlayerPath = null!;

    [Export]
    public NodePath TakeAllCraftingResultsPath = null!;

    [Export]
    public NodePath ClearCraftingInputsPath = null!;

    [Export]
    public NodePath GroundPanelPopupPath = null!;

    [Export]
    public NodePath GroundSlotContainerPath = null!;

    private readonly ButtonGroup inventorySlotGroup = new();

    private readonly List<IInteractableEntity> objectsOnGround = new();
    private readonly List<InventorySlot> groundInventorySlots = new();

    private readonly List<InventorySlot> inventorySlots = new();

    private readonly List<InventorySlot> equipmentSlots = new();

    private readonly List<InventorySlot> craftingSlots = new();

    private readonly List<InventorySlot> craftingResultSlots = new();

#pragma warning disable CA2213
    private CustomDialog inventoryPopup = null!;
    private Container inventorySlotContainer = null!;
    private Control equipmentSlotParent = null!;
    private TextureRect equipmentBackgroundImage = null!;
    private Button craftingPanelButton = null!;
    private Button groundPanelButton = null!;

    private CustomDialog craftingPanelPopup = null!;
    private Container craftingSlotsContainer = null!;
    private Container craftingResultSlotsContainer = null!;
    private Label craftingErrorStatusLabel = null!;
    private AnimationPlayer craftingAnimationPlayer = null!;
    private Button takeAllCraftingResults = null!;
    private TextureButton clearCraftingInputs = null!;

    private CustomDialog groundPanelPopup = null!;
    private Container groundSlotContainer = null!;

    private PackedScene inventorySlotScene = null!;
#pragma warning restore CA2213

    private ICharacterInventory? displayingInventoryOf;

    private InventorySlot? previouslySelectedSlot;
    private float timeUntilSlotSwap = -1;
    private InventorySlot? slotSwapFrom;
    private InventorySlot? slotSwapTo;

    private bool groundPanelManuallyHidden;
    private bool craftingPanelManuallyHidden = true;

    private bool craftingPanelSetup;
    private Vector2 craftingPanelDefaultPosition;

    public bool IsOpen => inventoryPopup.Visible;

    public override void _Ready()
    {
        inventoryPopup = GetNode<CustomDialog>(InventoryPopupPath);
        inventorySlotContainer = GetNode<Container>(InventorySlotContainerPath);
        equipmentSlotParent = GetNode<Control>(EquipmentSlotParentPath);
        equipmentBackgroundImage = GetNode<TextureRect>(EquipmentBackgroundImagePath);
        craftingPanelButton = GetNode<Button>(CraftingPanelButtonPath);
        groundPanelButton = GetNode<Button>(GroundPanelButtonPath);

        craftingPanelPopup = GetNode<CustomDialog>(CraftingPanelPopupPath);
        craftingSlotsContainer = GetNode<Container>(CraftingSlotsContainerPath);
        craftingResultSlotsContainer = GetNode<Container>(CraftingResultSlotsContainerPath);
        craftingErrorStatusLabel = GetNode<Label>(CraftingErrorStatusLabelPath);
        craftingAnimationPlayer = GetNode<AnimationPlayer>(CraftingAnimationPlayerPath);
        takeAllCraftingResults = GetNode<Button>(TakeAllCraftingResultsPath);
        clearCraftingInputs = GetNode<TextureButton>(ClearCraftingInputsPath);

        groundPanelPopup = GetNode<CustomDialog>(GroundPanelPopupPath);
        groundSlotContainer = GetNode<Container>(GroundSlotContainerPath);

        inventorySlotScene = GD.Load<PackedScene>("res://src/awakening_stage/InventorySlot.tscn");

        // TODO: a background that allows dropping by dragging items outside the inventory

        Visible = true;

        craftingPanelDefaultPosition = craftingPanelPopup.RectPosition;
    }

    public override void _Process(float delta)
    {
        // TODO: refresh the ground objects at some interval here

        // Perform slot swap once timer has expired for that
        if (timeUntilSlotSwap > 0)
        {
            timeUntilSlotSwap -= delta;

            if (timeUntilSlotSwap <= 0)
            {
                timeUntilSlotSwap = -1;

                if (slotSwapTo == null || slotSwapFrom == null || slotSwapTo == slotSwapFrom)
                {
                    GD.PrintErr("Slot swap was incorrectly setup");
                }
                else
                {
                    SwapSlotContentsIfPossible(slotSwapFrom, slotSwapTo);
                }
            }
        }
    }

    public void OpenInventory(ICharacterInventory creature)
    {
        if (!inventoryPopup.Visible)
            inventoryPopup.Show();

        SetInventoryDataFrom(creature);
        SetEquipmentDataFrom(creature);

        if (!groundPanelPopup.Visible && !groundPanelManuallyHidden)
            groundPanelPopup.Show();

        if (!craftingPanelPopup.Visible && !craftingPanelManuallyHidden)
        {
            ShowCraftingPanel();
        }

        UpdateToggleButtonStatus();
    }

    [RunOnKeyDown("ui_cancel")]
    public bool Close()
    {
        bool closedSomething = false;

        if (groundPanelPopup.Visible)
        {
            groundPanelPopup.Hide();
            closedSomething = true;
        }

        if (craftingPanelPopup.Visible)
        {
            craftingPanelPopup.Hide();
            closedSomething = true;
        }

        if (inventoryPopup.Visible)
        {
            inventoryPopup.Hide();
            closedSomething = true;
        }

        // TODO: if crafting *output* slots contain something, it needs to be dropped to the ground, otherwise it will
        // disappear / can be weirdly infinitely be kept there

        return closedSomething;
    }

    public void UpdateGroundItems(IEnumerable<IInteractableEntity> groundObjects)
    {
        objectsOnGround.Clear();
        objectsOnGround.AddRange(groundObjects);

        if (!groundPanelPopup.Visible)
            return;

        int nextIndex = 0;

        foreach (var objectOnGround in objectsOnGround)
        {
            InventorySlot slot;
            if (nextIndex >= groundInventorySlots.Count)
            {
                slot = CreateInventorySlot();

                groundSlotContainer.AddChild(slot);
                groundInventorySlots.Add(slot);
            }
            else
            {
                slot = groundInventorySlots[nextIndex];
            }

            slot.Item = objectOnGround;
            objectOnGround.LastNonTransientSlot = new WeakReference<InventorySlot>(slot);

            // TODO: update too heavy / not enough space indicator

            ++nextIndex;
        }

        EnsureAtLeastOneEmptyGroundSlot();
    }

    public void AddItemToCrafting(IInteractableEntity target)
    {
        craftingPanelManuallyHidden = false;

        if (!craftingPanelPopup.Visible)
            ShowCraftingPanel();

        throw new NotImplementedException();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (InventoryPopupPath != null)
            {
                InventoryPopupPath.Dispose();
                InventorySlotContainerPath.Dispose();
                EquipmentSlotParentPath.Dispose();
                EquipmentBackgroundImagePath.Dispose();
                CraftingPanelButtonPath.Dispose();
                GroundPanelButtonPath.Dispose();
                CraftingPanelPopupPath.Dispose();
                CraftingSlotsContainerPath.Dispose();
                CraftingResultSlotsContainerPath.Dispose();
                CraftingErrorStatusLabelPath.Dispose();
                CraftingAnimationPlayerPath.Dispose();
                TakeAllCraftingResultsPath.Dispose();
                ClearCraftingInputsPath.Dispose();
                GroundPanelPopupPath.Dispose();
                GroundSlotContainerPath.Dispose();
            }

            inventorySlotGroup.Dispose();

            // Unhook all C# callbacks
            foreach (var slot in groundInventorySlots.Concat(inventorySlots).Concat(equipmentSlots)
                         .Concat(craftingSlots).Concat(craftingResultSlots))
            {
                slot.AllowDropHandler -= CheckIsDropAllowed;
                slot.PerformDropHandler -= OnDropPerformed;
            }
        }

        base.Dispose(disposing);
    }

    private void EnsureAtLeastOneEmptyGroundSlot()
    {
        foreach (var inventorySlot in groundInventorySlots)
        {
            if (inventorySlot.Item == null)
                return;
        }

        var slot = CreateInventorySlot();

        groundSlotContainer.AddChild(slot);
        groundInventorySlots.Add(slot);
    }

    private void SetInventoryDataFrom(ICharacterInventory creature)
    {
        displayingInventoryOf = creature;

        int nextIndex = 0;

        foreach (var slotData in creature.ListInventoryContents())
        {
            InventorySlot slot;
            if (nextIndex >= inventorySlots.Count)
            {
                slot = CreateInventorySlot();

                inventorySlotContainer.AddChild(slot);
                inventorySlots.Add(slot);
            }
            else
            {
                slot = inventorySlots[nextIndex];
            }

            slot.Item = slotData.ContainedItem;
            slot.SlotId = slotData.Id;

            ++nextIndex;
        }

        // Remove excess slots
        while (nextIndex < inventorySlots.Count)
        {
            var slot = inventorySlots[inventorySlots.Count - 1];

            inventorySlotContainer.RemoveChild(slot);
            inventorySlots.RemoveAt(inventorySlots.Count - 1);
        }
    }

    private void SetEquipmentDataFrom(ICharacterInventory creature)
    {
        int nextIndex = 0;

        var areaSize = equipmentSlotParent.RectSize;

        equipmentBackgroundImage.RectSize = areaSize;

        foreach (var slotData in creature.ListEquipmentContents())
        {
            InventorySlot slot;
            if (nextIndex >= equipmentSlots.Count)
            {
                slot = CreateInventorySlot();

                equipmentSlotParent.AddChild(slot);
                equipmentSlots.Add(slot);
            }
            else
            {
                slot = equipmentSlots[nextIndex];
            }

            slot.Item = slotData.ContainedItem;
            slot.SlotId = slotData.Id;

            // Position this correctly on the creature image
            Vector2 position;

            if (slotData.EquipmentPosition == null)
            {
                GD.PrintErr("Equipment slot doesn't have specified position");
                position = new Vector2(0, 0);
            }
            else
            {
                position = slotData.EquipmentPosition.Value;
            }

            // TODO: take image alignment to the center into account at certain aspect ratios

            slot.RectPosition = new Vector2(position.x * areaSize.x, position.y * areaSize.y) - slot.RectSize * 0.5f;

            ++nextIndex;
        }

        // Remove excess slots
        while (nextIndex < equipmentSlots.Count)
        {
            var slot = equipmentSlots[equipmentSlots.Count - 1];

            equipmentSlotParent.RemoveChild(slot);
            equipmentSlots.RemoveAt(equipmentSlots.Count - 1);
        }
    }

    private void UpdateEquipmentSlotPositions()
    {
        if (displayingInventoryOf != null)
            SetEquipmentDataFrom(displayingInventoryOf);
    }

    private void SetupCraftingPanel()
    {
        if (craftingPanelSetup)
            return;

        craftingPanelSetup = true;

        EnsureCraftingPanelHasEmptyInputSlot();
        EnsureCraftingResultHasEmptySlots(1);

        // Something messes with the crafting panel not being the right size when opened, so we need to do some size
        // unstuck fixing here
        Invoke.Instance.QueueForObject(() =>
        {
            craftingPanelPopup.RectSize = Vector2.Zero;
            craftingPanelPopup.RectPosition = craftingPanelDefaultPosition;
        }, craftingPanelPopup);
    }

    private void EnsureCraftingPanelHasEmptyInputSlot()
    {
        foreach (var slot in craftingSlots)
        {
            if (slot.Item == null)
                return;
        }

        // New slot needed as all previous ones are filled
        var newSlot = CreateInventorySlot();

        craftingSlotsContainer.AddChild(newSlot);
        craftingSlots.Add(newSlot);
    }

    private bool HasAnyCraftingResultSlotsWithItems()
    {
        foreach (var slot in craftingResultSlots)
        {
            if (slot.Item != null)
                return true;
        }

        return false;
    }

    private void EnsureCraftingResultHasEmptySlots(int required)
    {
        foreach (var slot in craftingResultSlots)
        {
            if (slot.Item == null)
            {
                --required;
            }
        }

        while (required > 0)
        {
            var newSlot = CreateInventorySlot();

            // Crafting results can't have random stuff put in them
            newSlot.TakeOnly = true;

            craftingResultSlotsContainer.AddChild(newSlot);
            craftingResultSlots.Add(newSlot);

            // TODO: crafting output should only allow moving items out of it

            --required;
        }
    }

    private InventorySlot CreateInventorySlot()
    {
        var slot = inventorySlotScene.Instance<InventorySlot>();

        slot.Group = inventorySlotGroup;

        // Connect the required signals
        var binds = new Array();
        binds.Add(slot);
        slot.Connect(nameof(InventorySlot.OnSelected), this, nameof(OnInventorySlotSelected), binds);

        slot.Connect(nameof(InventorySlot.OnDragStarted), this, nameof(OnInventoryDragStarted));

        slot.AllowDropHandler += CheckIsDropAllowed;
        slot.PerformDropHandler += OnDropPerformed;

        return slot;
    }

    private void ToggleCraftingPanel(bool pressed)
    {
        if (craftingPanelPopup.Visible == pressed)
            return;

        if (craftingPanelPopup.Visible)
        {
            craftingPanelPopup.CustomHide();
            craftingPanelManuallyHidden = true;
        }
        else
        {
            ShowCraftingPanel();
        }
    }

    private void ToggleGroundPanel(bool pressed)
    {
        if (groundPanelPopup.Visible == pressed)
            return;

        if (groundPanelPopup.Visible)
        {
            groundPanelPopup.CustomHide();
            groundPanelManuallyHidden = true;
        }
        else
        {
            groundPanelPopup.Show();
            groundPanelManuallyHidden = false;
        }
    }

    private void ShowCraftingPanel()
    {
        // Setup the panel the first time it is opened to ensure the sizing is immediately correct
        SetupCraftingPanel();

        craftingPanelPopup.Show();
        craftingPanelManuallyHidden = false;

        ResetCraftingErrorLabel();
        UpdateCraftingGUIState();

        UpdateToggleButtonStatus();
    }

    private void OnGroundPanelClosed()
    {
        groundPanelManuallyHidden = true;
        UpdateToggleButtonStatus();
    }

    private void OnCraftingPanelClosed()
    {
        craftingPanelManuallyHidden = true;
        UpdateToggleButtonStatus();
    }

    private void OnInventoryPanelClosed()
    {
        // Closing the inventory panel closes the entire thing
        Close();
    }

    private void UpdateToggleButtonStatus()
    {
        craftingPanelButton.Pressed = craftingPanelPopup.Visible;
        groundPanelButton.Pressed = groundPanelPopup.Visible;
    }

    private void OnInventorySlotSelected(InventorySlot slot)
    {
        if (previouslySelectedSlot != null && previouslySelectedSlot != slot)
        {
            // When a second inventory slot is clicked after clicking another one, the contents of the two slots are
            // swapped. This is done with a delay to allow drag to work
            timeUntilSlotSwap = Constants.INVENTORY_DRAG_START_ALLOWANCE;
            slotSwapFrom = previouslySelectedSlot;
            slotSwapTo = slot;
        }

        previouslySelectedSlot = slot;
    }

    private void OnInventoryDragStarted()
    {
        // Cancel any pending click swaps when dragging starts
        timeUntilSlotSwap = -1;
    }

    private bool CheckIsDropAllowed(InventorySlot toSlot, InventoryDragData dragData)
    {
        return CanSwapContents(dragData.FromSlot, toSlot);
    }

    private DragResult OnDropPerformed(InventorySlot toSlot, InventoryDragData dragData)
    {
        SwapSlotContentsIfPossible(dragData.FromSlot, toSlot);
        return DragResult.AlreadyHandled;
    }

    /// <summary>
    ///   Handles keeping the backend data up to date when we swap things in slots
    /// </summary>
    /// <param name="from">The moved from slot</param>
    /// <param name="to">The target slot</param>
    private void OnSlotSwapHappened(InventorySlot from, InventorySlot to)
    {
        // Moving within a single category (when on ground or crafting) requires no action
        if (AreSlotsInCategory(from, to, groundInventorySlots) || AreSlotsInCategory(from, to, craftingSlots))
            return;

        // Putting things in the crafting panel doesn't need any action
        if (craftingSlots.Contains(to))
            return;

        // When moving things from the crafting slots, we need to care about the original status of the items
        if (craftingSlots.Contains(from))
        {
            // When originally a ground item is moved to the inventory, it needs to be picked up
            // TODO: handling for these cases
            throw new NotImplementedException();

            return;
        }

        if (displayingInventoryOf == null)
            throw new InvalidOperationException("Not opened to display inventory of anything");

        // Moving between inventory and equipment
        if ((equipmentSlots.Contains(from) || equipmentSlots.Contains(to)) &&
            (inventorySlots.Contains(from) || inventorySlots.Contains(to)))
        {
            displayingInventoryOf.MoveItemSlots(from.SlotId, to.SlotId);
            return;
        }

        // Moving within inventory or equipment
        if (AreSlotsInCategory(from, to, equipmentSlots) || AreSlotsInCategory(from, to, inventorySlots))
        {
            displayingInventoryOf.MoveItemSlots(from.SlotId, to.SlotId);
            return;
        }

        // Moving from ground to creature
        if (groundInventorySlots.Contains(from) && (inventorySlots.Contains(to) || equipmentSlots.Contains(to)))
        {
            HandleDropTypeSlotMove(to, from);
            return;
        }

        // Moving from inventory to ground
        if (groundInventorySlots.Contains(to) && (inventorySlots.Contains(from) || equipmentSlots.Contains(from)))
        {
            HandleDropTypeSlotMove(from, to);
            return;
        }

        GD.PrintErr("Unknown slot move! Inventory data may be out of sync now.");
    }

    private void HandleDropTypeSlotMove(InventorySlot creatureSlot, InventorySlot groundSlot)
    {
        IInteractableEntity? toPickup = null;

        // Detect first if something was picked up
        if (creatureSlot.Item != null)
        {
            toPickup = creatureSlot.Item as IInteractableEntity;

            if (toPickup == null)
            {
                GD.PrintErr("Picked up a non-interactable when swapping with a ground slot item");
                UndoSwap(creatureSlot, groundSlot);
                return;
            }
        }

        // Drop first to make room
        if (groundSlot.Item != null)
        {
            // TODO: we should check in swap that dropping is only allowed for interactables
            if (groundSlot.Item is not IInteractableEntity entity)
            {
                GD.PrintErr("Can't drop non-interactable item, TODO: handle this better");
                UndoSwap(creatureSlot, groundSlot);
                return;
            }

            if (!displayingInventoryOf!.DropItem(entity))
            {
                GD.PrintErr("Dropping item failed");
                UndoSwap(creatureSlot, groundSlot);
                return;
            }
        }

        // Then pickup the swapped item if any (it'll fit better in the inventory this way around)
        if (toPickup != null)
        {
            if (!displayingInventoryOf!.PickUpItem(toPickup, creatureSlot.SlotId))
            {
                GD.PrintErr("Failed to pick up item when swapping inventory slot contents with ground");

                // Clear the failed to be picked up item to not be in inconsistent state
                creatureSlot.Item = null;
            }
        }

        // Ensure there are enough ground slots if the player wants to drop many things at once
        EnsureAtLeastOneEmptyGroundSlot();
    }

    private bool AreSlotsInCategory(InventorySlot slot1, InventorySlot slot2,
        IReadOnlyCollection<InventorySlot> category)
    {
        return category.Contains(slot1) && category.Contains(slot2);
    }

    private bool SwapSlotContentsIfPossible(InventorySlot fromSlot, InventorySlot toSlot)
    {
        if (!CanSwapContents(fromSlot, toSlot))
            return false;

        (toSlot.Item, fromSlot.Item) = (fromSlot.Item, toSlot.Item);

        OnSlotSwapHappened(fromSlot, toSlot);
    }

    private bool CanSwapContents(InventorySlot fromSlot, InventorySlot toSlot)
    {
        if (toSlot.TakeOnly || fromSlot.Locked || toSlot.Locked)
            return false;

        // Can't swap items when a to slot would end up with a new item
        if (fromSlot.TakeOnly && toSlot.Item != null)
            return false;

        if (displayingInventoryOf == null)
            throw new InvalidOperationException("Not opened to display inventory of anything");

        // TODO: allow this by putting the crafting result in the inventory or on the ground automatically
        // For now user needs to press the take all button
        if (craftingResultSlots.Contains(fromSlot) && craftingSlots.Contains(toSlot))
            return false;

        var slotId1 = fromSlot.SlotId;
        var slotId2 = toSlot.SlotId;

        if (slotId1 != -1 && slotId2 != -1 && !displayingInventoryOf.IsItemSlotMoveAllowed(slotId1, slotId2))
            return false;

        // TODO: extra checks, check pick up weight or some other things?

        return true;
    }

    private void UndoSwap(InventorySlot fromSlot, InventorySlot toSlot)
    {
        GD.Print("Undoing item swap that was not allowed after all");
        (toSlot.Item, fromSlot.Item) = (fromSlot.Item, toSlot.Item);
    }

    private void UpdateCraftingGUIState()
    {
        bool hasCraftingResults = false;

        foreach (var resultSlot in craftingResultSlots)
        {
            // Lock all crafting result slots that are empty
            if (resultSlot.Item != null)
            {
                hasCraftingResults = true;
                resultSlot.Locked = false;
            }
            else
            {
                resultSlot.Locked = true;
            }
        }

        takeAllCraftingResults.Disabled = !hasCraftingResults;

        clearCraftingInputs.Disabled = craftingSlots.All(s => s.Item == null);
    }

    private void TryToCraft()
    {
        // TODO: implement selecting the resource to actually craft
        // if (selectedRecipe == null)
        {
            SetCraftingError(TranslationServer.Translate("CRAFTING_NO_RECIPE_SELECTED"));

            // return;
        }

        /*if (!TakeAllCraftingResults())
        {
            SetCraftingError(TranslationServer.Translate("CRAFTING_NO_ROOM_TO_TAKE_CRAFTING_RESULTS"));
            return;
        }*/

        // throw new NotImplementedException();
    }

    /// <summary>
    ///   Attempts to take all the crafting results into inventory (and equipment slots)
    /// </summary>
    /// <returns>True when crafting results are now empty, false if there wasn't enough space for the results</returns>
    private bool TakeAllCraftingResults()
    {
        bool anyLeft = false;

        foreach (var slot in craftingSlots)
        {
            if (slot.Item == null)
                continue;

            if (!TryToMoveToInventory(slot, true))
            {
                anyLeft = true;
            }
        }

        UpdateCraftingGUIState();

        return !anyLeft;
    }

    private bool ClearCraftingInputs()
    {
        bool anyLeft = false;

        // Move crafting inputs back to the slots they are from originally
        foreach (var slot in craftingSlots)
        {
            if (slot.Item == null)
                continue;

            if (slot.Item.LastNonTransientSlot != null)
            {
                if (slot.Item.LastNonTransientSlot.TryGetTarget(out var originalSlot))
                {
                    if (!originalSlot.Transient && originalSlot.Item == null)
                    {
                        // Can try to move this back to the original slot
                        if (SwapSlotContentsIfPossible(slot, originalSlot))
                            continue;
                    }
                }
            }

            anyLeft = true;
        }

        // If some could not be moved, then try to move them to inventory slots
        if (anyLeft)
        {
            anyLeft = false;

            foreach (var slot in craftingSlots)
            {
                if (slot.Item == null)
                    continue;

                if (!TryToMoveToInventory(slot))
                {
                    anyLeft = true;

                    // Once we run out of inventory space we can just end here
                    // TODO: if slots in the future can fit different stuff (like a total weight limit), this needs to
                    // be rethought
                    break;
                }
            }
        }

        UpdateCraftingGUIState();

        if (!anyLeft)
        {
            // Moves succeeded
            return true;
        }

        // If still no space, then fail
        return false;
    }

    private bool TryToMoveToInventory(InventorySlot fromSlot, bool alsoEquip = false)
    {
        foreach (var slot in inventorySlots)
        {
            if (slot.Item != null)
                continue;

            // Empty slot to move to
            if (SwapSlotContentsIfPossible(fromSlot, slot))
                return true;
        }

        if (!alsoEquip)
            return false;

        foreach (var slot in equipmentSlots)
        {
            if (slot.Item != null)
                continue;

            // Equipment slot that could potentially hold this item
            if (SwapSlotContentsIfPossible(fromSlot, slot))
                return true;
        }

        return false;
    }

    private void SetCraftingError(string message, bool flash = true)
    {
        craftingErrorStatusLabel.Text = message;

        if (flash)
            craftingAnimationPlayer.Play("Flash");
    }

    private void ResetCraftingErrorLabel()
    {
        craftingErrorStatusLabel.Text = TranslationServer.Translate("CRAFTING_SELECT_RECIPE_OR_ITEMS_TO_FILTER");
    }
}
