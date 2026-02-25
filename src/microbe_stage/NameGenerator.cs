using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Xoshiro.PRNG64;

public class NameGenerator(SpeciesNameConfig config)
{
    private static readonly HashSet<char> Vocals = ['a', 'e', 'i', 'o', 'u'];
    private static readonly HashSet<char> Bilabials = ['p', 'b', 'm'];
    private static readonly FrozenDictionary<(char, char), char> VocalicTransitions = new Dictionary<(char, char), char>
    {
        { ('o', 'a'), 'o' },
        { ('e', 'a'), 'e' },
        { ('e', 'u'), 'u' },
    }.ToFrozenDictionary();

    private static StringBuilder? stringBuffer;

    private SpeciesNameConfig config = config;

    public enum GrammaticalGender
    {
        Masculine,
        Feminine,
        Neuter,
        Ambiguous,
    }

    public string GenerateGenusName(Random? random, MicrobeSpecies? speciesOld, MicrobeSpecies speciesNew)
    {
        var stringBuilder = GetBuffer();
        random ??= new XoShiRo256starstar();

        GenerateGenusNameInternal(random, stringBuilder, speciesOld, speciesNew, out var isNumbered, out var isProto,
            out var newRoot, out var newGender, out var numberedOrganelle);

        speciesNew.NamingState = new NamingState(isNumbered, isProto, newRoot, newGender, numberedOrganelle);

        stringBuilder[0] = char.ToUpperInvariant(stringBuilder[0]);

        return stringBuilder.ToString();
    }

    public string GenerateEpithetName(Random? random, MicrobeSpecies? speciesOld, MicrobeSpecies speciesNew)
    {
        var stringBuilder = GetBuffer();
        random ??= new XoShiRo256starstar();

        if (speciesNew.NamingState is null)
            throw new Exception("GenerateEpithetName should be called after GenerateGenusName.");

        var gender = speciesNew.NamingState.Gender;

        ModifiedLegacyGenerateNameRoot(random, stringBuilder);
        GenerateGenderedSuffix(random, stringBuilder, gender);

        stringBuilder[0] = char.ToLowerInvariant(stringBuilder[0]);

        return stringBuilder.ToString();
    }

    private static StringBuilder GetBuffer()
    {
        stringBuffer ??= new StringBuilder(64);

        stringBuffer.Length = 0;
        return stringBuffer;
    }

    private void GenerateGenusNameInternal(Random random, StringBuilder stringBuilder,
        MicrobeSpecies? speciesOld, MicrobeSpecies speciesNew, out bool isNumbered, out bool isProto,
        out string newRoot, out GrammaticalGender newGender, out OrganelleDefinition? numberedOrganelle)
    {
        numberedOrganelle = null;

        if (speciesOld?.NamingState is null)
        {
            GenerateFreshGenusName(random, stringBuilder, speciesNew, out newRoot, out newGender);

            isNumbered = false;
            isProto = false;

            return;
        }

        var namingState = speciesOld.NamingState!;

        var species1UniqueOrganelles = speciesOld.Organelles.Select(o => o.Definition).ToHashSet();
        var species2UniqueOrganelles = speciesNew.Organelles.Select(o => o.Definition).ToHashSet();
        var newOrganelles = species2UniqueOrganelles.Except(species1UniqueOrganelles).ToHashSet();

        // var lostOrganelles = species1UniqueOrganelles.Except(species2UniqueOrganelles).ToHashSet();

        GD.Print($"generating a new name with {newOrganelles.Count} new organelle defs");

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
            newGender = GenerateGenderedSuffix(random, stringBuilder, namingState.Gender);

            return;
        }

        if (namingState.GenusIsProto && random.Next(3) == 0)
        {
            PhonotacticsFriendlyAppend(stringBuilder, config.QualityRoots["new"].Random(random));
            PhonotacticsFriendlyAppend(stringBuilder, namingState.GenusRoot);

            isNumbered = namingState.GenusIsNumbered;
            isProto = false;
            newRoot = namingState.GenusRoot;
            newGender = GenerateGenderedSuffix(random, stringBuilder, namingState.Gender);

            return;
        }

        var randomOrganelle = newOrganelles.Count == 0 ? species2UniqueOrganelles.Random(random) :
            newOrganelles.Random(random);

        int organelleCount = speciesNew.Organelles.Count(organelle => organelle.Definition == randomOrganelle);

        if (namingState.GenusIsNumbered && randomOrganelle == namingState.NumberedOrganelle)
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
            newGender = GenerateGenderedSuffix(random, stringBuilder, namingState.Gender);

