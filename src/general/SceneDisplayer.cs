using Godot;

/// <summary>
///   Displays a scene based on its path. Also stores the previous path to avoid duplicate loads
/// </summary>
public class SceneDisplayer : Spatial
{
    private string currentScene;
    private Node currentlyShown;

    public string Scene
    {
        get => currentScene;
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
            currentlyShown = null;
        }

        if (string.IsNullOrEmpty(currentScene))
            return;

        var scene = GD.Load<PackedScene>(currentScene);

        currentlyShown = scene.Instance();
        AddChild(currentlyShown);
    }
}
