using System;

/// <summary>
///   Thrown when a SimulationParameters registry contained type has invalid data
/// </summary>
[Serializable]
public class InvalidRegistryData : Exception
{
    public InvalidRegistryData()
    {
    }

    public InvalidRegistryData(string name, string type, string error)
        : base($"Invalid registry item ({name}) of type: {type} error: {error}")
    {
    }
}
