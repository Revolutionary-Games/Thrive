using GdUnit4;
using Godot;

public class NoiseTest
{
    public static void BakeWorleySliceTest()
    {
        const int size = 256;
        const int grid = 8;
        const int seed = 1234;

        var img = Image.CreateEmpty(size, size, false, Image.Format.L8);
        float z = 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector3 p = new Vector3(x / (float)size, y / (float)size, z);
                float v = NoiseUtils.PerlinWorley(p, seed);
                img.SetPixel(x, y, new Color(v, v, v));
            }
        }

        img.SavePng("C:/Users/franc/Desktop/worley_slice_test.png");
    }
}
