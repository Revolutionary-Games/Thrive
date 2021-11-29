using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   A single tutorial in the game. Tutorials are split into subclasses to make the structure of the code much better.
/// </summary>
public abstract class TutorialPhase
{
    /// <summary>
    ///   If set to false the trigger condition won't be checked for this tutorial
    /// </summary>
    [JsonProperty]
    public bool CanTrigger { get; set; } = true;

    [JsonProperty]
    public bool ShownCurrently { get; protected set; }

    [JsonProperty]
    public bool HasBeenShown { get; protected set; }

    [JsonProperty]
    public bool HandlesEvents { get; protected set; } = true;

    /// <summary>
    ///   When true this tutorial wants the game paused
    /// </summary>
    [JsonProperty]
    public bool Pauses { get; protected set; }

    /// <summary>
    ///   When true Process is called even when this is hidden
    /// </summary>
    [JsonProperty]
    public bool ProcessWhileHidden { get; protected set; }

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
    public bool WantsPaused => Pauses && ShownCurrently;

    // GUI state applying functions, there is one per the type of tutorial GUI
    // By default when a tutorial receives the call to apply states for a GUI it doesn't handle, it will be hidden
    // if visible

    public virtual void ApplyGUIState(MicrobeTutorialGUI gui)
    {
        DefaultGUIStateHandle();
    }

    public virtual void ApplyGUIState(MicrobeEditorTutorialGUI gui)
    {
        DefaultGUIStateHandle();
    }

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

    public virtual void Show()
    {
        ShownCurrently = true;
        HasBeenShown = true;
        Time = 0;
    }

    public virtual void Hide()
    {
        if (!ShownCurrently)
            return;

        ShownCurrently = false;

        // Disable the triggering again by making sure this is marked as shown
        HasBeenShown = true;
        CanTrigger = false;
        HandlesEvents = false;
    }

    /// <summary>
    ///   Enables trigger condition and sets run in background to true. Should only be used on tutorials that properly
    ///   handle processing while hidden.
    /// </summary>
    public void EnableTriggerAndBackgroundProcess()
    {
        if (HasBeenShown || ShownCurrently)
            return;

        CanTrigger = true;
        ProcessWhileHidden = true;
    }

    /// <summary>
    ///   Inhibits this tutorial from processing or being shown in the future. And also hides if shown currently
    /// </summary>
    public void Inhibit()
    {
        HasBeenShown = true;
        ProcessWhileHidden = false;

        if (ShownCurrently)
        {
            Hide();
        }
    }

    public virtual Vector2 GetPositionGuidance()
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

    private void DefaultGUIStateHandle()
    {
        if (ShownCurrently)
            Hide();
    }
}
