using Godot;

/// <summary>
///   Displays a layout of metaballs using multimeshing for more efficient rendering
/// </summary>
public class MulticellularMetaballDisplayer : MultiMeshInstance, IMetaballDisplayer<MulticellularMetaball>
{
    private const float AABBMargin = 1;

    private static readonly Mesh MetaballSphere = new SphereMesh()
    {
        Height = 1,
        Radius = 0.5f,

        // This is here in case we need custom shader effects at some point
        // Material = new ShaderMaterial()
        // {
        //     Shader = GD.Load<Shader>("res://shaders/Metaball.shader"),
        // },

        Material = new SpatialMaterial()
        {
            VertexColorUseAsAlbedo = true,
        },
    };

    public override void _Ready()
    {
        base._Ready();

        Multimesh = new MultiMesh()
        {
            Mesh = MetaballSphere,
            InstanceCount = 0,
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3d,
            ColorFormat = MultiMesh.ColorFormatEnum.Color8bit,
            CustomDataFormat = MultiMesh.CustomDataFormatEnum.None,
        };
    }

    public void DisplayFromLayout(MetaballLayout<Metaball> layout)
    {
        // Clear previous
        var mesh = Multimesh;
        if (mesh.InstanceCount != 0)
        {
            mesh.InstanceCount = 0;
        }

        int instances = layout.Count;
        mesh.InstanceCount = instances;

        if (instances < 1)
        {
            SetCustomAabb(new AABB(0, 0, 0, Vector3.One));
            return;
        }

        // TODO: drawing links between the metaballs (or maybe only just the editor needs that?)

        var basis = new Basis(Quat.Identity);

        var extends = Vector3.Zero;

        // Setup the metaball parameters for drawing
        for (int i = 0; i < instances; i++)
        {
            var metaball = layout[i];

            basis.Scale = new Vector3(metaball.Size, metaball.Size, metaball.Size);

            mesh.SetInstanceTransform(i, new Transform(basis, metaball.Position));
            mesh.SetInstanceColor(i, metaball.Color);

            // mesh.SetInstanceCustomData();

            // Keep track of the farthest points for AABB building
            var absPosition = metaball.Position.Abs() + new Vector3(metaball.Size, metaball.Size, metaball.Size);

            if (absPosition.x > extends.x)
                extends.x = absPosition.x;

            if (absPosition.y > extends.y)
                extends.y = absPosition.y;

            if (absPosition.y > extends.y)
                extends.y = absPosition.y;
        }

        SetCustomAabb(new AABB(0, 0, 0, extends + new Vector3(AABBMargin, AABBMargin, AABBMargin)));
    }
}
