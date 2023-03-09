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

    private CustomDialog groundPanelPopup = null!;
    private Container groundSlotContainer = null!;

    private PackedScene inventorySlotScene = null!;
#pragma warning restore CA2213

    private ICharacterInventory? displayingInventoryOf;

    private bool groundPanelManuallyHidden;
    private bool craftingPanelManuallyHidden = true;
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

        groundPanelPopup = GetNode<CustomDialog>(GroundPanelPopupPath);
        groundSlotContainer = GetNode<Container>(GroundSlotContainerPath);

        inventorySlotScene = GD.Load<PackedScene>("res://src/awakening_stage/InventorySlot.tscn");

        // TODO: a background that allows dropping by dragging items outside the inventory

        Visible = true;
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
            craftingPanelPopup.Show();

        craftingPanelButton.Pressed = craftingPanelPopup.Visible;
        groundPanelButton.Pressed = groundPanelPopup.Visible;
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
            craftingPanelPopup.Show();

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

    private InventorySlot CreateInventorySlot()
    {
        var slot = inventorySlotScene.Instance<InventorySlot>();

        slot.Group = inventorySlotGroup;

        // TODO: callbacks

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
            craftingPanelPopup.Show();
            craftingPanelManuallyHidden = false;
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

    private void OnGroundPanelClosed()
    {
        groundPanelManuallyHidden = true;
    }

    private void OnCraftingPanelClosed()
    {
        craftingPanelManuallyHidden = true;
    }

    private void OnInventoryPanelClosed()
    {
        // Closing the inventory panel closes the entire thing
        Close();
    }
}
