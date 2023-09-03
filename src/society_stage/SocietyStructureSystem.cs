﻿using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Handles structure update logic in the society stage
/// </summary>
public class SocietyStructureSystem
{
    private readonly List<PlacedStructure> thisFrameCompletedStructures = new();

    [JsonProperty]
    private readonly Dictionary<PlacedStructure, StructureProgressData> structureCompletionTimesRemaining = new();

    private readonly Node worldRoot;

    [JsonProperty]
    private float elapsed = 1;

    public SocietyStructureSystem(Node root)
    {
        worldRoot = root;
    }

    /// <summary>
    ///   The total amount of storage in all structures. Updated only once <see cref="Process"/> has been called
    /// </summary>
    [JsonIgnore]
    public float CachedTotalStorage { get; private set; }

    [JsonProperty]
    public long CachedHousingCapacity { get; private set; }

    [JsonProperty]
    public float CachedFactoryPower { get; private set; }

    public void Process(float delta, ISocietyStructureDataAccess societyData)
    {
        elapsed += delta;

        // Update completion times each frame for smooth animation
        UpdatedStructureCompletionProgress(delta);

        if (elapsed < Constants.SOCIETY_STAGE_BUILDING_PROCESS_INTERVAL)
            return;

        float storage = 0;
        long housing = 0;
        float factory = 0;

        foreach (var structure in worldRoot.GetChildrenToProcess<PlacedStructure>(Constants.STRUCTURE_ENTITY_GROUP))
        {
            if (!structure.Completed)
            {
                if (structureCompletionTimesRemaining.ContainsKey(structure))
                    continue;

                // Start completing a structure we have resources for
                if (structure.DepositBulkResources(societyData.SocietyResources))
                {
                    structureCompletionTimesRemaining[structure] = new StructureProgressData(structure);
                }

                continue;
            }

            // The following is pretty quick prototype code, there's probably better way to implement this
            var storageComponent = structure.GetComponent<StructureStorageComponent>();
            if (storageComponent != null)
                storage += storageComponent.Capacity;

            var housingComponent = structure.GetComponent<HousingComponent>();
            if (housingComponent != null)
                housing += housingComponent.Space;

            if (structure.GetComponent<FactoryComponent>() != null)
                factory += 1;

            structure.ProcessSociety(elapsed, societyData);
        }

        elapsed = 0;
        CachedTotalStorage = storage;
        CachedHousingCapacity = housing;
        CachedFactoryPower = factory;
    }

    /// <summary>
    ///   Immediately calculates total storage
    /// </summary>
    /// <returns>The total storage</returns>
    public float CalculateTotalStorage()
    {
        float storage = 0;

        foreach (var structure in worldRoot.GetChildrenToProcess<PlacedStructure>(Constants.STRUCTURE_ENTITY_GROUP))
        {
            if (!structure.Completed)
            {
                continue;
            }

            var storageComponent = structure.GetComponent<StructureStorageComponent>();
            if (storageComponent != null)
                storage += storageComponent.Capacity;
        }

        CachedTotalStorage = storage;
        return storage;
    }

    private void UpdatedStructureCompletionProgress(float delta)
    {
        thisFrameCompletedStructures.Clear();

        foreach (var structureProgressData in structureCompletionTimesRemaining)
        {
            if (structureProgressData.Value.ElapseTimeAndCompleteWhenReady(delta))
                thisFrameCompletedStructures.Add(structureProgressData.Key);
        }

        foreach (var completedStructure in thisFrameCompletedStructures)
        {
            structureCompletionTimesRemaining.Remove(completedStructure);
        }
    }

    private class StructureProgressData
    {
        private readonly PlacedStructure structure;
        private readonly float totalTime;

        private float elapsed;

        public StructureProgressData(PlacedStructure structure)
        {
            this.structure = structure;
            totalTime = structure.TimedActionDuration;
        }

        public bool ElapseTimeAndCompleteWhenReady(float delta)
        {
            elapsed += delta;

            structure.ReportActionProgress(elapsed / totalTime);

            if (elapsed > totalTime)
            {
                structure.OnFinishTimeTakingAction();
                return true;
            }

            return false;
        }
    }
}
