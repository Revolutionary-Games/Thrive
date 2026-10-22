using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Stores stats needed to track all achievement states
/// </summary>
public class AchievementStatStore : IAchievementStatStore
{
    private int statMicrobeKills;
    private int statEditorUsages;
    private int statCellColoniesFormed;
    private int statSurvivedWithNucleus;
    private int statPositiveGlucosePhotosynthesis;
    private int statNoChangesInEditor;
    private int statEngulfCount;
    private int statCellEatsRadiation;
    private int statCellUsesChemosynthesis;
    private int statMaxSpeciesGeneration;
    private int statEndosymbiosisCompleted;
    private int statReachedMulticellular;
    private int statReachedMacroscopic;
    private int statUltimateColony;
    private int statUltimateCellularFirepower;
    private int statYouAnimal;
    private int statSpecializedWeapons;
    private int statSpecializationSpecialist;
    private int statJustAFleshWound;
    private int statInLookOfAHome;
    private int statMicroscopicBirdsAndBees;
    private int statPerfectlyPacked;

    public int GetIntStat(int statId)
    {
        switch (statId)
        {
            case IAchievementStatStore.STAT_MICROBE_KILLS:
                return statMicrobeKills;
            case IAchievementStatStore.STAT_EDITOR_USAGE:
                return statEditorUsages;
            case IAchievementStatStore.STAT_CELL_COLONY_FORMED:
                return statCellColoniesFormed;
            case IAchievementStatStore.STAT_SURVIVED_WITH_NUCLEUS:
                return statSurvivedWithNucleus;
            case IAchievementStatStore.STAT_POSITIVE_GLUCOSE_PHOTOSYNTHESIS:
                return statPositiveGlucosePhotosynthesis;
            case IAchievementStatStore.STAT_NO_CHANGES_IN_EDITOR:
                return statNoChangesInEditor;
            case IAchievementStatStore.STAT_ENGULFMENT_COUNT:
                return statEngulfCount;
            case IAchievementStatStore.STAT_CELL_EATS_RADIATION:
                return statCellEatsRadiation;
            case IAchievementStatStore.STAT_CELL_USES_CHEMOSYNTHESIS:
                return statCellUsesChemosynthesis;
            case IAchievementStatStore.STAT_MAX_SPECIES_GENERATION:
                return statMaxSpeciesGeneration;
            case IAchievementStatStore.STAT_ENDOSYMBIOSIS_COMPLETED:
                return statEndosymbiosisCompleted;
            case IAchievementStatStore.STAT_REACHED_MULTICELLULAR:
                return statReachedMulticellular;
            case IAchievementStatStore.STAT_REACHED_MACROSCOPIC:
                return statReachedMacroscopic;
            case IAchievementStatStore.STAT_ULTIMATE_COLONY:
                return statUltimateColony;
            case IAchievementStatStore.STAT_ULTIMATE_CELLULAR_FIREPOWER:
                return statUltimateCellularFirepower;
            case IAchievementStatStore.STAT_YOU_ANIMAL:
                return statYouAnimal;
            case IAchievementStatStore.STAT_SPECIALIZED_WEAPONS:
                return statSpecializedWeapons;
            case IAchievementStatStore.STAT_SPECIALIZATION_SPECIALIST:
                return statSpecializationSpecialist;
            case IAchievementStatStore.STAT_JUST_A_FLESH_WOUND:
                return statJustAFleshWound;
            case IAchievementStatStore.STAT_IN_LOOK_OF_A_HOME:
                return statInLookOfAHome;
            case IAchievementStatStore.STAT_MICROSCOPIC_BIRDS_AND_BEES:
                return statMicroscopicBirdsAndBees;
            case IAchievementStatStore.STAT_PERFECTLY_PACKED:
                return statPerfectlyPacked;
        }

        GD.PrintErr("Unknown stat ID requested: ", statId);
        return 0;
    }

