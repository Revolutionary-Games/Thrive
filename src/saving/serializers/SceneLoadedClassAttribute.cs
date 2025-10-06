using System;

/// <summary>
///   When a class has this attribute it is loaded from a Godot scene on deserialization.
///   Implies UseThriveSerializerAttribute to automatically also use the converter that can handle this
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
