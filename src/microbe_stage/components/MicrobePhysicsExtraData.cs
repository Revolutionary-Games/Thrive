namespace Components;

using SharedBase.Archive;
using Systems;

/// <summary>
///   Extra data about microbe physics bodies. Stores info on colony members and pili for special physics handling
///   when certain sub-shapes collide
/// </summary>
/// <remarks>
///   <para>
///     TODO: should everything here be marked as not JSON properties for saves? This is all transient information
///     that shouldn't be used before the new shape is generated and this is updated after loading.
///   </para>
/// </remarks>
public struct MicrobePhysicsExtraData : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   When this is 0 this data is not initialized. Don't change the values in this struct from anywhere else
    ///   than <see cref="MicrobePhysicsCreationAndSizeSystem"/>
    /// </summary>
    public int TotalShapeCount;

    /// <summary>
    ///   Total microbe shapes. In the physics body there's this many physics shapes first that represent cells.
    ///   Sub-shape indexes are mapped to colony members by MicrobeShapeColonyMemberIndexes.
    /// </summary>
    public int MicrobeShapesCount;

    /// <summary>
    ///   Runtime mapping from cell sub-shapes to colony member indexes, excluding cavity cells.
    ///   Rebuilt with the physics shape after loading. Null when indexes match colony member order.
    /// </summary>
    public int[]? MicrobeShapeColonyMemberIndexes;

    /// <summary>
    ///   How many pilus collision shapes there are in the physics body after the microbe shapes. 0 means there
    ///   are no pili.
    /// </summary>
    public int PilusCount;

    /// <summary>
    ///   How many of the last <see cref="PilusCount"/> shapes are injectisomes. If 0 then all pili are normal
    ///   pili.
    /// </summary>
    public int PilusInjectisomeCount;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentMicrobePhysicsExtraData;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(TotalShapeCount);
        writer.Write(MicrobeShapesCount);
        writer.Write(PilusCount);
        writer.Write(PilusInjectisomeCount);
    }
}

public static class MicrobePhysicsExtraDataHelpers
{
    public static MicrobePhysicsExtraData ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > MicrobePhysicsExtraData.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, MicrobePhysicsExtraData.SERIALIZATION_VERSION);

        return new MicrobePhysicsExtraData
        {
            TotalShapeCount = reader.ReadInt32(),
            MicrobeShapesCount = reader.ReadInt32(),
            PilusCount = reader.ReadInt32(),
            PilusInjectisomeCount = reader.ReadInt32(),
        };
    }

    public static bool IsSubShapePilus(this ref MicrobePhysicsExtraData physicsExtraData, uint subShape)
    {
        // Index needs to be higher than all the microbes index but lower than the number of pili above that
        return subShape >= physicsExtraData.MicrobeShapesCount &&
            subShape < physicsExtraData.MicrobeShapesCount + physicsExtraData.PilusCount;
    }

    /// <summary>
    ///   After <see cref="IsSubShapePilus"/> returns true this can be used to check if a pilus is an injectisome
    ///   or normal pilus.
    /// </summary>
    /// <returns>True if injectisome</returns>
    public static bool IsSubShapeInjectisomeIfIsPilus(this ref MicrobePhysicsExtraData physicsExtraData,
        uint subShape)
    {
        var pilusIndex = subShape - physicsExtraData.MicrobeShapesCount;

        return pilusIndex >= physicsExtraData.PilusCount - physicsExtraData.PilusInjectisomeCount;
    }

    public static bool MicrobeIndexFromSubShape(this ref MicrobePhysicsExtraData physicsExtraData, uint subShape,
        out int index)
    {
        if (subShape < physicsExtraData.MicrobeShapesCount)
        {
            index = physicsExtraData.MicrobeShapeColonyMemberIndexes?[(int)subShape] ?? (int)subShape;
            return true;
        }

        // When there's one subs-shape it gets mapped to uint.max so handle that
        if (subShape == PhysicsCollision.COLLISION_UNKNOWN_SUB_SHAPE && physicsExtraData.MicrobeShapesCount == 1)
        {
            index = physicsExtraData.MicrobeShapeColonyMemberIndexes?[0] ?? 0;
            return true;
        }

        index = -1;
        return false;
    }
}
