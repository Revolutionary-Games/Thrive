using System;
using System.Collections.Generic;
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
    private List<string> sufixes = null!;

    private string vowels = "aeiou";
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
            sufixIndex = 2 * random.Next(0, 2);
        }
        else
        {
            sufixIndex = 1 + 2 * random.Next(0, 2);
        }

        name += sufixes[sufixIndex];

        // Convert first letter to uppercase
        char[] charName = name.ToCharArray();
        charName[0] = char.ToUpper(name[0]);
        name = new string(charName);

        return name;
    }

    // TODO
    public void Check(string name)
    {
    }

    // TODO
    public void ApplyTranslations()
    {
    }
}
