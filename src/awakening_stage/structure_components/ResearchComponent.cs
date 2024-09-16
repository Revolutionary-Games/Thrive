﻿using Newtonsoft.Json;
using ThriveScriptsShared;

/// <summary>
///   Provides research for a society
/// </summary>
public class ResearchComponent : StructureComponent
{
    private readonly float speed;
    private readonly ResearchLevel researchLevel;

    private float timeUntilTick = Constants.SOCIETY_STAGE_RESEARCH_PROGRESS_INTERVAL;

    public ResearchComponent(PlacedStructure owningStructure, float speed, ResearchLevel researchLevel) : base(
        owningStructure)
    {
        this.speed = speed;
        this.researchLevel = researchLevel;
    }

    public static void HandleResearchProgressAdding(float delta, object researchSource, float speed,
        ResearchLevel researchLevel, ISocietyStructureDataAccess dataAccess)
    {
        var technologyProgress = dataAccess.CurrentlyResearchedTechnology;

        bool canResearch = technologyProgress != null;

        // Skip if this building can't contribute to this research
        if (technologyProgress != null)
        {
            if (technologyProgress.Technology.RequiresResearchLevel > researchLevel)
                canResearch = false;

            if (technologyProgress.Completed)
                canResearch = false;
        }

        if (canResearch)
        {
            var researchPoints = speed * delta;
            technologyProgress!.AddProgress(researchPoints);
            dataAccess.AddActiveResearchContribution(researchSource, researchPoints);
        }
        else
        {
            dataAccess.RemoveActiveResearchContribution(researchSource);
        }
    }

    public override void ProcessSociety(float delta, ISocietyStructureDataAccess dataAccess)
    {
        timeUntilTick -= delta;

        if (timeUntilTick > 0)
            return;

        timeUntilTick += Constants.SOCIETY_STAGE_RESEARCH_PROGRESS_INTERVAL;

        HandleResearchProgressAdding(Constants.SOCIETY_STAGE_RESEARCH_PROGRESS_INTERVAL, this, speed, researchLevel,
            dataAccess);
    }
}

public class ResearchComponentFactory : IStructureComponentFactory
{
    /// <summary>
    ///   Speed multiplier for the research
    /// </summary>
    [JsonProperty]
    public float Speed { get; private set; } = 1;

    /// <summary>
    ///   The level of sophistication this research facility provides
    /// </summary>
    [JsonProperty]
    public ResearchLevel Level { get; private set; }

    public StructureComponent Create(PlacedStructure owningStructure)
    {
        return new ResearchComponent(owningStructure, Speed, Level);
    }

    public void Check(string name)
    {
        if (Speed <= 0)
            throw new InvalidRegistryDataException(name, GetType().Name, "Research component speed must be > 0.0f");

        if (Level == ResearchLevel.PreSociety)
        {
            throw new InvalidRegistryDataException(name, GetType().Name,
                "Research component can't have pre-society research level");
        }
    }
}
