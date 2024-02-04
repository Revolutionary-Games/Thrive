using System;

/// <summary>
///   Marks a system as having a precondition to running
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class RunsConditionallyAttribute : Attribute
{
    public RunsConditionallyAttribute(string condition)
    {
        Condition = condition;
    }

    public string Condition { get; set; }
}
