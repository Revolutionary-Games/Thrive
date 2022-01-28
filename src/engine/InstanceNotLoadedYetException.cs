using System;

/// <summary>
///   Thrown when trying to access a static instance that has not been loaded yet
/// </summary>
[Serializable]
public class InstanceNotLoadedYetException : Exception
{
    public InstanceNotLoadedYetException() { }

    protected InstanceNotLoadedYetException(System.Runtime.Serialization.SerializationInfo serializationInfo,
        System.Runtime.Serialization.StreamingContext streamingContext) : base(serializationInfo, streamingContext)
    {
    }
}
