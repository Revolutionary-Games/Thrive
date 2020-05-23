using System;
using Godot;

/// <summary>
///   Base class for Godot Node derived types converters
/// </summary>
public class BaseNodeConverter : BaseThriveConverter
{
    public BaseNodeConverter(ISaveContext context) : base(context)
    {
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(Node).IsAssignableFrom(objectType);
    }

    protected override bool SkipMember(string name)
    {
        // Skip Godot properties that we don't want in saves
        switch (name)
        {
            // Ignore a bunch of editor-only or data that should really be set from the scene this is loaded from
            case "EditorDescription":
            case "_ImportPath":
            case "PauseMode":
            case "Owner":
            case "ProcessPriority":
            case "NativeInstance":
            case "DynamicObject":
            // Probably don't want to save anything multiplayer related:
            case "Multiplayer":
            case "CustomMultiplayer":
                return true;
            default:
                return base.SkipMember(name);
        }
    }
}
