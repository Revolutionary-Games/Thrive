using System.Collections.Generic;
using System.Linq;

/// <summary>
///   Info and control popup for a <see cref="SpaceFleet"/>
/// </summary>
public class SpaceFleetInfoPopup : StrategicUnitScreen<SpaceFleet>
{
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
        return managedUnit!.Ships.Select(s => s.Name);
    }
}
