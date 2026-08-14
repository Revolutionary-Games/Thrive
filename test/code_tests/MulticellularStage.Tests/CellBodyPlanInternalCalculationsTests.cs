namespace ThriveTest.MulticellularStage.Tests;

using Xunit;

public class CellBodyPlanInternalCalculationsTests
{
    [Fact]
    public void CalculateFinalColonyRotation_AppliesCellCountAsPenaltyAndActomyosinAsBonus()
    {
        const float averageCellRotationSpeed = 2.0f;

        var singleCellRotationSpeed = CellBodyPlanInternalCalculations.CalculateFinalColonyRotation(
            averageCellRotationSpeed, 0, 1);
        var colonyRotationSpeed = CellBodyPlanInternalCalculations.CalculateFinalColonyRotation(
            averageCellRotationSpeed, 0, 2);
        var rotationSpeedWithActomyosin = CellBodyPlanInternalCalculations.CalculateFinalColonyRotation(
            averageCellRotationSpeed, 1, 1);

        Assert.True(colonyRotationSpeed > singleCellRotationSpeed);
        Assert.True(rotationSpeedWithActomyosin < singleCellRotationSpeed);
        Assert.True(rotationSpeedWithActomyosin < colonyRotationSpeed);
    }
}
