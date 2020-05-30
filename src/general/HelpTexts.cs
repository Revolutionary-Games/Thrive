using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
///   Help texts from json.
/// </summary>
public class HelpTexts : IRegistryType
{
    /// <summary>
    ///   Left side of the help texts
    /// </summary>
    public List<string> LeftTexts;

    /// <summary>
    ///   Right side of the help texts
    /// </summary>
    public List<string> RightTexts;

    /// <summary>
    ///   Total help texts
    /// </summary>
    [JsonIgnore]
    public List<string> Texts
    {
        get
        {
            var combined = new List<string>();

            combined.AddRange(LeftTexts);
            combined.AddRange(RightTexts);

            return combined;
        }
    }

    public string InternalName { get; set; }

    public void Check(string name)
    {
        if (LeftTexts == null || LeftTexts.Count < 1)
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing left text lists");

        if (RightTexts == null || RightTexts.Count < 1)
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing right text lists");
    }
}
