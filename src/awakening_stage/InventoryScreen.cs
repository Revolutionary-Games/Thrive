using System;
using System.Collections.Generic;
using Godot;

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

    private CustomDialog groundPanelPopup = null!;
    private Container groundSlotContainer = null!;

    private PackedScene inventorySlotScene = null!;
#pragma warning restore CA2213

    private ICharacterInventory? displayingInventoryOf;

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
                GroundPanelPopupPath.Dispose();
                GroundSlotContainerPath.Dispose();
            }

            inventorySlotGroup.Dispose();
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

        // TODO: callbacks for drag and for click to detect item moves

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
}
