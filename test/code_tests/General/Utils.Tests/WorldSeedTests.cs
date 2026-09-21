namespace ThriveTest.General.Utils.Tests;

using Xunit;

public class WorldSeedTests
{
    // Independently calculated from the 12 encoded bytes using a separate xxHash64 reference.
    // 0 and 2^32 have identical low 32 bits; -1 and MaxValue differ only in the highest bit.
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
        // The contract must also work when a caller uses checked arithmetic.
        checked
        {
            Assert.Equal(events, WorldSeed.Derive(seed, WorldSeed.Domain.WorldEvents));
            Assert.Equal(nitrogen, WorldSeed.Derive(seed, WorldSeed.Domain.NitrogenControl));
        }
    }
}
