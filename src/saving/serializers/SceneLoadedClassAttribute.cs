using System;

/// <summary>
///   When a class has this attribute, it can be loaded by the scene manager just by the class name
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SceneLoadedClassAttribute : Attribute
{
    public SceneLoadedClassAttribute(string scenePath)
    {
        ScenePath = scenePath;
    }

    /// <summary>
    ///   The scene path to load the object from
    /// </summary>
    public string ScenePath { get; }

    /// <summary>
    ///   If true, the type this is on implements IGodotEarlyNodeResolve
    /// </summary>
    public bool UsesEarlyResolve { get; set; } = true;
}
