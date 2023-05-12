using System;
using System.Runtime.Serialization;

/// <summary>
///   Thrown when a Godot Node derived type is missing a variable that should be set through the Godot editor.
///   If you see this, check that you didn't forget to set a value for an export variable in the Godot editor.
///   Another possible problem is that Godot has decided to automatically set some values to null (automatically
///   breaking things).
/// </summary>
[Serializable]
public class MissingExportVariableValueException : InvalidOperationException
{
    public MissingExportVariableValueException() { }

    protected MissingExportVariableValueException(SerializationInfo serializationInfo,
        StreamingContext streamingContext) : base(serializationInfo, streamingContext)
    {
    }
}
