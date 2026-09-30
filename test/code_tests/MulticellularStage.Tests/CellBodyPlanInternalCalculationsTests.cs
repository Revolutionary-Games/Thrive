namespace ThriveTest.MulticellularStage.Tests;

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

        // Higher values are slower, so this means that colony rotation is slower than single cell rotation
        Assert.True(colonyRotationSpeed > singleCellRotationSpeed);
    }
}
