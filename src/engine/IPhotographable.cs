using Godot;

public interface IPhotographable
{
    public string SceneToPhotographPath { get; }

    public void ApplySceneParameters(Spatial instancedScene);
    public float CalculatePhotographDistance(Spatial instancedScene);
}