    public int IncrementIntStat(int statId)
    {
        switch (statId)
        {
            case IAchievementStatStore.STAT_MICROBE_KILLS:
                ++statMicrobeKills;
                return statMicrobeKills;
            case IAchievementStatStore.STAT_EDITOR_USAGE:
                ++statEditorUsages;
                return statEditorUsages;
            case IAchievementStatStore.STAT_CELL_COLONY_FORMED:
                ++statCellColoniesFormed;
                return statCellColoniesFormed;
            case IAchievementStatStore.STAT_SURVIVED_WITH_NUCLEUS:
                ++statSurvivedWithNucleus;
                return statSurvivedWithNucleus;
            case IAchievementStatStore.STAT_NO_CHANGES_IN_EDITOR:
                ++statNoChangesInEditor;
                return statNoChangesInEditor;
            case IAchievementStatStore.STAT_ENGULFMENT_COUNT:
                ++statEngulfCount;
                return statEngulfCount;
            case IAchievementStatStore.STAT_ENDOSYMBIOSIS_COMPLETED:
                ++statEndosymbiosisCompleted;
                return statEndosymbiosisCompleted;

            // Stats that may not be incremented
            case IAchievementStatStore.STAT_POSITIVE_GLUCOSE_PHOTOSYNTHESIS:
            case IAchievementStatStore.STAT_CELL_EATS_RADIATION:
            case IAchievementStatStore.STAT_CELL_USES_CHEMOSYNTHESIS:
            case IAchievementStatStore.STAT_MAX_SPECIES_GENERATION:
            case IAchievementStatStore.STAT_REACHED_MULTICELLULAR:
            case IAchievementStatStore.STAT_REACHED_MACROSCOPIC:
            case IAchievementStatStore.STAT_ULTIMATE_COLONY:
            case IAchievementStatStore.STAT_ULTIMATE_CELLULAR_FIREPOWER:
            case IAchievementStatStore.STAT_YOU_ANIMAL:
            case IAchievementStatStore.STAT_SPECIALIZED_WEAPONS:
            case IAchievementStatStore.STAT_SPECIALIZATION_SPECIALIST:
            case IAchievementStatStore.STAT_JUST_A_FLESH_WOUND:
            case IAchievementStatStore.STAT_IN_LOOK_OF_A_HOME:
            case IAchievementStatStore.STAT_MICROSCOPIC_BIRDS_AND_BEES:
            case IAchievementStatStore.STAT_PERFECTLY_PACKED:
            {
                GD.PrintErr("Cannot increment stat of type: ", statId);
                return 0;
            }
        }

        GD.PrintErr("Unknown stat ID tried to be incremented: ", statId);
        return 0;
    }

    public bool SetIntStat(int statId, int value)
    {
        switch (statId)
        {
            case IAchievementStatStore.STAT_POSITIVE_GLUCOSE_PHOTOSYNTHESIS:
                statPositiveGlucosePhotosynthesis = Math.Clamp(value, 0, 1);
                return true;
            case IAchievementStatStore.STAT_CELL_EATS_RADIATION:
                statCellEatsRadiation = Math.Clamp(value, 0, 1);
                return true;
            case IAchievementStatStore.STAT_CELL_USES_CHEMOSYNTHESIS:
                statCellUsesChemosynthesis = Math.Clamp(value, 0, 1);
                return true;
            case IAchievementStatStore.STAT_MAX_SPECIES_GENERATION:
                statMaxSpeciesGeneration = Math.Clamp(value, 0, int.MaxValue);
                return true;
            case IAchievementStatStore.STAT_REACHED_MULTICELLULAR:
                statReachedMulticellular = Math.Clamp(value, 0, 1);
                return true;
            case IAchievementStatStore.STAT_REACHED_MACROSCOPIC:
                statReachedMacroscopic = Math.Clamp(value, 0, 1);
                return true;
            case IAchievementStatStore.STAT_ULTIMATE_COLONY:
                statUltimateColony = Math.Clamp(value, 0, int.MaxValue);
                return true;
            case IAchievementStatStore.STAT_ULTIMATE_CELLULAR_FIREPOWER:
                statUltimateCellularFirepower = Math.Clamp(value, 0, int.MaxValue);
                return true;
            case IAchievementStatStore.STAT_YOU_ANIMAL:
                statYouAnimal = Math.Clamp(value, 0, 1);
                return true;
            case IAchievementStatStore.STAT_SPECIALIZED_WEAPONS:
                statSpecializedWeapons = Math.Clamp(value, 0, 1);
                return true;
            case IAchievementStatStore.STAT_SPECIALIZATION_SPECIALIST:
                statSpecializationSpecialist = Math.Clamp(value, 0, int.MaxValue);
                return true;
            case IAchievementStatStore.STAT_JUST_A_FLESH_WOUND:
                statJustAFleshWound = Math.Clamp(value, 0, 1);
                return true;
            case IAchievementStatStore.STAT_IN_LOOK_OF_A_HOME:
                statInLookOfAHome = Math.Clamp(value, 0, int.MaxValue);
                return true;
            case IAchievementStatStore.STAT_MICROSCOPIC_BIRDS_AND_BEES:
                statMicroscopicBirdsAndBees = Math.Clamp(value, 0, 1);
                return true;
            case IAchievementStatStore.STAT_PERFECTLY_PACKED:
                statPerfectlyPacked = Math.Clamp(value, 0, 1);
                return true;
        }

        GD.PrintErr("Unknown stat ID tried to be set (or stat that cannot be set directly): ", statId);
        return false;
    }

