﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using Xoshiro.PRNG32;

/// <summary>
///   Creature component to display it using convolution surfaces
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

        Image image = Image.CreateEmpty(dimension, dimension, false, Image.Format.Rgba8);
        image.Fill(Colors.White);

        var arrays = mesh.SurfaceGetArrays(0);

        // var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        var indices = arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
        var uvs = arrays[(int)Mesh.ArrayType.TexUV].AsVector2Array();

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

            // Drawing image based on triangles
            // TODO: Find a better way
            float abStep = 1.0f / (MathF.Abs(aUV.DistanceTo(bUV)) * dimension);

            for (float aCoef = -abStep; aCoef <= 1.0f + abStep; aCoef += abStep)
            {
                var left = aUV.Lerp(bUV, aCoef);
                var right = cUV.Lerp(bUV, aCoef);

                float acStep = 0.5f / (left.DistanceTo(right) * dimension);

                for (float bCoef = -acStep; bCoef <= 1.0f + acStep; bCoef += acStep)
                {
                    int x = (int)(float.Lerp(left.X, right.X, bCoef) * dimension);
                    int y = (int)(float.Lerp(left.Y, right.Y, bCoef) * dimension);

                    image.SetPixel(x, y, color);
                }
            }
        }

        return image;
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
