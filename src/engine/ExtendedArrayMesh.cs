using System;
using Godot;

/// <summary>
///   <see cref="ArrayMesh"/> with additional functionality
/// </summary>
public partial class ExtendedArrayMesh : ArrayMesh
{
    public ExtendedArrayMesh() : base()
    {
        // Nothing here
    }

    public void Unwrap(float texelSize)
    {
        bool nativeCallResult = Call("unwrap", texelSize).As<bool>();
        GD.Print("Success in generating UVs?: " + nativeCallResult);
    }
}
