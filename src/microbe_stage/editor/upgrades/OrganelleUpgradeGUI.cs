using System.Collections.Generic;
using System.Linq;
using Godot;

public class OrganelleUpgradeGUI : Control
{
    [Export]
    public NodePath? PopupPath;

    [Export]
    public NodePath OrganelleSpecificContentPath = null!;

    [Export]
    public NodePath ScrollContainerPath = null!;

    [Export]
    public NodePath GeneralUpgradesContainerPath = null!;

    [Export]
    public NodePath UpgradeSelectorButtonsContainerPath = null!;

    private readonly Dictionary<string, MicrobePartSelection> generalUpgradeSelectorButtons = new();

#pragma warning disable CA2213
    private CustomConfirmationDialog popup = null!;
    private Container organelleSpecificContent = null!;
    private ScrollContainer scrollContainer = null!;

    private Control generalUpgradesContainer = null!;
    private Container upgradeSelectorButtonsContainer = null!;

    private PackedScene upgradeSelectionButtonScene = null!;
    private ButtonGroup generalUpgradeButtonGroup = null!;

    private PackedScene upgradeTooltipScene = null!;
#pragma warning restore CA2213

    private ICellEditorComponent? storedEditor;
    private IOrganelleUpgrader? upgrader;
    private OrganelleTemplate? openedForOrganelle;

    private bool registeredTooltips;
    private List<string>? currentlySelectedGeneralUpgrades;

    [Signal]
    public delegate void Accepted();

    public override void _Ready()
    {
        popup = GetNode<CustomConfirmationDialog>(PopupPath);
        organelleSpecificContent = GetNode<Container>(OrganelleSpecificContentPath);
        scrollContainer = GetNode<ScrollContainer>(ScrollContainerPath);

        generalUpgradesContainer = GetNode<Control>(GeneralUpgradesContainerPath);
        upgradeSelectorButtonsContainer = GetNode<Container>(UpgradeSelectorButtonsContainerPath);

        upgradeSelectionButtonScene = GD.Load<PackedScene>("res://src/microbe_stage/editor/MicrobePartSelection.tscn");
        generalUpgradeButtonGroup = new ButtonGroup();

        upgradeTooltipScene = GD.Load<PackedScene>("res://src/microbe_stage/editor/tooltips/SelectionMenuToolTip.tscn");
    }

    public void OpenForOrganelle(OrganelleTemplate organelle, string upgraderScene,
        ICellEditorComponent editorComponent, ICellEditorData editorData, float costMultiplier)
    {
        openedForOrganelle = organelle;

        // Organelles that have no special upgrades, specify no scene, but they must have general upgrades in that case
        var availableGeneralUpgrades = organelle.Definition.AvailableUpgrades;

        bool somethingAdded = false;

        if (!string.IsNullOrEmpty(upgraderScene))
        {
            var scene = GD.Load<PackedScene>(upgraderScene);

            if (scene == null)
            {
                GD.PrintErr($"Failed to load upgrader scene for organelle of type {organelle.Definition.InternalName}");
                return;
            }

            var instance = scene.Instance();
            upgrader = (IOrganelleUpgrader)instance;

            organelleSpecificContent.FreeChildren();
            organelleSpecificContent.AddChild(instance);

            scrollContainer.RectMinSize = upgrader.GetMinDialogSize();

            somethingAdded = true;
        }
        else
        {
            organelleSpecificContent.FreeChildren();
            upgrader = null;
        }

        upgradeSelectorButtonsContainer.FreeChildren();
        generalUpgradeSelectorButtons.Clear();

        if (availableGeneralUpgrades.Count > 0)
        {
            ReleaseTooltips();

            var tooltipGroup = GetTooltipGroup();

            var oldUpgrade = organelle.Upgrades ?? new OrganelleUpgrades();

            // Setup the buttons for each of the available upgrades
            foreach (var availableUpgrade in availableGeneralUpgrades)
            {
                var upgrade = availableUpgrade.Value;

                var selectionButton = upgradeSelectionButtonScene.Instance<MicrobePartSelection>();

                var newUpgrade = new OrganelleUpgrades();
                newUpgrade.UnlockedFeatures.Add(availableUpgrade.Key);

                var data = new OrganelleUpgradeActionData(oldUpgrade, newUpgrade, organelle)
                {
                    CostMultiplier = costMultiplier,
                };

                var cost = editorData.WhatWouldActionsCost(new[] { data });

                selectionButton.Name = availableUpgrade.Key;
                selectionButton.SelectionGroup = generalUpgradeButtonGroup;
                selectionButton.PartName = upgrade.Name;
                selectionButton.MPCost = cost;
                selectionButton.PartIcon = upgrade.LoadedIcon;

                selectionButton.Connect(nameof(MicrobePartSelection.OnPartSelected), this,
                    nameof(OnGeneralUpgradeSelected));

                // Tooltip
                var tooltip = upgradeTooltipScene.Instance<SelectionMenuToolTip>();
                tooltip.DisplayName = upgrade.Name;
                tooltip.Description = upgrade.Description;
                tooltip.MutationPointCost = cost;

                // TODO: add support for flavour text
                // tooltip.ProcessesDescription = upgrade.Description;
                // tooltip.Description = ...

                selectionButton.RegisterToolTipForControl(tooltip);
                ToolTipManager.Instance.AddToolTip(tooltip, tooltipGroup);
                registeredTooltips = true;

                upgradeSelectorButtonsContainer.AddChild(selectionButton);
                generalUpgradeSelectorButtons[availableUpgrade.Key] = selectionButton;
            }

            currentlySelectedGeneralUpgrades = organelle.Upgrades?.UnlockedFeatures ?? new List<string>();
            UpdateSelectedUpgradeButton();

            generalUpgradesContainer.Visible = true;
            somethingAdded = true;
        }
        else
        {
            generalUpgradesContainer.Visible = false;
            currentlySelectedGeneralUpgrades = null;
        }

        if (!somethingAdded)
        {
            GD.PrintErr("Organelle must have at least an upgrader scene or general upgrades, " +
                $"type: {organelle.Definition.InternalName}");
            return;
        }

        popup.PopupCenteredShrink();

        scrollContainer.ScrollVertical = 0;
        upgrader?.OnStartFor(organelle);
        storedEditor = editorComponent;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (PopupPath != null)
            {
                PopupPath.Dispose();
                OrganelleSpecificContentPath.Dispose();
                ScrollContainerPath.Dispose();
                GeneralUpgradesContainerPath.Dispose();
                UpgradeSelectorButtonsContainerPath.Dispose();
            }

            ReleaseTooltips();
        }

