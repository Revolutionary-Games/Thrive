using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using Xoshiro.PRNG32;

/// <summary>
///   Displays a creature using convolution surfaces based on a metaball layout
/// </summary>
public partial class MulticellularConvolutionDispayer : MeshInstance3D, IMetaballDisplayer<MulticellularMetaball>
{
    private const float AABBMargin = 0.1f;

    private StandardMaterial3D? material;

    private float? overrideColourAlpha;

    public float? OverrideColourAlpha
    {
        get => overrideColourAlpha;
        set
        {
            // Due to both being nullable, this would be a bit complicated to compare with an epsilon value
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (overrideColourAlpha == value)
                return;

            overrideColourAlpha = value;
            ApplyAlpha();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        // This is here in case we need custom shader effects at some point
        // Material = new ShaderMaterial()
        // {
        //     Shader = GD.Load<Shader>("res://shaders/Metaball.shader"),
        // },
        material = new StandardMaterial3D
        {
            VertexColorUseAsAlbedo = true,
        };

        ApplyAlpha();

        ExtraCullMargin = AABBMargin;
    }

    public void DisplayFromLayout(IReadOnlyCollection<MulticellularMetaball> layout)
    {
        Vector3 minExtends = Vector3.Zero;
        Vector3 maxExtends = Vector3.Zero;

        foreach (var metaball in layout)
        {
            minExtends.X = MathF.Min(minExtends.X, metaball.Position.X - metaball.Radius - 0.5f);
            minExtends.Y = MathF.Min(minExtends.Y, metaball.Position.Y - metaball.Radius - 0.5f);
            minExtends.Z = MathF.Min(minExtends.Z, metaball.Position.Z - metaball.Radius - 0.5f);

            maxExtends.X = MathF.Max(maxExtends.X, metaball.Position.X + metaball.Radius + 0.5f);
            maxExtends.Y = MathF.Max(maxExtends.Y, metaball.Position.Y + metaball.Radius + 0.5f);
            maxExtends.Z = MathF.Max(maxExtends.Z, metaball.Position.Z + metaball.Radius + 0.5f);
        }

        // TODO: find a way to cache those mesh generations in future as they are quite expensive.
        var mathFunction = new Scalis(layout);
        mathFunction.SurfaceValue = 1;

        var meshGen = new DualContourer(mathFunction);
        meshGen.PointsPerUnit = Constants.CREATURE_MESH_RESOLUTION;
        meshGen.UnitsFrom = minExtends;
        meshGen.UnitsTo = maxExtends;

        Mesh = meshGen.DualContour();

        Mesh.SurfaceSetMaterial(0, material);

        Task uvUnwrap = new Task(() => UVUnwrapAndTexturize((ArrayMesh)Mesh));
        TaskExecutor.Instance.AddTask(uvUnwrap);

        CustomAabb = new Aabb(minExtends, maxExtends);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            material?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void UVUnwrapAndTexturize(ArrayMesh mesh)
    {
        var variant = Variant.From(mesh);

        // Note: Unwrapper's Native code uses call_deferred (delayed call) to apply changes to the mesh surface
        // (so that the code can be multithreaded).
        // This means that there is no surface immediately after calling this function and texture application
        // has to be deferred too.
        if (NativeMethods.ArrayMeshUnwrap(ref variant, 1.0f))
        {
            CallDeferred(nameof(ApplyTextures), mesh);
        }
    }

    private void ApplyTextures(ArrayMesh mesh)
    {
        if (material != null)
        {
            Stopwatch sw = new();
            sw.Start();

            material.AlbedoTexture = ImageTexture.CreateFromImage(GenerateTexture(mesh));

            sw.Stop();
            GD.Print("Drew a texture in " + sw.Elapsed);
        }

        mesh.SurfaceSetMaterial(0, material);
    }

    private Image GenerateTexture(ArrayMesh mesh)
    {
        int dimension = 2048;

        var image = Image.CreateEmpty(dimension, dimension, false, Image.Format.Rgba8);

        var arrays = mesh.SurfaceGetArrays(0);

        // var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        var indices = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
        var uvs = arrays[(int)Mesh.ArrayType.TexUV].AsVector2Array();

        float pixelWidth = 1.0f / dimension;

        // Draw image based on triangles
        for (int i = 0; i < indices.Length; i += 3)
        {
            var aUV = uvs[indices[i]];
            var bUV = uvs[indices[i + 1]];
            var cUV = uvs[indices[i + 2]];

            // Vertex data
            // var a = vertices[indices[i]];
            // var b = vertices[indices[i + 1]];
            // var c = vertices[indices[i + 2]];

            var random = new XoShiRo128plus();

            var color = new Color(random.NextFloat(), random.NextFloat(), random.NextFloat());

            float minY = MathF.Min(aUV.Y, MathF.Min(bUV.Y, cUV.Y));
            float maxY = MathF.Max(aUV.Y, MathF.Max(bUV.Y, cUV.Y));

            float middleY = aUV.Y + bUV.Y + cUV.Y - minY - maxY;

            float prevLeftX = 0;
            float prevRightX = 0;

            for (float y = MathF.Round(minY * 2048.0f) / 2048.0f; y <= middleY; y += pixelWidth)
            {
                (float leftX, float rightX) = CalculateXBoundsForTrinangle(aUV, bUV, cUV, y);

                for (float x = leftX - pixelWidth; x <= rightX + pixelWidth; x += pixelWidth)
                {
                    image.SetPixel((int)(x * dimension), (int)(y * dimension), color);

                    // Margin pixel, so that there is no spaces between drawn triangles
                    if (x < prevLeftX || x > prevRightX)
                        image.SetPixel((int)(x * dimension), (int)((y - pixelWidth) * dimension), color);
                }

                prevLeftX = leftX;
                prevRightX = rightX;
            }

            for (float y = MathF.Round(maxY * 2048.0f) / 2048.0f; y >= middleY; y -= pixelWidth)
            {
                (float leftX, float rightX) = CalculateXBoundsForTrinangle(aUV, bUV, cUV, y);

                for (float x = leftX - pixelWidth; x <= rightX + pixelWidth; x += pixelWidth)
                {
                    image.SetPixel((int)(x * dimension), (int)(y * dimension), color);

                    // Margin pixel
                    if (x < prevLeftX || x > prevRightX)
                        image.SetPixel((int)(x * dimension), (int)((y + pixelWidth) * dimension), color);
                }

                prevLeftX = leftX;
                prevRightX = rightX;
            }
        }

        return image;
    }

    private (float LeftX, float RightX) CalculateXBoundsForTrinangle(Vector2 a, Vector2 b, Vector2 c, float y)
    {
        float leftX = 10.0f;
        float rightX = -10.0f;

        if (MathF.Max(a.Y, b.Y) >= y && MathF.Min(a.Y, b.Y) <= y)
        {
            float lineX = a.X + (b.X - a.X) * (y - a.Y) / (b.Y - a.Y);
            leftX = MathF.Min(leftX, lineX);
            rightX = MathF.Max(rightX, lineX);
        }

        if (MathF.Max(b.Y, c.Y) >= y && MathF.Min(b.Y, c.Y) <= y)
        {
            float lineX = b.X + (c.X - b.X) * (y - b.Y) / (c.Y - b.Y);
            leftX = MathF.Min(leftX, lineX);
            rightX = MathF.Max(rightX, lineX);
        }

        if (MathF.Max(a.Y, c.Y) >= y && MathF.Min(a.Y, c.Y) <= y)
        {
            float lineX = a.X + (c.X - a.X) * (y - a.Y) / (c.Y - a.Y);
            leftX = MathF.Min(leftX, lineX);
            rightX = MathF.Max(rightX, lineX);
        }

        return (leftX, rightX);
    }

    private void ApplyAlpha()
    {
        if (material == null)
            return;

        if (OverrideColourAlpha == null || overrideColourAlpha >= 1)
        {
            material.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
        }
        else
        {
            material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        }
    }
}
