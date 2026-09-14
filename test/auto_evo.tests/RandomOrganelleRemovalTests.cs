using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using GdUnit4;
using Xoshiro.PRNG64;
using static GdUnit4.Assertions;

/// <summary>
///   Checks filtered random removal through both species mutation paths.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class RandomOrganelleRemovalTests
{
    private readonly OrganelleDefinition cytoplasm = SimulationParameters.Instance.GetOrganelleType("cytoplasm");

    private readonly BiomeConditions biome = SimulationParameters.Instance.GetBiome("aavolcanic_vent").Conditions;

    [TestCase(false)]
    [TestCase(true)]
    public void FilteredCandidatesRespectLimitAndMapToOriginalOrganelles(bool multicellular)
    {
        var definitions = GetCandidates();
        AssertThat(definitions.Length).IsEqual(12);
        var original = CreateSpecies(multicellular, definitions);

        foreach (int count in new[] { 0, 1, 2, 9, 10, 11, 12 })
        {
            var candidates = definitions.Take(count).ToHashSet();
            var strategy = new RemoveOrganelle(candidates.Contains);
            foreach (int seed in new[] { 1, 71, 431 })
            {
                var mutants = strategy.MutationsOf(original, 1000, false, new XoShiRo256starstar(seed), biome);
                if (count == 0)
                {
                    AssertThat(mutants).IsNull();
                    continue;
                }

                AssertThat(mutants).IsNotNull();
                AssertThat(mutants!.Count).IsEqual(Math.Min(count, Constants.AUTO_EVO_ORGANELLE_REMOVE_ATTEMPTS));
                var removed = mutants.Select(m => definitions.Except(GetDefinitions(m.Species)).Single()).ToArray();
                AssertThat(removed.Distinct().Count()).IsEqual(removed.Length);
                AssertThat(removed.All(candidates.Contains)).IsTrue();
                foreach (var mutant in mutants)
                {
                    AssertThat(GetDefinitions(mutant.Species).Count()).IsEqual(definitions.Length * 2 - 1);
                    double cost = Constants.ORGANELLE_REMOVE_COST *
                        (multicellular ? Constants.MULTICELLULAR_EDITOR_COST_FACTOR : 1);
                    AssertThat(mutant.MP).IsEqual(1000 - cost);
                }
            }
        }

        AssertThat(GetDefinitions(original).Count()).IsEqual(definitions.Length * 2);
        AssertThat(definitions.Except(GetDefinitions(original)).Count()).IsEqual(0);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AllThreeFilteredCandidatesCanAppearInEveryOrder(bool multicellular)
    {
        var definitions = GetCandidates().Take(3).ToArray();
        var candidates = definitions.ToHashSet();
        var strategy = new RemoveOrganelle(candidates.Contains);
        var original = CreateSpecies(multicellular, definitions);
        var orders = new HashSet<string>();

        // Fixed seeds cover the resulting orders without depending on an algorithm's random-call sequence.
        for (int seed = 0; seed < 128; ++seed)
        {
            var mutants = strategy.MutationsOf(original, 1000, false, new XoShiRo256starstar(seed), biome);
            AssertThat(mutants).IsNotNull();
            AssertThat(mutants!.Count).IsEqual(3);
            var removed = mutants.Select(m => definitions.Except(GetDefinitions(m.Species)).Single().InternalName)
                .ToArray();
            AssertThat(removed.Distinct().Count()).IsEqual(3);
            orders.Add(string.Join(",", removed));
        }

        AssertThat(orders.Count).IsEqual(6);
    }

    [TestCase]
    public void MulticellularTypesHaveIndependentLimitsAndReuseOnlyInitializedIndices()
    {
        var definitions = GetCandidates();
        var counts = new[] { 12, 1, 0, 3 };
        var species = (MulticellularSpecies)CreateSpecies(true, definitions);
        for (int i = 1; i < counts.Length; ++i)
            AddCellType(species, definitions.Take(counts[i]).ToArray());

        var strategy = new RemoveOrganelle(definitions.Contains);
        var mutants = strategy.MutationsOf(species, 1000, false, new XoShiRo256starstar(71), biome);
        AssertThat(mutants).IsNotNull();
        AssertThat(mutants!.Count).IsEqual(14);
        var mutationsPerType = new int[counts.Length];
        foreach (var mutant in mutants)
        {
            var changedTypes = Enumerable.Range(0, counts.Length)
                .Where(i => GetDefinitions(mutant.Species, i).Count() != GetDefinitions(species, i).Count()).ToArray();
            AssertThat(changedTypes.Length).IsEqual(1);
            ++mutationsPerType[changedTypes[0]];
        }

        for (int i = 0; i < counts.Length; ++i)
            AssertThat(mutationsPerType[i]).IsEqual(Math.Min(counts[i], Constants.AUTO_EVO_ORGANELLE_REMOVE_ATTEMPTS));
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void ProtectedSelectedOrganellesAreSkippedWithoutReplacement(bool multicellular, bool binding)
    {
        var protectedOrganelle = SimulationParameters.Instance.GetOrganelleType(binding ? "bindingAgent" : "nucleus");
        foreach (int removableCount in new[] { 9, 10 })
        {
            var definitions = GetCandidates().Take(removableCount).ToArray();
            var original = CreateSpecies(multicellular, definitions);
            var layout = original is MicrobeSpecies microbe ?
                microbe.Organelles :
                ((MulticellularSpecies)original).ModifiableCellTypes[0].ModifiableOrganelles;
            layout.Add(new OrganelleTemplate(protectedOrganelle, new Hex(-10, 0), 0));
            var candidates = definitions.Append(protectedOrganelle).ToHashSet();
            var strategy = new RemoveOrganelle(candidates.Contains);
            bool sawProtectedSelected = false;
            bool sawProtectedNotSelected = false;

            for (int seed = 0; seed < 128; ++seed)
            {
                var mutants = strategy.MutationsOf(original, 1000, false, new XoShiRo256starstar(seed), biome);
                AssertThat(mutants).IsNotNull();
                AssertThat(mutants!.Count is 9 or 10).IsTrue();
                if (removableCount == 9)
                    AssertThat(mutants.Count).IsEqual(9);

                sawProtectedSelected |= mutants.Count == 9;
                sawProtectedNotSelected |= mutants.Count == 10;
                foreach (var mutant in mutants)
                    AssertThat(GetDefinitions(mutant.Species).Contains(protectedOrganelle)).IsTrue();

                if (sawProtectedSelected && (removableCount == 9 || sawProtectedNotSelected))
                    break;
            }

            AssertThat(sawProtectedSelected).IsTrue();
            if (removableCount == 10)
                AssertThat(sawProtectedNotSelected).IsTrue();
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EveryFilteredCandidateCanBeSelectedAboveLimit(bool multicellular)
    {
        var definitions = GetCandidates();
        var candidates = definitions.ToHashSet();
        var strategy = new RemoveOrganelle(candidates.Contains);
        var original = CreateSpecies(multicellular, definitions);
        var selected = new HashSet<OrganelleDefinition>();
        for (int seed = 0; seed < 128 && selected.Count < definitions.Length; ++seed)
        {
            var mutants = strategy.MutationsOf(original, 1000, false, new XoShiRo256starstar(seed), biome);
            AssertThat(mutants).IsNotNull();
            foreach (var mutant in mutants!)
                selected.Add(definitions.Except(GetDefinitions(mutant.Species)).Single());
        }

        AssertThat(selected.Count).IsEqual(definitions.Length);
    }

    private static IEnumerable<OrganelleDefinition> GetDefinitions(Species species, int cellType = 0)
    {
        return species is MicrobeSpecies microbe ?
            microbe.Organelles.Select(o => o.Definition) :
            ((MulticellularSpecies)species).CellTypes[cellType].Organelles.Select(o => o.Definition);
    }

    private OrganelleDefinition[] GetCandidates()
    {
        return SimulationParameters.Instance.GetAllOrganelles()
            .Where(o => o.AutoEvoCanPlace && o.Hexes.Count == 1 && !ReferenceEquals(o, cytoplasm) &&
                !o.HasBindingFeature)
            .Take(12).ToArray();
    }

    private Species CreateSpecies(bool multicellular, OrganelleDefinition[] definitions)
    {
        if (multicellular)
        {
            var species = new MulticellularSpecies(1, "Test", "RandomRemoval");
            AddCellType(species, definitions);
            return species;
        }

        var microbe = new MicrobeSpecies(1, "Test", "RandomRemoval")
        {
            IsBacteria = false,
            MembraneType = SimulationParameters.Instance.GetMembrane("single"),
        };
        FillOrganelles(microbe.Organelles, definitions);
        return microbe;
    }

    private void AddCellType(MulticellularSpecies species, OrganelleDefinition[] definitions)
    {
        var cellType = new CellType(SimulationParameters.Instance.GetMembrane("single"))
        {
            CellTypeName = $"Type{species.CellTypes.Count}",
        };
        FillOrganelles(cellType.ModifiableOrganelles, definitions);
        species.ModifiableCellTypes.Add(cellType);
    }

    private void FillOrganelles(OrganelleLayout<OrganelleTemplate> layout, OrganelleDefinition[] definitions)
    {
        // A cytoplasm backbone keeps the layout connected after any candidate is removed.
        for (int i = 0; i < Math.Max(2, definitions.Length); ++i)
        {
            layout.Add(new OrganelleTemplate(cytoplasm, new Hex(i, 0), 0));
            if (i < definitions.Length)
                layout.Add(new OrganelleTemplate(definitions[i], new Hex(i, 1), 0));
        }
    }
}
