using System;

/// <summary>
///   Marks a type as abstract i.e. no code in Thrive should instantiate it. This attribute exists as Godot 4 no longer
///   allows scripts attached in scenes to be abstract (so no <c>abstract partial</c> classes should be used).
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class GodotAbstractAttribute : Attribute
{
}

public class GodotAbstractMethodNotOverriddenException : NotImplementedException
{
}
