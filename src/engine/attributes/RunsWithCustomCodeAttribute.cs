using System;

/// <summary>
///   Overrides how the automatic system threading generator generates the call to this system
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class RunsWithCustomCodeAttribute : Attribute
{
    public RunsWithCustomCodeAttribute(string customCode)
    {
        CustomCode = customCode;
    }

    public string CustomCode { get; set; }
}
