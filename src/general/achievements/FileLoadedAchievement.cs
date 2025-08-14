using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Achievement that is loaded from the config JSON
/// </summary>
public class FileLoadedAchievement : IAchievement
{
    // These are assigned through JSON
#pragma warning disable 649
    [JsonProperty(nameof(Name))]
    private string? nameRaw;

    [JsonProperty(nameof(Description))]
    private string? descriptionRaw;

    [JsonProperty]
    private string? progressDescription;

    private Texture2D? loadedIcon;
#pragma warning restore 649

    [JsonProperty]
    public int Identifier { get; private set; }

    public string InternalName { get; private set; } = null!;

    [JsonIgnore]
    public LocalizedString Name { get; private set; } = null!;

    [JsonIgnore]
    public LocalizedString Description { get; private set; } = null!;

    [JsonIgnore]
    public bool Achieved { get; private set; }

    [JsonProperty]
    public bool HideIfNotAchieved { get; private set; }

    [JsonProperty]
    public string UnlockedIcon { get; private set; } = null!;

    [JsonProperty]
    public int LinkedStatistic { get; private set; }

    [JsonProperty]
    public int LinkedStatisticThreshold { get; private set; }

    [JsonProperty]
    public IReadOnlyList<int>? LinkedStatisticVisibleProgressThresholds { get; private set; }

    public bool ProcessPotentialUnlock(IAchievementStatStore updatedStats)
    {
        // If already achieved, doesn't need to process
        if (Achieved)
            return false;

        if (LinkedStatistic != 0)
        {
            if (updatedStats.GetIntStat(LinkedStatistic) >= LinkedStatisticThreshold)
            {
                Achieved = true;
                return true;
            }

            return false;
        }

        // This does not know what the stat to track should be
        return false;
    }

    public bool HasAnyProgress(IAchievementStatStore stats)
    {
        if (LinkedStatistic != 0)
        {
            return stats.GetIntStat(LinkedStatistic) > 0;
        }

        // Unknown progress
        return false;
    }

    public string GetProgress(IAchievementStatStore stats)
    {
        if (string.IsNullOrEmpty(progressDescription))
            return Description.ToString();

        if (LinkedStatistic != 0)
        {
            return Localization.Translate(progressDescription)
                .FormatSafe(stats.GetIntStat(LinkedStatistic), LinkedStatisticThreshold);
        }

        // No progress info available
        return Description.ToString();
    }

    public bool GetSteamProgress(IAchievementStatStore stats, out uint current, out uint max)
    {
        if (LinkedStatistic != 0)
        {
            // TODO: should this use bit cast?
            current = (uint)stats.GetIntStat(LinkedStatistic);
            max = (uint)LinkedStatisticThreshold;

            return true;
        }

        GD.PrintErr("No Steam progress info available for achievement (missing linked statistic): ", InternalName);
        current = 0;
        max = 0;
        return false;
    }

    public bool IsAtUnlockMilestone(IAchievementStatStore stats)
    {
        if (LinkedStatistic != 0)
        {
            if (LinkedStatisticVisibleProgressThresholds != null)
            {
                var count = LinkedStatisticVisibleProgressThresholds.Count;
                for (var i = 0; i < count; ++i)
                {
                    var threshold = LinkedStatisticVisibleProgressThresholds[i];
                    if (stats.GetIntStat(LinkedStatistic) == threshold)
                        return true;
                }
            }
        }

        return false;
    }

    public void Reset()
    {
        Achieved = false;
    }

    public Texture2D GetUnlockedIcon()
    {
        return loadedIcon ??= GD.Load<Texture2D>(UnlockedIcon);
    }

