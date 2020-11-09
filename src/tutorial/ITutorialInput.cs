/// <summary>
///   Accepts user input regarding the tutorial
/// </summary>
public interface ITutorialInput
{
    void OnTutorialDisabled();
    void OnTutorialEnabled();

    /// <summary>
    ///   Player closed the current tutorial
    /// </summary>
    /// <param name="name">
    ///   This has the name of the variable controlling the specific tutorial, is set when there can be multiple
    ///   tutorials open at once and only one needs to be closed
    /// </param>
    void OnCurrentTutorialClosed(string name);

    /// <summary>
    ///   Close all open tutorials
    /// </summary>
    void OnTutorialClosed();

    void OnNextPressed();
    void Process(ITutorialGUI tutorialGUI, float delta);
}
