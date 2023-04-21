using System;
using Newtonsoft.Json;

/// <summary>
///   Tracks progress towards a technology unlock
/// </summary>
public class TechnologyProgress
{
    private float progress;

    public TechnologyProgress(Technology technology)
    {
        if (technology.ResearchPoints <= 0)
            throw new ArgumentException("Given technology has invalid research points for researching");

        Technology = technology;
    }

    /// <summary>
    ///   The technology that is unlocked once this is done
    /// </summary>
    [JsonProperty]
    public Technology Technology { get; }

    /// <summary>
    ///   The overall progress of the unlock between 0-1
    /// </summary>
    [JsonIgnore]
    public float OverallProgress => progress / Technology.ResearchPoints;

    [JsonProperty]
    public bool Completed { get; private set; }

    public void AddProgress(float researchPoints)
    {
        if (Completed)
            return;

        progress += researchPoints;

        if (progress >= Technology.ResearchPoints)
            OnResearchComplete();
    }

    private void OnResearchComplete()
    {
        progress = Technology.ResearchPoints;
        Completed = true;
    }
}
