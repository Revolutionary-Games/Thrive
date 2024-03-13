namespace Components;

using Newtonsoft.Json;

/// <summary>
///   Allows entities to create simple shapes. Requires <see cref="PhysicsShapeHolder"/> to place the shape in.
/// </summary>
[JSONDynamicTypeAllowed]
public struct SimpleShapeCreator
{
    /// <summary>
    ///   Size of this shape. Depends on the shape type what this defines, most commonly this is the radius or half
    ///   side length
    /// </summary>
    public float Size;

    /// <summary>
    ///   Density of the shape. 0 uses a default value
    /// </summary>
    public float Density;

    public SimpleShapeType ShapeType;

    /// <summary>
    ///   If this is set to true then when this shape is created it doesn't force a <see cref="Physics"/> to
    ///   recreate the body for the changed shape (if the body was already created). When false it is ensured that
    ///   the body gets recreated when the shape changes.
    /// </summary>
    public bool SkipForceRecreateBodyIfCreated;

    /// <summary>
    ///   Must be set to false if parameters are changed for the shape to be re-created
    /// </summary>
    [JsonIgnore]
    public bool ShapeCreated;

    public SimpleShapeCreator(SimpleShapeType shapeType, float size, float density = 1000)
    {
        Size = size;
        Density = density;
        ShapeType = shapeType;

        // TODO: some shape types might need more parameters in the future so block them from using this constructor

        SkipForceRecreateBodyIfCreated = false;
        ShapeCreated = false;
    }
}
