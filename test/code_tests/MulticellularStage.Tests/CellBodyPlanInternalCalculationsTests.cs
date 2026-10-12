namespace ThriveTest.MulticellularStage.Tests;

using System;
using Xunit;

public class CellBodyPlanInternalCalculationsTests
{
    [Fact]
    public void CalculateFinalColonyRotation_AppliesCellCountAsPenalty()
    {
        const float averageCellRotationSpeed = 2.0f;

        var singleCellRotationSpeed =
            CellBodyPlanInternalCalculations.CalculateFinalColonyRotation(averageCellRotationSpeed, 1);
        var colonyRotationSpeed =
            CellBodyPlanInternalCalculations.CalculateFinalColonyRotation(averageCellRotationSpeed, 2);

        // Higher values are slower, so this means that colony rotation is slower than single cell rotation.
        Assert.True(colonyRotationSpeed > singleCellRotationSpeed);
    }

    [Fact]
    public void CalculateActomyosinRotationMultiplier_AppliesActomyosinCountAsBonus()
    {
        var zeroActomyosinBonus = CellBodyPlanInternalCalculations.CalculateActomyosinRotationMultiplier(0);
        var oneActomyosinBonus = CellBodyPlanInternalCalculations.CalculateActomyosinRotationMultiplier(1);
        var twoActomyosinBonus = CellBodyPlanInternalCalculations.CalculateActomyosinRotationMultiplier(2);
        // The higher the rotation value, the slower the rotation is,
        // but this bonus is used to multiply the denominator in the rotation calculation.

        // Having one Actomyosin provides a bonus, while 0 Actomyosin does not.
        Assert.True(Math.Abs(zeroActomyosinBonus - 1.0f) < Single.Epsilon);
        Assert.True(1 / zeroActomyosinBonus > 1 / oneActomyosinBonus);

        // Having two Actomyosins provides more of a bonus than having one.
        Assert.True(1 / oneActomyosinBonus > 1 / twoActomyosinBonus);
    }
}
