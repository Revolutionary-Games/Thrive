using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
///   The microbe species name generator.
/// </summary>
public partial class NameGenerator(SpeciesNameConfig config)
{
    private SpeciesNameConfig config = config;

    private bool GenerateOrganelleBasedName(Random random, StringBuilder stringBuilder, OrganelleDefinition organelle,
        int count, out string root)
    {
        var name = organelle.NameWithoutSpecialCharacters.Replace(" ", "_").ToLowerInvariant();

        // Check for aliases
        if (config.OrganelleMap.TryGetValue(name, out var alias))
            name = alias;

        // Generate prefix
        if (!config.Organelles.TryGetValue(name, out var namingData) ||
            !namingData.TryGetValue("prefixes", out var prefixes))
            throw new Exception($"Invalid data for name {name}");

        PhonotacticsFriendlyAppend(stringBuilder, prefixes.Random(random));

        // Generate optional root suffix
        if (random.Next(3) == 1 && namingData.TryGetValue("suffixes", out var suffixes))
            PhonotacticsFriendlyAppend(stringBuilder, suffixes.Random(random));

        // Generate optional quantity prefix
        int ruleSelector = random.Next(100);
        bool isNumbered;
        string quantityPrefix;
        if (ruleSelector < 5)
        {
            if (count < 1)
            {
                quantityPrefix = string.Empty;
                isNumbered = false;
            }
            else
            {
                // Rule 1: use explicit quantity prefix.
                quantityPrefix = config.Quantity[count.ToString()].Random(random);
                isNumbered = true;
            }
        }
        else if (ruleSelector < 10 + 2 * count)
        {
            // Rule 2: use approximate quantifier for multiple organelles.
            bool single = count == 1;
            quantityPrefix = config.Quantity[single ? "1" : "multiple"].Random(random);
            isNumbered = single;
        }
        else
        {
            // Do not use any quantifier.
            quantityPrefix = string.Empty;
            isNumbered = false;
        }

        root = stringBuilder.ToString();

        stringBuilder.Insert(0, quantityPrefix);

        return isNumbered;
    }

    private bool GenerateProcessBasedName(Random random, StringBuilder stringBuilder, BioProcess process,
        out string root)
    {
        var inputs = process.Inputs.ToArray();
        var outputs = process.Outputs.ToArray();
        var selectedInput = inputs[random.Next(inputs.Length)];
        var selectedOutput = outputs[random.Next(outputs.Length)];

        List<string>? outputNames = null;
        if (!config.Processes.TryGetValue(selectedInput.Key.InternalName, out var inputNames) &&
            !config.Processes.TryGetValue(selectedOutput.Key.InternalName, out outputNames))
        {
            // Both input and output are not mapped in the config. This shouldn't ever happen.
            throw new Exception($"Invalid data: both input and output elements are unmapped." +
                $"{selectedInput.Key.InternalName} {selectedOutput.Key.InternalName}");
        }

        var inputName = inputNames is not null ? inputNames.Random(random) : string.Empty;
        var outputName = outputNames is not null ? outputNames.Random(random) : string.Empty;

        bool oneIsEmpty = inputName == string.Empty || outputName == string.Empty;

        int ruleSelector = oneIsEmpty ? 0 : random.Next(100);
        int affixRule = random.Next(100);

        const int ruleOneThreshold = 80;

        if (affixRule < 5)
        {
            PhonotacticsFriendlyAppend(stringBuilder, config.QualityRoots["liking"].Random(random));
        }

        switch (ruleSelector)
        {
            case < ruleOneThreshold:
                // Rule 1: consider input or output (exclusive)
                GenerateProcessRuleOne(random, stringBuilder, affixRule, ruleSelector, ruleOneThreshold, inputName,
                    outputName);

                break;
            default:
                if (oneIsEmpty)
                {
                    // Rule 1.
                    GenerateProcessRuleOne(random, stringBuilder, affixRule, ruleSelector, ruleOneThreshold, inputName,
                        outputName);
                }
                else
                {
                    // Rule 2: consider both input and output
                    if (affixRule is >= 5 and < 20)
                    {
                        PhonotacticsFriendlyAppend(stringBuilder, config.QualityRoots["mutation"].Random(random));
                    }

                    PhonotacticsFriendlyAppend(stringBuilder, inputName);
                    PhonotacticsFriendlyAppend(stringBuilder, outputName);

                    if (affixRule is >= 20 and < 25)
                    {
                        PhonotacticsFriendlyAppend(stringBuilder, config.QualitySuffixes["mutation"].Random(random));
                    }
                }

                break;
        }

        if (affixRule > 79)
            PhonotacticsFriendlyAppend(stringBuilder, config.QualitySuffixes["liking"].Random(random));

        root = stringBuilder.ToString();

        // Process naming is never numbered
        return false;
    }

