using Godot;

/// <summary>
///   An object that can receive and consume input events
/// </summary>
public interface IInputReceiver
{
    /// <summary>
    ///   Checks the input inputEvent if it is for us and acts on it
    /// </summary>
    /// <param name="inputEvent">Event to check</param>
    /// <returns>True if the event was consumed</returns>
    bool CheckInput(InputEvent inputEvent);

    /// <summary>
    ///   Gets the current input value
    /// </summary>
    /// <returns>The current input value</returns>
    object GetValueForCallback();

    /// <summary>
    ///   Gets whether the input is not in its non-activated state like a key being pressed
    /// </summary>
    /// <returns>True if the input is triggered</returns>
    bool ShouldTriggerCallbacks();

    /// <summary>
    ///   Window focus is lost, all held keys should be released
    /// </summary>
    void FocusLost();
}
