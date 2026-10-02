namespace ThriveTest.General.Utils.Tests;

using Xunit;

public class WorldSeedTests
{
    // Independent xxHash64 vectors for the 24-byte task encoding, including full-width IDs and generations.
    [Theory]
    [InlineData(0L, WorldSeed.Domain.AutoEvoModifySpecies, 0, 0L, 4500431172382057997L)]
    [InlineData(0L, WorldSeed.Domain.AutoEvoMigrateSpecies, 0, 0L, -7495222163240469744L)]
    [InlineData(-1L, WorldSeed.Domain.AutoEvoModifySpecies, 1, 11L, -8915163019853617060L)]
    [InlineData(long.MinValue, WorldSeed.Domain.AutoEvoMigrateSpecies, int.MaxValue, uint.MaxValue,
        -4052545058602171213L)]
    [InlineData(long.MaxValue, WorldSeed.Domain.AutoEvoModifySpecies, 0, int.MinValue, -8759288772347118965L)]
    [InlineData(4294967296L, WorldSeed.Domain.AutoEvoModifySpecies, 0, 11L, 4529398752378197944L)]
    [InlineData(0L, WorldSeed.Domain.AutoEvoModifySpecies, 0, 11L, 7928256305485563229L)]
    [InlineData(0L, WorldSeed.Domain.AutoEvoModifySpecies, 1, 11L, -544354814216685562L)]
    [InlineData(0L, WorldSeed.Domain.AutoEvoModifySpecies, 17, 11L, -1233420873794869867L)]
    [InlineData(0L, WorldSeed.Domain.AutoEvoModifySpecies, 0, 29L, 2016445410036470242L)]
    [InlineData(0L, WorldSeed.Domain.AutoEvoMigrateSpecies, 0, uint.MaxValue, 4297654792673649266L)]
    [InlineData(0L, WorldSeed.Domain.AutoEvoMigrateSpecies, 0, int.MaxValue, 4009755841671443739L)]
    [InlineData(0L, WorldSeed.Domain.AutoEvoModifySpecies, int.MinValue, 11L, 6921462797254511457L)]
    [InlineData(0L, WorldSeed.Domain.AutoEvoModifySpecies, 0, int.MaxValue, -4354479864924727703L)]
    public void TaskDerivationUsesStableIdentity(long seed, WorldSeed.Domain domain, int generation, long owner,
        long expected)
    {
        checked
        {
            Assert.Equal(expected, WorldSeed.Derive(seed, domain, generation, owner));
        }
    }

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
