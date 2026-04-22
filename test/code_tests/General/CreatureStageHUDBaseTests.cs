namespace ThriveTest.General;

using Xunit;

public class CreatureStageHUDBaseTests
{
    [Fact]
    public void CalculateDisplayedATPAmount_ShowsAlmostFullStorageAsFull()
    {
        Assert.Equal(10, ATPDisplayHelper.CalculateDisplayedATPAmount(9.05f, 10));
    }

    [Fact]
    public void CalculateDisplayedATPAmount_KeepsClearlyNonFullStorage()
    {
        Assert.Equal(8.9f, ATPDisplayHelper.CalculateDisplayedATPAmount(8.9f, 10));
    }

    [Fact]
    public void CalculateDisplayedATPAmount_UsesMinimumMarginForSmallStorage()
    {
        Assert.Equal(0.5f, ATPDisplayHelper.CalculateDisplayedATPAmount(0.41f, 0.5f));
        Assert.Equal(0.39f, ATPDisplayHelper.CalculateDisplayedATPAmount(0.39f, 0.5f));
    }
}
