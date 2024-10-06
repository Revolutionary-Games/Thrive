using Godot;

/// <summary>
///   Wrapper of an <see cref="ArrayMesh"/> with additional functionality
/// </summary>
public class ExtendedArrayMesh
{
    public Variant NativeExtendedArrayMesh;

    public ExtendedArrayMesh()
    {
        NativeExtendedArrayMesh = ClassDB.Instantiate("ExtendedArrayMesh");
    }

    public ArrayMesh Mesh
    {
        get
        {
            return NativeExtendedArrayMesh.As<ArrayMesh>();
        }
    }

    public void Unwrap(float texelSize)
    {
        NativeExtendedArrayMesh.As<ArrayMesh>().Call("unwrap", texelSize);
    }
}
