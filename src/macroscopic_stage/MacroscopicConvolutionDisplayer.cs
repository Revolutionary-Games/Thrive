using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Displays a creature using convolution surfaces based on a metaball layout
/// </summary>
public partial class MacroscopicConvolutionDisplayer : MeshInstance3D, IMetaballDisplayer<MacroscopicMetaball>
{
    private const float AABBMargin = 0.1f;

    private StandardMaterial3D? material;

    private float? overrideColourAlpha;

    private IImageTask? texturizationTask;

    private UvGenerationStatus uvGenerationStatus;

    private ulong lastDisplayedLayoutHash;

#pragma warning disable CA2213
    [Export]
    private Material texturePaddingBlitMaterial = null!;
#pragma warning restore CA2213

    public enum UvGenerationStatus
    {
        NotStarted,
        Generating,
        Finished,
    }

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

    public bool Generating => uvGenerationStatus == UvGenerationStatus.Generating || texturizationTask != null;

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
            if (material == null)
            {
                GD.PrintErr("Creature texturization requested, but no material is present");
                return;
            }

            var texture = new DrawableTexture2D();
            texture.Setup(texturizationTask.FinalImage.GetWidth(), texturizationTask.FinalImage.GetHeight(),
                DrawableTexture2D.DrawableFormat.Rgba8, color: new Color(0.0f, 0.0f, 0.0f, 0.0f));

            ((ShaderMaterial)texturePaddingBlitMaterial).SetShaderParameter("jumpSize",
                1.0f / texturizationTask.FinalImage.GetWidth());

            texture.BlitRect(new Rect2I(Vector2I.Zero,
                    new Vector2I(texturizationTask.FinalImage.GetWidth(), texturizationTask.FinalImage.GetHeight())),
                texturizationTask.FinalImage, material: texturePaddingBlitMaterial);

            texture.BlitRect(new Rect2I(Vector2I.Zero,
                    new Vector2I(texturizationTask.FinalImage.GetWidth(), texturizationTask.FinalImage.GetHeight())),
                ImageTexture.CreateFromImage(texture.GetImage()), material: texturePaddingBlitMaterial);

            material.AlbedoTexture = texture;

            Mesh.SurfaceSetMaterial(0, material);

            texturizationTask = null;
        }
    }

    public void DisplayFromLayout(IReadOnlyCollection<MacroscopicMetaball> layout)
    {
        var newHash = MetaballLayoutHelpers.CalculateLayoutHash(layout);

        if (newHash == lastDisplayedLayoutHash)
        {
            return;
        }

        lastDisplayedLayoutHash = newHash;

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

        uvGenerationStatus = UvGenerationStatus.NotStarted;
        Mesh.SurfaceSetMaterial(0, material);

        CustomAabb = new Aabb(minExtends, maxExtends - minExtends);
    }

    public void Texturize(MetaballLayout<MacroscopicMetaball> layout, CreatureSkinType skinType)
    {
        if (uvGenerationStatus == UvGenerationStatus.Generating)
            return;

        if (uvGenerationStatus == UvGenerationStatus.NotStarted)
        {
            uvGenerationStatus = UvGenerationStatus.Generating;
            var uvUnwrap = new Task(() => UVUnwrapAndTexture((ArrayMesh)Mesh, layout, skinType));
            TaskExecutor.Instance.AddTask(uvUnwrap);
        }
        else
        {
            ApplyTextures(layout, skinType);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            material?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void UVUnwrapAndTexture(ArrayMesh mesh, MetaballLayout<MacroscopicMetaball> layout,
        CreatureSkinType skinType)
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
                uvGenerationStatus = UvGenerationStatus.Finished;

                Invoke.Instance.QueueForObject(() => ApplyTextures(layout, skinType), this);
            }
            else
            {
                uvGenerationStatus = UvGenerationStatus.NotStarted;

                GD.PrintErr("Native ArrayMesh unwrap failed");
            }
        }
        finally
        {
            // Must be disposed to not leak resources
            nativeVariant.Dispose();
        }
    }

    private void ApplyTextures(MetaballLayout<MacroscopicMetaball> layout, CreatureSkinType skinType)
    {
        var photographable = new CreatureTexturePhotographable(Mesh, layout, skinType);

        texturizationTask = PhotoStudio.Instance.GenerateImage(photographable, 1, 2048);
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
