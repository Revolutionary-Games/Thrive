/// <summary>
///   Helper class to include in game states to interact with the tutorial system
/// </summary>
public class Tutorial
{
    /// <summary>
    ///   The GUI to show what the tutorial state wants to show
    /// </summary>
    private readonly TutorialGUI gui;

    public Tutorial(TutorialState state, TutorialGUI gui)
    {
        this.gui = gui;
        gui.EventReceiver = state;
        State = state;
    }

    public TutorialState State { get; }

    public void Process(float delta)
    {
        State.Process(gui, delta);
    }
}
