﻿namespace Components;

using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Godot;
using SharedBase.Archive;

/// <summary>
///   Entity that triggers various microbe event callbacks when things happen to it. This is mostly used for
///   connecting the player cell to the GUI and game stage.
/// </summary>
public struct MicrobeEventCallbacks : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   Triggers whenever the player enters unbind mode
    ///   <remarks>
    ///     <para>
    ///       Only works for the player
    ///     </para>
    ///   </remarks>
    /// </summary>
    public Action<Entity>? OnUnbindEnabled;

    public Action<Entity>? OnUnbound;

    public Action<Entity, Entity>? OnIngestedByHostile;

    public Action<Entity>? OnEjectedFromHostileEngulfer;

    public Action<Entity, Entity>? OnSuccessfulEngulfment;

    public Action<Entity>? OnEngulfmentStorageFull;

    public Action<Entity>? OnEngulfmentStorageNearlyEmpty;

    public Action<Entity, IHUDMessage>? OnNoticeMessage;

    /// <summary>
    ///   Called when the reproduction status of this microbe changes
    /// </summary>
    public Action<Entity, bool>? OnReproductionStatus;

    /// <summary>
    ///   Called periodically to report the chemoreception settings of the microbe. Reports both compound and
    ///   species detections.
    /// </summary>
    public Action<Entity, List<(Compound Compound, Color Colour, Vector3 Target)>?,
        List<(Species Species, Entity Entity, Color Colour, Vector3 Target)>?>? OnChemoreceptionInfo;

    /// <summary>
    ///   Called when an organelle duplicates in this microbe in preparation for reproduction
    /// </summary>
    public Action<Entity, PlacedOrganelle>? OnOrganelleDuplicated;

    /// <summary>
    ///   Temporary callbacks can be deleted in certain situations (for example, used when creating microbe colony
    ///   event forwarders which are destroyed when the colony is disbanded)
    /// </summary>
    public bool IsTemporary;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentMicrobeEventCallbacks;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(IsTemporary);
        writer.WriteDelegateOrNull(OnUnbindEnabled);
        writer.WriteDelegateOrNull(OnUnbound);
        writer.WriteDelegateOrNull(OnIngestedByHostile);
        writer.WriteDelegateOrNull(OnEjectedFromHostileEngulfer);
        writer.WriteDelegateOrNull(OnSuccessfulEngulfment);
        writer.WriteDelegateOrNull(OnEngulfmentStorageFull);
        writer.WriteDelegateOrNull(OnEngulfmentStorageNearlyEmpty);
        writer.WriteDelegateOrNull(OnNoticeMessage);
        writer.WriteDelegateOrNull(OnReproductionStatus);
        writer.WriteDelegateOrNull(OnChemoreceptionInfo);
        writer.WriteDelegateOrNull(OnOrganelleDuplicated);
    }
}

public static class MicrobeEventCallbackHelpers
{
    public static MicrobeEventCallbacks ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > MicrobeEventCallbacks.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, MicrobeEventCallbacks.SERIALIZATION_VERSION);

        return new MicrobeEventCallbacks
        {
            IsTemporary = reader.ReadBool(),
            OnUnbindEnabled = reader.ReadDelegate<Action<Entity>>(),
            OnUnbound = reader.ReadDelegate<Action<Entity>>(),
            OnIngestedByHostile = reader.ReadDelegate<Action<Entity, Entity>>(),
            OnEjectedFromHostileEngulfer = reader.ReadDelegate<Action<Entity>>(),
            OnSuccessfulEngulfment = reader.ReadDelegate<Action<Entity, Entity>>(),
            OnEngulfmentStorageFull = reader.ReadDelegate<Action<Entity>>(),
            OnEngulfmentStorageNearlyEmpty = reader.ReadDelegate<Action<Entity>>(),
            OnNoticeMessage = reader.ReadDelegate<Action<Entity, IHUDMessage>>(),
            OnReproductionStatus = reader.ReadDelegate<Action<Entity, bool>>(),
            OnChemoreceptionInfo = reader
                .ReadDelegate<Action<Entity, List<(Compound Compound, Color Colour, Vector3 Target)>?,
                    List<(Species Species, Entity Entity, Color Colour, Vector3 Target)>?>>(),
            OnOrganelleDuplicated = reader.ReadDelegate<Action<Entity, PlacedOrganelle>>(),
        };
    }

    /// <summary>
    ///   Send a microbe notice message to the entity if possible
    /// </summary>
    /// <param name="entity">Entity to send the message to</param>
    /// <param name="message">The message text</param>
    /// <returns>True if sent, false if missing the component or callback</returns>
    public static bool SendNoticeIfPossible(this in Entity entity, LocalizedString message)
    {
        if (!entity.IsAliveAndHas<MicrobeEventCallbacks>())
            return false;

        ref var callbacks = ref entity.Get<MicrobeEventCallbacks>();

        if (callbacks.OnNoticeMessage == null)
            return false;

        callbacks.OnNoticeMessage.Invoke(entity, new SimpleHUDMessage(message.ToString()));
        return true;
    }

    /// <summary>
    ///   Variant that uses a factory method that is only called if the message can be sent to generate the message
    /// </summary>
    public static bool SendNoticeIfPossible(this in Entity entity, Func<SimpleHUDMessage> messageFactory)
    {
        if (!entity.IsAliveAndHas<MicrobeEventCallbacks>())
            return false;

        ref var callbacks = ref entity.Get<MicrobeEventCallbacks>();

        if (callbacks.OnNoticeMessage == null)
            return false;

        callbacks.OnNoticeMessage.Invoke(entity, messageFactory());
        return true;
    }

    /// <summary>
    ///   Variant that uses an always allocated HUD message
    /// </summary>
    public static bool SendNoticeIfPossible(this in Entity entity, SimpleHUDMessage message)
    {
        if (!entity.IsAliveAndHas<MicrobeEventCallbacks>())
            return false;

        ref var callbacks = ref entity.Get<MicrobeEventCallbacks>();

        if (callbacks.OnNoticeMessage == null)
            return false;

        callbacks.OnNoticeMessage.Invoke(entity, new SimpleHUDMessage(message.ToString()));
        return true;
    }

    /// <summary>
    ///   Clones callbacks of the colony leader for putting on a colony member.
    /// </summary>
    /// <param name="originalCallbacks">Callbacks to clone (should have at least one callback set)</param>
    /// <returns>A new callback instance with cloned callbacks that make sense for this context</returns>
    public static MicrobeEventCallbacks CloneEventCallbacksForColonyMember(this MicrobeEventCallbacks originalCallbacks)
    {
        return new MicrobeEventCallbacks
        {
            OnSuccessfulEngulfment = originalCallbacks.OnSuccessfulEngulfment,

            OnEngulfmentStorageFull = originalCallbacks.OnEngulfmentStorageFull,

            // This triggers a ton so for now this is left out, so the engulfing full tutorial no longer ends
            // automatically in a cell colony
            // OnEngulfmentStorageNearlyEmpty = originalCallbacks.OnEngulfmentStorageNearlyEmpty,

            OnNoticeMessage = originalCallbacks.OnNoticeMessage,

            // Mark this to be deleted on colony disband
            IsTemporary = true,
        };
    }
}
