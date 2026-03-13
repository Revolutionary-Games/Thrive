using System.Collections.Generic;
using Newtonsoft.Json;
using ThriveScriptsShared;

public class SpeciesNameConfig : IRegistryType
{
    [JsonRequired]
    public List<string> PrefixCofix = null!;

    [JsonRequired]
    public Dictionary<string, List<string>> Suffixes = null!;

    [JsonRequired]
    public Dictionary<string, Dictionary<string, List<string>>> BacteriaShapes = null!;

    [JsonRequired]
    public Dictionary<string, List<string>> Quantity = null!;

    [JsonRequired]
    public Dictionary<string, Dictionary<string, List<string>>> Organelles = null!;

    [JsonRequired]
    public Dictionary<string, string> OrganelleMap = null!;

    [JsonRequired]
    public Dictionary<string, List<string>> Processes = null!;

    [JsonRequired]
    public Dictionary<string, List<string>> QualityRoots = null!;

    [JsonRequired]
    public Dictionary<string, List<string>> QualitySuffixes = null!;

    // Legacy name generator species_names data
    [JsonRequired]
    public List<string> PrefixesV = null!;

    [JsonRequired]
    public List<string> PrefixesC = null!;

    [JsonRequired]
    public List<string> CofixesV = null!;

    [JsonRequired]
    public List<string> CofixesC = null!;

    /// <summary>
    ///   Unused
    /// </summary>
    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        if (PrefixCofix.Count < 1)
        {
            throw new InvalidRegistryDataException("SpeciesNameConfig", GetType().Name,
                "PrefixCofix is empty");
        }

        if (Suffixes.Count < 1)
        {
            throw new InvalidRegistryDataException("SpeciesNameConfig", GetType().Name,
                "Suffixes is empty");
        }

        if (BacteriaShapes.Count < 1)
        {
            throw new InvalidRegistryDataException("SpeciesNameConfig", GetType().Name,
                "BacteriaShapes is empty");
        }

        if (Quantity.Count < 1)
        {
            throw new InvalidRegistryDataException("SpeciesNameConfig", GetType().Name,
                "Quantity is empty");
        }

        if (Organelles.Count < 1)
        {
            throw new InvalidRegistryDataException("SpeciesNameConfig", GetType().Name,
                "Organelles is empty");
        }

        if (OrganelleMap.Count < 1)
        {
            throw new InvalidRegistryDataException("SpeciesNameConfig", GetType().Name,
                "OrganelleMap is empty");
        }

        if (Processes.Count < 1)
        {
            throw new InvalidRegistryDataException("SpeciesNameConfig", GetType().Name,
                "Processes is empty");
        }

        if (QualityRoots.Count < 1)
        {
            throw new InvalidRegistryDataException("SpeciesNameConfig", GetType().Name,
                "QualityRoots is empty");
        }

        if (QualitySuffixes.Count < 1)
        {
            throw new InvalidRegistryDataException("SpeciesNameConfig", GetType().Name,
                "QualitySuffixes is empty");
        }

        if (PrefixesV.Count < 1)
        {
            throw new InvalidRegistryDataException("SpeciesNameConfig", GetType().Name,
                "PrefixesV is empty");
        }

        if (PrefixesC.Count < 1)
        {
            throw new InvalidRegistryDataException("SpeciesNameConfig", GetType().Name,
                "PrefixesC is empty");
        }

        if (CofixesV.Count < 1)
        {
            throw new InvalidRegistryDataException("SpeciesNameConfig", GetType().Name,
                "CofixesV is empty");
        }

        if (CofixesC.Count < 1)
        {
            throw new InvalidRegistryDataException("SpeciesNameConfig", GetType().Name,
                "CofixesC is empty");
        }
    }

    public void Resolve(SimulationParameters simulationParameters)
    {
        _ = simulationParameters;
    }

    public void ApplyTranslations()
    {
    }
}
