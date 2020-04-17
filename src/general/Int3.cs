using System;

/// <summary>
///   Just a basic 3 component integer vector for use before we get Godot.Vector3i
/// </summary>
public struct Int3
{
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
    public int x;
    public int y;
    public int z;
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter

    public Int3(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}
