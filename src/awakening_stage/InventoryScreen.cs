﻿using System.Collections.Generic;
using Godot;

/// <summary>
///   Player inventory and crafting screen for the awakening stage
/// </summary>
public class InventoryScreen : ControlWithInput
{
    [Export]
    public NodePath? InventoryPopupPath;

    [Export]
    public NodePath CraftingPanelPopupPath = null!;

    [Export]
    public NodePath GroundPanelPopupPath = null!;

    [Export]
    public NodePath GroundSlotContainerPath = null!;

    private readonly ButtonGroup inventorySlotGroup = new();

    private readonly List<IInteractableEntity> objectsOnGround = new();
    private readonly List<InventorySlot> groundInventorySlots = new();

#pragma warning disable CA2213
    private CustomDialog inventoryPopup = null!;

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

        craftingPanelPopup = GetNode<CustomDialog>(CraftingPanelPopupPath);

        groundPanelPopup = GetNode<CustomDialog>(GroundPanelPopupPath);
        groundSlotContainer = GetNode<Container>(GroundSlotContainerPath);

        inventorySlotScene = GD.Load<PackedScene>("res://src/awakening_stage/InventorySlot.tscn");

        // TODO: add buttons for toggling the crafting and ground panels from the inventory screen

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

        if (!groundPanelPopup.Visible && !groundPanelManuallyHidden)
            groundPanelPopup.Show();

        if (!craftingPanelPopup.Visible && !craftingPanelManuallyHidden)
            craftingPanelPopup.Show();
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

        throw new System.NotImplementedException();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (InventoryPopupPath != null)
            {
                InventoryPopupPath.Dispose();
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

        foreach (var slotData in creature.ListInventoryContents())
        {
        }
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
