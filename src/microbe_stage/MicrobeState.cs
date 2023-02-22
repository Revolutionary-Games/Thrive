public enum MicrobeState
{
    /// <summary>
    ///   Not in any special state
    /// </summary>
    Normal,

    /// <summary>
    ///   The microbe is currently in binding mode
    /// </summary>
    Binding,

    /// <summary>
    ///   The microbe is currently in unbinding mode and cannot move
    /// </summary>
    Unbinding,

    /// <summary>
    ///   The microbe is currently in engulf mode
    /// </summary>
    Engulf,
}
