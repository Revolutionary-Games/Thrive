using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A single tutorial in the game. Tutorials are split into subclasses to make the structure of the code much better.
/// </summary>
public abstract class TutorialPhase
{
    [JsonProperty]
    public bool ShownCurrently { get; protected set; }

    [JsonProperty]
    public bool HasBeenShown { get; protected set; }

    [JsonProperty]
    public bool HandlesEvents { get; protected set; } = true;

    /// <summary>
    ///   True when this tutorial uses an exclusive popup preventing all other GUI from being interacted with
    /// </summary>
    [JsonProperty]
    public bool Exclusive { get; protected set; }

    /// <summary>
    ///   When true this tutorial wants the game paused
    /// </summary>
    [JsonProperty]
    public bool Pauses { get; protected set; }

    [JsonProperty]
    public float Time { get; protected set; }

    [JsonProperty]
    public bool UsesPlayerPositionGuidance { get; protected set; }

    /// <summary>
    ///   A name that this tutorial reacts to by hiding itself
    /// </summary>
    [JsonIgnore]
    public abstract string ClosedByName { get; }

    [JsonIgnore]
    public bool Complete => !ShownCurrently && HasBeenShown;

    [JsonIgnore]
    public bool CurrentlyExclusivelyOpen => Exclusive && ShownCurrently;

    [JsonIgnore]
    public bool WantsPaused => Pauses && ShownCurrently;

    /// <summary>
    ///   Sends this state to the GUI
    /// </summary>
    /// <param name="gui">Target GUI instance</param>
    public abstract void ApplyGUIState(TutorialGUI gui);

    /// <summary>
    ///   Checks (and handles) tutorial events that this tutorial reacts to
    /// </summary>
    /// <param name="overallState">State access to all the tutorials</param>
    /// <param name="eventType">Type of the event that happened</param>
    /// <param name="args">Event arguments or EventArgs.Empty</param>
    /// <param name="sender">The object that sent the event</param>
    /// <returns>True if handled (no other tutorial gets to see it), false if wasn't handled / consumed.</returns>
    public abstract bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
        object sender);

    public void Show()
    {
        ShownCurrently = true;
        HasBeenShown = true;
        Time = 0;
    }

    public void Hide()
    {
        if (!ShownCurrently)
            return;

        ShownCurrently = false;

        // Disable the triggering again by making sure this is marked as shown
        HasBeenShown = true;
    }

    public virtual Vector3 GetPositionGuidance()
    {
        throw new NotImplementedException("child class didn't override position guidance");
    }

    /// <summary>
    ///   Called while this is shown
    /// </summary>
    /// <param name="overallState">Access to all state</param>
    /// <param name="delta">Elapsed time</param>
    public void Process(TutorialState overallState, float delta)
    {
        Time += delta;

        OnProcess(overallState, delta);
    }

    /// <summary>
    ///   This is for subclasses to add custom process behaviour
    /// </summary>
    protected virtual void OnProcess(TutorialState overallState, float delta) { }
}
