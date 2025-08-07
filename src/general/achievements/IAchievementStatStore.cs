public interface IAchievementStatStore
{
    // This config must match what is in the Steamworks backend
    public const int STAT_MICROBE_KILLS = 1;
    public const string STAT_MICROBE_KILLS_NAME = "microbe_kills";

    public const int STAT_EDITOR_USAGE = 2;
    public const string STAT_EDITOR_USAGE_NAME = "editor_usages";

    public const int STAT_CELL_COLONY_FORMED = 3;
    public const string STAT_CELL_COLONY_FORMED_NAME = "cell_colonies_formed";

    public const int STAT_SURVIVED_WITH_NUCLEUS = 4;
    public const string STAT_SURVIVED_WITH_NUCLEUS_NAME = "survived_with_nucleus";

    public const int STAT_POSITIVE_GLUCOSE_PHOTOSYNTHESIS = 5;
    public const string STAT_POSITIVE_GLUCOSE_PHOTOSYNTHESIS_NAME = "positive_glucose_photosynthesis";

    public const int STAT_NO_CHANGES_IN_EDITOR = 6;
    public const string STAT_NO_CHANGES_IN_EDITOR_NAME = "no_changes_in_editor";

    public const int STAT_ENGULFMENT_COUNT = 7;
    public const string STAT_ENGULFMENT_COUNT_NAME = "engulf_count";

    public const int STAT_CELL_EATS_RADIATION = 8;
    public const string STAT_CELL_EATS_RADIATION_NAME = "cell_eats_radiation";

    public const int STAT_CELL_USES_CHEMOSYNTHESIS = 9;
    public const string STAT_CELL_USES_CHEMOSYNTHESIS_NAME = "cell_uses_chemosynthesis";

    public const int STAT_MAX_SPECIES_GENERATION = 10;
    public const string STAT_MAX_SPECIES_GENERATION_NAME = "max_species_generation";

    public const int STAT_ENDOSYMBIOSIS_COMPLETED = 11;
    public const string STAT_ENDOSYMBIOSIS_COMPLETED_NAME = "endosymbiosis_completed";

    public const int STAT_REACHED_MULTICELLULAR = 12;
    public const string STAT_REACHED_MULTICELLULAR_NAME = "reached_multicellular";

    public const string MICROBIAL_MASSACRE_ID = "MICROBIAL_MASSACRE";
    public const string THE_EDITOR_ID = "THE_EDITOR";
    public const string BETTER_TOGETHER_ID = "BETTER_TOGETHER";
    public const string GOING_NUCLEAR_ID = "GOING_NUCLEAR";
    public const string TASTE_THE_SUN_ID = "TASTE_THE_SUN";
    public const string CANNOT_IMPROVE_PERFECTION_ID = "CANNOT_IMPROVE_PERFECTION";
    public const string YUM_ID = "YUM";
    public const string TASTY_RADIATION_ID = "TASTY_RADIATION";
    public const string VENTS_ARE_HOME_ID = "VENTS_ARE_HOME";
    public const string THRIVING_ID = "THRIVING";
    public const string MICRO_BORG_ID = "MICRO_BORG";
    public const string BEYOND_THE_CELL_ID = "BEYOND_THE_CELL";

    public static bool IsValidStatistic(int statId)
    {
        return GetStatName(statId) != null;
    }

    public static string? GetStatName(int statId)
    {
        switch (statId)
        {
            case STAT_MICROBE_KILLS:
                return STAT_MICROBE_KILLS_NAME;
            case STAT_EDITOR_USAGE:
                return STAT_EDITOR_USAGE_NAME;
            case STAT_CELL_COLONY_FORMED:
                return STAT_CELL_COLONY_FORMED_NAME;
            case STAT_SURVIVED_WITH_NUCLEUS:
                return STAT_SURVIVED_WITH_NUCLEUS_NAME;
            case STAT_POSITIVE_GLUCOSE_PHOTOSYNTHESIS:
                return STAT_POSITIVE_GLUCOSE_PHOTOSYNTHESIS_NAME;
            case STAT_NO_CHANGES_IN_EDITOR:
                return STAT_NO_CHANGES_IN_EDITOR_NAME;
            case STAT_ENGULFMENT_COUNT:
                return STAT_ENGULFMENT_COUNT_NAME;
            case STAT_CELL_EATS_RADIATION:
                return STAT_CELL_EATS_RADIATION_NAME;
            case STAT_CELL_USES_CHEMOSYNTHESIS:
                return STAT_CELL_USES_CHEMOSYNTHESIS_NAME;
            case STAT_MAX_SPECIES_GENERATION:
                return STAT_MAX_SPECIES_GENERATION_NAME;
            case STAT_ENDOSYMBIOSIS_COMPLETED:
                return STAT_ENDOSYMBIOSIS_COMPLETED_NAME;
            case STAT_REACHED_MULTICELLULAR:
                return STAT_REACHED_MULTICELLULAR_NAME;
        }

        return null;
    }

    public int GetIntStat(int statId);
    public int IncrementIntStat(int statId);

    /// <summary>
    ///   Sets a int stat to a specific value.
    /// </summary>
    /// <param name="statId">Stat to modify</param>
    /// <param name="value">Value to set, note that this must follow the limits of the stat</param>
    /// <returns>True if the operation succeeded, false if not allowed</returns>
    public bool SetIntStat(int statId, int value);

    public void Reset();
}
