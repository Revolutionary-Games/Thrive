using System;
using Newtonsoft.Json;

/// <summary>
///   Info on loading a visual resource (with potentially different quality levels for graphics settings)
/// </summary>
public class VisualResourceData : IRegistryType
{
    [JsonIgnore]
    public VisualResourceIdentifier Identifier { get; private set; }

    [JsonProperty]
    public string NormalQualityPath { get; private set; } = string.Empty;

    [JsonIgnore]
    public string InternalName { get; set; } = null!;

    public void Check(string name)
    {
        if (string.IsNullOrWhiteSpace(NormalQualityPath))
            throw new InvalidRegistryDataException(name, GetType().Name, "Missing normal quality scene path");

        // TODO: for safety should these objects verify the scene paths? That would mean checking a ton of scenes if
        // a bunch of game visuals will go through this system

        // Parse the identifier from the internal name
        if (!Enum.TryParse(InternalName, out VisualResourceIdentifier identifier))
        {
            throw new InvalidRegistryDataException(name, GetType().Name, "Failed to parse internal name as identifier");
        }

        Identifier = identifier;

        if (Identifier == VisualResourceIdentifier.None)
            throw new InvalidRegistryDataException(name, GetType().Name, "Resource identifier type is none");

        // Don't load any of the scenes here as otherwise all of the game visuals would always be forced to be loaded
        // in memory. Instead individual game states should manage keeping scenes loaded while they might instance them
    }

    public void ApplyTranslations()
    {
    }
}
