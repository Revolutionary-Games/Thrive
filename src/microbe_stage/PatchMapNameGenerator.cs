using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Godot;
using System.Linq;
public class PatchMapNameGenerator : IRegistryType
{
    private int syllabelsLowerLimit;
    private int syllabelsHigherLimit;
    private string continentName = null!;
    private string patchName = null!;
    [JsonRequired]
    private List<string> syllabels = null!;
    [JsonRequired]
    private List<string> sufixes = null!;
    private string vowels = "aeiou";
    public string InternalName{ get; set; } = null!;
    public PatchMapNameGenerator(int lowerLimit, int higherLimit)
    {
        syllabelsLowerLimit = lowerLimit;
        syllabelsHigherLimit = higherLimit;
    }
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

        int nameLength = random.Next(syllabelsLowerLimit, syllabelsHigherLimit + 1);
        string name = "";
        int sufixIndex;

        for (int i = 0; i < nameLength; i++)
        {
            int syllabelsIndex = random.Next(0,syllabels.Count);
            name += syllabels [syllabelsIndex];
        }

        if (vowels.Contains(name[name.Length - 1]))
        {
            sufixIndex = 1 + 2 * random.Next(0, 2);
        }
        else
        {
            sufixIndex = 2 + 2 * random.Next(0, 2);
        }

        name += sufixes[sufixIndex];

        // Convert first letter to uppercase
        char[] charName = name.ToCharArray();
        charName[0] = Char.ToUpper(name[0]);
        name = charName.ToString();

        return name;

    }
    public void Check(string name)
    {

    }
    public void ApplyTranslations()
    {

    }
}
