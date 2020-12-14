﻿using System.Collections.Generic;

/// <summary>
///   This is its own interface to make JSON loading dynamic type more strict
/// </summary>
#pragma warning disable CA1040 // empty interface
public interface IMicrobeEditorActionData
{
}
#pragma warning restore CA1040

[JSONAlwaysDynamicType]
public class PlacementActionData : IMicrobeEditorActionData
{
    public List<OrganelleTemplate> ReplacedCytoplasm;
    public OrganelleTemplate Organelle;

    public PlacementActionData(OrganelleTemplate organelle)
    {
        Organelle = organelle;
    }
}

[JSONAlwaysDynamicType]
public class RemoveActionData : IMicrobeEditorActionData
{
    public OrganelleTemplate Organelle;

    public RemoveActionData(OrganelleTemplate organelle)
    {
        Organelle = organelle;
    }
}

[JSONAlwaysDynamicType]
public class MembraneActionData : IMicrobeEditorActionData
{
    public MembraneType OldMembrane;
    public MembraneType NewMembrane;

    public MembraneActionData(MembraneType oldMembrane, MembraneType newMembrane)
    {
        OldMembrane = oldMembrane;
        NewMembrane = newMembrane;
    }
}

[JSONAlwaysDynamicType]
public class RigidityChangeActionData : IMicrobeEditorActionData
{
    public float NewRigidity;
    public float PreviousRigidity;

    public RigidityChangeActionData(float newRigidity, float previousRigidity)
    {
        NewRigidity = newRigidity;
        PreviousRigidity = previousRigidity;
    }
}

[JSONAlwaysDynamicType]
public class NewMicrobeActionData : IMicrobeEditorActionData
{
    public OrganelleLayout<OrganelleTemplate> OldEditedMicrobeOrganelles;
    public int PreviousMP;
    public MembraneType OldMembrane;

    public NewMicrobeActionData(OrganelleLayout<OrganelleTemplate> oldEditedMicrobeOrganelles, int previousMP,
        MembraneType oldMembrane)
    {
        OldEditedMicrobeOrganelles = oldEditedMicrobeOrganelles;
        PreviousMP = previousMP;
        OldMembrane = oldMembrane;
    }
}
