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
///   Verifies world-seeded effects through repeatable outcomes and independent, saved random streams.
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

    [TestCase]
    public void NitrogenUsesTheWorldEffectSeedAllocationPolicy()
    {
        var actual = CreateWorld(0);
        var expected = CreateWorld(0);
        var actualPatch = PrepareNitrogenPatch(actual, 0.125f, 0.375f, 0.5f);
        var expectedPatch = PrepareNitrogenPatch(expected, 0.125f, 0.375f, 0.5f);

        // Check the agreed allocation policy through an actual gas correction, not a golden RNG state.
        var root = new XoShiRo256starstar(WorldSeed.Derive(0, WorldSeed.Domain.WorldEvents));
        new NitrogenControlEffect(expected, root.Next64()).OnTimePassed(1, 1);
        ReadSources(actual)[0].Effect.OnTimePassed(1, 1);
        AssertGas(actualPatch, Compound.Nitrogen, ReadGas(expectedPatch, Compound.Nitrogen));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SameSeedRepeatsWorldEventsIncludingFreshAToBToA(bool explicitStartingSpecies)
    {
        var previousWorlds = new Dictionary<long, GameWorld>();
        foreach (var seed in new[] { 0L, -1L, 0L, long.MinValue, long.MaxValue })
        {
            var actual = CreateWorld(seed, explicitStartingSpecies);
            var expected = CreateWorld(seed, explicitStartingSpecies);
            var sources = ReadSources(actual);
            var controls = ReadSources(expected);
            var initialStates = sources.Select(source => ReadState(source.Random)).ToArray();
            for (var i = 0; i < sources.Count; ++i)
                AssertState(controls[i].Random, initialStates[i]);
            var context = $"seed={seed}, explicitSpecies={explicitStartingSpecies}";
            AssertWorldResults(actual, expected, context + ", initial");

            bool sawEvent = false;
            for (var step = 0; step < 16; ++step)
            {
                AdvanceWorld(actual);
                AdvanceWorld(expected);
                AssertWorldResults(actual, expected, context + $", step={step}");
                sawEvent |= actual.Map.Patches.Values.Any(patch => patch.ActivePatchEvents.Count > 0);
            }

            // Equal worlds are not evidence if both skipped the random event paths.
            AssertThat(sawEvent).OverrideFailureMessage(context + ": no patch event was exercised").IsTrue();
            AssertThat(sources.Skip(1).Where((source, i) =>
                    !ReadState(source.Random).SequenceEqual(initialStates[i + 1])).Any())
                .OverrideFailureMessage(context + ": no event random stream advanced").IsTrue();
            if (previousWorlds.TryGetValue(seed, out var previous))
                AssertWorldResults(actual, previous, context + ", repeated after a different seed");
            previousWorlds[seed] = actual;
        }
    }

    [TestCase]
    public void DifferentWorldSeedsChangeEveryBoundRandomStream()
    {
        var baseline = ReadSources(CreateWorld(0));
        var different = ReadSources(CreateWorld(4294967296L));
        for (var i = 0; i < baseline.Count; ++i)
        {
            AssertThat(ReadState(baseline[i].Random).SequenceEqual(ReadState(different[i].Random)))
                .OverrideFailureMessage($"{baseline[i].Effect.GetType().Name} ignored the full world seed").IsFalse();
        }
    }

    [TestCase]
    public void RecordingActualSourcesDoesNotAdvanceOrShareTheirStreams()
    {
        var observed = ReadSources(CreateWorld(0));
        var unobserved = ReadSources(CreateWorld(0));
        var snapshots = observed.Select(source => ReadState(source.Random)).ToArray();

        for (var i = 0; i < observed.Count; ++i)
        {
            AssertState(observed[i].Random, snapshots[i]);
            AssertThat(observed[i].Random).IsNotSame(unobserved[i].Random);
            for (var draw = 0; draw < 3; ++draw)
                AssertThat(observed[i].Random.Next64U()).IsEqual(unobserved[i].Random.Next64U());
            snapshots[i] = ReadState(observed[i].Random);

            // Check earlier as well as later sources after advancing each private stream.
            for (var other = 0; other < observed.Count; ++other)
                AssertState(observed[other].Random, snapshots[other]);
        }
    }

    [TestCase(0.875f, 0.0625f, 0.0625f)]
    [TestCase(0.125f, 0.375f, 0.5f)]
    [TestCase(0.5f, 0.25f, 0.25f)]
    [TestCase(Constants.SOFT_MIN_NITROGEN_LEVEL, 0.2f, 0.5f)]
    [TestCase(Constants.MAX_NITROGEN_LEVEL, 0.125f, 0.125f)]
    [TestCase(0.875f, 0.03125f, 0.03125f)]
    [TestCase(0.125f, 0.25f, 0.375f)]
    [TestCase(0.5f, 0.125f, 0.125f)]
    public void NitrogenRepeatsItsCorrectionWithoutConsumingOtherStreams(float nitrogen, float oxygen,
        float carbonDioxide)
    {
        foreach (var seed in new[] { 0L, -1L, long.MinValue, long.MaxValue })
        {
            var actual = CreateWorld(seed);
            var expected = CreateWorld(seed);
            var actualPatch = PrepareNitrogenPatch(actual, nitrogen, oxygen, carbonDioxide);
            PrepareNitrogenPatch(expected, nitrogen, oxygen, carbonDioxide);
            var sources = ReadSources(actual);
            var controls = ReadSources(expected);

            // Unrelated consumption must not affect nitrogen's own correction.
            foreach (var source in sources.Skip(1))
            {
                for (var draw = 0; draw < 3; ++draw)
                    source.Random.Next64U();
            }

            var snapshots = sources.Select(source => ReadState(source.Random)).ToArray();
            sources[0].Effect.OnTimePassed(1, 1);
            controls[0].Effect.OnTimePassed(1, 1);
            AssertWorldResults(actual, expected, $"nitrogen={nitrogen}, seed={seed}");
            AssertNitrogenRules(actualPatch, nitrogen, oxygen, carbonDioxide);
            foreach (var other in actual.Map.Patches.Values.Where(other => other != actualPatch))
            {
                AssertGas(other, Compound.Nitrogen, 0.5f);
                AssertGas(other, Compound.Oxygen, 0.25f);
                AssertGas(other, Compound.Carbondioxide, 0.25f);
            }

            bool correctionNeeded = nitrogen < Constants.SOFT_MIN_NITROGEN_LEVEL ||
                nitrogen > Constants.MAX_NITROGEN_LEVEL;
            AssertThat(ReadState(sources[0].Random).SequenceEqual(snapshots[0])).IsEqual(!correctionNeeded);
            AssertThat(sources[0].Random.NextFloat()).IsEqual(controls[0].Random.NextFloat());
            for (var i = 1; i < sources.Count; ++i)
                AssertState(sources[i].Random, snapshots[i]);
        }
    }

    [TestCase]
    public void ArchiveRestoresSavedSourcesInsteadOfDerivingThemAgain()
    {
        var original = CreateWorld(0);
        var sources = ReadSources(original);
        foreach (var source in sources)
            source.Random.Next64U();
        var savedStates = sources.Select(source => ReadState(source.Random)).ToArray();

        // A changed settings seed and advanced streams expose accidental re-derivation on load.
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

            // No cached half was populated before saving. Full cached-half restoration is a separate contract.
            for (var draw = 0; draw < 3; ++draw)
                AssertThat(restored[i].Random.Next64U()).IsEqual(sources[i].Random.Next64U());
        }

        for (var step = 0; step < 3; ++step)
        {
            AdvanceWorld(original);
            AdvanceWorld(loaded);
            AssertWorldResults(loaded, original, $"after loading, step={step}");
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

    private static void AdvanceWorld(GameWorld world)
    {
        // Match gameplay's patch history recording, needed when a multi-generation event finishes.
        foreach (var patch in world.Map.Patches.Values)
            patch.RecordSnapshot(true);
        world.OnTimePassed(1);
    }

    private static void AssertWorldResults(GameWorld actual, GameWorld expected, string context)
    {
        AssertThat(actual.TotalPassedTime).IsEqual(expected.TotalPassedTime);
        AssertThat(actual.Map.Patches.Keys.Order().SequenceEqual(expected.Map.Patches.Keys.Order()))
            .OverrideFailureMessage(context + ": patch identities differ").IsTrue();
        foreach (var (id, patch) in actual.Map.Patches)
        {
            var other = expected.Map.Patches[id];
            var patchContext = context + $", patch={id}";
            AssertThat(patch.BiomeType).IsEqual(other.BiomeType);
            AssertThat(patch.Background).IsEqual(other.Background);
            AssertThat(patch.Biome.Compounds.OrderBy(pair => pair.Key)
                    .SequenceEqual(other.Biome.Compounds.OrderBy(pair => pair.Key)))
                .OverrideFailureMessage(patchContext + ": compound conditions differ").IsTrue();
            AssertThat(patch.Biome.Chunks.OrderBy(pair => pair.Key)
                    .Select(pair => (pair.Key, pair.Value.Density))
                    .SequenceEqual(other.Biome.Chunks.OrderBy(pair => pair.Key)
                        .Select(pair => (pair.Key, pair.Value.Density))))
                .OverrideFailureMessage(patchContext + ": chunk availability differs").IsTrue();
            AssertThat(patch.ActivePatchEvents.OrderBy(pair => pair.Key)
                    .Select(pair => (pair.Key, pair.Value.SunlightAmbientMultiplier,
                        pair.Value.TemperatureAmbientChange, pair.Value.TemperatureAmbientFixedValue,
                        pair.Value.CustomTooltip))
                    .SequenceEqual(other.ActivePatchEvents.OrderBy(pair => pair.Key)
                        .Select(pair => (pair.Key, pair.Value.SunlightAmbientMultiplier,
                            pair.Value.TemperatureAmbientChange, pair.Value.TemperatureAmbientFixedValue,
                            pair.Value.CustomTooltip))))
                .OverrideFailureMessage(patchContext + ": active event effects differ").IsTrue();
            AssertEvents(patch.EventsLog, other.EventsLog, patchContext);
        }

        AssertThat(actual.EventsLog.Keys.Order().SequenceEqual(expected.EventsLog.Keys.Order()))
            .OverrideFailureMessage(context + ": global event times differ").IsTrue();
        foreach (var (time, events) in actual.EventsLog)
            AssertEvents(events, expected.EventsLog[time], context + $", time={time}");
    }

    private static void AssertEvents(IReadOnlyList<GameEventDescription> actual,
        IReadOnlyList<GameEventDescription> expected, string context)
    {
        // LocalizedString equality compares translation keys and arguments, not rendered text.
        AssertThat(actual.Select(item => (item.Description, item.IconPath, item.Highlighted, item.ShowInReport))
                .SequenceEqual(expected.Select(item =>
                    (item.Description, item.IconPath, item.Highlighted, item.ShowInReport))))
            .OverrideFailureMessage(context + ": event records differ").IsTrue();
    }

    private static Patch PrepareNitrogenPatch(GameWorld world, float nitrogen, float oxygen, float carbonDioxide)
    {
        foreach (var other in world.Map.Patches.Values)
            SetGases(other, 0.5f, 0.25f, 0.25f);
        var patch = world.Map.CurrentPatch!;
        SetGases(patch, nitrogen, oxygen, carbonDioxide);
        return patch;
    }

    private static void AssertNitrogenRules(Patch patch, float nitrogen, float oxygen, float carbonDioxide)
    {
        var actualNitrogen = ReadGas(patch, Compound.Nitrogen);
        var actualOxygen = ReadGas(patch, Compound.Oxygen);
        var actualCarbonDioxide = ReadGas(patch, Compound.Carbondioxide);
        var otherGases = 1 - nitrogen - oxygen - carbonDioxide;
        var actualOtherGases = 1 - actualNitrogen - actualOxygen - actualCarbonDioxide;

        if (nitrogen > Constants.MAX_NITROGEN_LEVEL)
        {
            AssertThat(actualNitrogen < nitrogen).IsTrue();
            AssertThat(actualOxygen > oxygen && actualCarbonDioxide > carbonDioxide).IsTrue();
        }
        else if (nitrogen < Constants.SOFT_MIN_NITROGEN_LEVEL)
        {
            AssertThat(actualNitrogen > nitrogen).IsTrue();
            AssertThat(actualOxygen < oxygen && actualCarbonDioxide < carbonDioxide).IsTrue();
            otherGases *= 1 - Constants.OTHER_GASES_DECAY_SPEED;
        }
        else
        {
            AssertGas(patch, Compound.Nitrogen, nitrogen);
            AssertGas(patch, Compound.Oxygen, oxygen);
            AssertGas(patch, Compound.Carbondioxide, carbonDioxide);
        }

        // Changing nitrogen must preserve the relative proportions of the other named gases.
        AssertThat(actualOxygen / actualCarbonDioxide).IsEqualApprox(oxygen / carbonDioxide, 0.00001f);
        if (otherGases > MathUtils.EPSILON)
        {
            AssertThat(actualOtherGases > 0).IsTrue();
            AssertThat(actualOxygen / actualOtherGases).IsEqualApprox(oxygen / otherGases, 0.00001f);
        }
        else
        {
            AssertThat(actualNitrogen + actualOxygen + actualCarbonDioxide).IsEqualApprox(1, 0.000001f);
        }
    }

    private static List<(IWorldEffect Effect, XoShiRo256starstar Random)> ReadSources(GameWorld world)
    {
        // Reflection is limited to source ownership and stream/archive contracts, not world outcome prediction.
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
        // Copy actual words without sampling or retaining mutable state owned by the generator.
        return Enumerable.Range(0, 4).Select(i => ReadField<ulong>(random, "s" + i)).ToArray();
    }

    private static void AssertState(XoShiRo256starstar random, ulong[] expected)
    {
        var actual = ReadState(random);
        AssertThat(actual.Any(word => word != 0)).IsTrue();
        AssertThat(actual.SequenceEqual(expected)).IsTrue();
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

    private static float ReadGas(Patch patch, Compound compound)
    {
        AssertThat(patch.Biome.TryGetCompound(compound, CompoundAmountType.Biome, out var amount)).IsTrue();
        return amount.Ambient;
    }

    private static void AssertGas(Patch patch, Compound compound, float expected)
    {
        AssertThat(ReadGas(patch, compound)).IsEqualApprox(expected, 0.000001f);
    }
}
