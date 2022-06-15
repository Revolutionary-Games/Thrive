/// <summary>
///   Interface for all screen transitions.
/// </summary>
public interface ITransition
{
    public bool Finished { get; }

    /// <summary>
    ///   Starts this transition and displays it on the screen.
    /// </summary>
    public void Begin();

    /// <summary>
    ///   Clears this transition from the screen.
    /// </summary>
    public void Clear();

    public void Skip();
}
