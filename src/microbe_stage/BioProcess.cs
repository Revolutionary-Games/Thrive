using System.Collections.Generic;
using Newtonsoft.Json;
using ThriveScriptsShared;

/// <summary>
///   Definition of a bio process that cells can do in the form of a TweakedProcess.
/// </summary>
public class BioProcess : IRegistryType
{
    /// <summary>
    ///   User visible pretty name
    /// </summary>
    [TranslateFrom(nameof(untranslatedName))]
    public string Name = null!;

    /// <summary>
    ///   Inputs the process needs. The keys are compound names and values are amounts
    /// </summary>
    [JsonIgnore]
    public Dictionary<CompoundDefinition, float> Inputs = new();

    [JsonIgnore]
    public Dictionary<CompoundDefinition, float> Outputs = new();

    /// <summary>
    ///   True when this is a metabolism process
    /// </summary>
    public bool IsMetabolismProcess;

#pragma warning disable 169,649 // Used through reflection (and JSON)
    private string? untranslatedName;

    // To make JSON loading work, we need to use temporary data holders
    [JsonProperty(nameof(Inputs))]
    private Dictionary<Compound, float>? inputsRaw;

    [JsonProperty(nameof(Outputs))]
    private Dictionary<Compound, float>? outputsRaw;
#pragma warning restore 169,649

    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        if (inputsRaw == null || outputsRaw == null)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Empty inputs or outputs");
        }

        if (inputsRaw.Count == 0 && outputsRaw.Count == 0)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Process has no inputs AND no outputs");
        }

        foreach (var input in inputsRaw)
        {
            if (input.Value <= 0)
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "Non-positive amount of input compound " + input.Key + " found");
            }
        }

        foreach (var output in outputsRaw)
        {
            if (output.Value <= 0)
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "Non-positive amount of output compound " + output.Key + " found");
            }
        }

        TranslationHelper.CopyTranslateTemplatesToTranslateSource(this);
    }

    public void Resolve(SimulationParameters simulationParameters)
    {
        // Resolve inputs and outputs
        foreach (var entry in inputsRaw!)
        {
            Inputs[simulationParameters.GetCompoundDefinition(entry.Key)] = entry.Value;
        }

        foreach (var entry in outputsRaw!)
        {
            Outputs[simulationParameters.GetCompoundDefinition(entry.Key)] = entry.Value;
        }
    }

    public void ApplyTranslations()
    {
        TranslationHelper.ApplyTranslations(this);
    }

    public override string ToString()
    {
        return Name;
    }
}
