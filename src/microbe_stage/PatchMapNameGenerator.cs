using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Godot;
using System.Linq;
public class PatchMapNameGenerator : IRegistryType
{
    [JsonRequired]
    private int syllablesLowerLimit;
    [JsonRequired]
    private int syllablesHigherLimit;
    private string continentName = null!;
    private string patchName = null!;
    [JsonRequired]
    private List<string> syllables = null!;
    [JsonRequired]
    private List<string> sufixes = null!;
    private string vowels = "aeiou";
    public string InternalName{ get; set; } = null!;
    public string GetContinentName()
    {
        return continentName;
    }
    public string GetPatchName()
    {
        return patchName;
    }
    public string Next()
    {
        Random random = new Random();

        int nameLength = random.Next(syllablesLowerLimit, syllablesHigherLimit + 1);
        string name = "";
        int sufixIndex;

        // Contruct the word with syllables
        for (int i = 0; i < nameLength; i++)
        {
            int syllabelsIndex = random.Next(0,syllables.Count);
            name += syllables [syllabelsIndex];
        }
        GD.Print(name);
        // Continent name is the name without the genitive
        continentName = name;

        // Choose an apropiate suffix considering last letter
        if (vowels.Contains(name[name.Length - 1]))
        {
            sufixIndex =  2 * random.Next(0, 2);
        }
        else
        {
            sufixIndex = 1 + 2 * random.Next(0, 2);
        }

        name += sufixes[sufixIndex];
        GD.Print(name);
        // Convert first letter to uppercase
        char[] charName = name.ToCharArray();
        charName[0] = Char.ToUpper(name[0]);
        name = new string(charName);
        patchName = name.ToUpper();
        GD.Print(name);
        return name;

    }

    //TO DO
    public void Check(string name)
    {

    }

    //TO DO
    public void ApplyTranslations()
    {

    }
}
