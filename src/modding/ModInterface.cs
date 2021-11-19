using Godot;

/// <summary>
///   Implementation of the default <see cref="IModInterface"/>
/// </summary>
public class ModInterface : IModInterface
{
    public ModInterface(SceneTree sceneTree)
    {
        SceneTree = sceneTree;
    }

    public SceneTree SceneTree { get; }

    public Node CurrentScene => SceneTree.CurrentScene;
}
