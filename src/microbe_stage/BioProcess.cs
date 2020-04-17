using System.Collections.Generic;

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
    public Dictionary<string, float> Inputs;

    public Dictionary<string, float> Outputs;

    public string InternalName { get; set; }

    public void Check(string name)
    {
        if (Inputs == null || Outputs == null)
        {
            throw new InvalidRegistryData(name, this.GetType().Name,
                "Empty inputs or outputs");
        }

        if (Inputs.Count == 0 && Outputs.Count == 0)
        {
            throw new InvalidRegistryData(name, this.GetType().Name,
                "Process has no inputs AND no outputs");
        }
    }
}
