using System;
using Godot;

/// <summary>
///   Entity that can have a label shown by <see cref="StrategicEntityNameLabelSystem"/>
/// </summary>
public interface IEntityWithNameLabel : IEntity
{
    /// <summary>
    ///   Offset added to this entity's world position to get here the label should be positioned at
    /// </summary>
    public Vector3 LabelOffset { get; }

    /// <summary>
    ///   The type of the name label used, required for caching purposes to allow different entity types to share
    ///   compatible labels
    /// </summary>
    public Type NameLabelType { get; }

    /// <summary>
    ///   Scene used to instantiate the name label for this entity
    /// </summary>
    public PackedScene NameLabelScene { get; }

    /// <summary>
    ///   Called when this is selected through a name label
    /// </summary>
    public void OnSelectedThroughLabel();
}
