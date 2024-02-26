/// <summary>
///   Accepts user input regarding the tutorial
/// </summary>
public interface ITutorialInput
{
    public void OnTutorialDisabled();
    public void OnTutorialEnabled();

    /// <summary>
    ///   Player closed the current tutorial
    /// </summary>
    /// <param name="name">
    ///   This has the name of the variable controlling the specific tutorial, is set when there can be multiple
    ///   tutorials open at once and only one needs to be closed
    /// </param>
    public void OnCurrentTutorialClosed(string name);

    /// <summary>
    ///   Close all open tutorials
    /// </summary>
    public void OnTutorialClosed();

    public void OnNextPressed();
    public void Process(ITutorialGUI tutorialGUI, double delta);
}
