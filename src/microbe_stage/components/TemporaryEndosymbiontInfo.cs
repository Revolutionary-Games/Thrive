namespace Components;

using System.Collections.Generic;

/// <summary>
///   Holds data related to organelles that have been added as temporary endosymbionts
/// </summary>
public struct TemporaryEndosymbiontInfo
{
    public List<Species>? EndosymbiontSpeciesPresent;

    public List<Species>? CreatedOrganelleInstancesFor;

    public bool Applied;
}

public static class TemporaryEndosymbiontInfoHelpers
{
    public static void AddSpeciesEndosymbiont(this ref TemporaryEndosymbiontInfo info, Species species)
    {
        info.EndosymbiontSpeciesPresent ??= new List<Species>();
        info.EndosymbiontSpeciesPresent.Add(species);
        info.Applied = false;
    }

    public static void Clear(this ref TemporaryEndosymbiontInfo info)
    {
        info.EndosymbiontSpeciesPresent?.Clear();
        info.CreatedOrganelleInstancesFor?.Clear();
    }
}
