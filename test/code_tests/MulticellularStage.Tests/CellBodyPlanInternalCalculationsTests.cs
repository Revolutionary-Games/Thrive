namespace ThriveTest.MulticellularStage.Tests;

using Xunit;

public class CellBodyPlanInternalCalculationsTests
{
    [Fact]
    public void CalculateFinalColonyRotation_AppliesCellCountAsPenaltyAndActomyosinAsBonus()
    {
        const float averageCellRotationSpeed = 2.0f;

        var singleCellRotationSpeed =
            CellBodyPlanInternalCalculations.CalculateFinalColonyRotation(averageCellRotationSpeed, 0, 1);
        var colonyRotationSpeed =
            CellBodyPlanInternalCalculations.CalculateFinalColonyRotation(averageCellRotationSpeed, 0, 2);
        var rotationSpeedWithActomyosin =
            CellBodyPlanInternalCalculations.CalculateFinalColonyRotation(averageCellRotationSpeed, 1, 1);

        // Higher values are slower, so this means that colony rotation is slower than single cell rotation
        Assert.True(colonyRotationSpeed > singleCellRotationSpeed);

        // But actomyosin is faster than single cell rotation and the colony rotation
        Assert.True(rotationSpeedWithActomyosin < singleCellRotationSpeed);
        Assert.True(rotationSpeedWithActomyosin < colonyRotationSpeed);
    }
}
