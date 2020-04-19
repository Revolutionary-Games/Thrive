using Godot;

/// <summary>
///   Displayes a scene based on its path. Also stores the previous path to avoid duplicate loads
/// </summary>
public class SceneDisplayer : Spatial
{
    private string currentScene = null;
    private Node currentlyShown;

    public string Scene
    {
        get
        {
            return currentScene;
        }
        set
        {
            if (currentScene == value)
                return;
            currentScene = value;
            LoadNewScene();
        }
    }

    private void LoadNewScene()
    {
        if (currentlyShown != null)
        {
            RemoveChild(currentlyShown);
            currentlyShown.QueueFree();
        }

        if (string.IsNullOrEmpty(currentScene))
            return;

        var scene = GD.Load<PackedScene>(currentScene);

        currentlyShown = scene.Instance();
        AddChild(currentlyShown);
    }
}
