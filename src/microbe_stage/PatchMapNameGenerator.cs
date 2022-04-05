using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

public class PatchMapNameGenerator : IRegistryType
{
    [JsonRequired]
    private int syllablesLowerLimit = 2;

    [JsonRequired]
    private int syllablesHigherLimit = 5;

    [JsonRequired]
    private List<string> syllables = null!;

    [JsonRequired]
    private List<string> suffixes = null!;

    // ReSharper disable once StringLiteralTypo
    private string vowels = "aeiouy";

    private string? continentName;
    private string? patchName;

    public string ContinentName
    {
        get => continentName ?? throw new InvalidOperationException("Value not generated yet");
        private set => continentName = value;
    }

    public string PatchName
    {
        get => patchName ?? throw new InvalidOperationException("Value not generated yet");
        private set => patchName = value;
    }

    public string InternalName { get; set; } = null!;

    /// <summary>
    ///   Generates and returns a new patch name
    /// </summary>
    public string Next(Random random)
    {
        int nameLength = random.Next(syllablesLowerLimit, syllablesHigherLimit + 1);
        if (nameLength == 4)
        {
            if (random.Next() % 2 == 0)
                nameLength -= 1;
        }

        var name = new StringBuilder(50);
        int suffixIndex;

        // Construct the word with syllables
        for (int i = 0; i < nameLength; i++)
        {
            int syllablesIndex = random.Next(0, syllables.Count);

            // First letter is upper case
            name.Append(syllables[syllablesIndex]);
        }

        // Convert first letter to uppercase
        name[0] = char.ToUpper(name[0], CultureInfo.InvariantCulture);

        // Continent name is the name without the genitive
        ContinentName = name.ToString();

        // Choose an appropriate suffix considering last letter
        if (vowels.Contains(name[name.Length - 1]))
        {
            suffixIndex = 2 * random.Next(0, 4);
        }
        else
        {
            suffixIndex = 1 + 2 * random.Next(0, 4);
        }

        name.Append(suffixes[suffixIndex]);

        PatchName = name.ToString();

        return PatchName;
    }

    public void Check(string name)
    {
        if (syllablesLowerLimit >= syllablesHigherLimit)
        {
            throw new InvalidRegistryDataException(nameof(PatchMapGenerator), GetType().Name,
                "Syllable count lower limit is higher or equal to the higher limit");
        }

        if (syllables.Count < 1)
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "No syllables specified");
        }

        if (suffixes.Count < 1)
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "No suffixes specified");
        }
    }

    public void ApplyTranslations()
    {
        // Our data is currently not language specific
    }
}
