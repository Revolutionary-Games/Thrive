using System;
using System.Collections.Generic;
using Components;
using Godot;

public static class CellBodyPlanInternalCalculations
{
    public static Dictionary<Compound, float> GetTotalSpecificCapacity(IEnumerable<CellTemplate> cells,
        out float nominalCapacity)
    {
        nominalCapacity = 0.0f;

        var capacities = new Dictionary<Compound, float>();

        // TODO: Check if it's possible to do those calculations per cell type and multiply by the types' cell counts
        foreach (var cell in cells)
        {
            var totalNominalCap = MicrobeInternalCalculations.GetTotalNominalCapacity(cell.Organelles);
            nominalCapacity += totalNominalCap;

            MicrobeInternalCalculations.AddSpecificCapacity(cell.Organelles, capacities);
        }

        return capacities;
    }

    /// <summary>
    ///   Calculates a colony's speed. The algorithm is an approximation, but should be based on the one in
    ///   MicrobeMovementSystem.cs
    /// </summary>
    public static float CalculateSpeed(IReadOnlyList<HexWithData<CellTemplate>> cells)
    {
        var leader = cells[0].Data!;

        var speed = MicrobeInternalCalculations.CalculateSpeed(leader.Organelles, leader.MembraneType,
            leader.MembraneRigidity, leader.IsBacteria);

        if (cells.Count == 1)
            return speed;

        ModifyCellSpeedWithColony(ref speed, cells.Count);

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

    public static void ModifyCellSpeedWithColony(ref float speed, int cellCount)
    {
        // Multiplies the movement factor as if the colony has the normal microbe speed
        // Then it subtracts movement speed from 100% up to 75%(soft cap),
        // using a series that converges to 1 , value = (1/2 + 1/4 + 1/8 +.....) = 1 - 1/2^n
        // when specialized cells become a reality the cap could be lowered to encourage cell specialization
        // Note that the multiplier below was added as a workaround for colonies being faster than individual cells
        // TODO: a proper rebalance of the algorithm would be excellent to do

        speed *= cellCount * Constants.CELL_COLONY_MOVEMENT_FORCE_MULTIPLIER;
        var seriesValue = 1 - 1 / (float)Math.Pow(2, cellCount - 1);
        speed -= speed * 0.15f * seriesValue;
    }

    /// <summary>
    ///   Calculates a colony's rotation speed. The code here should be based on the algorithm in
    ///   <see cref="MicrobeColonyHelpers.CalculateRotationSpeed"/>
    /// </summary>
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
