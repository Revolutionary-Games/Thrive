using Godot;

/// <summary>
///   Displays a scene based on its path. Also stores the previous path to avoid duplicate loads
/// </summary>
public class SceneDisplayer : Spatial
{
    private string currentScene;

    public Node CurrentlyShown { get; private set; }

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
        if (CurrentlyShown != null)
        {
            RemoveChild(CurrentlyShown);
            CurrentlyShown.QueueFree();
            CurrentlyShown = null;
        }

        if (string.IsNullOrEmpty(currentScene))
            return;

        var scene = GD.Load<PackedScene>(currentScene);

        CurrentlyShown = scene.Instance();
        AddChild(CurrentlyShown);
    }
}
