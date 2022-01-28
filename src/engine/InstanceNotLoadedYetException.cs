using System;
using System.Runtime.Serialization;

/// <summary>
///   Thrown when trying to access a static instance that has not been loaded yet
/// </summary>
[Serializable]
public class InstanceNotLoadedYetException : Exception
{
    public InstanceNotLoadedYetException() { }

    protected InstanceNotLoadedYetException(SerializationInfo serializationInfo,
        StreamingContext streamingContext) : base(serializationInfo, streamingContext)
    {
    }
}
