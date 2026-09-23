using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GdUnit4;
using Godot;
using Saving.Serializers;
using SharedBase.Archive;
using Xoshiro.PRNG64;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class WorldInitializationRandomTests
{
    private static readonly Type[] ExpectedOwners =
    [
        typeof(NitrogenControlEffect), typeof(GlobalGlaciationEvent), typeof(MeteorImpactEvent),
        typeof(UnderwaterVentEruptionEvent), typeof(RunoffEvent), typeof(UpwellingEvent),
        typeof(CurrentDilutionEvent), typeof(PatchEventsManager),
    ];

    // Nitrogen's independently derived seed, followed by the event root's seven Next64 results in registration order.
    // These are fixed vectors, not values obtained by calling WorldSeed from the test.
    private static readonly long[] ZeroSeeds =
    [
        6913772110171421832, 2775066380116812634, 589628802888867105, 6955380975589419051,
        5725177324026221672, 1060377002406057009, 8453713671770385678, 4863178692786053979,
    ];

    private static readonly long[] NegativeSeeds =
    [
        -2392893625113065920, 7212019949885252386, 4769744258949206495, 3789567500142275836,
        1013020125375984558, 1581183057121051742, 4865375162978162573, 6235756070902111663,
    ];

    private static readonly long[] MinSeeds =
    [
        415512700360381126, 835522343545287004, 1462590641609672119, 8080147206919308679,
        3266112573045535063, 5447125889997485514, 2240027581737230392, 7061791768083211253,
    ];

    private static readonly long[] MaxSeeds =
    [
        -2799585024043673938, 1169022852851441421, 1401086606840350939, 2089524601541456652,
        4480458147477068645, 5716312261470279984, 1445290130822436306, 2259414426884854175,
    ];

    [TestCase(false)]
    [TestCase(true)]
    public void NewGamesBindAllEightSourcesIncludingFreshAToBToA(bool explicitStartingSpecies)
    {
        foreach (var seed in new[] { 0L, -1L, 0L, long.MinValue, long.MaxValue })
        {
            var world = CreateWorld(seed, explicitStartingSpecies);
            var sources = ReadSources(world);
            var expectedSeeds = SeedsFor(seed);
            for (var i = 0; i < sources.Count; ++i)
            {
                AssertState(sources[i].Random, ReadState(new XoShiRo256starstar(expectedSeeds[i])));
                var state = ReadState(sources[i].Random);
                GD.Print($"World RNG: seed={seed}, explicitSpecies={explicitStartingSpecies}, " +
                    $"owner={sources[i].Effect.GetType().Name}, state={string.Join(',', state)}");
            }
        }
    }

    [TestCase]
    public void RecordingActualSourcesDoesNotAdvanceOrShareTheirStreams()
    {
        var observed = ReadSources(CreateWorld(0));
        var unobserved = ReadSources(CreateWorld(0));
        var snapshot = observed.Select(source => ReadState(source.Random)).ToArray();
        AssertThat(snapshot.Length).IsEqual(8);

        for (var i = 0; i < observed.Count; ++i)
        {
            // Repeated observation never samples. Check against a separate fresh world and a fixed seed fixture.
            AssertState(observed[i].Random, snapshot[i]);
            var expectedNext = new XoShiRo256starstar(ZeroSeeds[i]).Next64U();
            AssertThat(observed[i].Random.Next64U()).IsEqual(expectedNext);
            AssertThat(unobserved[i].Random.Next64U()).IsEqual(expectedNext);
            for (var untouched = i + 1; untouched < observed.Count; ++untouched)
                AssertState(observed[untouched].Random, snapshot[untouched]);
        }
    }

    // These expected environment values include the real gas normalization, not just the nitrogen delta.
    // Seed 0's first two nitrogen floats are 0.5549342 and 0.8754634.
    [TestCase(0.875f, 0.0625f, 0.0625f, 0.85050613f, 0.074746944f, 0.074746944f, 0.8754634f)]
    [TestCase(0.125f, 0.375f, 0.5f, 0.229792f, 0.33008912f, 0.44011885f, 0.8754634f)]
    [TestCase(0.5f, 0.25f, 0.25f, 0.5f, 0.25f, 0.25f, 0.5549342f)]
    [TestCase(0.3f, 0.2f, 0.5f, 0.3f, 0.2f, 0.5f, 0.5549342f)]
    [TestCase(0.75f, 0.125f, 0.125f, 0.75f, 0.125f, 0.125f, 0.5549342f)]

    // Leave room for other gases: retain them when lowering nitrogen, decay them when raising it, and leave
    // the normal branch unchanged. The low-nitrogen case reduces the other gas volume from 0.25 to 0.23.
    [TestCase(0.875f, 0.03125f, 0.03125f, 0.85050613f, 0.037373472f, 0.037373472f, 0.8754634f)]
    [TestCase(0.125f, 0.25f, 0.375f, 0.23390993f, 0.22400294f, 0.33600442f, 0.8754634f)]
    [TestCase(0.5f, 0.125f, 0.125f, 0.5f, 0.125f, 0.125f, 0.5549342f)]
    public void NitrogenUsesItsBoundStreamAndPreservesEnvironmentalRules(float nitrogen, float oxygen,
        float carbonDioxide, float expectedNitrogen, float expectedOxygen, float expectedCarbonDioxide,
        float expectedNextFloat)
    {
        var world = CreateWorld(0);
        var sources = ReadSources(world);
        var patch = world.Map.CurrentPatch!;
        AssertThat(patch.BiomeTemplate.GasVolume).IsEqual(1.0f);

        // Keep all other patches in the normal branch, so exactly the selected patch may draw a value.
        foreach (var other in world.Map.Patches.Values)
            SetGases(other, 0.5f, 0.25f, 0.25f);

        SetGases(patch, nitrogen, oxygen, carbonDioxide);
        sources[0].Effect.OnTimePassed(1, 1);

        AssertGas(patch, Compound.Nitrogen, expectedNitrogen);
        AssertGas(patch, Compound.Oxygen, expectedOxygen);
        AssertGas(patch, Compound.Carbondioxide, expectedCarbonDioxide);
        foreach (var other in world.Map.Patches.Values.Where(other => other != patch))
        {
            AssertGas(other, Compound.Nitrogen, 0.5f);
            AssertGas(other, Compound.Oxygen, 0.25f);
            AssertGas(other, Compound.Carbondioxide, 0.25f);
        }

        AssertThat(sources[0].Random.NextFloat()).IsEqual(expectedNextFloat);
        for (var i = 1; i < sources.Count; ++i)
            AssertState(sources[i].Random, ReadState(new XoShiRo256starstar(ZeroSeeds[i])));
    }

    [TestCase]
    public void ArchiveRestoresSavedSourcesInsteadOfDerivingThemAgain()
    {
        var original = CreateWorld(0);
        var sources = ReadSources(original);
        foreach (var source in sources)
            source.Random.Next64U();

        var savedStates = sources.Select(source => ReadState(source.Random)).ToArray();

        // If loading mistakenly derives sources from settings, this seed and the advanced states expose it.
        original.WorldSettings.Seed = -1;
        using var data = new MemoryStream();
        var manager = new ThriveArchiveManager();
        using var writer = new SArchiveMemoryWriter(data, manager, false);
        manager.OnStartNewWrite(writer);
        writer.WriteObject(original);
        manager.OnFinishWrite(writer);
        AssertThat(data.Length > 0).IsTrue();

        data.Position = 0;
        using var reader = new SArchiveMemoryReader(data, manager);
        manager.OnStartNewRead(reader);
        var loaded = reader.ReadObjectOrNull<GameWorld>();
        manager.OnFinishRead(reader);
        AssertThat(loaded).IsNotNull();
        AssertThat(loaded).IsNotSame(original);
        AssertThat(loaded!.WorldSettings.Seed).IsEqual(-1L);

        var restored = ReadSources(loaded);
        for (var i = 0; i < restored.Count; ++i)
        {
            AssertThat(restored[i].Random).IsNotSame(sources[i].Random);
            AssertState(restored[i].Random, savedStates[i]);

            // The current archive stores the four core words. Full cached-half continuation is a separate contract.
            var expected = new XoShiRo256starstar(ZeroSeeds[i]);
            expected.Next64U();
            AssertThat(restored[i].Random.Next64U()).IsEqual(expected.Next64U());
        }
    }

    private static GameWorld CreateWorld(long seed, bool explicitStartingSpecies = false)
    {
        var settings = new WorldGenerationSettings
        {
            Seed = seed,
            WorldSize = WorldGenerationSettings.WorldSizeEnum.Small,
        };

        MicrobeSpecies? species = null;
        if (explicitStartingSpecies)
        {
            species = new MicrobeSpecies(1, "Test", "Player")
            {
                IsBacteria = true,
                MembraneType = SimulationParameters.Instance.GetMembrane("single"),
            };
            species.Organelles.Add(new OrganelleTemplate(SimulationParameters.Instance.GetOrganelleType("cytoplasm"),
                new Hex(0, 0), 0));
        }

        return GameProperties.StartNewMicrobeGame(settings, startingSpecies: species).GameWorld;
    }

    private static long[] SeedsFor(long seed)
    {
        return seed switch
        {
            0 => ZeroSeeds,
            -1 => NegativeSeeds,
            long.MinValue => MinSeeds,
            long.MaxValue => MaxSeeds,
            _ => throw new ArgumentOutOfRangeException(nameof(seed)),
        };
    }

    private static List<(IWorldEffect Effect, XoShiRo256starstar Random)> ReadSources(GameWorld world)
    {
        var effects = ReadField<List<IWorldEffect>>(world.TimedEffects, "effects");
        var sources = effects.Where(effect => effect.GetType().GetField("random",
                BindingFlags.Instance | BindingFlags.NonPublic)?.FieldType == typeof(XoShiRo256starstar))
            .Select(effect => (Effect: effect, Random: ReadField<XoShiRo256starstar>(effect, "random"))).ToList();

        AssertThat(sources.Count).IsEqual(8);
        AssertThat(sources.Select(source => source.Random).Distinct().Count()).IsEqual(8);
        for (var i = 0; i < sources.Count; ++i)
        {
            AssertThat(sources[i].Effect.GetType()).IsEqual(ExpectedOwners[i]);
            AssertThat(ReadField<GameWorld>(sources[i].Effect, "targetWorld")).IsSame(world);
        }

        return sources;
    }

    private static ulong[] ReadState(XoShiRo256starstar random)
    {
        // Copy actual words without Next*, serialization, or retaining mutable state owned by the generator.
        return Enumerable.Range(0, 4).Select(i => ReadField<ulong>(random, "s" + i)).ToArray();
    }

    private static void AssertState(XoShiRo256starstar random, ulong[] expected)
    {
        var actual = ReadState(random);
        AssertThat(expected.Length).IsEqual(4);
        AssertThat(actual.Any(word => word != 0)).IsTrue();
        for (var i = 0; i < 4; ++i)
            AssertThat(actual[i]).IsEqual(expected[i]);
    }

    private static T ReadField<T>(object instance, string name)
    {
        return (T)(instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(instance) ?? throw new InvalidOperationException($"Missing field {name}"));
    }

    private static void SetGases(Patch patch, float nitrogen, float oxygen, float carbonDioxide)
    {
        foreach (var compound in patch.Biome.ChangeableCompounds.Keys.ToArray())
        {
            if (SimulationParameters.Instance.GetCompoundDefinition(compound).IsGas)
                patch.Biome.ModifyLongTermCondition(compound, default);
        }

        patch.Biome.ModifyLongTermCondition(Compound.Nitrogen, new BiomeCompoundProperties { Ambient = nitrogen });
        patch.Biome.ModifyLongTermCondition(Compound.Oxygen, new BiomeCompoundProperties { Ambient = oxygen });
        patch.Biome.ModifyLongTermCondition(Compound.Carbondioxide,
            new BiomeCompoundProperties { Ambient = carbonDioxide });
    }

    private static void AssertGas(Patch patch, Compound compound, float expected)
    {
        AssertThat(patch.Biome.TryGetCompound(compound, CompoundAmountType.Biome, out var amount)).IsTrue();
        AssertThat(amount.Ambient).IsEqualApprox(expected, 0.000001f);
    }
}
