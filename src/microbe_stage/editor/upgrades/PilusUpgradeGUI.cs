using System.Collections.Generic;
using Godot;

public class PilusUpgradeGUI : VBoxContainer, IOrganelleUpgrader
{
    [Export]
    public NodePath? IsInjectisomeCheckboxPath;

#pragma warning disable CA2213
    private CheckBox isInjectisomeCheckbox = null!;
#pragma warning restore CA2213

    public override void _Ready()
    {
        isInjectisomeCheckbox = GetNode<CheckBox>(IsInjectisomeCheckboxPath);
    }

    public void OnStartFor(OrganelleTemplate organelle)
    {
        // Apply current upgrade values or defaults
        if (organelle.Upgrades?.CustomUpgradeData is PilusUpgrades configuration)
        {
            isInjectisomeCheckbox.Pressed = configuration.IsInjectisome;
        }
        else
        {
            isInjectisomeCheckbox.Pressed = false;
        }
    }

    public bool ApplyChanges(ICellEditorComponent editorComponent, OrganelleUpgrades organelleUpgrades)
    {
        organelleUpgrades.CustomUpgradeData = new PilusUpgrades(isInjectisomeCheckbox.Pressed);
        return true;
    }

    public Vector2 GetMinDialogSize()
    {
        return new Vector2(420, 135);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (IsInjectisomeCheckboxPath != null)
            {
                IsInjectisomeCheckboxPath.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
