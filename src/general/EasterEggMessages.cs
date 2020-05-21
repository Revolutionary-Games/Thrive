using System.Collections.Generic;

/// <summary>
///   Easter egg messages / tips on the help screen
/// </summary>
public class EasterEggMessages : IRegistryType
{
    /// <summary>
    ///   List of the easter egg messages
    /// </summary>
    public List<string> Messages;

    // TODO: Separate the messages into categories
    // (e.g MicrobeStage, MulticellularStage, General)

    public string InternalName { get; set; }

    public void Check(string name)
    {
        if (Messages == null || Messages.Count < 1)
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing message lists");
    }
}
