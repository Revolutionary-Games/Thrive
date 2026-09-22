using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoEvo;
using GdUnit4;
using Godot;
using Xoshiro.PRNG32;
using Xoshiro.PRNG64;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class GatherTaskSeedTests
{
    // Independently calculated xxHash64 values. Order: patches 11/29/47, species 1/7/13/UInt32.MaxValue.
    private static readonly (WorldSeed.Domain Domain, long Owner)[] Keys =
    [
        (WorldSeed.Domain.AutoEvoModifySpecies, 11), (WorldSeed.Domain.AutoEvoModifySpecies, 29),
        (WorldSeed.Domain.AutoEvoModifySpecies, 47), (WorldSeed.Domain.AutoEvoMigrateSpecies, 1),
        (WorldSeed.Domain.AutoEvoMigrateSpecies, 7), (WorldSeed.Domain.AutoEvoMigrateSpecies, 13),
        (WorldSeed.Domain.AutoEvoMigrateSpecies, uint.MaxValue),
    ];

    [TestCase(false)]
    [TestCase(true)]
    public void NormalGatherBindsFreshInputsRegardlessOfPreparationOrderAndOtherOwners(bool fullSpeed)
    {
        // Every scenario creates new worlds, tasks, owners and RNGs, including A -> B -> A.
        foreach (var seed in new[] { 0L, -1L, 0L, 4294967296L })
        {
            foreach (var layout in new[] { "baseline", "reversed", "expanded", "reduced" })
            {
                var world = CreateWorld(seed, layout);
                var bindings = Gather(world, fullSpeed);
                VerifyBindings(world, bindings, layout, 0, fullSpeed);
            }
        }
    }

    [TestCase]
    public void AppliedGenerationControlsRebuildsAndIgnoresEarlyPlayerGenerationChanges()
    {
        var world = CreateWorld(0, "baseline");
        VerifyBindings(world, Gather(world, false), "baseline", 0, false);

        // The editor increments the player generation before waiting for auto-evo. No results applied yet.
        ++world.PlayerSpecies.Generation;
        VerifyBindings(world, Gather(world, true), "baseline", 0, true);

        // These are the real history keys written when editor/exploring results are applied.
        world.GenerationHistory.Add(1, new GenerationRecord(1, new Dictionary<uint, SpeciesRecordLite>()));
        VerifyBindings(world, Gather(world, false), "baseline", 1, false);
        VerifyBindings(world, Gather(world, true), "baseline", 1, true);

        // A history's logical key, not its entry count or dictionary insertion position, identifies the input.
        world.GenerationHistory.Add(17, new GenerationRecord(17, new Dictionary<uint, SpeciesRecordLite>()));
        var initial = world.GenerationHistory[0];
        world.GenerationHistory.Remove(0);
        world.GenerationHistory.Add(0, initial);
        VerifyBindings(world, Gather(world, false), "baseline", 17, false);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ObservingAndConsumingTaskStreamsDoesNotAdvanceOtherInstances(bool fullSpeed)
    {
        var observedWorld = CreateWorld(0, "expanded");
        var observed = Gather(observedWorld, fullSpeed);
        var unobserved = Gather(CreateWorld(0, "expanded"), fullSpeed);
        var snapshots = observed.Select(binding => ReadState(binding.Random)).ToArray();
        VerifyBindings(observedWorld, observed, "expanded", 0, fullSpeed);

        AssertThat(observed.Count).IsEqual(7);
        for (var i = 0; i < observed.Count; ++i)
        {
            var binding = observed[i];
            var expected = CreateRandom(binding.Domain, ExpectedSeeds(0, 0)[Array.IndexOf(Keys, binding.Key)]);
            AssertState(binding.Random, snapshots[i]);
            AssertThat(binding.Random).IsNotSame(unobserved[i].Random);

            for (var draw = 0; draw < 7; ++draw)
            {
                var expectedValue = expected.Next();
                AssertThat(binding.Random.Next()).IsEqual(expectedValue);
                AssertThat(unobserved[i].Random.Next()).IsEqual(expectedValue);
            }

            snapshots[i] = ReadState(binding.Random);

            // Check both earlier and later tasks after advancing each stream.
            for (var other = 0; other < observed.Count; ++other)
                AssertState(observed[other].Random, snapshots[other]);
        }
    }

    [TestCase]
    public void EmptyGenerationHistoryFailsBeforeAnyTasksAreQueued()
    {
        var world = CreateWorld(0, "baseline");
        world.GenerationHistory.Clear();
        var run = new GatherAccess(world);
        var steps = new Queue<IRunStep>();
        AssertThrown(() => run.Prepare(steps)).IsInstanceOf<InvalidOperationException>()
            .HasMessage("Cannot seed auto-evo tasks without generation history");
        AssertThat(steps.Count).IsEqual(0);
    }

    [TestCase]
    public void BindingValidationRejectsEmptyMissingUnexpectedAndDuplicateIdentities()
    {
        var bindings = Gather(CreateWorld(0, "expanded"), false);
        AssertThat(bindings.Count).IsEqual(7);
        AssertThrown(() => ValidateIdentities([], Keys)).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => ValidateIdentities(bindings.Skip(1).ToList(), Keys))
            .IsInstanceOf<InvalidOperationException>();
        var duplicate = bindings.ToList();
        duplicate[0] = duplicate[1];
        AssertThrown(() => ValidateIdentities(duplicate, Keys)).IsInstanceOf<InvalidOperationException>();
        var unexpected = bindings.ToList();
        unexpected[0] = unexpected[0] with { Owner = 999 };
        AssertThrown(() => ValidateIdentities(unexpected, Keys)).IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    public void EditorPredictionCreatesNoMutationOrMigrationStreams()
    {
        var world = CreateWorld(0, "baseline");
        var run = new EditorAutoEvoRun(world, world.AutoEvoGlobalCache, world.PlayerSpecies,
            (Species)world.PlayerSpecies.Clone(), world.Map.CurrentPatch);
        run.OneStep();
        AssertThat(run.Aborted).IsFalse();
        var steps = ReadField<Queue<IRunStep>>(run, "runSteps").ToArray();
        AssertThat(steps.Length).IsEqual(4);
        AssertThat(steps.Count(step => step is GenerateMiche)).IsEqual(2);
        AssertThat(steps.Count(step => step is CalculatePopulation)).IsEqual(1);
        AssertThat(steps.Any(step => step is ModifyExistingSpecies or MigrateSpecies)).IsFalse();
    }

    private static GameWorld CreateWorld(long seed, string layout)
    {
        var world = GameProperties.StartNewMicrobeGame(new WorldGenerationSettings
        {
            Seed = seed,
            WorldSize = WorldGenerationSettings.WorldSizeEnum.Small,
        }).GameWorld;

        var template = world.Map.CurrentPatch!;
        var species = new List<Species> { world.PlayerSpecies };
        if (layout != "reduced")
            species.Add(new MicrobeSpecies(7, "Test", "microbe"));

        // Gather only needs identity and species type; these owners never execute mutations in this fixture.
        species.Add(new MulticellularSpecies(uint.MaxValue, "Test", "multicellular"));
        species.Add(new MacroscopicSpecies(99, "Test", "macroscopic"));
        if (layout == "expanded")
            species.Add(new MicrobeSpecies(13, "Test", "extra"));

        var patchIds = layout switch
        {
            "expanded" => new[] { 47, 11, 29 },
            "reduced" => new[] { 11 },
            "reversed" => new[] { 29, 11 },
            _ => new[] { 11, 29 },
        };
        if (layout is "reversed" or "expanded")
            species.Reverse();

        // Rebuild test input containers only; production preparation keeps its original traversal and queue rules.
        world.Map.Patches.Clear();
        foreach (var id in patchIds)
        {
            var patch = new Patch(new LocalizedString("TEST_PATCH"), id, template.BiomeTemplate, template.BiomeType,
                template.Region, 0);
            foreach (var owner in species)
                patch.AddSpecies(owner, 1000);
            world.Map.AddPatch(patch);
        }

        world.Map.CurrentPatch = world.Map.Patches[11];
        return world;
    }

    private static List<Binding> Gather(GameWorld world, bool fullSpeed)
    {
        var run = new AutoEvoRun(world, world.AutoEvoGlobalCache) { FullSpeed = fullSpeed };
        run.OneStep();
        AssertThat(run.Aborted).IsFalse();
        AssertThat(run.CompleteSteps).IsEqual(1);
        var steps = ReadField<Queue<IRunStep>>(run, "runSteps").ToArray();
        var patches = world.Map.Patches.Values.ToArray();
        var species = patches.SelectMany(patch => patch.SpeciesInPatch.Keys).Distinct()
            .Where(owner => owner is MicrobeSpecies or MulticellularSpecies).ToArray();
        AssertThat(steps.Length).IsEqual(patches.Length * 2 + species.Length + 4);

        for (var i = 0; i < patches.Length; ++i)
        {
            AssertThat(steps[i] is GenerateMiche).IsTrue();
            AssertThat(ReadField<Patch>(steps[i], "patch")).IsSame(patches[i]);
            AssertThat(steps[i + patches.Length] is ModifyExistingSpecies).IsTrue();
            AssertThat(ReadField<Patch>(steps[i + patches.Length], "patch")).IsSame(patches[i]);
        }

        for (var i = 0; i < species.Length; ++i)
            AssertThat(ReadField<Species>(steps[i + patches.Length * 2], "species")).IsSame(species[i]);

        AssertThat(steps[^4] is CalculatePopulation).IsTrue();
        AssertThat(steps[^3] is RegisterNewSpecies).IsTrue();
        AssertThat(steps[^2] is LambdaStep).IsTrue();
        AssertThat(steps[^1] is RemoveInvalidMigrations).IsTrue();

        return steps.Where(step => step is ModifyExistingSpecies or MigrateSpecies).Select(step => step switch
        {
            ModifyExistingSpecies modify => new Binding(step, WorldSeed.Domain.AutoEvoModifySpecies,
                ReadField<Patch>(modify, "patch").ID, modify.RandomSeed, ReadField<Random>(modify, "random")),
            MigrateSpecies migrate => new Binding(step, WorldSeed.Domain.AutoEvoMigrateSpecies,
                ReadField<Species>(migrate, "species").ID, migrate.RandomSeed, ReadField<Random>(migrate, "random")),
            _ => throw new InvalidOperationException("Unexpected seeded task"),
        }).ToList();
    }

    private static void VerifyBindings(GameWorld world, List<Binding> bindings, string layout, int generation,
        bool fullSpeed)
    {
        var expected = Keys.Where(key => layout == "expanded" || key.Owner is not (47 or 13))
            .Where(key => layout != "reduced" || key.Owner is not (29 or 7)).ToArray();
        ValidateIdentities(bindings, expected);
        AssertThat(bindings.Count).IsEqual(expected.Length);
        AssertThat(bindings.Count > 0).IsTrue();
        AssertThat(bindings.Select(binding => binding.Key).Distinct().Count()).IsEqual(expected.Length);
        AssertThat(bindings.Select(binding => binding.Task).Distinct().Count()).IsEqual(expected.Length);
        AssertThat(bindings.Select(binding => binding.Random).Distinct().Count()).IsEqual(expected.Length);
        var seeds = ExpectedSeeds(world.WorldSettings.Seed, generation);
        foreach (var key in expected)
        {
            var binding = bindings.Single(item => item.Key == key);
            var expectedSeed = seeds[Array.IndexOf(Keys, key)];
            AssertThat(binding.Seed).IsEqual(expectedSeed);
            var expectedRandom = CreateRandom(key.Domain, expectedSeed);
            AssertThat(binding.Random.GetType()).IsEqual(expectedRandom.GetType());
            AssertState(binding.Random, ReadState(expectedRandom));
            GD.Print($"Gather binding: worldSeed={world.WorldSettings.Seed}, generation={generation}, " +
                $"layout={layout}, fullSpeed={fullSpeed}, task={binding.Task.GetType().Name}, " +
                $"owner={binding.Owner}, seed={binding.Seed}, state={string.Join(',', ReadState(binding.Random))}");
        }
    }

    private static long[] ExpectedSeeds(long seed, int generation)
    {
        return (seed, generation) switch
        {
            (0, 0) =>
            [
                7928256305485563229, 2016445410036470242, -5920079988611106036, -4666703049727181512,
                -1538428128458231943, -3113950613136291240, 4297654792673649266,
            ],
            (-1, 0) =>
            [
                -7652811045156176369, -2746914478845955932, -6253695635953309623, 2887033574628305675,
                705456884907471236, -7717954221389712342, -8875733570799607220,
            ],
            (0, 1) =>
            [
                -544354814216685562, -5163832395438372668, -344851761345497136, -9028467954618502021,
                -697181409134199599, -4094859542069145327, -1955476889104334563,
            ],
            (0, 17) =>
            [
                -1233420873794869867, -1839708361903619673, -3308194372401824604, -2330438254329815159,
                -2164873818632215101, 8190748678792144279, -6846403773451756577,
            ],
            (4294967296, 0) =>
            [
                4529398752378197944, -4617119359244739759, 1966775464650352265, -5211814045052483747,
                1742341434157098787, -8527807804283010353, -9187076198896631827,
            ],
            _ => throw new InvalidOperationException("Missing independent seed fixture"),
        };
    }

    private static void ValidateIdentities(List<Binding> bindings, (WorldSeed.Domain Domain, long Owner)[] expected)
    {
        if (expected.Length == 0 || bindings.Count != expected.Length ||
            bindings.Select(binding => binding.Key).Distinct().Count() != expected.Length ||
            expected.Any(key => bindings.Count(binding => binding.Key == key) != 1))
        {
            throw new InvalidOperationException("Empty, missing, unexpected or ambiguous task identity binding");
        }
    }

    private static Random CreateRandom(WorldSeed.Domain domain, long seed)
    {
        return domain == WorldSeed.Domain.AutoEvoModifySpecies ?
            new XoShiRo256starstar(seed) :
            new XoShiRo128starstar(seed);
    }

    private static ulong[] ReadState(Random random)
    {
        // Observe only the four core words; no draws and no changes to cached random values.
        return Enumerable.Range(0, 4).Select(i => Convert.ToUInt64(ReadField<object>(random, "s" + i))).ToArray();
    }

    private static void AssertState(Random random, ulong[] expected)
    {
        var actual = ReadState(random);
        AssertThat(actual.Length).IsEqual(4);
        AssertThat(actual.Any(word => word != 0)).IsTrue();
        for (var i = 0; i < 4; ++i)
            AssertThat(actual[i]).IsEqual(expected[i]);
    }

    private static T ReadField<T>(object instance, string name)
    {
        // EditorAutoEvoRun inherits the queue from AutoEvoRun.
        var type = instance is AutoEvoRun ? typeof(AutoEvoRun) : instance.GetType();
        return (T)(type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance)
            ?? throw new InvalidOperationException($"Missing field {name}"));
    }

    private sealed record Binding(IRunStep Task, WorldSeed.Domain Domain, long Owner, long Seed, Random Random)
    {
        public (WorldSeed.Domain Domain, long Owner) Key => (Domain, Owner);
    }

    private sealed class GatherAccess(GameWorld world) : AutoEvoRun(world, world.AutoEvoGlobalCache)
    {
        public void Prepare(Queue<IRunStep> steps)
        {
            GatherInfo(steps);
        }
    }
}
