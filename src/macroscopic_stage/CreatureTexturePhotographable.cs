using Godot;
using Xoshiro.PRNG32;

public class CreatureTexturePhotographable : IScenePhotographable
{
    public MacroscopicSpecies Species;

    public Mesh CreatureMesh;

    public CreatureTexturePhotographable(Mesh mesh, MacroscopicSpecies species)
    {
        CreatureMesh = mesh;
        Species = species;
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

        for (int i = 0; i < 500; ++i)
        {
            int index = random.Next() % vertices.Length;

            // TODO: better calculations of the up vector
            var basis = Basis.LookingAt(-normals[index], Vector3.Right);

            matrices.Add(new Transform3D(basis, vertices[index]).Inverse());
        }

        photobuilder.SetProjectionMatrices(matrices);
        photobuilder.SetTextures(Species.GetMainTexture(), Species.GetProjectedTexture());
    }

    public ulong GetVisualHashCode()
    {
        return Species.GetVisualHashCode();
    }
}
