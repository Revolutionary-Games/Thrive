using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   Help texts from json.
/// </summary>
public class HelpTexts : IRegistryType
{
    public List<string> Messages;

    public string InternalName { get; set; }

    public void Check(string name)
    {
        if (Messages == null || Messages.Count < 1)
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing help messages");
    }
}
