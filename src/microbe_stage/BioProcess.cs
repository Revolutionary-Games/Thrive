using System.Collections.Generic;
using Godot;

/// <summary>
///   Definition of a bio process that cells can do in the form of a TweakedProcess.
/// </summary>
public class BioProcess : IRegistryType
{
    /// <summary>
    ///   User visible pretty name
    /// </summary>
    public string Name;

    /// <summary>
    ///   Inputs the process needs. The keys are compound names and values are amounts
    /// </summary>
    public Dictionary<Compound, float> Inputs;

    public Dictionary<Compound, float> Outputs;

    public string InternalName { get; set; }

    public void Check(string name)
    {
        if (Inputs == null || Outputs == null)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Empty inputs or outputs");
        }

        if (Inputs.Count == 0 && Outputs.Count == 0)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Process has no inputs AND no outputs");
        }

        foreach (var input in Inputs)
        {
            if (input.Value <= 0)
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "Non-positive amount of input compound " + input.Key + " found");
            }
        }

        foreach (var output in Outputs)
        {
            if (output.Value <= 0)
            {
                throw new InvalidRegistryDataException(name, GetType().Name,
                    "Non-positive amount of output compound " + output.Key + " found");
            }
        }
    }

    public string GetName()
    {
        return TranslationServer.Translate(Name);
    }
}
