namespace Components;

using System.Collections.Generic;
using Godot;

/// <summary>
///   Holds data related to organelles that have been added as temporary endosymbionts
/// </summary>
[JSONDynamicTypeAllowed]
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

    /// <summary>
    ///   Apply progress of endosymbiosis to the target species based on the data in the component. Note that this
    ///   doesn't clear the progress, so the organelle layout the progress is from should be cleared, or
    ///   <see cref="Clear"/> called before this method is called again.
    /// </summary>
    /// <param name="info">Where to check what was engulfed to give progress on</param>
    /// <param name="species">Where to put the progress info</param>
    public static void UpdateEndosymbiosisProgress(this ref TemporaryEndosymbiontInfo info, Species species)
    {
        if (info.CreatedOrganelleInstancesFor is not { Count: > 0 })
            return;

        foreach (var progressedSpecies in info.CreatedOrganelleInstancesFor)
        {
            if (!species.Endosymbiosis.ReportEndosymbiosisProgress(progressedSpecies))
                GD.PrintErr("Couldn't update progress on endosymbiosis of: ", progressedSpecies.FormattedIdentifier);
        }
    }
}
