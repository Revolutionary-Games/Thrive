public interface ITimedActionSource
{
    /// <summary>
    ///   Time in seconds how long this needs to be acted on before reporting success
    /// </summary>
    public float TimedActionDuration { get; }

    /// <summary>
    ///   Called when a creature has spent enough time (<see cref="TimedActionDuration"/>) acting on this entity for
    ///   it to now be considered acted on
    /// </summary>
    public void OnFinishTimeTakingAction();
}
