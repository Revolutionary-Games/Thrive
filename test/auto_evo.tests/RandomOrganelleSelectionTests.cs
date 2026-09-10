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
    private static OrganelleDefinition Cytoplasm => SimulationParameters.Instance.GetOrganelleType("cytoplasm");

    private static BiomeConditions Biome => SimulationParameters.Instance.GetBiome("aavolcanic_vent").Conditions;

    [TestCase]
    public void EmptyAddCandidatesDoNotConsumeRandom()
    {
        var random = new RecordingRandom();
        var mutants = new AddOrganelleAnywhere(_ => false).MutationsOf(CreateMicrobe(), 1000, false, random, Biome);

        AssertThat(mutants).IsNotNull();
        AssertThat(mutants!.Count).IsEqual(0);
        AssertThat(random.Calls.Count).IsEqual(0);
    }

    [TestCase]
    public void AddEntryGuardsDoNotConsumeRandom()
    {
        var strategy = new AddOrganelleAnywhere(o => o.InternalName == "cytoplasm");
        var random = new RecordingRandom();

        AssertThat(strategy.MutationsOf(CreateMicrobe(), 0, false,
            random, Biome)).IsNull();
        AssertThat(strategy.MutationsOf(new MulticellularSpecies(2, "Test", "Multicellular"),
            1000, false, random, Biome)).IsNull();
        AssertThat(random.Calls.Count).IsEqual(0);
    }

    [TestCase]
    public void RejectedSingleAddCandidateDoesNotNeedRandomSelection()
    {
        var random = new RecordingRandom();
        var mutants = new AddOrganelleAnywhere(o => o.InternalName == "nucleus")
            .MutationsOf(CreateMicrobe(), Constants.ORGANELLE_CHEAPEST_COST,
                false, random, Biome);

        AssertThat(mutants).IsNotNull();
        AssertThat(mutants!.Count).IsEqual(0);
        AssertThat(random.KeyCount).IsEqual(0);
        AssertThat(random.SideCount).IsEqual(0);
    }

    [TestCase]
    public void SingleAddCandidateOnlyConsumesPlacementRandom()
    {
        var original = CreateMicrobe();
        var random = new RecordingRandom();
        var mutants = new AddOrganelleAnywhere(o => o.InternalName == "cytoplasm")
            .MutationsOf(original, 1000, false, random, Biome);

        AssertThat(mutants).IsNotNull();
        AssertThat(mutants!.Count).IsEqual(1);
        AssertThat(mutants[0].MP).IsEqual(1000.0 - Cytoplasm.MPCost);
        AssertThat(original.Organelles.Count).IsEqual(1);
        var mutant = (MicrobeSpecies)mutants[0].Species;
        AssertThat(mutant.Organelles.Count).IsEqual(2);
        AssertThat(mutant.Organelles[1].Position).IsEqual(new Hex(0, 1));
        AssertThat(mutant.Organelles[1].Orientation).IsEqual(0);
        AssertThat(random.KeyCount).IsEqual(1);
        AssertThat(random.SideCount).IsEqual(1);
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
                    new SelectionRandom([first, second]), Biome);
                AssertThat(mutants).IsNotNull();
                AssertThat(mutants!.Count).IsEqual(3);
                var names = mutants.Select(m => ((MicrobeSpecies)m.Species).Organelles[1].Definition.InternalName);
                AssertThat(names.Distinct().Count()).IsEqual(3);
                AssertThat(orders.Add(string.Join(",", names))).IsTrue();
            }
        }

        AssertThat(orders.Count).IsEqual(6);
    }

    [TestCase]
    public void AddCandidatesRespectAttemptLimitWithoutDuplicates()
    {
        var definitions = GetAddableDefinitions();
        foreach (int count in new[] { 0, 1, 2, 14, 15, 16, definitions.Length })
        {
            var candidates = definitions.Take(count).ToHashSet();
            var strategy = new AddOrganelleAnywhere(candidates.Contains);
            var original = CreateMicrobe();
            original.IsBacteria = false;

            foreach (int seed in new[] { 1, 71, 431 })
            {
                var mutants = strategy.MutationsOf(original, 1000, false,
                    new XoShiRo256starstar(seed), Biome);
                AssertThat(mutants).IsNotNull();
                AssertThat(mutants!.Count).IsEqual(Math.Min(count, Constants.AUTO_EVO_ORGANELLE_ADD_ATTEMPTS));
                var added = mutants.Select(m => ((MicrobeSpecies)m.Species).Organelles[1].Definition).ToArray();
                AssertThat(added.Distinct().Count()).IsEqual(added.Length);
                AssertThat(added.All(candidates.Contains)).IsTrue();
                AssertThat(original.Organelles.Count).IsEqual(1);
            }
        }
    }

    [TestCase]
    public void RejectedCandidatesAreNotReplacedWithUnselectedCandidates()
    {
        var candidates = GetAddableDefinitions()
            .Where(o => o.MPCost > Constants.ORGANELLE_CHEAPEST_COST)
            .Take(Constants.AUTO_EVO_ORGANELLE_ADD_ATTEMPTS).ToHashSet();
        AssertThat(candidates.Count).IsEqual(Constants.AUTO_EVO_ORGANELLE_ADD_ATTEMPTS);
        candidates.Add(Cytoplasm);

        // Use a separate dense list to describe an order selecting every expensive candidate, but not cytoplasm.
        var remaining = SimulationParameters.Instance.GetAllOrganelles().Where(candidates.Contains).ToList();
        var ranks = new List<int>();
        foreach (var candidate in remaining.Where(o => o != Cytoplasm).ToArray())
        {
            int index = remaining.IndexOf(candidate);
            ranks.Add(index);
            remaining.RemoveAt(index);
        }

        var mutants = new AddOrganelleAnywhere(candidates.Contains).MutationsOf(CreateMicrobe(),
            Constants.ORGANELLE_CHEAPEST_COST, false, new SelectionRandom(ranks.ToArray()),
            Biome);

        AssertThat(mutants).IsNotNull();
        AssertThat(mutants!.Count).IsEqual(0);
    }

    [TestCase]
    public void AddCellCandidatesCoverEveryOrderForThreeCandidates()
    {
        var strategy = new AddCellWithOrganelle(o =>
                o.InternalName is "metabolosome" or "flagellum" or "rusticyanin",
            CommonMutationFunctions.Direction.Front);
        var orders = new HashSet<string>();

        for (int first = 0; first < 3; ++first)
        {
            for (int second = 0; second < 2; ++second)
            {
                var mutants = strategy.MutationsOf(CreateMulticellular(), 1000, false,
                    new SelectionRandom([first, second]), Biome);
                AssertThat(mutants).IsNotNull();
                AssertThat(mutants!.Count).IsEqual(3);
                var names = mutants.Select(m => ((MulticellularSpecies)m.Species)
                    .CellTypes[1].Organelles.Last().Definition.InternalName);
                AssertThat(names.Distinct().Count()).IsEqual(3);
                AssertThat(orders.Add(string.Join(",", names))).IsTrue();
            }
        }

        AssertThat(orders.Count).IsEqual(6);
    }

    [TestCase]
    public void AddCellCandidatesRespectAttemptLimitWithoutDuplicates()
    {
        // Each candidate creates one new cell type, making selected candidates observable in the variants.
        var definitions = GetAddableDefinitions().Where(o => o != Cytoplasm).ToArray();
        foreach (int count in new[] { 0, 1, 2, 14, 15, 16, definitions.Length })
        {
            var candidates = definitions.Take(count).ToHashSet();
            var strategy = new AddCellWithOrganelle(candidates.Contains, CommonMutationFunctions.Direction.Front);
            var original = CreateMulticellular();

            foreach (int seed in new[] { 1, 71, 431 })
            {
                var mutants = strategy.MutationsOf(original, 1000, false, new XoShiRo256starstar(seed), Biome);
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

    private static MicrobeSpecies CreateMicrobe()
    {
        var species = new MicrobeSpecies(1, "Test", "RandomSelection")
        {
            IsBacteria = true,
            MembraneType = SimulationParameters.Instance.GetMembrane("single"),
        };

        species.Organelles.Add(new OrganelleTemplate(Cytoplasm, new Hex(0, 0), 0));
        species.OnEdited();

        return species;
    }

    private static MulticellularSpecies CreateMulticellular()
    {
        var species = new MulticellularSpecies(1, "Test", "RandomCellSelection");
        var cellType = new CellType(SimulationParameters.Instance.GetMembrane("single"))
        {
            CellTypeName = "Original",
        };
        cellType.ModifiableOrganelles.Add(new OrganelleTemplate(Cytoplasm, new Hex(0, 0), 0));
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

        public int KeyCount { get; private set; }

        public int SideCount { get; private set; }

        public override int Next()
        {
            int result = source.Next();
            ++KeyCount;
            Calls.Add($"Next:{result}");
            return result;
        }

        public override int Next(int maxValue)
        {
            int result = source.Next(maxValue);
            ++SideCount;
            Calls.Add($"Next({maxValue}):{result}");
            return result;
        }
    }
}
