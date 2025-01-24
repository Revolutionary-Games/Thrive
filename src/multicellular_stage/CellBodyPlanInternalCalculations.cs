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

    public static float CalculateSpeed(IReadOnlyList<HexWithData<CellTemplate>> cells)
    {
        var leader = cells[0].Data!;

        var speed = MicrobeInternalCalculations.CalculateSpeed(leader.Organelles, leader.MembraneType,
            leader.MembraneRigidity, leader.IsBacteria);

        if (cells.Count == 1)
            return speed;

        speed *= cells.Count * Constants.CELL_COLONY_MOVEMENT_FORCE_MULTIPLIER;
        var seriesValue = 1 - 1 / (float)Math.Pow(2, cells.Count - 1);
        speed -= speed * 0.15f * seriesValue;

        var massEstimate = 0.0f;

        var addedSpeed = 0.0f;

        foreach (var hex in cells)
        {
            var cell = hex.Data!;

            if (cell == leader)
                continue;

            foreach (var organelle in cell.Organelles)
            {
                massEstimate += organelle.Definition.Density * organelle.Definition.HexCount;

                if (!organelle.Definition.HasMovementComponent)
                    continue;

                var upgradeForce = 0.0f;

                if (organelle.Upgrades?.CustomUpgradeData is FlagellumUpgrades flagellumUpgrades)
                {
                    upgradeForce = Constants.FLAGELLA_MAX_UPGRADE_FORCE * flagellumUpgrades.LengthFraction;
                }

                var flagellumForce = (Constants.FLAGELLA_BASE_FORCE + upgradeForce)
                    * organelle.Definition.Components.Movement!.Momentum;

                if (!cell.IsBacteria)
                    flagellumForce *= Constants.EUKARYOTIC_MOVEMENT_FORCE_MULTIPLIER;

                addedSpeed += flagellumForce;
            }
        }

        return speed / cells.Count + addedSpeed / (massEstimate * 1.4f);
    }

    public static float CalculateRotationSpeed(IReadOnlyList<HexWithData<CellTemplate>> cells)
    {
        var leader = cells[0].Data!;

        var colonyRotation = MicrobeInternalCalculations
            .CalculateRotationSpeed(leader.Organelles);

        Vector3 leaderPosition = Hex.AxialToCartesian(leader.Position);

        foreach (var colonyMember in cells)
        {
            var distanceSquared = leaderPosition.DistanceSquaredTo(Hex.AxialToCartesian(colonyMember.Position));

            var memberRotation = MicrobeInternalCalculations
                    .CalculateRotationSpeed(colonyMember.Data!.Organelles)
                * (1 + 0.03f * distanceSquared);

            colonyRotation += memberRotation;
        }

        return colonyRotation / cells.Count;
    }
}
