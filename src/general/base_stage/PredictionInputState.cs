/// <summary>
///   Whether the current editor inputs can be used for an Auto-Evo prediction.
/// </summary>
public enum PredictionInputState
{
    Valid,

    Invalid,

    /// <summary>
    ///   An editing action is in progress or the input provider has not been prepared yet.
    /// </summary>
    NotReady,
}
