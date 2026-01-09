using System;
using Godot;

/// <summary>
///   Names of upgrades for the various ATP processing methods ("Calvin" cycles)
/// </summary>
public static class CalvinUpgradeNames
{
    /// <summary>
    ///   Technically this is "none" but internally upgrades don't save the "none" upgrade in the upgrades list.
    /// </summary>
    public const string NOCALVIN_UPGRADE_NAME = "nocalvin";

    public const string GLUCOSE_UPGRADE_NAME = "glucose";

    public const string STARCH_UPGRADE_NAME = "starch";

    public static CalvinType GetCalvinTypeFromUpgrades(this IReadOnlyOrganelleUpgrades? upgrades, string organelle)
    {
        if (upgrades == null || upgrades.UnlockedFeatures.Count < 1)
            return CalvinType.NoCalvin;

        foreach (var feature in upgrades.UnlockedFeatures)
        {
            if (TryGetCalvinTypeFromName(feature, organelle, out var type))
                return type;
        }

        GD.Print("Calvin type not found.");
        return CalvinType.NoCalvin;
    }

    public static CalvinType CalvinTypeFromName(string name, string organelle)
    {
        if (TryGetCalvinTypeFromName(name, organelle, out var result))
            return result;

        throw new ArgumentException("Name doesn't match any calvin upgrade name, name was " + 
            name + " and organelle was " + organelle);
    }

    public static bool TryGetCalvinTypeFromName(string name, string organelle, out CalvinType type)
    {
        string name2 = name;
        if (name2 == "none")
        {
            OrganelleDefinition? definition = SimulationParameters.Instance.GetOrganelleType(organelle);
            if (definition.DefaultUpgradeName != null)
            {
                name2 = definition.DefaultUpgradeName;
            }
            else
            {
                name2 = "chromatophore";
                GD.Print("Organelle for Calvin cycling not found, assumed thylakoids.");
            }
        }

        switch (name2)
        {
            case GLUCOSE_UPGRADE_NAME:
                type = CalvinType.Glucose;
                return true;
            case STARCH_UPGRADE_NAME:
                // Starch not yet implemented (type = CalvinType.Starch;)
                type = CalvinType.Glucose;
                GD.Print("Starch calvin cycling not yet implemented, Glucose used instead.");
                return true;
            case NOCALVIN_UPGRADE_NAME:
                type = CalvinType.NoCalvin;
                return true;
        }

        type = CalvinType.NoCalvin;
        GD.Print("Calvin type not found.");
        return false;
    }

    public static string CalvinNameFromType(CalvinType calvinType, string organelle)
    {
        OrganelleDefinition definition = SimulationParameters.Instance.GetOrganelleType(organelle);
        string placeholder;
        switch (calvinType)
        {
            case CalvinType.Glucose:
                placeholder = GLUCOSE_UPGRADE_NAME;
                break;
            case CalvinType.NoCalvin:
                placeholder = NOCALVIN_UPGRADE_NAME;
                break;

            // Starch would go here as well, if implemented.
            default:
                throw new ArgumentOutOfRangeException(nameof(calvinType), calvinType, null);
        }

        if (placeholder == definition.DefaultUpgradeName)
        {
            return "none"; // we can't return 'glucose' for a chloroplast if a chloroplast's default upgrade is glucose
        }

        return placeholder;
    }
}
