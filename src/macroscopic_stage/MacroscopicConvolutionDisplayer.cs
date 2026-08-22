using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using Xoshiro.PRNG32;

/// <summary>
///   Displays a creature using convolution surfaces based on a metaball layout
/// </summary>
public partial class MacroscopicConvolutionDisplayer : MeshInstance3D, IMetaballDisplayer<MacroscopicMetaball>
{
    private const float AABBMargin = 0.1f;

    private StandardMaterial3D? material;

    private float? overrideColourAlpha;

    private IImageTask? texturizationTask;

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

    /// <summary>
    ///   Note: not supported (doesn't do anything)
    /// </summary>
    public bool DisplayHierarchyLines { get; set; }

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

    public override void _Process(double delta)
    {
        if (texturizationTask?.Finished == true)
        {
            if (material != null)
            {
                material.AlbedoTexture = texturizationTask.FinalImage;
            }

            texturizationTask.PlainImage.SavePng($"G:/Downloads/SMTH.png");

            var gltfDocumentLoad = new GltfDocument();
            var gltfStateLoad = new GltfState();
            var error = gltfDocumentLoad.AppendFromScene(this, gltfStateLoad);

            if (error is Error.Ok)
            {
                // The file extension in the output `path` (`.gltf` or `.glb`) determines
                // whether the output uses text or binary format.
                // `GltfDocument.GenerateBuffer()` is also available for saving to memory.
                gltfDocumentLoad.WriteToFilesystem(gltfStateLoad, "G:/Downloads/A.gltf");
            }

            Mesh.SurfaceSetMaterial(0, material);

            texturizationTask = null;
        }
    }

    public void DisplayFromLayout(IReadOnlyCollection<MacroscopicMetaball> layout)
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
        var mathFunction = new Scalis(layout)
        {
            SurfaceValue = 1,
        };

        var meshGen = new DualContourer(mathFunction)
        {
            PointsPerUnit = Constants.CREATURE_MESH_RESOLUTION,
            UnitsFrom = minExtends,
            UnitsTo = maxExtends,
        };

        Mesh = meshGen.DualContour();

        Mesh.SurfaceSetMaterial(0, material);

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

    public void Texturize(MacroscopicSpecies species)
    {
        var uvUnwrap = new Task(() => UVUnwrapAndTexture((ArrayMesh)Mesh, species));
        TaskExecutor.Instance.AddTask(uvUnwrap);
    }

    private void UVUnwrapAndTexture(ArrayMesh mesh, MacroscopicSpecies species)
    {
        // TODO: investigate if it is somehow possible to avoid this data copy here (and another probably caused in
        // the native interop call ArrayMeshUnwrap)
        // Godot does the following in a unsafe block:
        // `godot_variant arg3_in = (godot_variant)arg3.NativeVar;` and uses `&arg3_in` to get a pointer to it.
        var nativeVariant = Variant.From(mesh).CopyNativeVariant();

        try
        {
            // Note: Unwrapper's Native code uses call_deferred (delayed call) to apply changes to the mesh surface
            // (so that the code can be multithreaded).
            // This means that there is no surface immediately after calling this function and texture application
            // has to be deferred too.
            if (NativeMethods.ArrayMeshUnwrap(nativeVariant, 1.0f))
            {
                Invoke.Instance.QueueForObject(() => ApplyTextures(species), this);
            }
            else
            {
                GD.PrintErr("Native ArrayMesh unwrap failed");
            }
        }
        finally
        {
            // Must be disposed to not leak resources
            nativeVariant.Dispose();
        }
    }

    private void ApplyTextures(MacroscopicSpecies species)
    {
        texturizationTask = PhotoStudio.Instance.GenerateImage(new CreatureTexturePhotographable(Mesh, species), 1,
            2048);
    }

    private (float LeftX, float RightX) CalculateXBoundsForTriangle(Vector2 a, Vector2 b, Vector2 c, float y)
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