        base.Dispose(disposing);
    }

    private void OnGeneralUpgradeSelected(string upgradeName)
    {
        GUICommon.Instance.PlayButtonPressSound();

        // Remove all options that are not selected
        currentlySelectedGeneralUpgrades = currentlySelectedGeneralUpgrades!.Where(u => u == upgradeName).ToList();

        // Add the new upgrade to the list to be selected (except if it is the special none value)
        if (upgradeName != Constants.ORGANELLE_UPGRADE_SPECIAL_NONE)
            currentlySelectedGeneralUpgrades.Add(upgradeName);

        UpdateSelectedUpgradeButton();
    }

    private void OnAccept()
    {
        if (storedEditor == null || openedForOrganelle == null)
        {
            GD.PrintErr("Can't apply organelle upgrades as this upgrade GUI was not opened properly");
            return;
        }

        GUICommon.Instance.PlayButtonPressSound();

        // Use the existing data as the old data or create new data if the organelle didn't have upgrades data yet
        var oldUpgrades = openedForOrganelle.Upgrades ?? new OrganelleUpgrades();
        var newUpgrades = (OrganelleUpgrades)oldUpgrades.Clone();

        if (currentlySelectedGeneralUpgrades != null)
        {
            // Apply the general upgrades
            newUpgrades.UnlockedFeatures = currentlySelectedGeneralUpgrades;
        }

        if (upgrader != null)
        {
            if (!upgrader.ApplyChanges(storedEditor, newUpgrades))
            {
                GD.Print("Upgrader can't apply changes, not closing upgrade GUI");
                return;
            }
        }

        // TODO: when no changes are done the action creation should be skipped
        var action = new OrganelleUpgradeActionData(oldUpgrades, newUpgrades, openedForOrganelle);

        if (!storedEditor.ApplyOrganelleUpgrade(action))
        {
            GD.Print("Can't apply organelle upgrade action");
            return;
        }

        EmitSignal(nameof(Accepted));
    }

    private void OnCancel()
    {
        GUICommon.Instance.PlayButtonPressSound();
    }

    private void UpdateSelectedUpgradeButton()
    {
        bool selectedAButton = false;
        MicrobePartSelection? defaultOption = null;

        foreach (var selectorButton in generalUpgradeSelectorButtons)
        {
            // Select the upgrade button that is currently in the organelle data
            if (currentlySelectedGeneralUpgrades!.Contains(selectorButton.Key))
            {
                selectorButton.Value.Selected = true;
                selectedAButton = true;
            }
            else
            {
                selectorButton.Value.Selected = false;
            }

            if (selectorButton.Key == Constants.ORGANELLE_UPGRADE_SPECIAL_NONE)
                defaultOption = selectorButton.Value;
        }

        // Select the default button if nothing else is selected
        if (!selectedAButton && defaultOption != null)
        {
            defaultOption.Selected = true;
        }
    }

    private void ReleaseTooltips()
    {
        if (registeredTooltips)
            ToolTipManager.Instance.ClearToolTips(GetTooltipGroup());

        registeredTooltips = false;
    }

    private string GetTooltipGroup()
    {
        return $"organelleUpgradeGUI_{GetInstanceId()}";
    }
}
