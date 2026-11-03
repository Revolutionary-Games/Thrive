namespace ThriveTest.General.Utils.Tests;

using Xunit;

public class WorldSeedTests
{
    /// <summary>
    ///   Verifies task seed derivation against independent xxHash64 vectors for the 20-byte identity encoding,
    ///   protecting field order, domain separation and preservation of the full root seed and owner ID.
    /// </summary>
    [Theory]
    [InlineData(0L, WorldSeed.Domain.AutoEvoModifySpecies, 0L, -9183548790926087643L)]
    [InlineData(0L, WorldSeed.Domain.AutoEvoMigrateSpecies, 0L, -7054944205724847261L)]
    [InlineData(long.MinValue, WorldSeed.Domain.AutoEvoMigrateSpecies, uint.MaxValue, 7156370087293823420L)]
    [InlineData(long.MaxValue, WorldSeed.Domain.AutoEvoModifySpecies, int.MinValue, -1472908689342849021L)]
    [InlineData(4294967296L, WorldSeed.Domain.AutoEvoModifySpecies, 11L, 2689368312846164605L)]
    public void TaskDerivationUsesStableIdentity(long seed, WorldSeed.Domain domain, long owner, long expected)
    {
        checked
        {
            Assert.Equal(expected, WorldSeed.Derive(seed, domain, owner));
        }
    }

    /// <summary>
    ///   Verifies the stable seed encoding and domain identifiers against independently calculated xxHash64 vectors,
    ///   protecting signed and high seed bits from truncation and keeping the two random domains distinct.
    /// </summary>
    [Theory]
    [InlineData(0L, -233534761883721657L, 6913772110171421832L)]
    [InlineData(-1L, -7109989232700394914L, -2392893625113065920L)]
    [InlineData(long.MinValue, -3075024637765545736L, 415512700360381126L)]
    [InlineData(long.MaxValue, 4139772185886876499L, -2799585024043673938L)]
    [InlineData(1L, 3135043923019394854L, 4108857273318351831L)]
    [InlineData(4294967296L, 2107482195430559446L, -2228275848591121815L)]
    [InlineData(0x123456789ABCDEFL, -5421809425841855491L, -832912001464073923L)]
    public void DerivationUsesFullSeedAndStableDomains(long seed, long events, long nitrogen)
    {
        Assert.Equal(events, WorldSeed.Derive(seed, WorldSeed.Domain.WorldEvents));
        Assert.Equal(nitrogen, WorldSeed.Derive(seed, WorldSeed.Domain.NitrogenControl));
    }
}
