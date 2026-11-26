using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using GdUnit4;
using Xoshiro.PRNG64;
using static GdUnit4.Assertions;

/// <summary>
///   Checks random candidate sampling when adding organelles and cells.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class RandomOrganelleSelectionTests
{
    private readonly OrganelleDefinition cytoplasm = SimulationParameters.Instance.GetOrganelleType("cytoplasm");

    private readonly BiomeConditions biome = SimulationParameters.Instance.GetBiome("aavolcanic_vent").Conditions;

    /// <summary>
    ///   Unsupported inputs consume no random draws, and a microbe with enough MP to enter the strategy
    ///   still cannot add its only candidate when the nucleus costs more than its budget.
    /// </summary>
    [TestCase]
    public void AddEntryGuardsDoNotConsumeRandom()
    {
        var strategy = new AddOrganelleAnywhere(o => o.InternalName == "cytoplasm");
        var random = new RecordingRandom();

        AssertThat(strategy.MutationsOf(CreateMicrobe(), 0, false,
            random, biome)).IsNull();
        AssertThat(strategy.MutationsOf(new MulticellularSpecies(2, "Test", "Multicellular"),
            1000, false, random, biome)).IsNull();
        AssertThat(random.Calls.Count).IsEqual(0);

        var nucleus = SimulationParameters.Instance.GetOrganelleType("nucleus");
        var parent = CreateMicrobe();
        AssertThat(parent.Organelles.Any(o => ReferenceEquals(o.Definition, nucleus))).IsFalse();
        AssertThat(nucleus.MPCost > Constants.ORGANELLE_CHEAPEST_COST).IsTrue();
        var mutants = new AddOrganelleAnywhere(o => ReferenceEquals(o, nucleus))
            .MutationsOf(parent, Constants.ORGANELLE_CHEAPEST_COST, false, random, biome);
        AssertThat(mutants?.Count ?? 0).IsEqual(0);
    }

    [TestCase]
    public void AddCandidatesCoverEveryOrderForThreeCandidates()
    {
        var strategy = new AddOrganelleAnywhere(o =>
            o.InternalName is "cytoplasm" or "flagellum" or "rusticyanin");
        var orders = new HashSet<string>();

        for (int first = 0; first < 3; ++first)
        {
            for (int second = 0; second < 2; ++second)
            {
                var mutants = strategy.MutationsOf(CreateMicrobe(), 1000, false,
                    new SelectionRandom([first, second]), biome);
                AssertThat(mutants).IsNotNull();
                AssertThat(mutants!.Count).IsEqual(3);
                var names = mutants.Select(m => ((MicrobeSpecies)m.Species).Organelles[1].Definition.InternalName)
                    .ToArray();
                AssertThat(names.Distinct().Count()).IsEqual(3);
                AssertThat(orders.Add(string.Join(",", names))).IsTrue();
            }
        }

        AssertThat(orders.Count).IsEqual(6);
    }

    /// <summary>
    ///   Sampling respects the attempt limit without repeats. Each variant adds exactly one eligible organelle
    ///   and spends the MP for that definition while the parent's layout stays intact.
    /// </summary>
    [TestCase]
    public void AddCandidatesRespectAttemptLimitWithoutDuplicates()
    {
        const int budget = 1000;
        var definitions = GetAddableDefinitions();
        foreach (int count in new[] { 0, 1, 2, 14, 15, 16, definitions.Length })
        {
            var candidates = definitions.Take(count).ToHashSet();
            var strategy = new AddOrganelleAnywhere(candidates.Contains);
            var original = CreateMicrobe();
            original.IsBacteria = false;

            foreach (int seed in new[] { 1, 71, 431 })
            {
                var mutants = strategy.MutationsOf(original, budget, false,
                    new XoShiRo256starstar(seed), biome);
                AssertThat(mutants).IsNotNull();
                AssertThat(mutants!.Count).IsEqual(Math.Min(count, Constants.AUTO_EVO_ORGANELLE_ADD_ATTEMPTS));
                var added = new HashSet<OrganelleDefinition>();
                foreach (var mutant in mutants)
                {
                    var organelles = ((MicrobeSpecies)mutant.Species).Organelles;
                    AssertThat(organelles.Count).IsEqual(original.Organelles.Count + 1);
                    var definition = organelles[1].Definition;
                    AssertThat(candidates.Contains(definition)).IsTrue();
                    AssertThat(added.Add(definition)).IsTrue();
                    AssertThat(mutant.MP).IsEqual(budget - definition.MPCost);
                }

                AssertThat(original.Organelles.Count).IsEqual(1);
            }
        }
    }

    [TestCase]
    public void AddCellCandidatesRespectAttemptLimitWithoutDuplicates()
    {
        // Each candidate creates one new cell type, making selected candidates observable in the variants.
        var definitions = GetAddableDefinitions().Where(o => !ReferenceEquals(o, cytoplasm)).ToArray();
        foreach (int count in new[] { 0, 1, 2, 14, 15, 16, definitions.Length })
        {
            var candidates = definitions.Take(count).ToHashSet();
            var strategy = new AddCellWithOrganelle(candidates.Contains, CommonMutationFunctions.Direction.Front);
            var original = CreateMulticellular();

            foreach (int seed in new[] { 1, 71, 431 })
            {
                var mutants = strategy.MutationsOf(original, 1000, false, new XoShiRo256starstar(seed), biome);
                AssertThat(mutants).IsNotNull();
                AssertThat(mutants!.Count).IsEqual(Math.Min(count, Constants.AUTO_EVO_ORGANELLE_ADD_ATTEMPTS));
                var added = mutants.Select(m => ((MulticellularSpecies)m.Species)
                    .CellTypes[1].Organelles.Last().Definition).ToArray();
                AssertThat(added.Distinct().Count()).IsEqual(added.Length);
                AssertThat(added.All(candidates.Contains)).IsTrue();
                AssertThat(original.CellTypes.Count).IsEqual(1);
                AssertThat(original.CellTypes[0].Organelles.Count).IsEqual(1);
                AssertThat(original.EditorCells.Count).IsEqual(1);
            }
        }
    }

    private static OrganelleDefinition[] GetAddableDefinitions()
    {
        var membrane = SimulationParameters.Instance.GetMembrane("single");
        return SimulationParameters.Instance.GetAllOrganelles().Where(o => o.AutoEvoCanPlace &&
            o.EditorButtonGroup != OrganelleDefinition.OrganelleGroup.Multicellular &&
            o.EditorButtonGroup != OrganelleDefinition.OrganelleGroup.Macroscopic &&
            !o.IsIncompatibleWithMembrane(membrane)).ToArray();
    }

    private MicrobeSpecies CreateMicrobe()
    {
        var species = new MicrobeSpecies(1, "Test", "RandomSelection")
        {
            IsBacteria = true,
            MembraneType = SimulationParameters.Instance.GetMembrane("single"),
        };

        species.Organelles.Add(new OrganelleTemplate(cytoplasm, new Hex(0, 0), 0));
        species.OnEdited();

        return species;
    }

    private MulticellularSpecies CreateMulticellular()
    {
        var species = new MulticellularSpecies(1, "Test", "RandomCellSelection");
        var cellType = new CellType(SimulationParameters.Instance.GetMembrane("single"))
        {
            CellTypeName = "Original",
        };
        cellType.ModifiableOrganelles.Add(new OrganelleTemplate(cytoplasm, new Hex(0, 0), 0));
        species.ModifiableCellTypes.Add(cellType);
        species.ModifiableGameplayCells.AddFast(new CellTemplate(cellType, new Hex(0, 0), 0), [], []);
        return species;
    }

    private sealed class SelectionRandom(int[]? ranks = null) : Random
    {
        private int nextRank;

        public override int Next()
        {
            return 0;
        }

        public override int Next(int maxValue)
        {
            int result = ranks != null && nextRank < ranks.Length ? ranks[nextRank++] : 0;
            if (result < 0 || result >= maxValue)
                throw new InvalidOperationException("Unexpected selection bound");

            return result;
        }
    }

    private sealed class RecordingRandom : Random
    {
        private readonly Random source = new XoShiRo256starstar(431);

        public List<string> Calls { get; } = new();

        public override int Next()
        {
            int result = source.Next();
            Calls.Add($"Next:{result}");
            return result;
        }

        public override int Next(int maxValue)
        {
            int result = source.Next(maxValue);
            Calls.Add($"Next({maxValue}):{result}");
            return result;
        }

        public override int Next(int minValue, int maxValue)
        {
            int result = source.Next(minValue, maxValue);
            Calls.Add($"Next({minValue}, {maxValue}):{result}");
            return result;
        }
    }
}
