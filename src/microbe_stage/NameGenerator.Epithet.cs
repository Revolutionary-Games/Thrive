using System;
using System.Buffers;
using System.Text;
using Xoshiro.PRNG64;

/// <summary>
///   The microbe species name generator.
/// </summary>
public partial class NameGenerator
{
    public string GenerateEpithetName(Random? random, MicrobeSpecies? speciesOld, MicrobeSpecies speciesNew,
        Patch? patch)
    {
        var stringBuilder = GetBuffer();
        random ??= new XoShiRo256starstar();

        if (speciesNew.NamingState is null)
            throw new Exception("GenerateEpithetName should be called after GenerateGenusName.");

        var gender = speciesNew.NamingState.Gender;

        GenerateEpithetRoot(random, stringBuilder, speciesNew, patch);
        GenerateGenderedSuffix(random, stringBuilder, gender, false);

        stringBuilder[0] = char.ToLowerInvariant(stringBuilder[0]);

        return stringBuilder.ToString();
    }

    private void GenerateEpithetRoot(Random random, StringBuilder stringBuilder, MicrobeSpecies species, Patch? patch)
    {
        double ruleSelector = random.NextDouble() * 100.0;

        if (ruleSelector > 98)
        {
            // Easter egg naming.
            PhonotacticsFriendlyAppend(stringBuilder, config.PrefixCofix.Random(random));
            return;
        }

        const int numberOfRules = 5;

        int[] rules = ArrayPool<int>.Shared.Rent(numberOfRules);
        for (int i = 0; i < numberOfRules; ++i)
            rules[i] = i;

        rules.Shuffle(random, numberOfRules);

        string root = string.Empty;
        for (int i = 0; i < numberOfRules && root == string.Empty; ++i)
        {
            int rule = rules[i];

            switch (rule)
            {
                case 0:
                    // Rule 1: quality-based naming
                    GenerateQualityBasedRoot(random, species, out root);
                    break;
                case 1:
                    // Rule 2: tolerance-based naming
                    GenerateToleranceBasedRoot(random, species, out root);
                    break;
                case 2:
                    // Rule 3: Membrane-based naming
                    GenerateMembraneBasedRoot(random, species, out root);
                    break;
                case 3:
                    // Rile 4: colour-based naming
                    GenerateColourBasedRoot(random, species, out root);
                    break;
                case 4:
                    // Rule 5: patch-based naming
                    GeneratePatchBasedRoot(random, patch, out root);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        if (root == string.Empty)
        {
            // If everything failed, fall back to the old generator.
            ModifiedLegacyGenerateNameRoot(random, stringBuilder);
        }
        else
        {
            PhonotacticsFriendlyAppend(stringBuilder, root);
        }

        ArrayPool<int>.Shared.Return(rules);
    }

    private void GenerateQualityBasedRoot(Random random, MicrobeSpecies species, out string adjective)
    {
        var dictionary = CalculateRelevantQualities(species);
        var key = ExtractWeightedKey(random, dictionary);

        if (key == string.Empty)
        {
            adjective = string.Empty;
            return;
        }

        if (!config.QualityRoots.TryGetValue(key, out var roots))
        {
            throw new Exception($"Quality adjectives not mapped in species_names.json: {key}");
        }

        adjective = roots.Random(random);
    }

    private void GenerateToleranceBasedRoot(Random random, MicrobeSpecies species, out string adjective)
    {
        var dictionary = CalculateRelevantTolerances(species);
        var key = ExtractWeightedKey(random, dictionary);

        if (key == string.Empty)
        {
            adjective = string.Empty;
            return;
        }

        if (!config.Tolerances.TryGetValue(key, out var roots))
        {
            throw new Exception($"Process adjectives not mapped in species_names.json: {key}");
        }

        adjective = roots.Random(random);
    }

    private void GenerateMembraneBasedRoot(Random random, MicrobeSpecies species, out string adjective)
    {
        var key = species.MembraneType.InternalName;

        if (key == "single")
        {
            adjective = string.Empty;
            return;
        }

        if (!config.Membranes.TryGetValue(key, out var roots))
        {
            throw new Exception($"Membrane adjectives not mapped in species_names.json: {key}");
        }

        adjective = roots.Random(random);
    }

    private void GenerateColourBasedRoot(Random random, MicrobeSpecies species, out string adjective)
    {
        var colour = CalculateColourAdjective(species);

        if (colour == string.Empty)
        {
            adjective = string.Empty;
            return;
        }

        if (!config.Colours.TryGetValue(colour, out var colourRoots))
        {
            throw new Exception($"Colour adjectives not mapped in species_names.json: {colour}");
        }

        adjective = colourRoots.Random(random);
    }

    private void GeneratePatchBasedRoot(Random random, Patch? patch, out string adjective)
    {
        adjective = patch is null ?
            SimulationParameters.Instance.PatchMapNameGenerator.Next(random).ContinentName :
            patch.Region.Name;
    }
}
