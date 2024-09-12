using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Upgrade GUI for the vacuole that allows specializing it
/// </summary>
public partial class FlagellumUpgradeGUI : VBoxContainer, IOrganelleUpgrader
{
#pragma warning disable CA2213
    [Export]
    private Slider lengthSlider = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
    }

    public void OnStartFor(OrganelleTemplate organelle, GameProperties currentGame, float costMultiplier)
    {
        if (organelle.Upgrades?.CustomUpgradeData is FlagellumUpgrades flagellumUpgrades)
        {
            lengthSlider.Value = flagellumUpgrades.LengthFraction;
        }
    }

    public bool ApplyChanges(ICellEditorComponent editorComponent, OrganelleUpgrades organelleUpgrades)
    {
        organelleUpgrades.CustomUpgradeData = new FlagellumUpgrades((float)lengthSlider.Value);

        return true;
    }

    public Vector2 GetMinDialogSize()
    {
        return new Vector2(420, 135);
    }
}
