using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GdUnit4;
using Saving.Serializers;
using SharedBase.Archive;
using Xoshiro.PRNG64;
using static GdUnit4.Assertions;

/// <summary>
///   Protects world-seeded effects and the continuation of their private random streams after loading.
/// </summary>
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

    /// <summary>
    ///   Verifies that every registered random source repeats after creating an intervening world and responds to
    ///   high seed bits. It also checks nitrogen's first root allocation, catching incorrect seed and stream wiring.
    /// </summary>
    [TestCase]
    public void RegisteredStreamsRepeatAndUseTheFullWorldSeed()
    {
        var first = ReadSources(CreateWorld(0));
        var different = ReadSources(CreateWorld(1L << 32));
        var repeated = ReadSources(CreateWorld(0));

        // Nitrogen takes the first allocation from the event root; it must not use the reserved nitrogen domain.
        var root = new XoShiRo256starstar(WorldSeed.Derive(0, WorldSeed.Domain.WorldEvents));
        AssertState(first[0].Random, ReadState(new XoShiRo256starstar(root.Next64())));

        for (var i = 0; i < first.Count; ++i)
        {
            var initialState = ReadState(first[i].Random);
            AssertState(repeated[i].Random, initialState);
            AssertThat(ReadState(different[i].Random).SequenceEqual(initialState))
                .OverrideFailureMessage($"{first[i].Effect.GetType().Name} ignored the high world seed bits").IsFalse();
            AssertThat(first[i].Random).IsNotSame(repeated[i].Random);
            for (var draw = 0; draw < 3; ++draw)
                AssertThat(first[i].Random.Next64U()).IsEqual(repeated[i].Random.Next64U());
        }
    }

    /// <summary>
    ///   Verifies repeatable high and low nitrogen corrections despite unrelated random draws, protecting nitrogen's
    ///   seeded runtime stream while ensuring the correction runs and leaves the other effect streams untouched.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void NitrogenCorrectionUsesItsOwnSeededStream(bool aboveMaximum)
    {
        var nitrogen = aboveMaximum ? Constants.MAX_NITROGEN_LEVEL + 0.1f : Constants.SOFT_MIN_NITROGEN_LEVEL * 0.5f;
        AssertThat(nitrogen > 0 && nitrogen < 1).IsTrue();
        var actual = CreateWorld(-1);
        var expected = CreateWorld(-1);
        var actualPatch = PrepareNitrogenPatch(actual, nitrogen);
        var expectedPatch = PrepareNitrogenPatch(expected, nitrogen);
        var sources = ReadSources(actual);
        var controls = ReadSources(expected);

        foreach (var source in sources.Skip(1))
        {
            for (var draw = 0; draw < 3; ++draw)
                source.Random.Next64U();
        }

        var snapshots = sources.Select(source => ReadState(source.Random)).ToArray();
        sources[0].Effect.OnTimePassed(1, 1);
        controls[0].Effect.OnTimePassed(1, 1);

        var corrected = ReadGas(actualPatch, Compound.Nitrogen);
        AssertThat(aboveMaximum ? corrected < nitrogen : corrected > nitrogen)
            .OverrideFailureMessage("The fixture must exercise a nitrogen correction").IsTrue();
        foreach (var compound in new[] { Compound.Nitrogen, Compound.Oxygen, Compound.Carbondioxide })
            AssertThat(ReadGas(actualPatch, compound)).IsEqual(ReadGas(expectedPatch, compound));

        AssertThat(ReadState(sources[0].Random).SequenceEqual(snapshots[0]))
            .OverrideFailureMessage("Nitrogen must consume its registered random stream").IsFalse();
        AssertState(sources[0].Random, ReadState(controls[0].Random));
        for (var i = 1; i < sources.Count; ++i)
            AssertState(sources[i].Random, snapshots[i]);
    }

    /// <summary>
    ///   Verifies that loading restores advanced effect streams and their world ownership, protecting saved games
    ///   from restarting randomness through the new seeded constructors.
    /// </summary>
    [TestCase]
    public void ArchiveRestoresSavedSourcesInsteadOfDerivingThemAgain()
    {
        var original = CreateWorld(0);
        var sources = ReadSources(original);
        foreach (var source in sources)
            source.Random.Next64U();
        var savedStates = sources.Select(source => ReadState(source.Random)).ToArray();

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
        AssertThat(loaded!.WorldSettings.Seed).IsEqual(original.WorldSettings.Seed);

        var restored = ReadSources(loaded);
        for (var i = 0; i < restored.Count; ++i)
        {
            AssertThat(restored[i].Random).IsNotSame(sources[i].Random);
            AssertState(restored[i].Random, savedStates[i]);

            // Full-width draws avoid introducing a cached-half restoration contract here.
            for (var draw = 0; draw < 3; ++draw)
                AssertThat(restored[i].Random.Next64U()).IsEqual(sources[i].Random.Next64U());
        }
    }

    private static GameWorld CreateWorld(long seed)
    {
        return GameProperties.StartNewMicrobeGame(new WorldGenerationSettings
        {
            Seed = seed,
            WorldSize = WorldGenerationSettings.WorldSizeEnum.Small,
        }).GameWorld;
    }

    private static Patch PrepareNitrogenPatch(GameWorld world, float nitrogen)
    {
        var neutral = (Constants.SOFT_MIN_NITROGEN_LEVEL + Constants.MAX_NITROGEN_LEVEL) * 0.5f;
        AssertThat(neutral > Constants.SOFT_MIN_NITROGEN_LEVEL && neutral < Constants.MAX_NITROGEN_LEVEL).IsTrue();
        var patch = world.Map.CurrentPatch!;
        foreach (var other in world.Map.Patches.Values)
        {
            foreach (var compound in other.Biome.ChangeableCompounds.Keys.ToArray())
            {
                if (SimulationParameters.Instance.GetCompoundDefinition(compound).IsGas)
                    other.Biome.ModifyLongTermCondition(compound, default);
            }

            var amount = other == patch ? nitrogen : neutral;
            other.Biome.ModifyLongTermCondition(Compound.Nitrogen, new BiomeCompoundProperties { Ambient = amount });
            other.Biome.ModifyLongTermCondition(Compound.Oxygen,
                new BiomeCompoundProperties { Ambient = (1 - amount) * 0.5f });
            other.Biome.ModifyLongTermCondition(Compound.Carbondioxide,
                new BiomeCompoundProperties { Ambient = (1 - amount) * 0.5f });
        }

        AssertThat(ReadGas(patch, Compound.Nitrogen)).IsEqual(nitrogen);
        return patch;
    }

    private static List<(IWorldEffect Effect, XoShiRo256starstar Random)> ReadSources(GameWorld world)
    {
        // Observe the actual registered sources without sampling them or adding a production test interface.
        var effects = ReadField<List<IWorldEffect>>(world.TimedEffects, "effects");
        var sources = effects.Where(effect => effect.GetType().GetField("random",
                BindingFlags.Instance | BindingFlags.NonPublic)?.FieldType == typeof(XoShiRo256starstar))
            .Select(effect => (Effect: effect, Random: ReadField<XoShiRo256starstar>(effect, "random"))).ToList();

        AssertThat(sources.Count).IsEqual(ExpectedOwners.Length);
        AssertThat(sources.Select(source => source.Random).Distinct().Count()).IsEqual(sources.Count);
        for (var i = 0; i < sources.Count; ++i)
        {
            AssertThat(sources[i].Effect.GetType()).IsEqual(ExpectedOwners[i]);
            AssertThat(ReadField<GameWorld>(sources[i].Effect, "targetWorld")).IsSame(world);
        }

        return sources;
    }

    private static ulong[] ReadState(XoShiRo256starstar random)
    {
        return Enumerable.Range(0, 4).Select(i => ReadField<ulong>(random, "s" + i)).ToArray();
    }

    private static void AssertState(XoShiRo256starstar random, ulong[] expected)
    {
        AssertThat(ReadState(random).SequenceEqual(expected)).IsTrue();
    }

    private static T ReadField<T>(object instance, string name)
    {
        return (T)(instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(instance) ?? throw new InvalidOperationException($"Missing field {name}"));
    }

    private static float ReadGas(Patch patch, Compound compound)
    {
        AssertThat(patch.Biome.TryGetCompound(compound, CompoundAmountType.Biome, out var amount)).IsTrue();
        return amount.Ambient;
    }
}
