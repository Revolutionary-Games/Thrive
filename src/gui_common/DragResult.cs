public enum DragResult
{
    Success,
    Failure,

    /// <summary>
    ///   The dropped item is accepted but the target's item should start a forced drag
    /// </summary>
    Replaced,
}
