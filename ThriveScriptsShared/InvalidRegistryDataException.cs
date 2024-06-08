using System;

/// <summary>
///   Thrown when a SimulationParameters registry contained type has invalid data
/// </summary>
[Serializable]
public class InvalidRegistryDataException : Exception
{
    public InvalidRegistryDataException()
    {
    }

    public InvalidRegistryDataException(string name, string type, string error)
        : base($"Invalid registry item ({name}) of type: {type} error: {error}")
    {
    }

    public InvalidRegistryDataException(string message) : base(message)
    {
    }

    public InvalidRegistryDataException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