    /// <summary>
    ///   Resets ALL stats to initial values, losing all progress towards achievements.
    /// </summary>
    public void Reset()
    {
        GD.Print("Resetting tracked stats");

        statMicrobeKills = 0;
        statEditorUsages = 0;
        statCellColoniesFormed = 0;
        statSurvivedWithNucleus = 0;
        statPositiveGlucosePhotosynthesis = 0;
        statNoChangesInEditor = 0;
        statEngulfCount = 0;
        statCellEatsRadiation = 0;
        statCellUsesChemosynthesis = 0;
        statMaxSpeciesGeneration = 0;
        statEndosymbiosisCompleted = 0;
        statReachedMulticellular = 0;
        statReachedMacroscopic = 0;
        statUltimateColony = 0;
        statUltimateCellularFirepower = 0;
        statYouAnimal = 0;
        statSpecializedWeapons = 0;
        statSpecializationSpecialist = 0;
        statJustAFleshWound = 0;
        statInLookOfAHome = 0;
        statMicroscopicBirdsAndBees = 0;
        statPerfectlyPacked = 0;
    }

    public void Save(Dictionary<int, int> intValues)
    {
        intValues[IAchievementStatStore.STAT_MICROBE_KILLS] = statMicrobeKills;
        intValues[IAchievementStatStore.STAT_EDITOR_USAGE] = statEditorUsages;
        intValues[IAchievementStatStore.STAT_CELL_COLONY_FORMED] = statCellColoniesFormed;
        intValues[IAchievementStatStore.STAT_SURVIVED_WITH_NUCLEUS] = statSurvivedWithNucleus;
        intValues[IAchievementStatStore.STAT_POSITIVE_GLUCOSE_PHOTOSYNTHESIS] = statPositiveGlucosePhotosynthesis;
        intValues[IAchievementStatStore.STAT_NO_CHANGES_IN_EDITOR] = statNoChangesInEditor;
        intValues[IAchievementStatStore.STAT_ENGULFMENT_COUNT] = statEngulfCount;
        intValues[IAchievementStatStore.STAT_CELL_EATS_RADIATION] = statCellEatsRadiation;
        intValues[IAchievementStatStore.STAT_CELL_USES_CHEMOSYNTHESIS] = statCellUsesChemosynthesis;
        intValues[IAchievementStatStore.STAT_MAX_SPECIES_GENERATION] = statMaxSpeciesGeneration;
        intValues[IAchievementStatStore.STAT_ENDOSYMBIOSIS_COMPLETED] = statEndosymbiosisCompleted;
        intValues[IAchievementStatStore.STAT_REACHED_MULTICELLULAR] = statReachedMulticellular;
        intValues[IAchievementStatStore.STAT_REACHED_MACROSCOPIC] = statReachedMacroscopic;
        intValues[IAchievementStatStore.STAT_ULTIMATE_COLONY] = statUltimateColony;
        intValues[IAchievementStatStore.STAT_ULTIMATE_CELLULAR_FIREPOWER] = statUltimateCellularFirepower;
        intValues[IAchievementStatStore.STAT_YOU_ANIMAL] = statYouAnimal;
        intValues[IAchievementStatStore.STAT_SPECIALIZED_WEAPONS] = statSpecializedWeapons;
        intValues[IAchievementStatStore.STAT_SPECIALIZATION_SPECIALIST] = statSpecializationSpecialist;
        intValues[IAchievementStatStore.STAT_JUST_A_FLESH_WOUND] = statJustAFleshWound;
        intValues[IAchievementStatStore.STAT_IN_LOOK_OF_A_HOME] = statInLookOfAHome;
        intValues[IAchievementStatStore.STAT_MICROSCOPIC_BIRDS_AND_BEES] = statMicroscopicBirdsAndBees;
        intValues[IAchievementStatStore.STAT_PERFECTLY_PACKED] = statPerfectlyPacked;
    }

