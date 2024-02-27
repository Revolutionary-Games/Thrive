using System;

/// <summary>
///   Marks a component as acting by default as if <see cref="ReadsComponentAttribute"/> was applied to it on any
///   systems it is used. Writes can still be explicitly referenced with <see cref="WritesToComponentAttribute"/>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class ComponentIsReadByDefaultAttribute : Attribute
{
}
