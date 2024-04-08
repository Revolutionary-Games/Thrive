using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Info and control popup for a <see cref="SpaceFleet"/>
/// </summary>
public partial class SpaceFleetInfoPopup : StrategicUnitScreen<SpaceFleet>
{
    [Signal]
    public delegate void OnConstructOptionSelectedEventHandler(SpaceFleet fleet);

    protected override void UpdateAll()
    {
    }

    protected override void RefreshShownData()
    {
        // TODO: refresh the shown data

        // TODO: show fleet status from the order queue (idle or performing some order)
    }

    protected override IEnumerable<string> ListSubUnits()
    {
        return UnitEvenWhileClosed?.Ships.Select(s => s.Name) ?? Array.Empty<string>();
    }

    protected override void OnMoveStart()
    {
        // TODO: turn cursor into a movement selector and make left click trigger movement for one click
        // TODO: for controller input this needs a bit more complicated UX
    }

    protected override void OnConstructStart()
    {
        var target = OpenedForUnit;
        if (target != null)
        {
            EmitSignal(SignalName.OnConstructOptionSelected, target);
        }
        else
        {
            base.OnConstructStart();
        }
    }
}
