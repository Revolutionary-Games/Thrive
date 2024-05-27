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

    /// <summary>
    ///   Force all Node types to behave properly with referencing, slightly increases the JSON output size but
    ///   avoids having to place a ton of annotations on classes and also getting them on Godot's classes
    /// </summary>
    public override bool ForceReferenceWrite => true;

    public static bool IsIgnoredGodotNodeMember(string name)
    {
        switch (name)
        {
            // Ignore a bunch of editor-only or data that should really be set from the scene this is loaded from
            case "EditorDescription":
            case "_ImportPath":
            // TODO: this may cause problems if we ever want to allow objects to dynamically change their pause mode
            case "ProcessMode":
            case "ProcessPhysicsPriority":
            case "ProcessThreadGroup":
            case "ProcessThreadGroupOrder":
            case "ProcessThreadMessages":

            case "Owner":
            case "UniqueNameInOwner":
            // TODO: or process priority
            case "ProcessPriority":
            case "NativeInstance":
            case "DynamicObject":
            case "Gizmo":
            // Ignore the extra rotation and translation related stuff that just duplicates what Transform has
            case "GlobalTranslation":
            case "Position":
            case "RotationDegrees":
            case "GlobalRotation":
            case "Rotation":
            case "Scale":
            case "Quaternion":
            case "Basis":
            // Maybe some editor stuff?
            case "RotationEditMode":
            case "RotationOrder":
            // Ignore this as this is parent relative and probably causes problems loading
            case "GlobalTransform":
            case "GlobalPosition":
            case "GlobalBasis":
            case "GlobalRotationDegrees":
            // Ignore physics properties that cause deprecation warnings
            case "Friction":
            case "Bounce":
            // Probably don't want to save anything multiplayer related:
            case "Multiplayer":
            case "CustomMultiplayer":
            // These are very big objects when saved, and probably can't be properly loaded, so these are ignored
            case "Material":
            case "MaterialOverride":
            case "MaterialOverlay":
            case "Environment":
            // Name as a StringName cannot be saved without a custom converter
            case "Name":
            case "VisibilityParent":
            // Bunch of new Control node things that aren't saved
            case "Theme":
            case "ShortcutContext":
            case "FocusNeighborLeft":
            case "FocusNeighborTop":
            case "FocusNeighborRight":
            case "FocusNeighborBottom":
            case "FocusNext":
            case "FocusPrevious":
            // Node groups are no longer saved as they are now never important to change dynamically
            case "NodeGroups":
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
        return IsIgnoredGodotNodeMember(name) || base.SkipMember(name);
    }
}