    private void GenerateProcessRuleOne(Random random, StringBuilder stringBuilder, int affixRule, int ruleSelector,
        int ruleOneThreshold, string inputName, string outputName)
    {
        bool useInput = (inputName != string.Empty && ruleSelector < ruleOneThreshold / 2) ||
            outputName == string.Empty;

        if (useInput)
        {
            if (affixRule is >= 5 and < 10)
                PhonotacticsFriendlyAppend(stringBuilder, config.QualityRoots["consuming"].Random(random));

            PhonotacticsFriendlyAppend(stringBuilder, inputName);

            if (affixRule is >= 10 and < 25)
                PhonotacticsFriendlyAppend(stringBuilder, config.QualitySuffixes["consuming"].Random(random));
        }
        else
        {
            if (affixRule is >= 5 and < 10)
                PhonotacticsFriendlyAppend(stringBuilder, config.QualityRoots["producing"].Random(random));

            PhonotacticsFriendlyAppend(stringBuilder, outputName);

            if (affixRule is >= 10 and < 25)
                PhonotacticsFriendlyAppend(stringBuilder, config.QualitySuffixes["producing"].Random(random));
        }
    }

    /// <summary>
    ///   A tweaked version of the original name generator that generates the root without the suffix to take gender
    ///   into account.
    /// </summary>
    private void ModifiedLegacyGenerateNameRoot(Random random, StringBuilder stringBuilder)
    {
        if (random.Next(0, 100) >= 10)
        {
            switch (random.Next(0, 4))
            {
                case 0:
                    PhonotacticsFriendlyAppend(stringBuilder, config.PrefixesC.Random(random));
                    break;
                case 1:
                    PhonotacticsFriendlyAppend(stringBuilder, config.PrefixesV.Random(random));
                    break;
                case 2:
                    PhonotacticsFriendlyAppend(stringBuilder, config.PrefixesV.Random(random));
                    PhonotacticsFriendlyAppend(stringBuilder, config.CofixesC.Random(random));
                    break;
                case 3:
                    PhonotacticsFriendlyAppend(stringBuilder, config.PrefixesC.Random(random));
                    PhonotacticsFriendlyAppend(stringBuilder, config.CofixesV.Random(random));
                    break;
                default:
                    throw new Exception("unreachable");
            }
        }
        else
        {
            // ReSharper disable once CommentTypo
            // Developer Easter Eggs and really silly long names here
            // Our own version of wigglesworthia for example
            switch (random.Next(0, 4))
            {
                case 0:
                case 1:
                    stringBuilder.Append(config.PrefixCofix.Random(random));
                    break;
                case 2:
                    stringBuilder.Append(config.PrefixesV.Random(random));
                    stringBuilder.Append(config.CofixesC.Random(random));
                    break;
                case 3:
                    stringBuilder.Append(config.PrefixesC.Random(random));
                    stringBuilder.Append(config.CofixesV.Random(random));
                    break;
                default:
                    throw new Exception("unreachable");
            }
        }
    }
}
