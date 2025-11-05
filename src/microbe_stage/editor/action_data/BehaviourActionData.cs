using System;
using System.Collections.Generic;
using SharedBase.Archive;

public class BehaviourActionData : EditorCombinableActionData
{
    public const ushort SERIALIZATION_VERSION = 1;

    public float NewValue;
    public float OldValue;
    public BehaviouralValueType Type;

    public BehaviourActionData(float newValue, float oldValue, BehaviouralValueType type)
    {
        OldValue = oldValue;
        NewValue = newValue;
        Type = type;
    }

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.BehaviourActionData;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.BehaviourActionData)
            throw new NotSupportedException();

        writer.WriteObject((BehaviourActionData)obj);
    }

    public static BehaviourActionData ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new BehaviourActionData(reader.ReadFloat(), reader.ReadFloat(),
            (BehaviouralValueType)reader.ReadInt32());

        instance.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());

        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(NewValue);
        writer.Write(OldValue);
        writer.Write((int)Type);

        writer.Write(SERIALIZATION_VERSION_EDITOR);
        base.WriteToArchive(writer);
    }

    protected override double CalculateBaseCostInternal()
    {
        // TODO: add a cost for this. CalculateCostInternal needs tweaking to handle this
        return 0;
    }

    protected override (double Cost, double RefundCost) CalculateCostInternal(
        IReadOnlyList<EditorCombinableActionData> history, int insertPosition)
    {
        var cost = CalculateBaseCostInternal();
        double refund = 0;
        bool seenOther = false;

        var count = history.Count;
        for (int i = 0; i < insertPosition && i < count; ++i)
        {
            var other = history[i];

            if (other is BehaviourActionData behaviourChangeActionData && behaviourChangeActionData.Type == Type)
            {
                // If the value has been changed back to a previous value
                if (Math.Abs(NewValue - behaviourChangeActionData.OldValue) < MathUtils.EPSILON &&
                    Math.Abs(behaviourChangeActionData.NewValue - OldValue) < MathUtils.EPSILON)
                {
                    cost = 0;
                    refund += other.GetCalculatedSelfCost();
                    continue;
                }

                // If the value has been changed twice
                if (Math.Abs(NewValue - behaviourChangeActionData.OldValue) < MathUtils.EPSILON ||
                    Math.Abs(behaviourChangeActionData.NewValue - OldValue) < MathUtils.EPSILON)
                {
                    if (!seenOther)
                    {
                        seenOther = true;

                        // TODO: need to calculate real total cost from the other old value to our new value once
                        // there are costs and not just 0
                        // cost = CalculateBehaviourCost(behaviourChangeActionData.OldValue, NewValue);
                        cost = 0;
                    }

                    refund += other.GetCalculatedSelfCost();
                }
            }
        }

        return (cost, refund);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        if (other is not BehaviourActionData otherBehaviour)
            return false;

        // Only combine the same type. Otherwise, terrible bugs happen.
        return otherBehaviour.Type == Type;
    }

    protected override void MergeGuaranteed(CombinableActionData other)
    {
        var behaviourChangeActionData = (BehaviourActionData)other;

        if (Math.Abs(OldValue - behaviourChangeActionData.NewValue) < MathUtils.EPSILON)
        {
            // Handle cancels out
            if (Math.Abs(NewValue - behaviourChangeActionData.OldValue) < MathUtils.EPSILON)
            {
                NewValue = behaviourChangeActionData.NewValue;
                return;
            }

            OldValue = behaviourChangeActionData.OldValue;
            return;
        }

        NewValue = behaviourChangeActionData.NewValue;
    }
}
