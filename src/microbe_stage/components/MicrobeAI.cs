namespace Components;

using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Godot;
using SharedBase.Archive;
using Systems;

/// <summary>
///   AI for a single Microbe (enables the <see cref="MicrobeAISystem"/>. to run on this). And also the memory for
///   the AI.
/// </summary>
public struct MicrobeAI : IArchivableComponent
{
    public const ushort SERIALIZATION_VERSION = 2;

    public float TimeUntilNextThink;

    public float PreviousAngle;

    public Vector3 TargetPosition;

    public Entity FocusedPrey;

    public Vector3? LastSmelledCompoundPosition;

    public float PursuitThreshold;

    /// <summary>
    ///   A value between 0.0f and 1.0f, this is the portion of the microbe's atp bar that needs to refill
    ///   before resuming motion.
    /// </summary>
    public float ATPThreshold;

    /// <summary>
    ///   Stores the value of microbe.totalAbsorbedCompound at tick t-1 before it is cleared and updated at tick t.
    ///   Used for compounds gradient computation.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Memory of the previous absorption step is required to compute the gradient (which is a variation).
    ///     Values dictionary rather than a single value as they will be combined with variable weights.
    ///   </para>
    /// </remarks>
    public Dictionary<Compound, float>? PreviouslyAbsorbedCompounds;

    public Dictionary<Compound, float>? CompoundsSearchWeights;

    public bool HasBeenNearPlayer;

    /// <summary>
    ///   Whether this microbe is currently fleeing
    /// </summary>
    public bool Fleeing;

    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;
    public ThriveArchiveObjectType ArchiveObjectType => ThriveArchiveObjectType.ComponentMicrobeAI;

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(TimeUntilNextThink);
        writer.Write(PreviousAngle);
        writer.Write(TargetPosition);
        writer.WriteAnyRegisteredValueAsObject(FocusedPrey);

        writer.Write(PursuitThreshold);
        writer.Write(ATPThreshold);
        writer.Write(HasBeenNearPlayer);
        writer.Write(Fleeing);

        writer.Write(LastSmelledCompoundPosition.HasValue);
        if (LastSmelledCompoundPosition.HasValue)
            writer.Write(LastSmelledCompoundPosition.Value);

        if (PreviouslyAbsorbedCompounds == null)
        {
            writer.WriteNullObject();
        }
        else
        {
            writer.WriteObject(PreviouslyAbsorbedCompounds);
        }
    }
}

public static class MicrobeAIHelpers
{
    public static MicrobeAI ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > MicrobeAI.SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, MicrobeAI.SERIALIZATION_VERSION);

        var instance = new MicrobeAI
        {
            TimeUntilNextThink = reader.ReadFloat(),
            PreviousAngle = reader.ReadFloat(),
            TargetPosition = reader.ReadVector3(),
            FocusedPrey = reader.ReadObject<Entity>(),
            PursuitThreshold = reader.ReadFloat(),
            ATPThreshold = reader.ReadFloat(),
            HasBeenNearPlayer = reader.ReadBool(),
            Fleeing = version > 1 && reader.ReadBool(),
        };

        if (reader.ReadBool())
        {
            instance.LastSmelledCompoundPosition = reader.ReadVector3();
        }
        else
        {
            instance.LastSmelledCompoundPosition = null;
        }

        instance.PreviouslyAbsorbedCompounds = reader.ReadObjectOrNull<Dictionary<Compound, float>>();

        return instance;
    }

    /// <summary>
    ///   Resets AI status when this AI-controlled microbe is removed from a colony
    /// </summary>
    public static void ResetAI(this ref MicrobeAI ai, in Entity entity)
    {
        ai.PreviousAngle = 0;
        ai.TargetPosition = Vector3.Zero;
        ai.FocusedPrey = Entity.Null;
        ai.PursuitThreshold = 0;

        ref var absorber = ref entity.Get<CompoundAbsorber>();
        absorber.TotalAbsorbedCompounds?.Clear();
    }

    public static void MoveToLocation(this ref MicrobeAI ai, Vector3 targetPosition, ref MicrobeControl control,
        in Entity entity)
    {
        control.SetStateColonyAware(entity, MicrobeState.Normal);
        ai.TargetPosition = targetPosition;
        control.LookAtPoint = ai.TargetPosition;
        control.SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
    }

    public static void MoveWithRandomTurn(this ref MicrobeAI ai, float minTurn, float maxTurn,
        Vector3 currentPosition, ref MicrobeControl control, float speciesActivity, Random random)
    {
        var turn = random.Next(minTurn, maxTurn);
        if (random.Next(2) == 1)
        {
            turn = -turn;
        }

        var randDist = random.Next(speciesActivity, Constants.MAX_SPECIES_ACTIVITY);
        ai.TargetPosition = currentPosition
            + new Vector3(MathF.Cos(ai.PreviousAngle + turn) * randDist,
                0,
                MathF.Sin(ai.PreviousAngle + turn) * randDist);
        ai.PreviousAngle += turn;
        control.LookAtPoint = ai.TargetPosition;
        control.SetMoveSpeed(Constants.AI_BASE_MOVEMENT);
    }

    public static void LowerPursuitThreshold(this ref MicrobeAI ai)
    {
        ai.PursuitThreshold *= 0.95f;
    }
}
