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
///   Let's dividing cells clip through each other until too far
/// </summary>
public struct CellDivisionCollisionDisablerComponent : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    public Entity? IgnoredCollisionWith;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentCellDivisionCollisionDisabler;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        // Save only persistent state

        // doesnt work
        //writer.WriteObjectOrNull(IgnoredCollisionWith);
    }
}
