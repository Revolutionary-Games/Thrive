using System;
using System.Collections.Generic;
using System.Reflection;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public abstract class RunOnInputAttribute : Attribute
{
    ~RunOnInputAttribute()
    {
        AttributesWithMethods.RemoveAll(p => p.Item2.Equals(this));
    }

    public static IList<object> InputClasses { get; } = new List<object>();
    public abstract IInputReceiver InputReceiver { get; }
    internal static List<Tuple<MethodBase, RunOnInputAttribute>> AttributesWithMethods { get; }
        = new List<Tuple<MethodBase, RunOnInputAttribute>>();
}
