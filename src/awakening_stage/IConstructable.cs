/// <summary>
///   Something that can be constructed by a creature
/// </summary>
public interface IConstructable : IAcceptsResourceDeposit
{
    /// <summary>
    ///   True when this is completed and no longer needs to be built
    /// </summary>
    public bool Completed { get; }

    public bool HasRequiredResourcesToConstruct { get; }

    /// <summary>
    ///   Time in seconds how long this needs to be constructed before reporting success
    /// </summary>
    public float ConstructionDuration { get; }

    /// <summary>
    ///   Used to report the progress before the construction is finished to allow playing construction animations
    /// </summary>
    /// <param name="progress">The current progress in range 0-1</param>
    public void ReportConstructionActionProgress(float progress);

    /// <summary>
    ///   Called when a creature has spent enough time (<see cref="ConstructionDuration"/>) acting on this entity for
    ///   it to now be considered fully constructed
    /// </summary>
    public void OnFinishConstruction();
}
