using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public static class NoiseUtils
{
    public static Vector3 CellPoint(int x, int y, int z, int grid, int seed)
    {
        x = (x % grid + grid) % grid;
        y = (y % grid + grid) % grid;
        z = (z % grid + grid) % grid;

        uint h = (uint)(x * 73856093 ^ y * 19349663 ^ z * 83492791 ^ seed * 2654435761);

        h ^= h >> 13;
        h *= 0x5bd1e995u;
        h ^= h >> 15;

        float rx = (h & 0xFFFF) / 65535.0f;
        h *= 0x27d4eb2du;
        float ry = (h & 0xFFFF) / 65535.0f;
        h ^= h >> 12;
        h *= 0x165667b1u;
        float rz = (h & 0xFFFF) / 65535.0f;

        return new Vector3(rx, ry, rz);
    }

    public static float WorleyTiling(Vector3 point, int grid, int seed)
    {
        Vector3 scaled = point * grid;
        Vector3I baseCell = new Vector3I(Mathf.FloorToInt(scaled.X),
            Mathf.FloorToInt(scaled.Y),
            Mathf.FloorToInt(scaled.Z));

        float minDist = float.MaxValue;

        for (int dz = -1; dz <= 1; ++dz)
        {
            for (int dy = -1; dy <= 1; ++dy)
            {
                for (int dx = -1; dx <= 1; ++dx)
                {
                    Vector3I neighbor = baseCell + new Vector3I(dx, dy, dz);

                    Vector3 pt = CellPoint(neighbor.X, neighbor.Y, neighbor.Z, grid, seed);

                    Vector3 pointPos = new Vector3(neighbor.X, neighbor.Y, neighbor.Z) + pt;

                    float d = (scaled - pointPos).Length();
                    minDist = MathF.Min(minDist, d);
                }
            }
        }

        return 1.0f - Math.Clamp(minDist, 0.0f, 1.0f);
    }

    public static Vector3 CellGradient(int x, int y, int z, int period, int seed)
    {
        x = ((x % period) + period) % period;
        y = ((y % period) + period) % period;
        z = ((z % period) + period) % period;

        uint h = (uint)(x * 73856093 ^ y * 19349663 ^ z * 83492791 ^ seed * 374761393);
        h ^= h >> 13;
        h *= 0x5bd1e995u;
        h ^= h >> 15;

        float u = (h & 0xFFFF) / 65535.0f;
        h *= 0x27d4eb2du;

        float v = (h & 0xFFFF) / 65535.0f;
        float theta = u * 6.2831853f;
        float phi = MathF.Acos(2.0f * v - 1.0f);
        float s = MathF.Sin(phi);

        return new Vector3(MathF.Cos(theta) * s, MathF.Sin(theta) * s, MathF.Cos(phi));
    }

    public static float PerlinTiling(Vector3 point, int period, int seed)
    {
        Vector3 scaled = point * period;
        Vector3I c0 = new Vector3I(Mathf.FloorToInt(scaled.X),
            Mathf.FloorToInt(scaled.Y),
            Mathf.FloorToInt(scaled.Z));

        Vector3 f = scaled - new Vector3(c0.X, c0.Y, c0.Z);

        float ux = MathUtils.FadeQuintic(f.X), uy = MathUtils.FadeQuintic(f.Y), uz = MathUtils.FadeQuintic(f.Z);

        float Corner(int dx, int dy, int dz)
        {
            Vector3 g = CellGradient(c0.X + dx, c0.Y + dy, c0.Z + dz, period, seed);
            Vector3 delta = f - new Vector3(dx, dy, dz);
            return g.Dot(delta);
        }

        float x00 = Mathf.Lerp(Corner(0, 0, 0), Corner(1, 0, 0), ux);
        float x10 = Mathf.Lerp(Corner(0, 1, 0), Corner(1, 1, 0), ux);
        float x01 = Mathf.Lerp(Corner(0, 0, 1), Corner(1, 0, 1), ux);
        float x11 = Mathf.Lerp(Corner(0, 1, 1), Corner(1, 1, 1), ux);

        float y0 = Mathf.Lerp(x00, x10, uy);
        float y1 = Mathf.Lerp(x01, x11, uy);

        float val = Mathf.Lerp(y0, y1, uz);

        return val * 0.5f + 0.5f;
    }

    public static float FractionalBrownianMotionWorley(Vector3 point, int baseGrid, int octaves, int seed)
    {
        float sum = 0.0f, amp = 0.5f, norm = 0.0f;
        int grid = baseGrid;
        for (int o = 0; o < octaves; ++o)
        {
            sum += amp * WorleyTiling(point, grid, seed + o * 17);
            norm += amp;
            amp *= 0.5f;
            grid *= 2;
        }

        return sum / norm;
    }

    public static float FractionalBrownianMotionPerlin(Vector3 point, int baseGrid, int octaves, int seed)
    {
        float sum = 0.0f, amp = 0.5f, norm = 0.0f;
        int grid = baseGrid;
        for (int o = 0; o < octaves; ++o)
        {
            sum += amp * PerlinTiling(point, grid, seed + o * 17);
            norm += amp;
            amp *= 0.5f;
            grid *= 2;
        }

        return sum / norm;
    }

    public static float PerlinWorley(Vector3 point, int seed)
    {
        float perlin = FractionalBrownianMotionPerlin(point, 4, 4, seed);
        float worley = FractionalBrownianMotionWorley(point, 8, 3, seed + 1000);

        return Remap(perlin, worley - 1.0f, 1.0f, 0.0f, 1.0f);
    }

    public static ImageTexture3D BakePerlinWorleyChunkParallel(int size, int seed)
    {
        GD.Print($"Baking Perlin-Worley noise chunk of size {size}.");

        var buffers = new byte[size][];
        float sizeFloat = size;
        Parallel.For(0, size, z =>
        {
            var buffer = new byte[size * size * 4];
            int i = 0;
            for (int y = 0; y < size; ++y)
            {
                for (int x = 0; x < size; ++x)
                {
                    Vector3 p = new Vector3(x / sizeFloat, y / sizeFloat, z / sizeFloat);
                    buffer[i++] = (byte)(Math.Clamp(PerlinWorley(p, seed), 0, 1) * 255);
                    buffer[i++] = (byte)(Math.Clamp(FractionalBrownianMotionWorley(p, 8, 1, seed + 1000), 0, 1) * 255);
                    buffer[i++] = (byte)
                        (Math.Clamp(FractionalBrownianMotionWorley(p, 16, 1, seed + 2000), 0, 1) * 255);
                    buffer[i++] = (byte)
                        (Math.Clamp(FractionalBrownianMotionWorley(p, 32, 1, seed + 3000), 0, 1) * 255);
                }
            }

            buffers[z] = buffer;
        });

        var images = new Array<Image>();
        for (int z = 0; z < size; ++z)
        {
            images.Add(Image.CreateFromData(size, size, false, Image.Format.Rgba8, buffers[z]));
        }

        var texture = new ImageTexture3D();
        texture.Create(Image.Format.Rgba8, size, size, size, false, images);

        return texture;
    }

    private static float Remap(float value, float inMin, float inMax, float outMin, float outMax)
    {
        return outMin + (value - inMin) / (inMax - inMin) * (outMax - outMin);
    }
}
