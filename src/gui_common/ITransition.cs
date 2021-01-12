using Godot;

public interface ITransition
{
    Control ControlNode { get; }

    bool Skippable { get; set; }

    void OnStarted();
    void OnFinished();
}
