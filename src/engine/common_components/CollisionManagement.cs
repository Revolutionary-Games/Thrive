namespace Components;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Arch.Core;
using Godot;
using SharedBase.Archive;

/// <summary>
///   Allows modifying <see cref="Physics"/> collisions of this entity
/// </summary>
public struct CollisionManagement : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 2;

    /// <summary>
    ///   Collisions experienced by this entity note that <see cref="RecordActiveCollisions"/> needs to be 1 or
    ///   more for this list to the populated. Don't reassign this list as otherwise it will stop being updated
    ///   by the underlying physics body.
    /// </summary>
    public PhysicsCollision[]? ActiveCollisions;

    /// <summary>
    ///   Pointer to the field that stores the size of valid collisions inside <see cref="ActiveCollisions"/>.
    ///   Use
    /// </summary>
    public IntPtr ActiveCollisionCountPtr;

    public List<Entity>? IgnoredCollisionsWith;

    /// <summary>
    ///   If the same entity from <see cref="IgnoredCollisionsWith"/> is here, actual physics engine ignores will be
    ///   removed when the entry no longer exists in that list.
    /// </summary>
    public List<Entity>? RemoveIgnoredCollisions;

    /// <summary>
    ///   When specified this callback is called before any physics collisions are allowed to happen. Returning
    ///   false will prevent that collision. Note that no state should be modified (that is not completely
    ///   thread-safe and entity order safe) by this. Also this will increase the physics processing expensiveness
    ///   of an entity so if at all possible other approaches should be used to filter out unwanted collisions.
    ///   Or only react to detected collisions of wanted type. The filter works by calling from the native side
    ///   back to the C# side inside the physics simulation.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     When clearing this, it is extremely important to set <see cref="StateApplied"/> as otherwise the C++
    ///     side will hold onto an invalid callback and cause very weird method call bugs. The only case where that
    ///     is fine if this delegate refers to a static method.
    ///   </para>
    /// </remarks>
    /// <remarks>
    ///   <para>
    ///     TODO: plan if this should be saved (in which case some objects don't want their callbacks to save,
    ///     for example the toxin collision system) or if all systems will need to reapply their filters after load
    ///   </para>
    /// </remarks>
    public PhysicalWorld.OnCollisionFilterCallback? CollisionFilter;

    /// <summary>
    ///   When set above 0 up to this many collisions are recorded in <see cref="ActiveCollisions"/>
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Note that increasing or lowering this value after recording has been enabled has no effect. All
    ///     entities should just initially figure out how many max collisions they should handle.
    ///   </para>
    /// </remarks>
    public volatile int RecordActiveCollisions;

    /// <summary>
    ///   Must be set to false after changing any properties to have them apply (after the initial creation)
    /// </summary>
    public bool StateApplied;

    // The following variables are internal for the collision management system and should not be modified
    public bool CollisionFilterCallbackRegistered;

    /// <summary>
    ///   Internal flag, don't touch. Used as an optimization to not always have to call the native side library.
    /// </summary>
    public bool CollisionIgnoresUsed;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentCollisionManagement;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        // Save only persistent state
        writer.WriteObjectOrNull(IgnoredCollisionsWith);
        writer.Write(RecordActiveCollisions);
        writer.WriteObjectOrNull(RemoveIgnoredCollisions);
    }
}

public static class CollisionManagementHelpers
{
    public static CollisionManagement ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > CollisionManagement.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, CollisionManagement.SERIALIZATION_VERSION);

        return new CollisionManagement
        {
            IgnoredCollisionsWith = reader.ReadObjectOrNull<List<Entity>>(),
            RecordActiveCollisions = reader.ReadInt32(),
            RemoveIgnoredCollisions = CollisionManagement.SERIALIZATION_VERSION > 1 ? reader.ReadObjectOrNull<List<Entity>>() : null,
        };
    }

    public static void StartCollisionRecording(this ref CollisionManagement collisionManagement, int maxCollisions)
    {
        if (collisionManagement.RecordActiveCollisions >= maxCollisions)
            return;

        Interlocked.Add(ref collisionManagement.RecordActiveCollisions, maxCollisions);
        collisionManagement.StateApplied = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetActiveCollisions(this ref CollisionManagement collisionManagement,
        out PhysicsCollision[]? collisions)
    {
        // If state is not correct for reading
        collisions = collisionManagement.ActiveCollisions;
        if (collisions == null || collisionManagement.ActiveCollisionCountPtr.ToInt64() == 0)
        {
            return 0;
        }

        var count = Marshal.ReadInt32(collisionManagement.ActiveCollisionCountPtr);

#if DEBUG
        if (count > collisions.Length)
        {
            GD.PrintErr("Active collisions from native side read as having more items than possible, returning " +
                $"{collisions.Length} instead of {count}");
            return collisions.Length;
        }
#endif

        return count;
    }

    public static void AddPermanentCollisionIgnoreWith(this ref CollisionManagement collisionManagement, Entity entity)
    {
        collisionManagement.IgnoredCollisionsWith ??= new List<Entity>();

        if (collisionManagement.IgnoredCollisionsWith.Contains(entity))
            return;

        collisionManagement.IgnoredCollisionsWith.Add(entity);

        collisionManagement.StateApplied = false;
    }

    public static void AddTemporaryCollisionIgnoreWith(this ref CollisionManagement collisionManagement, Entity entity)
    {
        collisionManagement.IgnoredCollisionsWith ??= new List<Entity>();

        if (collisionManagement.IgnoredCollisionsWith.Contains(entity))
            return;

        collisionManagement.IgnoredCollisionsWith.Add(entity);

        collisionManagement.RemoveIgnoredCollisions ??= new List<Entity>();

        if (!collisionManagement.RemoveIgnoredCollisions.Contains(entity))
            collisionManagement.RemoveIgnoredCollisions.Add(entity);

        collisionManagement.StateApplied = false;
    }

    public static void RemoveTemporaryCollisionIgnoreWith(this ref CollisionManagement collisionManagement,
        Entity entity)
    {
        if (collisionManagement.IgnoredCollisionsWith == null)
            return;

        if (!collisionManagement.IgnoredCollisionsWith.Contains(entity))
            return;

        collisionManagement.IgnoredCollisionsWith.Remove(entity);

        collisionManagement.StateApplied = false;
    }
}