    public void Load(Dictionary<int, int> intValues)
    {
        if (intValues.TryGetValue(IAchievementStatStore.STAT_MICROBE_KILLS, out var value))
            statMicrobeKills = value;

        if (intValues.TryGetValue(IAchievementStatStore.STAT_EDITOR_USAGE, out value))
            statEditorUsages = value;

        if (intValues.TryGetValue(IAchievementStatStore.STAT_CELL_COLONY_FORMED, out value))
            statCellColoniesFormed = value;

        if (intValues.TryGetValue(IAchievementStatStore.STAT_SURVIVED_WITH_NUCLEUS, out value))
            statSurvivedWithNucleus = value;

        if (intValues.TryGetValue(IAchievementStatStore.STAT_POSITIVE_GLUCOSE_PHOTOSYNTHESIS, out value))
            statPositiveGlucosePhotosynthesis = value;

        if (intValues.TryGetValue(IAchievementStatStore.STAT_NO_CHANGES_IN_EDITOR, out value))
            statNoChangesInEditor = value;

        if (intValues.TryGetValue(IAchievementStatStore.STAT_ENGULFMENT_COUNT, out value))
            statEngulfCount = value;

        if (intValues.TryGetValue(IAchievementStatStore.STAT_CELL_EATS_RADIATION, out value))
            statCellEatsRadiation = value;

        if (intValues.TryGetValue(IAchievementStatStore.STAT_CELL_USES_CHEMOSYNTHESIS, out value))
            statCellUsesChemosynthesis = value;

        if (intValues.TryGetValue(IAchievementStatStore.STAT_MAX_SPECIES_GENERATION, out value))
            statMaxSpeciesGeneration = value;

        if (intValues.TryGetValue(IAchievementStatStore.STAT_ENDOSYMBIOSIS_COMPLETED, out value))
            statEndosymbiosisCompleted = value;

        if (intValues.TryGetValue(IAchievementStatStore.STAT_REACHED_MULTICELLULAR, out value))
            statReachedMulticellular = value;

        if (intValues.TryGetValue(IAchievementStatStore.STAT_REACHED_MACROSCOPIC, out value))
            statReachedMacroscopic = value;

        if (intValues.TryGetValue(IAchievementStatStore.STAT_ULTIMATE_COLONY, out value))
            statUltimateColony = value;

        if (intValues.TryGetValue(IAchievementStatStore.STAT_ULTIMATE_CELLULAR_FIREPOWER, out value))
            statUltimateCellularFirepower = value;

        if (intValues.TryGetValue(IAchievementStatStore.STAT_YOU_ANIMAL, out value))
            statYouAnimal = value;

        if (intValues.TryGetValue(IAchievementStatStore.STAT_SPECIALIZED_WEAPONS, out value))
            statSpecializedWeapons = value;

        if (intValues.TryGetValue(IAchievementStatStore.STAT_SPECIALIZATION_SPECIALIST, out value))
            statSpecializationSpecialist = value;

        if (intValues.TryGetValue(IAchievementStatStore.STAT_JUST_A_FLESH_WOUND, out value))
            statJustAFleshWound = value;

        if (intValues.TryGetValue(IAchievementStatStore.STAT_IN_LOOK_OF_A_HOME, out value))
            statInLookOfAHome = value;

        if (intValues.TryGetValue(IAchievementStatStore.STAT_MICROSCOPIC_BIRDS_AND_BEES, out value))
            statMicroscopicBirdsAndBees = value;

        if (intValues.TryGetValue(IAchievementStatStore.STAT_PERFECTLY_PACKED, out value))
            statPerfectlyPacked = value;
    }
}
