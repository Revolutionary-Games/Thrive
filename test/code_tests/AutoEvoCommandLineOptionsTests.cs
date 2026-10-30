using System;
using Xunit;

/// <summary>
///   Command-line requests are validated before any world is constructed.
/// </summary>
public class AutoEvoCommandLineOptionsTests
{
    /// <summary>
    ///   Verifies that an omitted count requests one generation and an omitted seed remains unset,
    ///   protecting the world settings default from an unintended fixed seed.
    /// </summary>
    [Fact]
    public void DefaultsLeaveSeedToWorldSettings()
    {
        var options = AutoEvoCommandLineOptions.Parse(new[] { "--auto-evo" });
        Assert.Equal(1, options.Generations);
        Assert.Null(options.Seed);
    }

    /// <summary>
    ///   Verifies rejection of zero, non-integral and overflowing counts,
    ///   protecting the positive integer contract before simulation starts.
    /// </summary>
    [Theory]
    [InlineData("0")]
    [InlineData("1.5")]
    [InlineData("2147483648")]
    public void RejectsInvalidGenerationCount(string count)
    {
        Assert.Throws<ArgumentException>(() =>
            AutoEvoCommandLineOptions.Parse(new[] { "--auto-evo", "--generations=" + count }));
    }

    /// <summary>
    ///   Verifies rejection of invalid seeds, a missing value and an unknown option,
    ///   protecting numeric parsing and option-name validation.
    /// </summary>
    [Theory]
    [InlineData("--seed=9223372036854775808")]
    [InlineData("--seed=text")]
    [InlineData("--seed")]
    [InlineData("--generation=2")]
    public void RejectsMalformedOptions(string argument)
    {
        Assert.Throws<ArgumentException>(() =>
            AutoEvoCommandLineOptions.Parse(new[] { "--auto-evo", argument }));
    }

    /// <summary>
    ///   Verifies that the full signed seed range and positive generation boundaries are preserved,
    ///   protecting valid input against truncation or overly restrictive validation.
    /// </summary>
    [Theory]
    [InlineData("-9223372036854775808", "1", long.MinValue, 1)]
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

    /// <summary>
    ///   Verifies rejection of repeated valued options and the command switch,
    ///   protecting duplicate detection before the switch-specific early continuation.
    /// </summary>
    [Theory]
    [InlineData("--seed=1", "--seed=2")]
    [InlineData("--auto-evo", "--auto-evo")]
    public void RejectsDuplicateOptions(string first, string second)
    {
        Assert.Throws<ArgumentException>(() => AutoEvoCommandLineOptions.Parse(new[] { first, second }));
    }
}
