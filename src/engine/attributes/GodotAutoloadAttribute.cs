using System;

/// <summary>
///   Marks classes that are autoloaded in Godot. Note that this needs to be manually added, and just this attribute
///   doesn't imply that something is an autoload. This just exists to easily find autoload classes in C#.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class GodotAutoloadAttribute : Attribute
{
}
