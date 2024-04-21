using System;

/// <summary>
///   Marks a type as abstract i.e. no code in Thrive should instantiate it. This attribute exists as Godot 4 no longer
///   allows scripts attached in scenes to be abstract (so no <c>abstract partial</c> classes should be used).
/// </summary>
/// <remarks>
///   <para>
///     Types that have this attribute should have a protected constructor to avoid accidental use from code.
///   </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public class GodotAbstractAttribute : Attribute
{
}

/// <summary>
///   Thrown when a virtual method in a class marked with <see cref="GodotAbstractAttribute"/> is not overridden by
///   a derived class even though it must be done so. Such methods are not marked <c>abstract</c> as that is no longer
///   possible to have Godot-derived types be abstract.
/// </summary>
public class GodotAbstractMethodNotOverriddenException : NotImplementedException
{
}

public class GodotAbstractPropertyNotOverriddenException : NotSupportedException
{
    public GodotAbstractPropertyNotOverriddenException() : base(
        "This property should have been overridden by a derived class")
    {
    }
}