    public void OnLoaded(string internalName, bool unlocked)
    {
        if (string.IsNullOrWhiteSpace(internalName))
            throw new ArgumentException("internalName must not be null or whitespace");

        InternalName = internalName;

        if (string.IsNullOrEmpty(nameRaw))
            throw new Exception("Missing name for achievement " + internalName);

        Name = new LocalizedString(nameRaw);

        if (string.IsNullOrEmpty(descriptionRaw))
            throw new Exception("Missing description for achievement " + internalName);

        Description = new LocalizedString(descriptionRaw);

        if (string.IsNullOrEmpty(UnlockedIcon))
            throw new Exception("Missing unlocked icon for achievement " + internalName);

        if (unlocked)
            Achieved = true;

        if (LinkedStatistic != 0)
        {
            if (LinkedStatisticThreshold < 1)
            {
                throw new Exception($"Linked statistic threshold for achievement {internalName} must be at least 1, " +
                    $"but is {LinkedStatisticThreshold}");
            }

            // Verify statistic is correct
            if (!IAchievementStatStore.IsValidStatistic(LinkedStatistic))
            {
                throw new Exception(
                    $"Linked statistic {LinkedStatistic} for achievement {internalName} is not a valid statistic");
            }
        }

        CheckIdentifierMatchesSteam();
    }

    private void CheckIdentifierMatchesSteam()
    {
        if (Identifier == 0)
            throw new Exception("Identifier 0 is not valid for achievement");

        switch (Identifier)
        {
            case 1:
                if (InternalName == IAchievementStatStore.MICROBIAL_MASSACRE_ID &&
                    LinkedStatistic == IAchievementStatStore.STAT_MICROBE_KILLS)
                {
                    return;
                }

                break;

            case 2:
                if (InternalName == IAchievementStatStore.THE_EDITOR_ID &&
                    LinkedStatistic == IAchievementStatStore.STAT_EDITOR_USAGE)
                {
                    return;
                }

                break;

            case 3:
                if (InternalName == IAchievementStatStore.BETTER_TOGETHER_ID &&
                    LinkedStatistic == IAchievementStatStore.STAT_CELL_COLONY_FORMED)
                {
                    return;
                }

                break;

            case 4:
                if (InternalName == IAchievementStatStore.GOING_NUCLEAR_ID &&
                    LinkedStatistic == IAchievementStatStore.STAT_SURVIVED_WITH_NUCLEUS)
                {
                    return;
                }

                break;

            case 5:
                if (InternalName == IAchievementStatStore.TASTE_THE_SUN_ID &&
                    LinkedStatistic == IAchievementStatStore.STAT_POSITIVE_GLUCOSE_PHOTOSYNTHESIS)
                {
                    return;
                }

                break;

            case 6:
                if (InternalName == IAchievementStatStore.CANNOT_IMPROVE_PERFECTION_ID &&
                    LinkedStatistic == IAchievementStatStore.STAT_NO_CHANGES_IN_EDITOR)
                {
                    return;
                }

                break;

            case 7:
                if (InternalName == IAchievementStatStore.YUM_ID &&
                    LinkedStatistic == IAchievementStatStore.STAT_ENGULFMENT_COUNT)
                {
                    return;
                }

                break;

            case 8:
                if (InternalName == IAchievementStatStore.TASTY_RADIATION_ID &&
                    LinkedStatistic == IAchievementStatStore.STAT_CELL_EATS_RADIATION)
                {
                    return;
                }

                break;

            case 9:
                if (InternalName == IAchievementStatStore.VENTS_ARE_HOME_ID &&
                    LinkedStatistic == IAchievementStatStore.STAT_CELL_USES_CHEMOSYNTHESIS)
                {
                    return;
                }

                break;

            case 10:
                if (InternalName == IAchievementStatStore.THRIVING_ID &&
                    LinkedStatistic == IAchievementStatStore.STAT_MAX_SPECIES_GENERATION)
                {
                    return;
                }

                break;

            case 11:
                if (InternalName == IAchievementStatStore.MICRO_BORG_ID &&
                    LinkedStatistic == IAchievementStatStore.STAT_ENDOSYMBIOSIS_COMPLETED)
                {
                    return;
                }

                break;

            case 12:
                if (InternalName == IAchievementStatStore.BEYOND_THE_CELL_ID &&
                    LinkedStatistic == IAchievementStatStore.STAT_REACHED_MULTICELLULAR)
                {
                    return;
                }

                break;
        }

        throw new Exception(
            "Invalid configuration of achievement. The id doesn't match the internal name or statistic");
    }
}
