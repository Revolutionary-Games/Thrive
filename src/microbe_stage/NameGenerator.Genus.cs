using System;
using System.Linq;
using System.Text;
using Xoshiro.PRNG64;

/// <summary>
///   The microbe species name generator.
/// </summary>
public partial class NameGenerator
{
    public string GenerateGenusName(Random? random, MicrobeSpecies? speciesOld, MicrobeSpecies speciesNew)
    {
        var stringBuilder = GetBuffer();
        random ??= new XoShiRo256starstar();

        GenerateGenusNameInternal(random, stringBuilder, speciesOld, speciesNew, out var isNumbered, out var isProto,
            out var newRoot, out var newGender, out var target);

        speciesNew.NamingState = new NamingState(isNumbered, isProto, newRoot, newGender, target);

        stringBuilder[0] = char.ToUpperInvariant(stringBuilder[0]);

        var name = stringBuilder.ToString();

        return name;
    }

    public void GenerateGenusNameInternal(Random random, StringBuilder stringBuilder,
        MicrobeSpecies? speciesOld, MicrobeSpecies speciesNew, out bool isNumbered, out bool isProto,
        out string newRoot, out GrammaticalGender newGender, out INameGenerationTarget? target)
    {
        target = null;

        if (speciesOld is null)
        {
            GenerateFreshGenusName(random, stringBuilder, speciesNew, out target, out newRoot, out newGender,
                out isProto);

            isNumbered = false;

            return;
        }

        var species1UniqueOrganelles = speciesOld.Organelles.Select(o => o.Definition).ToHashSet();
        var species2UniqueOrganelles = speciesNew.Organelles.Select(o => o.Definition).ToHashSet();
        var newOrganelles = species2UniqueOrganelles.Except(species1UniqueOrganelles).ToHashSet();
        var lostOrganelles = species1UniqueOrganelles.Except(species2UniqueOrganelles).ToHashSet();

        var randomOrganelle = newOrganelles.Count == 0 ?
            species2UniqueOrganelles.Random(random) :
            newOrganelles.Random(random);

        int organelleCount = speciesNew.Organelles.Count(organelle => organelle.Definition == randomOrganelle);

        bool useBacteriaSuffix = random.Next(100) < 10 && speciesNew.IsBacteria;

        // Without too many headaches, we regenerate the genus root when we lose at least one organelle.
        // This ensures having a new unique name that is unrelated to the lost organelle or processes without comparing
        // all the new processes to the old target.
        bool regenerateRoot = lostOrganelles.Count > 0;

        if (speciesOld.NamingState is not null)
        {
            var namingState = speciesOld.NamingState!;

            if (namingState.Target == null || !regenerateRoot)
            {
                if (newOrganelles.Count == 0 && speciesOld.NamingState is { GenusIsNumbered: false })
                {
                    // We don't need to create a new genus name, so we return the old one.
                    if (!namingState.GenusIsProto)
                    {
                        stringBuilder.Append(speciesOld.Genus);

                        isNumbered = namingState.GenusIsNumbered;
                        isProto = namingState.GenusIsProto;
                        newRoot = namingState.GenusRoot;
                        newGender = namingState.Gender;

                        return;
                    }

                    PhonotacticsFriendlyAppend(stringBuilder, "Eu");
                    PhonotacticsFriendlyAppend(stringBuilder, namingState.GenusRoot);

                    isNumbered = false;
                    isProto = false;
                    newRoot = namingState.GenusRoot;
                    newGender = GenerateGenderedSuffix(random, stringBuilder, namingState.Gender, useBacteriaSuffix,
                        speciesNew);

                    return;
                }

                if (namingState.GenusIsProto && random.Next(3) == 0)
                {
                    PhonotacticsFriendlyAppend(stringBuilder, config.QualityRoots["new"].Random(random));
                    PhonotacticsFriendlyAppend(stringBuilder, namingState.GenusRoot);

                    isNumbered = namingState.GenusIsNumbered;
                    isProto = false;
                    newRoot = namingState.GenusRoot;
                    newGender = GenerateGenderedSuffix(random, stringBuilder, namingState.Gender, useBacteriaSuffix,
                        speciesNew);

                    // Force genus change on the old species to acquire the "Eu-" prefix.
                    speciesOld.Genus = GenerateGenusName(random, speciesOld, speciesOld);
                    speciesOld.Epithet = GenerateEpithetName(random, speciesOld, speciesOld, null);

                    return;
                }

                target = null;
            }

            if (namingState.GenusIsNumbered && randomOrganelle == namingState.Target)
            {
                // Override other rules: we increment the organelle count to maintain consistency in the naming system.
                if (!config.Quantity.TryGetValue(organelleCount.ToString(), out var quantity))
                {
                    quantity = config.Quantity["multiple"];
                    isNumbered = false;
                }
                else
                {
                    isNumbered = true;
                }

                PhonotacticsFriendlyAppend(stringBuilder, quantity.Random(random));
                PhonotacticsFriendlyAppend(stringBuilder, namingState.GenusRoot);

                isProto = false;
                newRoot = namingState.GenusRoot;
                newGender = GenerateGenderedSuffix(random, stringBuilder, namingState.Gender, useBacteriaSuffix,
                    speciesNew);

                return;
            }
        }

        isProto = false;

        GenerateGenusRoot(random, stringBuilder, randomOrganelle, organelleCount, out target, out isNumbered,
            out var root);
        var gender = GenerateGenderedSuffix(random, stringBuilder, null, useBacteriaSuffix, speciesNew);

        newRoot = root;
        newGender = gender;
    }

