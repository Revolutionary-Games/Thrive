using Godot;

/// <summary>
///   Displays a scene based on its path. Also stores the previous path to avoid duplicate loads
/// </summary>
public class SceneDisplayer : Spatial
{
	private string? currentScene;

#pragma warning disable CA2213 // manually managed
	private Node? currentlyShown;
#pragma warning restore CA2213

	public string? Scene
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
	///   Get the material of this scene's model.
	/// </summary>
	/// <param name="modelPath">Path to model within the scene. If null takes scene root as model.</param>
	/// <returns>ShaderMaterial of the GeometryInstance. Null if no scene.</returns>
	public ShaderMaterial? GetMaterial(NodePath? modelPath = null)
	{
		return currentlyShown?.GetMaterial(modelPath);
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

		if (string.IsNullOrEmpty(currentScene))
			return;

		var scene = GD.Load<PackedScene>(currentScene);

		currentlyShown = scene.Instance();
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
