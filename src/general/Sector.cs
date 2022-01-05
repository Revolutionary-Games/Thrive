using System;
using Godot;

public readonly struct Sector : IEquatable<Sector>
{
    public Sector(Int2 pos)
    {
        Pos = pos;
    }

    public Sector(int x, int y) : this(new Int2(x, y))
    {
    }

    public Int2 Pos { get; }
    public int X => Pos.x;
    public int Y => Pos.y;

    public static bool operator ==(Sector left, Sector right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Sector left, Sector right)
    {
        return !(left == right);
    }

    public static Sector FromPosition(Vector3 position)
    {
        var (x, y) = GetSectorCoords(position);
        return new Sector(x, y);
    }

    public bool IsInSector(Vector3 position)
    {
        var (x, y) = GetSectorCoords(position);
        return x == X && y == Y;
    }

    public bool Equals(Sector other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object obj)
    {
        return obj is Sector other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (X * 397) ^ Y;
        }
    }

    public override string ToString()
    {
        return $"{X};{Y}";
    }

    private static (int X, int Y) GetSectorCoords(Vector3 position)
    {
        var x = (int)(position.x / Constants.SECTOR_SIZE);
        var y = (int)(position.z / Constants.SECTOR_SIZE);
        return (x, y);
    }
}
