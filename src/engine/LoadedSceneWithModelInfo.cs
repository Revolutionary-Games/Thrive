using Godot;

/// <summary>
///   Wraps together a loaded scene with model info related to it (path to real model, animation players etc.). This
///   exists as these variables are very closely linked and mixing them from multiple sources will lead to errors
/// </summary>
/// <remarks>
///   <para>
///     This is not disposable as disposing these when copied all over would be pretty difficult. Anyway it isn't
///     too bad to delay <see cref="NodePath"/> dispose, and anyway most models stay loaded for a long time.
///   </para>
///   <para>
///     This is a struct as this is just a few variables so that seems appropriate especially as that makes storing
///     instances of this easy for comparing if data is still the same (though it should be doable later to convert
///     this to a class if there are so many variables that it no longer makes much sense to copy so much data around
///     all the time)
///   </para>
/// </remarks>
#pragma warning disable CA1001
public struct LoadedSceneWithModelInfo(PackedScene loadedScene, NodePath? modelPath, NodePath? animationPath)
#pragma warning restore CA1001
{
    /// <summary>
    ///   Loaded visual scene
    /// </summary>
    public PackedScene LoadedScene = loadedScene;

    /// <summary>
    ///   Optional path to the primary model in the scene if it is not the root node
    /// </summary>
    public NodePath? ModelPath = modelPath;

    /// <summary>
    ///   If the scene is animated this is a relative path to the animation
    /// </summary>
    public NodePath? AnimationPlayerPath = animationPath;

    public void LoadFrom(in SceneWithModelInfo sceneWithModelInfo)
    {
        LoadedScene = GD.Load<PackedScene>(sceneWithModelInfo.ScenePath);

        if (!string.IsNullOrEmpty(sceneWithModelInfo.ModelPath))
            ModelPath = new NodePath(sceneWithModelInfo.ModelPath);

        if (!string.IsNullOrEmpty(sceneWithModelInfo.AnimationPlayerPath))
            AnimationPlayerPath = new NodePath(sceneWithModelInfo.AnimationPlayerPath);
    }
}

/// <summary>
///   Unloaded variant of <see cref="LoadedSceneWithModelInfo"/>
/// </summary>
public struct SceneWithModelInfo(string scenePath, string? modelPath, string? animationPath)
{
    /// <summary>
    ///   Visual scene path
    /// </summary>
    public string ScenePath = scenePath;

    /// <summary>
    ///   If the model is not the scene root this needs to specify the relative path from the scene root to the actual
    ///   model
    /// </summary>
    public string? ModelPath = modelPath;

    /// <summary>
    ///   If the model has an animation player, this should be a relative path to where the animation player is
    /// </summary>
    public string? AnimationPlayerPath = animationPath;
}
