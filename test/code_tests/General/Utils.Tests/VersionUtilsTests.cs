namespace ThriveTest.General.Utils.Tests;

using Xunit;

public class VersionUtilsTests
{
    [Theory]
    [InlineData("1.2.3-pre-alpha", "1.2.3", -1)]
    [InlineData("1.2.3-rc1", "1.2.3-pre-alpha", 1)]
    [InlineData("1.2.3-rc1", "1.2.4-pre-alpha", -1)]
    [InlineData("3.2.1-pre-alpha", "1.2.3", 1)]
    [InlineData("1.2.3-alpha", "1.2.3-potato", -15)]
    [InlineData("1.2.3-alpha", "1.2.3-bat", -1)]
    [InlineData("1.2.3", "1.2.3.0", 0)]
    [InlineData("1.2.3.1", "1.2.3.0", 1)]
    [InlineData("0.5.3.1-alpha", "0.5.3.1", -1)]
    [InlineData("0.5.3.1-alpha", "0.5.3", 1)]
    public static void VersionUtils_ComparisonResultIsCorrect(string versionA, string versionB, int expectedResult)
    {
        Assert.Equal(expectedResult, VersionUtils.Compare(versionA, versionB));
    }
}
