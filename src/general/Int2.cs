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

    // Unary operators
    public static Int2 operator +(Int2 p) => p;
    public static Int2 operator -(Int2 p) => new Int2(-p.x, -p.y);

    // Vector-Scalar operators
    public static Int2 operator /(Int2 p, int i) => new Int2(p.x / i, p.y / i);
    public static Int2 operator *(Int2 p, int i) => new Int2(p.x * i, p.y * i);
    public static Int2 operator *(int i, Int2 p) => new Int2(p.x * i, p.y * i);

    // Vector-Vector operators
    public static Int2 operator +(Int2 p1, Int2 p2) => new Int2(p1.x + p2.x, p1.y + p2.y);
    public static Int2 operator -(Int2 p1, Int2 p2) => new Int2(p1.x - p2.x, p1.y - p2.y);
    public static Int2 operator *(Int2 p1, Int2 p2) => new Int2(p1.x * p2.x, p1.y * p2.y);
    public static Int2 operator /(Int2 p1, Int2 p2) => new Int2(p1.x / p2.x, p1.y / p2.y);

    // Comparators
    public static bool operator >(Int2 p1, Int2 p2) => p1.x > p2.x || (p1.x == p2.x && p1.y > p2.y);
    public static bool operator <(Int2 p1, Int2 p2) => p1.x < p2.x || (p1.x == p2.x && p1.y < p2.y);
    public static bool operator >=(Int2 p1, Int2 p2) => !(p1 < p2);
    public static bool operator <=(Int2 p1, Int2 p2) => !(p1 > p2);
}