            return;
        }

        isProto = false;

        GenerateGenusRoot(random, stringBuilder, randomOrganelle, organelleCount, out isNumbered, out var root);
        var gender = GenerateGenderedSuffix(random, stringBuilder, null);

        if (isNumbered)
            numberedOrganelle = randomOrganelle;

        newRoot = root;
        newGender = gender;
    }

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
        switch (ruleSelector)
        {
            case < 5:
                if (count < 1)
                    goto default;

                // Rule 1: use explicit quantity prefix.
                quantityPrefix = config.Quantity[count.ToString()].Random(random);
                isNumbered = true;
                break;
            case < 10:
                if (count < 1)
                    goto default;

                // Rule 2: use approximate quantifier for multiple organelles.
                bool single = count == 1;
                quantityPrefix = config.Quantity[single ? "1" : "multiple"].Random(random);
                isNumbered = single;
                break;
            default:
                // Do not use any quantifier.
                quantityPrefix = string.Empty;
                isNumbered = false;
                break;
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
            // Both input and output are not mapped in the config. This shouldn't happen with the current species_names,
            // nor it should ever happen.
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

    private void GenerateFreshGenusName(Random random, StringBuilder stringBuilder, MicrobeSpecies species,
        out string root, out GrammaticalGender gender)
    {
        var speciesUniqueOrganelles = species.Organelles.Select(o => o.Definition).ToHashSet();

        var randomOrganelle = speciesUniqueOrganelles.Random(random);

        int organelleCount = species.Organelles.Count(organelle => organelle.Definition == randomOrganelle);

        bool isProto = false;
        bool isNumbered = false;

        if (random.Next(100) < 50)
        {
            PhonotacticsFriendlyAppend(stringBuilder, config.QualityRoots["first"].Random(random));

            isProto = true;
        }

        int ruleSelector = random.Next(100);

        root = string.Empty;

        switch (ruleSelector)
        {
            case < 20:
                // Rule 1: cell root is "Cyto".
                PhonotacticsFriendlyAppend(stringBuilder, "cyto");
                break;
            case < 50:
                // Rule 2: cell root is "Prim".
                PhonotacticsFriendlyAppend(stringBuilder, "prim");
                break;
            default:
                GenerateGenusRoot(random, stringBuilder, randomOrganelle, organelleCount, out isNumbered, out root);
                break;
        }

        if (root == string.Empty)
            root = stringBuilder.ToString();

        species.NamingState = new NamingState(isNumbered, isProto, root, GrammaticalGender.Neuter);

        gender = GenerateGenderedSuffix(random, stringBuilder, null);
    }

    private void GenerateGenusRoot(Random random, StringBuilder stringBuilder, OrganelleDefinition randomOrganelle,
        int organelleCount, out bool isNumbered, out string root)
    {
        isNumbered = false;

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
                    isNumbered = GenerateProcessBasedName(random, stringBuilder,
                        randomOrganelleProcesses.Random(random).Process, out root);
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

    private GrammaticalGender GenerateGenderedSuffix(Random random, StringBuilder stringBuilder, GrammaticalGender?
        parent)
    {
        var gender = parent ?? GetRandomElement(random, (GrammaticalGender[])
            Enum.GetValuesAsUnderlyingType<GrammaticalGender>());

        var genderName = gender.ToString().ToLowerInvariant();
        var genderedSuffix = config.Suffixes[genderName].Random(random);

        PhonotacticsFriendlyAppend(stringBuilder, genderedSuffix);

        return gender;
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

    private void PhonotacticsFriendlyAppend(StringBuilder stringBuilder, string data)
    {
        if (stringBuilder.Length == 0)
        {
            stringBuilder.Append(data);
            return;
        }

        var next = data[0];
        var previous = stringBuilder[^1];

        if (Vocals.Contains(next) && Vocals.Contains(previous))
        {
            // Both are vocals
            if (next == previous)
            {
                --stringBuilder.Length;
            }
            else if (VocalicTransitions.TryGetValue((previous, next), out var vocal))
            {
                --stringBuilder.Length;
                stringBuilder.Append(vocal);
                stringBuilder.Append(data, 1, data.Length - 1);

                return;
            }
        }
        else if (!Vocals.Contains(next) && !Vocals.Contains(previous))
        {
            switch (previous)
            {
                // Both are consonants
                case 'n' when Bilabials.Contains(next):
                    // n before bilabial becomes m
                    --stringBuilder.Length;
                    stringBuilder.Append('m');
                    break;
                case 'n' when next is 'r' or 'l':
                    // n before liquid gets assimilated
                    --stringBuilder.Length;
                    stringBuilder.Append(next);
                    break;
            }
        }

        stringBuilder.Append(data);
    }

    /*
    private string GetColorByGender(ColorAdjective color, GrammaticalGender gender)
    {
        return gender switch
        {
            GrammaticalGender.Masculine => color.Masculine,
            GrammaticalGender.Feminine => color.Feminine,
            GrammaticalGender.Neuter => color.Neuter,
            _ => color.Masculine,
        };
    }
    */

    private T GetRandomElement<T>(Random random, IList<T> list)
    {
        return list[random.Next(list.Count)];
    }

    private T GetRandomElement<T>(Random random, T[] array)
    {
        return array[random.Next(array.Length)];
    }

    public record NamingState(bool GenusIsNumbered = false, bool GenusIsProto = false, string GenusRoot = "",
        GrammaticalGender Gender = GrammaticalGender.Neuter, OrganelleDefinition? NumberedOrganelle = null)
    {
        public bool GenusIsNumbered = GenusIsNumbered;
        public bool GenusIsProto = GenusIsProto;
        public string GenusRoot = GenusRoot;
        public GrammaticalGender Gender = Gender;
        public OrganelleDefinition? NumberedOrganelle = NumberedOrganelle;
    }
}
