using System.Collections.Generic;
using Godot;

/// <summary>
///   Displays a loaded scene object. Also stores the previous instance to avoid duplicate instantiations.
/// </summary>
public partial class SceneDisplayer : Node3D
{
#pragma warning disable CA2213
    private PackedScene? currentScene;
#pragma warning restore CA2213

#pragma warning disable CA2213 // manually managed
    private Node? currentlyShown;
#pragma warning restore CA2213

    public PackedScene? Scene
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

    public Node? InstancedNode => currentlyShown;

    /// <summary>
    ///   Can be used to store anything. For example can be used to know if the visual hash of this item is the same
    ///   as what is desired to be loaded.
    /// </summary>
    public long UserData { get; set; }

    /// <summary>
    ///   Get the material of this scene's model.
    /// </summary>
    /// <param name="result">Where the resulting materials are stored</param>
    /// <param name="modelPath">Path to model within the scene. If null takes scene root as model.</param>
    /// <returns>False if no scene set (or fetch failed). True when <see cref="result"/> was filled.</returns>
    public bool GetMaterial(List<ShaderMaterial> result, NodePath? modelPath = null)
    {
        if (currentlyShown == null)
            return false;

        return currentlyShown.GetMaterial(result, modelPath);
    }

    public void LoadFromAlreadyLoadedNode(Node sceneToShow)
    {
        if (sceneToShow == InstancedNode)
            return;

        RemovePreviousScene();

        // We don't know the scene name now
        currentScene = null;

        currentlyShown = sceneToShow;
        AddChild(currentlyShown);
    }

    private void LoadNewScene()
    {
        RemovePreviousScene();

        if (currentScene == null)
            return;

        currentlyShown = currentScene.Instantiate();
        AddChild(currentlyShown);
    }

    private void RemovePreviousScene()
    {
        if (currentlyShown != null)
        {
            RemoveChild(currentlyShown);
            currentlyShown.QueueFree();
            currentlyShown = null;
        }
    }
}
