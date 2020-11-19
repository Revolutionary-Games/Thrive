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

    public abstract IInputReceiver InputReceiver { get; }
    internal static List<WeakReference<object>> InputReceivingInstances { get; } = new List<WeakReference<object>>();

    internal static List<(MethodBase method, RunOnInputAttribute inputAttribute)> AttributesWithMethods { get; }
        = new List<(MethodBase method, RunOnInputAttribute inputAttribute)>();

    public static void AddInstance(object obj)
    {
        InputReceivingInstances.Add(new WeakReference<object>(obj));
    }
}
