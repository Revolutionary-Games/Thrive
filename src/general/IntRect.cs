using System;
using System.Collections.Generic;
using System.Linq;

public struct IntRect : IEquatable<IntRect>
{
    public IntRect(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public int EndX => X + Width;

    public int EndY => Y + Height;

    public static bool operator ==(IntRect left, IntRect right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(IntRect left, IntRect right)
    {
        return !(left == right);
    }

    public override bool Equals(object obj)
    {
        return obj is IntRect rect && Equals(rect);
    }

    public bool Equals(IntRect other)
    {
        return X == other.X &&
               Y == other.Y &&
               Width == other.Width &&
               Height == other.Height;
    }

    public override int GetHashCode()
    {
        int hashCode = 466501756;
        hashCode = hashCode * -1521134295 + X.GetHashCode();
        hashCode = hashCode * -1521134295 + Y.GetHashCode();
        hashCode = hashCode * -1521134295 + Width.GetHashCode();
        hashCode = hashCode * -1521134295 + Height.GetHashCode();
        return hashCode;
    }

    public IntRect CreateSubRectangle(int cutWidth, int cutHeight)
    {
        return new IntRect(X + cutWidth / 2, Y + cutHeight / 2, Width - cutWidth, Height - cutHeight);
    }

    public IntRect CreateSubRectangle(int cutSize)
    {
        return CreateSubRectangle(cutSize, cutSize);
    }

    public void ScaleByOrigin(int factor)
    {
        X *= factor;
        Y *= factor;
        Width *= factor;
        Height *= factor;
    }

    public IEnumerable<Int2> GetPointEnumerator() => GetPointEnumerator(1);

    /// <summary>
    ///   Enumerator to the points contained within the rectangle with a step between two consecutive points.
    /// </summary>
    /// <param name="step">The distance between two points enumerated</param>
    public IEnumerable<Int2> GetPointEnumerator(int step)
    {
        var width = Width;
        var x = X;
        var y = Y;

        return Enumerable.Range(0, Width / step * Height / step).Select(p => new Int2(x + step * p / width, y + step * p % width));
    }

    public IEnumerable<IntRect> GetSubdivisionEnumerator(int subdivisionSize)
    {
        var width = Width;
        var x = X;
        var y = Y;

        return GetPointEnumerator(subdivisionSize).Select(p => new IntRect(p.x, p.y, subdivisionSize, subdivisionSize));
    }
}
