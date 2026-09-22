using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoEvo;
using GdUnit4;
using Godot;
using SharedBase.Archive;
using Xoshiro.PRNG64;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class SpeciesNamingRandomTests
{
    // Independently evaluated from the shipped word lists and pinned PRNG, without invoking Thrive's naming code.
    // Each group covers ordinary shapes 0-3 and rare shapes 0-3, in that order.
    private static readonly NameVector[] Vectors =
    [
        new(true, 0, "Parent", "hymis", new Color(0.30150998f, 0.45829454f, 0.6164984f), 2146403189),
        new(true, 1, "Parent", "paquito", new Color(0.53705287f, 0.4456643f, 0.5985892f), 616637202),
        new(true, 7, "Parent", "paynula", new Color(0.74054885f, 0.7454301f, 0.68638694f), 355236246),
        new(true, 2, "Parent", "nubionea", new Color(0.6239261f, 0.59307486f, 0.3679934f), 2028037),
        new(true, 45, "Parent", "glossinensis", new Color(0.34800494f, 0.5669061f, 0.4407277f), 1006239588),
        new(true, 6, "Parent", "whalales", new Color(0.6894129f, 0.32081008f, 0.34426624f), 195029414),
        new(true, 16, "Parent", "squytpiun", new Color(0.27586854f, 0.65154976f, 0.60044396f), 1744596735),
        new(true, 8, "Parent", "varupstir", new Color(0.7217381f, 0.44794258f, 0.36082104f), 222490630),
        new(false, 0, "Hymis", "unus", new Color(0.6164984f, 0.74987423f, 0.46110576f), 153136484),
        new(false, 1, "Paquito", "vipales", new Color(0.5985892f, 0.32178602f, 0.2855226f), 1637174732),
        new(false, 7, "Paynula", "uritustir", new Color(0.68638694f, 0.28037605f, 0.3022179f), 1733906326),
        new(false, 2, "Nubionea", "eneccys", new Color(0.3679934f, 0.57356256f, 0.35952622f), 467883415),
        new(false, 45, "Glossinensis", "pasulapsis", new Color(0.6171417f, 0.5187133f, 0.73051006f), 1117981185),
        new(false, 6, "Whalales", "olivux", new Color(0.34426624f, 0.27270442f, 0.27261162f), 1332812298),
        new(false, 16, "Squytpiun", "squstir", new Color(0.60044396f, 0.7420478f, 0.45393115f), 1739906036),
        new(false, 8, "Varupstir", "lacanus", new Color(0.36082104f, 0.4823361f, 0.55376965f), 1472384357),
    ];

    [TestCase(false)]
    [TestCase(true)]
    public void NamingBranchesAdvanceTheSuppliedStreamBeforeColour(bool multicellular)
    {
        foreach (var vector in Vectors)
        {
            var parent = CreateSpecies(multicellular, 50);
            var candidate = (Species)parent.Clone();
            if (!vector.KeepGenus)
            {
                var organelle = new OrganelleTemplate(SimulationParameters.Instance.GetOrganelleType("rusticyanin"),
                    new Hex(1, 0), 0);
                if (candidate is MicrobeSpecies microbe)
                {
                    microbe.Organelles.Add(organelle);
                }
                else
                {
                    ((MulticellularSpecies)candidate).ModifiableCellTypes[0].ModifiableOrganelles.Add(organelle);
                }
            }

            var random = new XoShiRo256starstar(vector.Seed);
            MutationLogicFunctions.NameNewSpecies(random, candidate, parent);
            AssertThat(candidate.Genus).IsEqual(vector.Genus);
            AssertThat(candidate.Epithet).IsEqual(vector.Epithet);
            if (candidate is MicrobeSpecies microbeCandidate)
            {
                MutationLogicFunctions.ColourNewMicrobeSpecies(random, microbeCandidate, (MicrobeSpecies)parent);
            }
            else
            {
                var multicellularCandidate = (MulticellularSpecies)candidate;
                MutationLogicFunctions.ColourNewMulticellularSpecies(random, multicellularCandidate,
                    (MulticellularSpecies)parent);
                AssertColour(multicellularCandidate.CellTypes[0].Colour, vector.Colour);
            }

            AssertColour(candidate.SpeciesColour, vector.Colour);
            AssertThat(random.Next()).IsEqual(vector.Next);
        }
    }

    [TestCase]
    public void NamingRejectsNullInsteadOfFallingBackToExternalEntropy()
    {
        var parent = CreateSpecies(false, 50);
        var candidate = (Species)parent.Clone();
        AssertThrown(() => MutationLogicFunctions.NameNewSpecies(null!, candidate, parent))
            .IsInstanceOf<ArgumentNullException>();
        AssertThat(candidate.Genus).IsEqual("Parent");
        AssertThat(candidate.Epithet).IsEqual("original");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void GatheredModifyTaskNamesTwoRetainedCandidatesOnItsExistingStream(bool multicellular)
    {
        var settings = new WorldGenerationSettings
        {
            Seed = 0,
            WorldSize = WorldGenerationSettings.WorldSizeEnum.Small,
            AutoEvoConfiguration = new AutoEvoConfiguration { MutationsPerSpecies = 1 },
        };
        var world = new GameWorld(settings);
        var template = world.Map.CurrentPatch!;
        var patch = new Patch(new LocalizedString("TEST_PATCH"), 11, template.BiomeTemplate, template.BiomeType,
            template.Region, 0);
        var parents = new[] { CreateSpecies(multicellular, 50), CreateSpecies(multicellular, 51) };
        foreach (var parent in parents)
            patch.AddSpecies(parent, 1000);
        world.Map.Patches.Clear();
        world.Map.AddPatch(patch);
        world.Map.CurrentPatch = patch;

        // Use normal Gather, preserving the actual task and private RNG. No RNG replacement or reseeding.
        var run = new AutoEvoRun(world, world.AutoEvoGlobalCache);
        run.OneStep();
        AssertThat(run.Aborted).IsFalse();
        var task = ReadField<Queue<IRunStep>>(run, "runSteps").OfType<ModifyExistingSpecies>().Single();
        AssertThat(task.RandomSeed).IsEqual(7928256305485563229L);
        var random = ReadField<Random>(task, "random");

        // Bound the ecology, not the task: two independent niches yield one viable candidate per parent.
        // All mutation, filter, insertion, population, naming, colour and final publication stages run normally.
        var root = new Miche(new NamingPressure(0, null));
        var strategies = parents.Select(parent => new NamingMutation(parent.ID)).ToArray();
        foreach (var strategy in strategies)
            root.AddChild(new Miche(new NamingPressure(strategy.Owner, strategy)));
        var results = new RunResults();
        results.AddNewMicheForPatch(patch, root);
        for (var i = 0; i < 5; ++i)
            AssertThat(task.RunStep(results)).IsFalse();
        AssertThat(results.GetPossibleSpeciesList().Count).IsEqual(0);
        AssertThat(task.RunStep(results)).IsTrue();
        AssertThat(ReadField<Random>(task, "random")).IsSame(random);

        var kept = results.GetPossibleSpeciesList();
        AssertThat(kept.Count).IsEqual(2);
        AssertThat(strategies.All(strategy => ReferenceEquals(strategy.Random, random))).IsTrue();

        // One Next(0, 2) shuffle precedes the two name/colour pairs. Fixed oracle includes both pairs.
        AssertThat(kept[0].ParentSpecies).IsSame(parents[1]);
        AssertThat(kept[1].ParentSpecies).IsSame(parents[0]);
        var names = new[] { "eten", "nelarilia" };
        var colours = new[]
        {
            new Color(0.522839f, 0.29509908f, 0.35396814f),
            new Color(0.5321932f, 0.25626385f, 0.6674973f),
        };
        for (var i = 0; i < kept.Count; ++i)
        {
            var candidate = kept[i].Species;
            AssertThat(candidate).IsSame(strategies.Single(strategy => strategy.Owner == candidate.ID).Candidate);
            AssertThat(kept[i].InitialPopulationInPatches.Value)
                .IsGreater(Constants.AUTO_EVO_MINIMUM_VIABLE_POPULATION);
            AssertThat(candidate.Genus).IsEqual("Parent");
            AssertThat(candidate.Epithet).IsEqual(names[i]);
            AssertColour(candidate.SpeciesColour, colours[i]);
            if (candidate is MulticellularSpecies colony)
                AssertColour(colony.CellTypes[0].Colour, colours[i]);
            GD.Print($"Task naming: multicellular={multicellular}, seed={task.RandomSeed}, index={i}, " +
                $"owner={candidate.ID}, name={candidate.Genus} {candidate.Epithet}, colour={candidate.SpeciesColour}");
        }

        AssertThat(random.Next()).IsEqual(1847776896);
        foreach (var parent in parents)
        {
            AssertThat(parent.Epithet).IsEqual("original");
            AssertColour(parent.SpeciesColour, new Color(0.5f, 0.5f, 0.5f));
        }
    }

    private static Species CreateSpecies(bool multicellular, uint id)
    {
        var parameters = SimulationParameters.Instance;
        var cytoplasm = parameters.GetOrganelleType("cytoplasm");
        var membrane = parameters.GetMembrane("single");
        var colour = new Color(0.5f, 0.5f, 0.5f);
        if (!multicellular)
        {
            var microbe = new MicrobeSpecies(id, "Parent", "original")
            {
                MembraneType = membrane,
                IsBacteria = true,
                SpeciesColour = colour,
            };
            microbe.Organelles.Add(new OrganelleTemplate(cytoplasm, new Hex(0, 0), 0));
            microbe.OnEdited();
            return microbe;
        }

        var species = new MulticellularSpecies(id, "Parent", "original") { SpeciesColour = colour };
        var cellType = new CellType(membrane) { CellTypeName = "TestType", IsBacteria = true, Colour = colour };
        cellType.ModifiableOrganelles.Add(new OrganelleTemplate(cytoplasm, new Hex(0, 0), 0));
        species.ModifiableCellTypes.Add(cellType);
        species.ModifiableGameplayCells.AddFast(new CellTemplate(cellType, new Hex(0, 0), 0), [], []);
        species.OnEdited();
        return species;
    }

    private static void AssertColour(Color actual, Color expected)
    {
        AssertThat(actual.R).IsEqual(expected.R);
        AssertThat(actual.G).IsEqual(expected.G);
        AssertThat(actual.B).IsEqual(expected.B);
        AssertThat(actual.A).IsEqual(1.0f);
    }

    private static T ReadField<T>(object instance, string name)
    {
        return (T)(instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance)
            ?? throw new InvalidOperationException($"Missing field {name}"));
    }

    private sealed record NameVector(bool KeepGenus, long Seed, string Genus, string Epithet, Color Colour, int Next);

    private sealed class NamingMutation(uint owner) : IMutationStrategy<Species>
    {
        public uint Owner => owner;
        public Species? Candidate { get; private set; }
        public Random? Random { get; private set; }
        public bool Repeatable => false;

        public List<CommonMutationFunctions.Mutant>? MutationsOf(Species baseSpecies, double mp, bool lawk,
            Random random, BiomeConditions biomeToConsider)
        {
            if (baseSpecies.ID != owner || Candidate != null)
                return null;

            Random = random;
            Candidate = baseSpecies is MulticellularSpecies colony ?
                colony.Clone(true, false) :
                (Species)baseSpecies.Clone();
            var organelle = new OrganelleTemplate(SimulationParameters.Instance.GetOrganelleType("cytoplasm"),
                new Hex(1, 0), 0);
            if (Candidate is MicrobeSpecies microbe)
            {
                microbe.Organelles.Add(organelle);
            }
            else
            {
                ((MulticellularSpecies)Candidate).ModifiableCellTypes[0].ModifiableOrganelles.Add(organelle);
            }

            return [new CommonMutationFunctions.Mutant(Candidate, mp - 1)];
        }
    }

    private sealed class NamingPressure(uint owner, NamingMutation? strategy) :
        SelectionPressure(1, strategy == null ? [] : [strategy])
    {
        public override LocalizedString Name => new("TEST_NAMING");
        public override ushort CurrentArchiveVersion => 1;
        public override ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.RootPressure;

        public override float Score(Species species, Patch patch, SimulationCache cache)
        {
            if (owner == 0)
                return 1;
            if (species.ID != owner)
                return 0;

            return ReferenceEquals(species, strategy!.Candidate) ? 2 : 1;
        }

        public override float GetEnergy(Patch patch)
        {
            return owner == 0 ? 0 : 10000;
        }
    }
}
