namespace Components;

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
[JSONDynamicTypeAllowed]
public struct MicrobePhysicsExtraData
{
    /// <summary>
    ///   When this is 0 this data is not initialized. Don't change the values in this struct from anywhere else
    ///   than <see cref="MicrobePhysicsCreationAndSizeSystem"/>
    /// </summary>
    public int TotalShapeCount;

    /// <summary>
    ///   Total microbe shapes. In the physics body there's this many physics shapes first that represent cells.
    ///   The indexes of the sub-shapes match the order of cells in the microbe colony.
    /// </summary>
    public int MicrobeShapesCount;

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
}

public static class MicrobePhysicsExtraDataHelpers
{
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
            index = (int)subShape;
            return true;
        }

        // When there's one subs-shape it gets mapped to uint.max so handle that
        if (subShape == PhysicsCollision.COLLISION_UNKNOWN_SUB_SHAPE && physicsExtraData.MicrobeShapesCount == 1)
        {
            index = 0;
            return true;
        }

        index = -1;
        return false;
    }
}
