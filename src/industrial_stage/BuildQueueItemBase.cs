using System;
using Newtonsoft.Json;

/// <summary>
///   Base class for items that can be in the build queue, not to be confused with the GUI for displaying these:
///   <see cref="BuildQueueItemGUI"/>
/// </summary>
[JSONAlwaysDynamicType]
public abstract class BuildQueueItemBase : IBuildQueueProgressItem
{
    [JsonProperty]
    private float elapsedTime;

    [JsonProperty]
    private float totalTime;

    protected BuildQueueItemBase(string itemName, float totalTime)
    {
        if (totalTime <= 0)
            throw new ArgumentException("total time to build needs to be positive", nameof(totalTime));

        ItemName = itemName;
        this.totalTime = totalTime;
    }

    [JsonProperty]
    public string ItemName { get; }

    [JsonProperty]
    public float Progress { get; private set; }

    public void ElapseTime(float delta)
    {
        if (elapsedTime >= totalTime)
            return;

        elapsedTime += delta;

        if (elapsedTime > totalTime)
        {
            elapsedTime = totalTime;
            Progress = 1;
        }
        else
        {
            Progress = elapsedTime / totalTime;
        }
    }

    /// <summary>
    ///   Determines if this item is done and if so triggers the finish callbacks
    /// </summary>
    /// <returns>True if finished, false if more time is needed</returns>
    public bool CheckAndProcessFinishedStatus()
    {
        if (elapsedTime < totalTime)
            return false;

        Progress = 1;
        OnFinished();
        return true;
    }

    protected abstract void OnFinished();
}

[JSONAlwaysDynamicType]
public class UnitBuildQueueItem : BuildQueueItemBase
{
    [JsonProperty]
    private readonly UnitType unitType;

    [JsonProperty]
    private readonly Action<UnitType> onFinished;

    [JsonConstructor]
    public UnitBuildQueueItem(UnitType unitType, Action<UnitType> onFinished) : base(unitType.Name, unitType.BuildTime)
    {
        this.unitType = unitType;
        this.onFinished = onFinished;
    }

    protected override void OnFinished()
    {
        onFinished.Invoke(unitType);
    }
}
