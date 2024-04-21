using System;

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
}
