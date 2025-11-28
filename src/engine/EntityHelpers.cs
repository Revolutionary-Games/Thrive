using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Godot;

/// <summary>
///   Extensions to the <see cref="Entity"/> to make it less-verbose or easier to use
/// </summary>
public static class EntityHelpers
{
    /// <summary>
    ///   Variant of checking for a component that is safe to call on entities that are possibly dead or null.
    ///   Makes Arch act much more like the previous DefaultECS we used before.
    /// </summary>
    /// <param name="entity">Entity to check for component in</param>
    /// <typeparam name="T">Type of component to check</typeparam>
    /// <returns>True if the entity is alive and has the specified component</returns>
    public static bool IsAliveAndHas<T>(this Entity entity)
    {
        if (entity == Entity.Null)
            return false;

#if DEBUG
        if (entity.IsAllZero())
        {
            GD.PrintErr("Detected entity that is all 0 values, this is a data initialization error");
            Debugger.Break();
            throw new ArgumentException("Entity value is all 0 values");
        }

#endif

        return entity.IsAlive() && entity.Has<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAllZero(this Entity entity)
    {
        return entity is { Version: 0, WorldId: 0, Id: 0 };
    }
}
