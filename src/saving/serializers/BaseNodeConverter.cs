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

    public static bool IsIgnoredGodotNodeMember(string name)
    {
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
            case "Gizmo":
            // Ignore the extra rotation and translation related stuff that just duplicates what Transform has
            case "Translation":
            case "RotationDegrees":
            case "Rotation":
            case "Scale":
            // Ignore physics properties that cause deprecation warnings
            case "Friction":
            case "Bounce":
            // Probably don't want to save anything multiplayer related:
            case "Multiplayer":
            case "CustomMultiplayer":
            // These are very big objects when saved, and probably can't be properly loaded, so these are ignored
            case "Material":
            case "MaterialOverride":
                return true;
            default:
                return false;
        }
    }

    public override bool CanConvert(Type objectType)
    {
        return typeof(Node).IsAssignableFrom(objectType);
    }

    protected override bool SkipMember(string name)
    {
        // Skip Godot properties that we don't want in saves
        return IsIgnoredGodotNodeMember(name);
    }
}
