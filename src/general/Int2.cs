using System;

/// <summary>
///   Just a basic 2 component integer vector for use before we get Godot.Vector2i
/// </summary>
public struct Int2
{
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
    public int x;
    public int y;
#pragma warning restore SA1307 // Accessible fields should begin with upper-case letter

    public Int2(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}
