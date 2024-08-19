using System.Collections.Generic;
using Godot;

/// <summary>
///   Creature component to display it using convolution surfaces
/// </summary>
public partial class MulticellularConvolutionDispayer : MeshInstance3D, IMetaballDisplayer<MulticellularMetaball>
{
    private const float AABBMargin = 0.1f;

    private StandardMaterial3D? material;
    private Mesh metaballSphere = null!;

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

    public void DisplayFromList(IReadOnlyCollection<MulticellularMetaball> layout)
    {
        Vector3 minExtends = Vector3.Zero;
        Vector3 maxExtends = Vector3.Zero;

        foreach (var metaball in layout)
        {
            minExtends.X = Mathf.Min(minExtends.X, metaball.Position.X - metaball.Radius - 0.5f);
            minExtends.Y = Mathf.Min(minExtends.Y, metaball.Position.Y - metaball.Radius - 0.5f);
            minExtends.Z = Mathf.Min(minExtends.Z, metaball.Position.Z - metaball.Radius - 0.5f);

            maxExtends.X = Mathf.Max(maxExtends.X, metaball.Position.X + metaball.Radius + 0.5f);
            maxExtends.Y = Mathf.Max(maxExtends.Y, metaball.Position.Y + metaball.Radius + 0.5f);
            maxExtends.Z = Mathf.Max(maxExtends.Z, metaball.Position.Z + metaball.Radius + 0.5f);
        }

        // GD.Print(minExtends + ", " + maxExtends);

        var meshGen = new DualContourer();
        meshGen.PointsPerUnit = 3;
        meshGen.UnitsFrom = minExtends;
        meshGen.UnitsTo = maxExtends;

        var mathFunction = new Scalis();
        mathFunction.SurfaceValue = 2;
        mathFunction.FindBones(layout);

        meshGen.MathFunction = mathFunction;
        Mesh = meshGen.DualContour();
        Mesh.SurfaceSetMaterial(0, material);

        CustomAabb = new Aabb(minExtends, maxExtends);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (material != null)
            {
                material.Dispose();
                metaballSphere.Dispose();
            }
        }

        base.Dispose(disposing);
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
