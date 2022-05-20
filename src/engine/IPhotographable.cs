using Godot;

public interface IPhotographable
{
    string SceneToPhotographPath { get; }

    void ApplySceneParameters(Spatial instancedScene);
    float CalculatePhotographDistance(Spatial instancedScene);
}
