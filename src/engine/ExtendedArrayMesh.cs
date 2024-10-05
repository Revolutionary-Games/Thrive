using System;
using Godot;

/// <summary>
///   <see cref="ArrayMesh"/> with additional functionality
/// </summary>
[GodotAutoload]
public partial class ExtendedArrayMesh : ArrayMesh
{
    /*protected IntPtr nativeInstance;

    public IntPtr NativeInstance1
    {
        get
        {
            if (nativeInstance != IntPtr.Zero)
            {
                return nativeInstance;
            }

            var nativeCallResult = Call("get_native_instance");

            nativeInstance = new IntPtr(nativeCallResult.AsInt64());

            if (nativeInstance == IntPtr.Zero)
            {
                GD.PrintErr("Failed to get native side of ExtendedArrayMesh");
            }

            return nativeInstance;
        }
    }*/

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
