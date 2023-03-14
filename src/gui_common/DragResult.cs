public enum DragResult
{
    Success,
    Failure,

    /// <summary>
    ///   Special callback already handled the drag, default action is blocked
    /// </summary>
    AlreadyHandled,
}
