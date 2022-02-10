using System.Collections.Generic;

/// <summary>
///   Efficiency of an organelle in a patch
/// </summary>
public class OrganelleEfficiency
{
    public OrganelleEfficiency(OrganelleDefinition organelle)
    {
        Organelle = organelle;
    }

    public OrganelleDefinition Organelle { get; }

    public List<ProcessSpeedInformation> Processes { get; } = new();
}
