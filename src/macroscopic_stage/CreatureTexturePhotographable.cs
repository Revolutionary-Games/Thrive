using Godot;
using System;

public class CreatureTexturePhotographable : IScenePhotographable
{
    public Mesh CreatureMesh = null!;

    public CreatureTexturePhotographable(Mesh mesh)
    {
        CreatureMesh = mesh;
    }

    public string SceneToPhotographPath => "res://src/macroscopic_stage/CreatureTexturePhotoBuilder.tscn";

    public float Radius
    {
        get
        {
            return 0.5f;
        }
    }

    public ulong GetVisualHashCode()
    {
        // TBD
        return 0;
    }

    public Vector3 CalculatePhotographDistance(Node3D instancedScene)
    {
        return new Vector3(0.0f, MathUtils.CameraDistanceFromRadiusOfObject(Radius, Constants.PHOTO_STUDIO_CAMERA_FOV),
            0.0f);
    }

    public void ApplySceneParameters(Node3D instancedScene)
    {
        ((CreatureTexturePhotoBuilder)instancedScene).SetMesh(CreatureMesh);
    }
}
