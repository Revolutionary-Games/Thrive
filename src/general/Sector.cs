using System;
using Godot;

public readonly struct Sector : IEquatable<Sector>
{
    public Sector(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }
    public int Y { get; }

    public static bool operator ==(Sector left, Sector right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Sector left, Sector right)
    {
        return !(left == right);
    }

    public static Sector FromPosition(Vector2 position)
    {
        var (x, y) = GetSectorCoords(position);
        return new Sector(x, y);
    }

    public bool IsInSector(Vector2 position)
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

    private static (int X, int Y) GetSectorCoords(Vector2 position)
    {
        var x = (int)(position.x / Constants.SECTOR_SIZE);
        var y = (int)(position.y / Constants.SECTOR_SIZE);
        return (x, y);
    }
}
