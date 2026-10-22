namespace ThriveTest.General.Utils.Tests;

using System;
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
        Assert.Equal(expected, StringUtils.FormatAsRomanNumerals(number));
    }

    [Fact]
    public static void StringUtils_WrapTextToApproximateWidthDoesNotAllocateWhenNoWrappingIsNeeded()
    {
        var text = new string('x', 80);

        // With same assertion, this verifies the identity of the string is not changed
        Assert.Same(text, text.WrapTextToApproximateWidth());
        Assert.Same(string.Empty, string.Empty.WrapTextToApproximateWidth());
    }

    [Theory]
    [InlineData("abcdefgh xy zz", "abcdefgh xy\nzz")]
    [InlineData("abcdefghi xy zz", "abcdefghi\nxy zz")]
    [InlineData("123456789\tabc", "123456789\nabc")]
    [InlineData("12345678\n123", "12345678\n123")]
    [InlineData("one two\nthree\nfour", "one two\nthree\nfour")]
    [InlineData("unbreakableword one two three", "unbreakableword\none two\nthree")]
    public static void StringUtils_WrapTextToApproximateWidthCutsAtClosestWhitespace(string text, string expected)
    {
        Assert.Equal(expected, text.WrapTextToApproximateWidth(10));
    }

    [Fact]
    public static void StringUtils_WrapTextToApproximateWidthPreservesUnixLineEndingsAndBlankLines()
    {
        Assert.Equal("alpha beta\ngamma\nsecond\nline", "alpha beta gamma\nsecond line"
            .WrapTextToApproximateWidth(10));

        Assert.Equal("1234567890\nalpha\n\nshort", "1234567890 alpha\n\nshort"
            .WrapTextToApproximateWidth(10));
    }

    [Fact]
    public static void StringUtils_WrapTextToApproximateWidthDoesNotWrapTrailingWhitespaceBeforeNewline()
    {
        Assert.Equal("123456789 \nabc", "123456789 \nabc".WrapTextToApproximateWidth(10));
        Assert.Equal("123456789  \nabc", "123456789  \nabc".WrapTextToApproximateWidth(10));
    }

    [Fact]
    public static void StringUtils_WrapTextToApproximateWidthPreservesWindowsLineEndings()
    {
        Assert.Equal("alpha beta\ngamma\r\nsecond\nline", "alpha beta gamma\r\nsecond line"
            .WrapTextToApproximateWidth(10));

        var textWithoutWrapping = "short line\r\nsecond line";
        Assert.Same(textWithoutWrapping, textWithoutWrapping.WrapTextToApproximateWidth(20));
    }

    [Fact]
    public static void StringUtils_WrapTextToApproximateWidthLeavesUnbreakableTextAlone()
    {
        var text = "unbreakableword";

        Assert.Same(text, text.WrapTextToApproximateWidth(5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public static void StringUtils_WrapTextToApproximateWidthRejectsInvalidWidths(int width)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => "text".WrapTextToApproximateWidth(width));
    }

    // ReSharper restore StringLiteralTypo
}
