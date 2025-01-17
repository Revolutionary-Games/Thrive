using System;
using System.Collections.Generic;
using Godot;

public static class CellBodyPlanInternalCalculations
{
    public static Dictionary<Compound, float> GetTotalSpecificCapacity(IEnumerable<CellTemplate> cells,
        out float nominalCapacity)
    {
        nominalCapacity = 0.0f;

        var capacities = new Dictionary<Compound, float>();

        foreach (var cell in cells)
        {
            var totalNominalCap = MicrobeInternalCalculations.GetTotalNominalCapacity(cell.Organelles);
            nominalCapacity += totalNominalCap;

            MicrobeInternalCalculations.AddSpecificCapacity(cell.Organelles, capacities, totalNominalCap);
        }

        return capacities;
    }
}