    private void GenerateFreshGenusName(Random random, StringBuilder stringBuilder, MicrobeSpecies species,
        out INameGenerationTarget? target, out string root, out GrammaticalGender gender, out bool isProto)
    {
        target = null;

        var speciesUniqueOrganelles = species.Organelles.Select(o => o.Definition).ToHashSet();

        var randomOrganelle = speciesUniqueOrganelles.Random(random);

        int organelleCount = species.Organelles.Count(organelle => organelle.Definition == randomOrganelle);

        isProto = false;

        if (random.Next(100) < 15)
        {
            PhonotacticsFriendlyAppend(stringBuilder, config.QualityRoots["first"].Random(random));

            isProto = true;
        }

        int ruleSelector = random.Next(100);

        root = string.Empty;

        switch (ruleSelector)
        {
            case < 10:
                // Rule 1: cell root is "Cyto".
                PhonotacticsFriendlyAppend(stringBuilder, "cyto");
                break;
            case < 25:
                // Rule 2: cell root is "Prim".
                PhonotacticsFriendlyAppend(stringBuilder, "prim");
                break;
            default:
                GenerateGenusRoot(random, stringBuilder, randomOrganelle, organelleCount, out target, out _, out root);
                break;
        }

        if (root == string.Empty)
            root = stringBuilder.ToString();

        gender = GenerateGenderedSuffix(random, stringBuilder, null, true, species);
    }

    private void GenerateGenusRoot(Random random, StringBuilder stringBuilder, OrganelleDefinition randomOrganelle,
        int organelleCount, out INameGenerationTarget target, out bool isNumbered, out string root)
    {
        isNumbered = false;
        target = randomOrganelle;

        var randomOrganelleProcesses = randomOrganelle.RunnableProcesses;

        int ruleSelector = random.Next(100);

        const int ruleThreshold = 48;

        switch (ruleSelector)
        {
            case < ruleThreshold:
                if (randomOrganelleProcesses.Count == 0)
                {
                    // There are no processes for the selected organelle.
                    // Rule 1: use the organelle definition naming system
                    isNumbered = GenerateOrganelleBasedName(random, stringBuilder, randomOrganelle, organelleCount,
                        out root);
                }
                else
                {
                    // Rule 2: use the process definition naming system
                    var process = randomOrganelleProcesses.Random(random).Process;

                    isNumbered = GenerateProcessBasedName(random, stringBuilder, process, out root);

                    target = process;
                }

                break;
            case < 2 * ruleThreshold:
                // Rule 1.
                isNumbered = GenerateOrganelleBasedName(random, stringBuilder, randomOrganelle, organelleCount,
                    out root);
                break;
            default:
                // Rule 3: easter egg naming
                PhonotacticsFriendlyAppend(stringBuilder, config.PrefixCofix.Random(random));
                root = stringBuilder.ToString();
                break;
        }
    }
}
