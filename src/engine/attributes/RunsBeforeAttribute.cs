using System;

/// <summary>
///   Marks that the system with this attribute has to run after another system has finished. For example due to
///   another system clearing data that this needs to run
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class RunsBeforeAttribute : Attribute
{
    public RunsBeforeAttribute(Type beforeSystem)
    {
        BeforeSystem = beforeSystem;
    }

    public Type BeforeSystem { get; set; }
}
