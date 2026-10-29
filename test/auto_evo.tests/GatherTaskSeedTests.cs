using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoEvo;
using GdUnit4;
using Xoshiro.PRNG32;
using Xoshiro.PRNG64;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class GatherTaskSeedTests
{
    private static readonly (WorldSeed.Domain Domain, long Owner)[] Keys =
    [
        (WorldSeed.Domain.AutoEvoModifySpecies, 11), (WorldSeed.Domain.AutoEvoModifySpecies, 29),
        (WorldSeed.Domain.AutoEvoModifySpecies, 47), (WorldSeed.Domain.AutoEvoMigrateSpecies, 1),
        (WorldSeed.Domain.AutoEvoMigrateSpecies, 7), (WorldSeed.Domain.AutoEvoMigrateSpecies, 13),
        (WorldSeed.Domain.AutoEvoMigrateSpecies, uint.MaxValue),
    ];

    /// <summary>
    ///   Verifies that fresh tasks bind private random streams to their world and owner identities despite
    ///   traversal changes or added and removed owners, protecting seed wiring and isolation between tasks and runs.
    /// </summary>
    [TestCase]
    public void GatherBindsIndependentStreamsRegardlessOfOrderAndOtherOwners()
    {
        var streams = new HashSet<Random>();
        foreach (var seed in new[] { 0L, 4294967296L })
        {
            foreach (var layout in new[] { "baseline", "reversed", "expanded", "reduced" })
            {
                var world = CreateWorld(seed, layout);
                var bindings = Gather(world);
                foreach (var binding in bindings)
                    AssertThat(streams.Add(binding.Random)).IsTrue();

                VerifyBindings(world, bindings, layout, 0);
            }
        }
    }

    /// <summary>
    ///   Verifies that rebuilt tasks use the greatest applied generation key, protecting their streams from early
    ///   player generation changes and from using history entry counts or insertion order instead of that key.
    /// </summary>
    [TestCase]
    public void AppliedGenerationControlsRebuildsAndIgnoresEarlyPlayerGenerationChanges()
    {
        var world = CreateWorld(0, "baseline");
        VerifyBindings(world, Gather(world), "baseline", 0);

        // The editor increments the player generation before waiting for auto-evo. No results applied yet.
        ++world.PlayerSpecies.Generation;
        VerifyBindings(world, Gather(world), "baseline", 0);

        // A history's logical key, not its entry count or dictionary insertion position, identifies the input.
        var initial = world.GenerationHistory[0];
        world.GenerationHistory.Clear();
        world.GenerationHistory.Add(17, new GenerationRecord(17, new Dictionary<uint, SpeciesRecordLite>()));
        world.GenerationHistory.Add(0, initial);
        VerifyBindings(world, Gather(world), "baseline", 17);
    }

    /// <summary>
    ///   Verifies that missing generation history is rejected before any tasks are queued, preventing a partially
    ///   prepared run from using an undefined generation to seed its streams.
    /// </summary>
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

    private static List<Binding> Gather(GameWorld world)
    {
        var run = new AutoEvoRun(world, world.AutoEvoGlobalCache);
        run.OneStep();
        AssertThat(run.Aborted).IsFalse();
        AssertThat(run.CompleteSteps).IsEqual(1);
        var bindings = new List<Binding>();
        foreach (var step in ReadField<Queue<IRunStep>>(run, "runSteps"))
        {
            if (step is ModifyExistingSpecies modify)
            {
                var patch = ReadField<Patch>(modify, "patch");
                AssertThat(patch).IsSame(world.Map.Patches[patch.ID]);
                bindings.Add(new Binding(WorldSeed.Domain.AutoEvoModifySpecies, patch.ID, modify.RandomSeed,
                    ReadField<Random>(modify, "random")));
            }
            else if (step is MigrateSpecies migrate)
            {
                var species = ReadField<Species>(migrate, "species");
                var owner = world.Map.Patches.Values.SelectMany(p => p.SpeciesInPatch.Keys)
                    .Distinct().Single(s => s.ID == species.ID);
                AssertThat(species).IsSame(owner);
                bindings.Add(new Binding(WorldSeed.Domain.AutoEvoMigrateSpecies, species.ID, migrate.RandomSeed,
                    ReadField<Random>(migrate, "random")));
            }
        }

        return bindings;
    }

    private static void VerifyBindings(GameWorld world, List<Binding> bindings, string layout, int generation)
    {
        var expected = Keys.Where(k => layout == "expanded" || k.Owner is not (47 or 13))
            .Where(k => layout != "reduced" || k.Owner is not (29 or 7)).ToArray();
        AssertThat(bindings.Count).IsEqual(expected.Length);
        var streams = new List<(Random Actual, Random Expected)>();
        foreach (var key in expected)
        {
            // Count plus Single rejects missing, duplicate and unexpected task identities.
            var binding = bindings.Single(b => b.Key == key);

            // Independent vectors test Derive separately; here the contract is its production argument binding.
            var seed = WorldSeed.Derive(world.WorldSettings.Seed, key.Domain, generation, key.Owner);
            AssertThat(binding.Seed).IsEqual(seed);
            Random random = key.Domain == WorldSeed.Domain.AutoEvoModifySpecies ?
                new XoShiRo256starstar(seed) :
                new XoShiRo128starstar(seed);
            AssertThat(binding.Random.GetType()).IsEqual(random.GetType());
            streams.Add((binding.Random, random));
        }

        // Revisit earlier streams after later ones advance to check both directions of interference.
        for (var round = 0; round < 2; ++round)
        {
            foreach (var stream in streams)
            {
                for (var draw = 0; draw < 3; ++draw)
                    AssertThat(stream.Actual.Next()).IsEqual(stream.Expected.Next());
            }
        }
    }

    private static T ReadField<T>(object instance, string name)
    {
        return (T)(instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance)
            ?? throw new InvalidOperationException($"Missing field {name}"));
    }

    private sealed record Binding(WorldSeed.Domain Domain, long Owner, long Seed, Random Random)
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
