namespace ThriveTest.General.Utils.Tests;

using Xunit;

public class StringUtilsTests
{
    // ReSharper disable StringLiteralTypo
    [Theory]
    [InlineData(1, "I")]
    [InlineData(3, "III")]
    [InlineData(4, "IV")]
    [InlineData(7, "VII")]
    [InlineData(14, "XIV")]
    [InlineData(39, "XXXIX")]
    [InlineData(246, "CCXLVI")]
    [InlineData(789, "DCCLXXXIX")]
    [InlineData(2421, "MMCDXXI")]
    [InlineData(160, "CLX")]
    [InlineData(207, "CCVII")]
    [InlineData(1009, "MIX")]
    [InlineData(1066, "MLXVI")]
    [InlineData(1776, "MDCCLXXVI")]
    [InlineData(1918, "MCMXVIII")]
    [InlineData(1944, "MCMXLIV")]
    [InlineData(2023, "MMXXIII")]
    public static void StringUtils_RomanFormatIsCorrect(int number, string expected)
    {
        // ReSharper restore StringLiteralTypo

        Assert.Equal(expected, StringUtils.FormatAsRomanNumerals(number));
    }
}
