using System;

/// <summary>
///   Adjusts the estimates runtime cost (time) of a system run. Systems without this attribute have a cost of 1.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class RuntimeCostAttribute : Attribute
{
    public RuntimeCostAttribute(float cost = 1, bool basedOnProfiling = true)
    {
        Cost = cost;
        BasedOnProfiling = basedOnProfiling;
    }

    public float Cost { get; set; }

    /// <summary>
    ///   When true just indicates the cost is based on a profiling run. Systems with this set to false probably were
    ///   so fast that they didn't show up in profiling so a lowered cost is assigned to them.
    /// </summary>
    public bool BasedOnProfiling { get; set; }
}
