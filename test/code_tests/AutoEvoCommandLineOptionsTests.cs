using System;
using Xunit;

/// <summary>
///   Command-line requests are validated before any world is constructed.
/// </summary>
public class AutoEvoCommandLineOptionsTests
{
    [Fact]
    public void DefaultsLeaveSeedToWorldSettings()
    {
        var options = AutoEvoCommandLineOptions.Parse(new[] { "--auto-evo" });
        Assert.Equal(1, options.Generations);
        Assert.Null(options.Seed);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("")]
    [InlineData("2147483648")]
    public void RejectsInvalidGenerationCount(string count)
    {
        Assert.Throws<ArgumentException>(() =>
            AutoEvoCommandLineOptions.Parse(new[] { "--auto-evo", "--generations=" + count }));
    }

    [Theory]
    [InlineData("--seed=9223372036854775808")]
    [InlineData("--seed=text")]
    [InlineData("--seed=")]
    [InlineData("--seed=-9223372036854775809")]
    [InlineData("--seed")]
    [InlineData("--generation=2")]
    [InlineData("--generations")]
    public void RejectsMalformedOptions(string argument)
    {
        Assert.Throws<ArgumentException>(() =>
            AutoEvoCommandLineOptions.Parse(new[] { "--auto-evo", argument }));
    }

    [Theory]
    [InlineData("123", "2", 123L, 2)]
    [InlineData("-123", "1", -123L, 1)]
    [InlineData("-9223372036854775808", "2", long.MinValue, 2)]
    [InlineData("9223372036854775807", "2147483647", long.MaxValue, int.MaxValue)]
    public void AcceptsExplicitSeedAndGenerationCount(string seed, string generations, long expectedSeed,
        int expectedGenerations)
    {
        var options = AutoEvoCommandLineOptions.Parse(new[]
        {
            "--auto-evo", "--seed=" + seed, "--generations=" + generations,
        });
        Assert.Equal(expectedGenerations, options.Generations);
        Assert.Equal(expectedSeed, options.Seed);
    }

    [Theory]
    [InlineData("--seed=1", "--seed=2")]
    [InlineData("--generations=1", "--generations=2")]
    [InlineData("--auto-evo", "--auto-evo")]
    public void RejectsDuplicateOptions(string first, string second)
    {
        Assert.Throws<ArgumentException>(() => AutoEvoCommandLineOptions.Parse(new[] { first, second }));
    }
}
