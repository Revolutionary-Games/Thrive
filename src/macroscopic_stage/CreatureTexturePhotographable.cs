using Godot;
using Xoshiro.PRNG32;

public class CreatureTexturePhotographable : IScenePhotographable
{
    public MetaballLayout<MacroscopicMetaball> Layout;

    public CreatureSkinType SkinType;

    public Mesh CreatureMesh;

    public CreatureTexturePhotographable(Mesh mesh, MetaballLayout<MacroscopicMetaball> layout,
        CreatureSkinType skinType)
    {
        CreatureMesh = mesh;
        Layout = layout;
        SkinType = skinType;
    }

    public string SceneToPhotographPath => "res://src/macroscopic_stage/CreatureTexturePhotoBuilder.tscn";

    public float Radius
    {
        get
        {
            return 0.5f;
        }
    }

    public Vector3 CalculatePhotographDistance(Node3D instancedScene)
    {
        return new Vector3(0.0f, MathUtils.CameraDistanceFromRadiusOfObject(Radius, Constants.PHOTO_STUDIO_CAMERA_FOV),
            0.0f);
    }

    public void ApplySceneParameters(Node3D instancedScene)
    {
        var photobuilder = (CreatureTexturePhotoBuilder)instancedScene;
        photobuilder.SetMesh(CreatureMesh);

        var arrays = CreatureMesh.SurfaceGetArrays(0);

        var vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        var normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();

        var matrices = new Godot.Collections.Array();

        var random = new XoShiRo128starstar();

        // This amount shouldn't exceed the array size of projectionMatrices in CreatureTexturizer.gdshader
        int projectionMatrixCount = 500;

        if (SkinType is CreatureSkinType.Plain or CreatureSkinType.Fur or CreatureSkinType.Skin)
        {
            projectionMatrixCount = 0;
        }

        for (int i = 0; i < projectionMatrixCount; ++i)
        {
            int index = random.Next() % vertices.Length;

            var basis = Basis.LookingAt(-normals[index]);

            matrices.Add(new Transform3D(basis, vertices[index]).Inverse());
        }

        photobuilder.SetProjectionMatrices(matrices);
        photobuilder.SetTextures(MacroscopicSpecies.GetMainTexture(SkinType),
            MacroscopicSpecies.GetProjectedTexture(SkinType));
    }

    public ulong GetVisualHashCode()
    {
        return MetaballLayoutHelpers.CalculateLayoutHash(Layout) + (ulong)SkinType;
    }
}
