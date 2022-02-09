using System;
using System.Runtime.Serialization;

[Serializable]
public class SceneTreeAttachRequired : InvalidOperationException
{
    public SceneTreeAttachRequired() : base(
        "This Node needs to be attached to the scene tree before performing this operation")
    {
    }

    public SceneTreeAttachRequired(string message) : base(message)
    {
    }

    protected SceneTreeAttachRequired(SerializationInfo serializationInfo,
        StreamingContext streamingContext) : base(serializationInfo, streamingContext)
    {
    }
}
