using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Creature component to display it using convolution surfaces
/// </summary>
public partial class MulticellularConvolutionDispayer : MeshInstance3D, IMetaballDisplayer<MulticellularMetaball>
{
    private const float AABBMargin = 0.1f;

#pragma warning disable CA2213
    [Export]
    private StandardMaterial3D? material;
#pragma warning disable CA2213

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

        GD.Print("Generating mesh");

        Mesh = meshGen.DualContour();

        GD.Print("Applying initial material");

        Mesh.SurfaceSetMaterial(0, material);

        GD.Print("Starting an unwrap and texturize task");

        Task uvUnwrap = new Task(() => UVUnwrapAndTexturize((ArrayMesh)Mesh));
        TaskExecutor.Instance.AddTask(uvUnwrap);

        CustomAabb = new Aabb(minExtends, maxExtends);
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
            GD.Print("Finishing initial material");

            CallDeferred(nameof(ApplyTextures), mesh);
        }
    }

    private void ApplyTextures(ArrayMesh mesh)
    {
        GD.Print("Finally applying a material");

        material.AlbedoTexture = ImageTexture.CreateFromImage(GenerateTexture(mesh));

        mesh.SurfaceSetMaterial(0, material);
    }

    private Image GenerateTexture(ArrayMesh mesh)
    {
        Image image = Image.CreateEmpty(2048, 2048, false, Image.Format.Rgba8);
        image.Fill(Colors.White);

        var arrays = mesh.SurfaceGetArrays(0);

        var indices = arrays[(int)ArrayMesh.ArrayType.Index].AsInt32Array();
        var uvs = arrays[(int)ArrayMesh.ArrayType.TexUV].AsVector2Array();

        float min = float.MaxValue;
        float max = float.MinValue;

        foreach (var uv in uvs)
        {
            if (uv.X < min)
                min = uv.X;
            if (uv.Y < min)
                min = uv.Y;
            if (uv.X > max)
                max = uv.X;
            if (uv.Y > max)
                max = uv.Y;
        }

        GD.Print("Min: " + min + ", max: " + max);

        for (int i = 0; i < indices.Length; i += 3)
        {
            for (float j = 0; j < 1.0f; j += 0.01f)
            {
                int pixelPosX = (int)(float.Lerp(uvs[indices[i]].X, uvs[indices[i + 1]].X, j) * 2048f);
                int pixelPosY = (int)(float.Lerp(uvs[indices[i]].Y, uvs[indices[i + 1]].Y, j) * 2048f);

                image.SetPixel(pixelPosX, pixelPosY, Colors.Black);
            }

            for (float j = 0; j < 1.0f; j += 0.01f)
            {
                int pixelPosX = (int)(float.Lerp(uvs[indices[i + 1]].X, uvs[indices[i + 2]].X, j) * 2048f);
                int pixelPosY = (int)(float.Lerp(uvs[indices[i + 1]].Y, uvs[indices[i + 2]].Y, j) * 2048f);

                image.SetPixel(pixelPosX, pixelPosY, Colors.Black);
            }

            for (float j = 0; j < 1.0f; j += 0.01f)
            {
                int pixelPosX = (int)(float.Lerp(uvs[indices[i]].X, uvs[indices[i + 2]].X, j) * 2048f);
                int pixelPosY = (int)(float.Lerp(uvs[indices[i]].Y, uvs[indices[i + 2]].Y, j) * 2048f);

                image.SetPixel(pixelPosX, pixelPosY, Colors.Black);
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
