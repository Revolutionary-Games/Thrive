/// <summary>
///   Interface for all screen transitions.
/// </summary>
public interface ITransition
{
    bool Skippable { get; set; }

    bool Visible { get; set; }

    void OnStarted();
    void OnFinished();
}
