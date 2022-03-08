using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Godot;

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
    public void Next()
    {
        
    }
    public void Check(string name)
    {

    }
    public void ApplyTranslations()
    {

    }
}
