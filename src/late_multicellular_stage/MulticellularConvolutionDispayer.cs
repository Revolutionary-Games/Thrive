using System;
using System.Collections.Generic;
using System.Threading;
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

        Mesh = meshGen.DualContour();
        Mesh.SurfaceSetMaterial(0, material);

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
        NativeMethods.ArrayMeshUnwrap(ref variant, 1.0f);

        CallDeferred(nameof(ApplyTextures), mesh);
    }

    private void ApplyTextures(ArrayMesh mesh)
    {
        mesh.SurfaceSetMaterial(0, material);
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
