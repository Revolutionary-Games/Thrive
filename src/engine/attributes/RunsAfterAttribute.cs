using System;

/// <summary>
///   Marks that the system with this attribute has to run after another system has finished
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class RunsAfterAttribute : Attribute
{
    public RunsAfterAttribute(Type afterSystem)
    {
        AfterSystem = afterSystem;
    }

    public Type AfterSystem { get; set; }
}
