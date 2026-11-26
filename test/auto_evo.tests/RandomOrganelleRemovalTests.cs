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

    /// <summary>
    ///   Both species paths remove distinct matching organelles within the attempt limit and charge each variant
    ///   the removal cost. When both matches are sampled, either one can be removed by the first variant.
    /// </summary>
    [TestCase(false)]
    [TestCase(true)]
    public void FilteredCandidatesRespectLimitAndMapToOriginalOrganelles(bool multicellular)
    {
        var definitions = GetCandidates();
        AssertThat(definitions.Length).IsEqual(12);
        var original = CreateSpecies(multicellular, definitions);
        var firstOfPair = new HashSet<OrganelleDefinition>();

        foreach (int count in new[] { 0, 1, 2, 9, 10, 11, 12 })
        {
            var candidates = definitions.Take(count).ToHashSet();
            var strategy = new RemoveOrganelle(candidates.Contains);
            var seeds = count == 2 ? Enumerable.Range(0, 8) : new[] { 1, 71, 431 };
            foreach (int seed in seeds)
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
                if (count == 2)
                {
                    AssertThat(removed.Length).IsEqual(2);
                    firstOfPair.Add(removed[0]);
                }

                foreach (var mutant in mutants)
                {
                    AssertThat(GetDefinitions(mutant.Species).Count()).IsEqual(definitions.Length * 2 - 1);
                    double cost = Constants.ORGANELLE_REMOVE_COST *
                        (multicellular ? Constants.MULTICELLULAR_EDITOR_COST_FACTOR : 1);
                    AssertThat(mutant.MP).IsEqual(1000 - cost);
                }
            }
        }

        AssertThat(firstOfPair.SetEquals(definitions.Take(2))).IsTrue();
        AssertThat(GetDefinitions(original).Count()).IsEqual(definitions.Length * 2);
        AssertThat(definitions.Except(GetDefinitions(original)).Count()).IsEqual(0);
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

    [TestCase(false, 9)]
    [TestCase(false, 10)]
    [TestCase(false, 11)]
    [TestCase(true, 9)]
    [TestCase(true, 10)]
    [TestCase(true, 11)]
    public void NucleusIsExcludedBeforeSampling(bool multicellular, int removableCount)
    {
        var nucleus = SimulationParameters.Instance.GetOrganelleType("nucleus");
        var definitions = GetCandidates().Take(removableCount).ToArray();
        AssertThat(definitions.Length).IsEqual(removableCount);
        var original = CreateSpecies(multicellular, definitions);
        var layout = original is MicrobeSpecies microbe ?
            microbe.Organelles :
            ((MulticellularSpecies)original).ModifiableCellTypes[0].ModifiableOrganelles;
        layout.Add(new OrganelleTemplate(nucleus, new Hex(-10, 0), 0));

        var originalLayout = layout
            .Select(o => (o.Definition, o.Position, o.Orientation, o.Upgrades, o.IsEndosymbiont)).ToArray();
        AssertThat(originalLayout.Length).IsEqual(removableCount * 2 + 1);
        AssertThat(GetDefinitions(original).Count(o => ReferenceEquals(o, cytoplasm))).IsEqual(removableCount);
        AssertThat(GetDefinitions(original).Count(o => ReferenceEquals(o, nucleus))).IsEqual(1);
        foreach (var definition in definitions)
            AssertThat(GetDefinitions(original).Count(o => ReferenceEquals(o, definition))).IsEqual(1);

        // Include the nucleus in the criteria so its exclusion must come from the removal strategy.
        var candidates = definitions.Append(nucleus).ToHashSet();
        var strategy = new RemoveOrganelle(candidates.Contains);
        double cost = Constants.ORGANELLE_REMOVE_COST *
            (multicellular ? Constants.MULTICELLULAR_EDITOR_COST_FACTOR : 1);

        foreach (int seed in new[] { 1, 71, 431 })
        {
            var mutants = strategy.MutationsOf(original, 1000, false, new XoShiRo256starstar(seed), biome);
            AssertThat(mutants).IsNotNull();
            AssertThat(mutants!.Count)
                .IsEqual(Math.Min(removableCount, Constants.AUTO_EVO_ORGANELLE_REMOVE_ATTEMPTS));
            var removed = new HashSet<OrganelleDefinition>();
            foreach (var mutant in mutants)
            {
                var mutantDefinitions = GetDefinitions(mutant.Species).ToArray();
                var removedDefinition = definitions.Except(mutantDefinitions).Single();
                AssertThat(removed.Add(removedDefinition)).IsTrue();
                AssertThat(mutantDefinitions.Length).IsEqual(removableCount * 2);
                AssertThat(mutantDefinitions.Count(o => ReferenceEquals(o, nucleus))).IsEqual(1);
                AssertThat(mutantDefinitions.Count(o => ReferenceEquals(o, cytoplasm))).IsEqual(removableCount);
                foreach (var definition in definitions)
                {
                    AssertThat(mutantDefinitions.Count(o => ReferenceEquals(o, definition)))
                        .IsEqual(ReferenceEquals(definition, removedDefinition) ? 0 : 1);
                }

                AssertThat(mutant.MP).IsEqual(1000 - cost);
            }

            AssertThat(GetDefinitions(original).SequenceEqual(originalLayout.Select(o => o.Definition))).IsTrue();
            AssertThat(layout.Select(o => (o.Definition, o.Position, o.Orientation, o.Upgrades, o.IsEndosymbiont))
                .SequenceEqual(originalLayout)).IsTrue();
        }
    }

    [TestCase]
    public void MulticellularBindingAgentIsSkippedWithoutReplacement()
    {
        var bindingAgent = SimulationParameters.Instance.GetOrganelleType("bindingAgent");
        foreach (int removableCount in new[] { 9, 10 })
        {
            var definitions = GetCandidates().Take(removableCount).ToArray();
            var original = (MulticellularSpecies)CreateSpecies(true, definitions);
            var layout = original.ModifiableCellTypes[0].ModifiableOrganelles;
            layout.Add(new OrganelleTemplate(bindingAgent, new Hex(-10, 0), 0));
            var candidates = definitions.Append(bindingAgent).ToHashSet();
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
                    AssertThat(GetDefinitions(mutant.Species).Contains(bindingAgent)).IsTrue();

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

    [TestCase]
    public void SharedOrganelleReferenceIsRemovedOnlyFromTargetedCellType()
    {
        var toxin = SimulationParameters.Instance.GetOrganelleType("oxytoxy");
        AssertThat(toxin.GetRotatedHexes(2).SequenceEqual(new[] { new Hex(0, 0) })).IsTrue();
        var original = (MulticellularSpecies)CreateSpecies(true, Array.Empty<OrganelleDefinition>());
        AddCellType(original, Array.Empty<OrganelleDefinition>());
        var shared = new OrganelleTemplate(toxin, new Hex(0, 1), 2)
        {
            ModifiableUpgrades = CreateToxinUpgrades(0.25f),
            IsEndosymbiont = true,
        };
        var layouts = new[] { GetLayout(original), GetLayout(original, 1) };
        foreach (var layout in layouts)
        {
            layout.Add(shared);
            AssertThat(layout.ComputeHexCache().Count).IsEqual(layout.HexCount);
            AssertThat(layout.GetIslandHexes(new List<Hex>(), new HashSet<Hex>(), new List<Hex>(), new Queue<Hex>()))
                .IsEqual(0);
        }

        AssertThat(layouts[0][2]).IsSame(layouts[1][2]);
        var sourceOrganelles = layouts.Select(o => o.Organelles.ToArray()).ToArray();
        var snapshots = sourceOrganelles.Select(o => o.Select(p => p.Clone()).ToArray()).ToArray();
        var mutants = new RemoveOrganelle(o => ReferenceEquals(o, toxin))
            .MutationsOf(original, 1000, false, new XoShiRo256starstar(71), biome);
        AssertThat(mutants).IsNotNull();
        AssertThat(mutants!.Count).IsEqual(2);
        var targetedTypes = new HashSet<int>();
        var retainedCopies = new List<OrganelleTemplate>();
        foreach (var mutant in mutants)
        {
            AssertThat(mutant.Species).IsNotSame(original);
            var counts = new[] { GetLayout(mutant.Species).Count, GetLayout(mutant.Species, 1).Count };
            AssertThat(counts.Count(c => c == 2)).IsEqual(1);
            AssertThat(counts.Count(c => c == 3)).IsEqual(1);
            var targetedType = Array.IndexOf(counts, 2);
            AssertThat(targetedTypes.Add(targetedType)).IsTrue();
            for (int i = 0; i < layouts.Length; ++i)
            {
                var layout = GetLayout(mutant.Species, i);
                AssertThat(layout).IsNotSame(layouts[i]);
                var expected = i == targetedType ?
                    sourceOrganelles[i].Where(o => !ReferenceEquals(o, shared)).ToArray() :
                    sourceOrganelles[i];
                AssertLayoutClones(layout.Organelles, expected);
            }

            retainedCopies.Add(GetLayout(mutant.Species, 1 - targetedType).Organelles
                .Single(o => ReferenceEquals(o.Definition, toxin)));
        }

        AssertThat(targetedTypes.SetEquals([0, 1])).IsTrue();
        AssertLayoutClones([retainedCopies[0]], [retainedCopies[1]]);
        var firstCopy = retainedCopies[0];
        firstCopy.Position = new Hex(20, 20);
        firstCopy.Orientation = 5;
        firstCopy.IsEndosymbiont = false;
        firstCopy.ModifiableUpgrades!.ModifiableUnlockedFeatures.Clear();
        ((ToxinUpgrades)firstCopy.ModifiableUpgrades.CustomUpgradeData!).Toxicity = -0.5f;
        for (int i = 0; i < layouts.Length; ++i)
            AssertLayoutClones(layouts[i].Organelles, snapshots[i]);
        AssertLayoutClones([retainedCopies[1]], [shared]);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ClonedLayoutsPreserveOrderAndKeepMutableUpgradesIndependent(bool multicellular)
    {
        var toxin = SimulationParameters.Instance.GetOrganelleType("oxytoxy");
        var proteins = SimulationParameters.Instance.GetOrganelleType("oxytoxyProteins");
        var mitochondrion = SimulationParameters.Instance.GetOrganelleType("mitochondrion");
        AssertThat(mitochondrion.GetRotatedHexes(1)
            .SequenceEqual(new[] { new Hex(0, 0), new Hex(1, -1) })).IsTrue();

        var original = CreateSpecies(multicellular, new[] { toxin, toxin });
        var source = GetLayout(original);
        source[1].Orientation = 2;
        source[1].ModifiableUpgrades = CreateToxinUpgrades(0.25f);
        source[3].Orientation = 4;
        source[3].ModifiableUpgrades = CreateToxinUpgrades(0.75f);
        source[3].IsEndosymbiont = true;
        source.Add(new OrganelleTemplate(mitochondrion, new Hex(-1, 0), 1));
        var survivor = new OrganelleTemplate(proteins, new Hex(2, 0), 3)
        {
            ModifiableUpgrades = CreateToxinUpgrades(0.5f),
            IsEndosymbiont = true,
        };
        source.Add(survivor);

        OrganelleTemplate[]? otherSource = null;
        if (original is MulticellularSpecies colony)
        {
            AddCellType(colony, Array.Empty<OrganelleDefinition>());
            var otherLayout = GetLayout(colony, 1);
            otherLayout.Add(new OrganelleTemplate(mitochondrion, new Hex(-1, 0), 1));
            otherLayout.Add(survivor.Clone());
            otherSource = otherLayout.Organelles.ToArray();
        }

        var originalOrganelles = source.Organelles.ToArray();
        var originalSnapshot = originalOrganelles.Select(o => o.Clone()).ToArray();
        var otherSnapshot = otherSource?.Select(o => o.Clone()).ToArray();
        var mutants = new RemoveOrganelle(o => ReferenceEquals(o, toxin))
            .MutationsOf(original, 1000, false, new XoShiRo256starstar(71), biome);
        AssertThat(mutants).IsNotNull();
        AssertThat(mutants!.Count).IsEqual(2);
        var remainingToxicities = new List<float>();
        foreach (var mutant in mutants)
        {
            AssertThat(mutant.Species).IsNotSame(original);
            var layout = GetLayout(mutant.Species);
            AssertThat(layout).IsNotSame(source);
            var retained = layout.Organelles.Single(o => ReferenceEquals(o.Definition, toxin));
            remainingToxicities.Add(((ToxinUpgrades)retained.ModifiableUpgrades!.CustomUpgradeData!).Toxicity);
            var removed = originalOrganelles.Single(o => ReferenceEquals(o.Definition, toxin) &&
                !Equals(o.Upgrades, retained.Upgrades));
            AssertLayoutClones(layout.Organelles,
                originalOrganelles.Where(o => !ReferenceEquals(o, removed)).ToArray());
            if (otherSource != null)
                AssertLayoutClones(GetLayout(mutant.Species, 1).Organelles, otherSource);
        }

        AssertThat(remainingToxicities.Order().SequenceEqual([0.25f, 0.75f])).IsTrue();
        var firstCopy = GetLayout(mutants[0].Species).Organelles.Single(o => ReferenceEquals(o.Definition, proteins));
        var siblingCopy = GetLayout(mutants[1].Species).Organelles.Single(o => ReferenceEquals(o.Definition, proteins));
        AssertLayoutClones([firstCopy], [siblingCopy]);
        firstCopy.Position = new Hex(20, 20);
        firstCopy.Orientation = 5;
        firstCopy.IsEndosymbiont = false;
        firstCopy.ModifiableUpgrades!.ModifiableUnlockedFeatures.Clear();
        ((ToxinUpgrades)firstCopy.ModifiableUpgrades.CustomUpgradeData!).Toxicity = -0.5f;
        AssertLayoutClones(source.Organelles, originalSnapshot);
        AssertLayoutClones([siblingCopy], [survivor]);

        if (otherSource != null)
        {
            var firstOther = GetLayout(mutants[0].Species, 1).Organelles;
            var siblingOther = GetLayout(mutants[1].Species, 1).Organelles;
            AssertLayoutClones(firstOther, siblingOther);
            var otherCopy = firstOther.Single(o => ReferenceEquals(o.Definition, proteins));
            otherCopy.ModifiableUpgrades!.ModifiableUnlockedFeatures.Clear();
            ((ToxinUpgrades)otherCopy.ModifiableUpgrades.CustomUpgradeData!).Toxicity = -0.75f;
            AssertLayoutClones(GetLayout(original, 1).Organelles, otherSnapshot!);
            AssertLayoutClones(siblingOther, otherSource);
        }
    }

    private static OrganelleUpgrades CreateToxinUpgrades(float toxicity)
    {
        return new OrganelleUpgrades
        {
            ModifiableUnlockedFeatures = [ToxinUpgradeNames.ToxinNameFromType(ToxinType.Oxytoxy)],
            CustomUpgradeData = new ToxinUpgrades(ToxinType.Oxytoxy, toxicity),
        };
    }

    private static OrganelleLayout<OrganelleTemplate> GetLayout(Species species, int cellType = 0)
    {
        return species is MicrobeSpecies microbe ?
            microbe.Organelles :
            ((MulticellularSpecies)species).ModifiableCellTypes[cellType].ModifiableOrganelles;
    }

    private static void AssertLayoutClones(IReadOnlyList<OrganelleTemplate> actual,
        IReadOnlyList<OrganelleTemplate> expected)
    {
        AssertThat(actual.Count).IsEqual(expected.Count);
        for (int i = 0; i < expected.Count; ++i)
        {
            AssertThat(actual[i]).IsNotSame(expected[i]);
            AssertThat(actual[i].Definition).IsSame(expected[i].Definition);
            AssertThat(actual[i].Position).IsEqual(expected[i].Position);
            AssertThat(actual[i].Orientation).IsEqual(expected[i].Orientation);
            AssertThat(actual[i].IsEndosymbiont).IsEqual(expected[i].IsEndosymbiont);
            AssertThat(Equals(actual[i].Upgrades, expected[i].Upgrades)).IsTrue();
            if (expected[i].ModifiableUpgrades == null)
                continue;

            var upgrades = actual[i].ModifiableUpgrades!;
            var expectedUpgrades = expected[i].ModifiableUpgrades!;
            AssertThat(upgrades).IsNotSame(expectedUpgrades);
            AssertThat(ReferenceEquals(upgrades.ModifiableUnlockedFeatures,
                expectedUpgrades.ModifiableUnlockedFeatures)).IsFalse();
            if (expectedUpgrades.CustomUpgradeData != null)
                AssertThat(upgrades.CustomUpgradeData).IsNotSame(expectedUpgrades.CustomUpgradeData);
        }
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

        // Represent every cell type in the editor body plan used by species cloning.
        var editorCells = species.CellTypes.Count == 0 ?
            new IndividualHexLayout<CellTemplate>() :
            species.ModifiableEditorCells;
        var position = new Hex(species.CellTypes.Count, 0);
        editorCells.Add(new HexWithData<CellTemplate>(new CellTemplate(cellType, position, 0), position, 0));
        species.ModifiableEditorCells = editorCells;
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
