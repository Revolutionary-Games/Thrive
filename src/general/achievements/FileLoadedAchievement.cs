using System;
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
    public int LinkedStatistic { get; private set; }

    [JsonProperty]
    public int LinkedStatisticThreshold { get; private set; }

    public bool ProcessPotentialUnlock(AchievementStatStore updatedStats)
    {
        if (LinkedStatistic != 0)
        {
            if (updatedStats.GetIntStat(LinkedStatistic) >= LinkedStatisticThreshold)
            {
                if (!Achieved)
                {
                    Achieved = true;
                    return true;
                }
            }

            return false;
        }

        // This does not know what the stat to track should be
        return false;
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
            if (!AchievementStatStore.IsValidStatistic(LinkedStatistic))
            {
                throw new Exception(
                    $"Linked statistic {LinkedStatistic} for achievement {internalName} is not a valid statistic");
            }
        }
    }
}
