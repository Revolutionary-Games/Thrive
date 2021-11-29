using Godot;

public readonly struct Sector : System.IEquatable<Sector>
{
    private const int SECTOR_SIZE = 64;
    private const float NOISE_MULTIPLIER = 0.01f;

    public Sector(int x, int y, float noiseDensity)
    {
        X = x;
        Y = y;
        NoiseDensity = noiseDensity;
    }

    public Sector(int x, int y, FastNoiseLite noise) : this(x, y, GetNormalizedNoiseValue(noise, x, y))
    {
    }

    public int X { get; }
    public int Y { get; }
    public float NoiseDensity { get; }

    public static bool operator ==(Sector left, Sector right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Sector left, Sector right)
    {
        return !(left == right);
    }

    public static Sector FromPosition(Vector3 position, FastNoiseLite noise)
    {
        var (x, y) = GetSectorCoords(position);
        var density = GetNormalizedNoiseValue(noise, x, y);
        return new Sector(x, y, density);
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
        return $"{X};{Y}: {NoiseDensity}";
    }

    private static float GetNormalizedNoiseValue(FastNoiseLite noise, int x, int y)
    {
        return (noise.GetNoise(x * NOISE_MULTIPLIER, y * NOISE_MULTIPLIER) + 1f) / 2f;
    }

    private static (int x, int y) GetSectorCoords(Vector3 position)
    {
        var x = (int)position.x / SECTOR_SIZE;
        var y = (int)position.z / SECTOR_SIZE;
        return (x, y);
    }
}
