using System;
using Godot;
using SharedBase.Archive;

public class NewMicrobeActionData : EditorCombinableActionData<CellType>
{
    public const ushort SERIALIZATION_VERSION = 1;

    public OrganelleLayout<OrganelleTemplate> OldEditedMicrobeOrganelles;
    public MembraneType OldMembrane;
    public float OldMembraneRigidity;

    /// <summary>
    ///   Old behaviour values to restore. Doesn't exist in the multicellular editor as in that the cell editor doesn't
    ///   handle behaviour.
    /// </summary>
    public BehaviourDictionary? OldBehaviourValues;

    public EnvironmentalTolerances? OldTolerances;

    public Color OldMembraneColour;

    public NewMicrobeActionData(OrganelleLayout<OrganelleTemplate> oldEditedMicrobeOrganelles,
        MembraneType oldMembrane, float oldRigidity, Color oldColour, BehaviourDictionary? oldBehaviourValues,
        EnvironmentalTolerances? oldTolerances)
    {
        OldEditedMicrobeOrganelles = oldEditedMicrobeOrganelles;
        OldMembrane = oldMembrane;
        OldMembraneRigidity = oldRigidity;

        if (oldBehaviourValues != null)
            OldBehaviourValues = new BehaviourDictionary(oldBehaviourValues);

        if (oldTolerances != null)
        {
            OldTolerances = new EnvironmentalTolerances();
            OldTolerances.CopyFrom(oldTolerances);
        }

        OldMembraneColour = oldColour;
    }

    public override bool ResetsHistory => true;

    public override ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    public override ArchiveObjectType ArchiveObjectType =>
        (ArchiveObjectType)ThriveArchiveObjectType.NewMicrobeActionData;

    public static void WriteToArchive(ISArchiveWriter writer, ArchiveObjectType type, object obj)
    {
        if (type != (ArchiveObjectType)ThriveArchiveObjectType.NewMicrobeActionData)
            throw new NotSupportedException();

        writer.WriteObject((NewMicrobeActionData)obj);
    }

    public static NewMicrobeActionData ReadFromArchive(ISArchiveReader reader, ushort version, int referenceId)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        var instance = new NewMicrobeActionData(reader.ReadObject<OrganelleLayout<OrganelleTemplate>>(),
            reader.ReadObject<MembraneType>(), reader.ReadFloat(), reader.ReadColor(),
            reader.ReadObjectOrNull<BehaviourDictionary>(), null);

        if (reader.ReadBool())
        {
            instance.OldTolerances = new EnvironmentalTolerances();
            reader.ReadObjectProperties(instance.OldTolerances);
        }

        instance.ReadBasePropertiesFromArchive(reader, reader.ReadUInt16());

        return instance;
    }

    public override void WriteToArchive(ISArchiveWriter writer)
    {
        writer.WriteObject(OldEditedMicrobeOrganelles);
        writer.WriteObject(OldMembrane);
        writer.Write(OldMembraneRigidity);
        writer.Write(OldMembraneColour);
        writer.WriteObjectOrNull(OldBehaviourValues);

        writer.Write(OldTolerances != null);

        if (OldTolerances != null)
            writer.WriteObjectProperties(OldTolerances);

        writer.Write(SERIALIZATION_VERSION_CONTEXT);
        base.WriteToArchive(writer);
    }

    protected override bool CanMergeWithInternal(CombinableActionData other)
    {
        return false;
    }
}
