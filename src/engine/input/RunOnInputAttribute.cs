using System;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
///   An abstract class for handling various InputAttributes
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public abstract class RunOnInputAttribute : Attribute
{
    ~RunOnInputAttribute()
    {
        AttributesWithMethods.RemoveAll(p => p.Item2.Equals(this));
    }

    public static IList<object> InputClasses { get; } = new List<object>();
    public abstract IInputReceiver InputReceiver { get; }

    internal static List<(MethodBase method, RunOnInputAttribute inputAttribute)> AttributesWithMethods { get; }
        = new List<(MethodBase method, RunOnInputAttribute inputAttribute)>();
}
