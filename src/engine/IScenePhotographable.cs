using Godot;

/// <summary>
///   A thing that can be photographed by instantiating a Godot scene that has visuals for this
/// </summary>
public interface IScenePhotographable : IPhotographable<Node3D>
{
    public string SceneToPhotographPath { get; }

    public void ApplySceneParameters(Node3D instancedScene);
}
