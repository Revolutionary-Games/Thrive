using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Displays a layout of metaballs using multimeshing for more efficient rendering
/// </summary>
public partial class MulticellularMetaballDisplayer : MultiMeshInstance3D, IMetaballDisplayer<MulticellularMetaball>
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
        // material = new ShaderMaterial()
        // {
        //     Shader = GD.Load<Shader>("res://shaders/Metaball.shader"),
        // },
        material = new StandardMaterial3D
        {
            VertexColorUseAsAlbedo = true,
        };

        ApplyAlpha();

        metaballSphere = new SphereMesh
        {
            Height = 1,
            Radius = 0.5f,

            // TODO: reduce the vertex further of the shape if we can without much visual impact for more performance
            // or maybe we should make a graphics option to select from a few levels of vertices
            RadialSegments = 48,
            Rings = 24,

            // These are the defaults
            // RadialSegments = 64,
            // Rings = 32,

            Material = material,
        };

        Multimesh = new MultiMesh
        {
            Mesh = metaballSphere,
            InstanceCount = 0,
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            UseCustomData = false,
        };

        ExtraCullMargin = AABBMargin;
    }

    public void DisplayFromLayout(IReadOnlyCollection<MulticellularMetaball> layout)
    {
        var mesh = Multimesh;

        int instances = layout.Count;
        mesh.InstanceCount = instances;

        if (instances < 1)
        {
            CustomAabb = new Aabb(0, 0, 0, Vector3.One);
            return;
        }

        // TODO: drawing links between the metaballs (or maybe only just the editor needs that?)

        var extends = Vector3.Zero;

        int i = 0;

        // Setup the metaball parameters for drawing
        foreach (var metaball in layout)
        {
            if (i >= instances)
                throw new ArgumentException("List count doesn't matches indexes when setting parameters");

            var basis = Basis.Identity.Scaled(new Vector3(metaball.Size, metaball.Size, metaball.Size));

            var colour = metaball.Colour;

            if (OverrideColourAlpha != null)
                colour.A = OverrideColourAlpha.Value;

            // TODO: check if using SetAsBulkArray is faster
            mesh.SetInstanceTransform(i, new Transform3D(basis, metaball.Position));

            mesh.SetInstanceColor(i, colour);

            // mesh.SetInstanceCustomData();

            // Keep track of the farthest points for AABB building
            var absPosition = metaball.Position.Abs() + new Vector3(metaball.Size, metaball.Size, metaball.Size);

            if (absPosition.X > extends.X)
                extends.X = absPosition.X;

            if (absPosition.Y > extends.Y)
                extends.Y = absPosition.Y;

            if (absPosition.Z > extends.Z)
                extends.Z = absPosition.Z;

            ++i;
        }

        CustomAabb = new Aabb(-extends, extends * 2);
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
