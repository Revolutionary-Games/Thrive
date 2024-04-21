using System;
using System.Collections.Generic;
using System.Threading;
using DefaultEcs;
using Godot;

/// <summary>
///   Debugging helper class for verifying that component accesses are properly declared with attributes on systems
/// </summary>
public static class ComponentAccessChecks
{
    private static readonly ThreadLocal<HashSet<string>> AllowedAccessesPerThread = new(() => new HashSet<string>());

    private static bool enforcing;

    public static void ReportSimulationActive(bool active)
    {
        enforcing = active;
    }

    public static void ReportAllowedAccess(string type)
    {
        if (!enforcing)
            throw new InvalidOperationException("Can only be done while a simulation is running");

        AllowedAccessesPerThread.Value!.Add(type);
    }

    public static void ReportAllowedAccessType<T>()
    {
        ReportAllowedAccess(typeof(T).Name);
    }

    public static void ClearAccessForCurrentThread()
    {
        AllowedAccessesPerThread.Value!.Clear();
    }

    public static void CheckHasAccess(string type)
    {
        // When simulation is not running, this should not enforce any checks as all components are safe to access when
        // not running
        if (!enforcing)
            return;

        if (AllowedAccessesPerThread.Value!.Contains(type))
            return;

        GD.PrintErr("Not allowed access by currently running system to component of type: ", type);
        throw new InvalidOperationException($"Not allowed access to {type} (missing attribute marking access)");
    }
}

public static class ComponentFetchHelpers
{
    /// <summary>
    ///   Checks that currently running system is allowed to access the given component and then access it like normal
    /// </summary>
    /// <typeparam name="T">Type of component to try to access</typeparam>
    /// <returns>The component</returns>
    public static ref T GetChecked<T>(this Entity entity)
    {
        ComponentAccessChecks.CheckHasAccess(typeof(T).Name);
        return ref entity.Get<T>();
    }
}
