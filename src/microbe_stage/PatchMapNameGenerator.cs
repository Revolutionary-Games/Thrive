using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class PatchMapNameGenerator : IRegistryType
{
    [JsonRequired]
    private int syllablesLowerLimit = 2;

    [JsonRequired]
    private int syllablesHigherLimit = 5;

    private string continentName = null!;
    private string patchName = null!;

    [JsonRequired]
    private List<string> syllables = null!;

    [JsonRequired]
    private List<string> suffixes = null!;

    private string vowels = "aeiouy";
    public string InternalName { get; set; } = null!;

    public string GetContinentName()
    {
        return continentName;
    }

    public string GetPatchName()
    {
        return patchName;
    }

    /// <summary>
    ///   Generates and returns a new patch name
    /// </summary>
    public string Next()
    {
        Random random = new Random();

        int nameLength = random.Next(syllablesLowerLimit, syllablesHigherLimit + 1);
        if (nameLength == 4)
        {
            if (random.Next() % 2 == 0)
                nameLength -= 1;
        }

        string name = string.Empty;
        int sufixIndex;

        // Contruct the word with syllables
        for (int i = 0; i < nameLength; i++)
        {
            int syllabelsIndex = random.Next(0, syllables.Count);
            name += syllables[syllabelsIndex];
        }

        // Continent name is the name without the genitive
        continentName = name;

        // Choose an apropiate suffix considering last letter
        if (vowels.Contains(name[name.Length - 1]))
        {
            sufixIndex = 2 * random.Next(0, 4);
        }
        else
        {
            sufixIndex = 1 + 2 * random.Next(0, 4);
        }

        name += suffixes[sufixIndex];

        // Convert first letter to uppercase
        char[] charName = name.ToCharArray();
        charName[0] = char.ToUpper(name[0]);
        name = new string(charName);

        return name;
    }

    public void Check(string name)
    {
        if (syllablesLowerLimit >= syllablesHigherLimit)
        {
            throw new InvalidRegistryDataException(nameof(PatchMapGenerator), GetType().Name,
                "Syllable count lower limit is higher or equal to the higher limit");
        }
    }

    // TODO
    public void ApplyTranslations()
    {
    }
}
