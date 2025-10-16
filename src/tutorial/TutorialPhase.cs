using System;
using Godot;
using SharedBase.Archive;

/// <summary>
///   A single tutorial in the game. Tutorials are split into subclasses to make the structure of the code much better.
/// </summary>
public abstract class TutorialPhase : IArchiveUpdatable
{
    public delegate void OnTutorialOpenDelegate();

    public delegate void OnTutorialCompleteDelegate();

    /// <summary>
    ///   If set to false, the trigger condition won't be checked for this tutorial
    /// </summary>
    public bool CanTrigger { get; set; } = true;

    public bool ShownCurrently { get; protected set; }

    public bool HasBeenShown { get; protected set; }

    public bool HandlesEvents { get; protected set; } = true;

    /// <summary>
    ///   When true, this tutorial wants the game paused
    /// </summary>
    public bool Pauses { get; protected set; }

    /// <summary>
    ///   When true Process is called even when this is hidden
    /// </summary>
    public bool ProcessWhileHidden { get; protected set; }

    public float Time { get; protected set; }

    public bool UsesPlayerPositionGuidance { get; protected set; }

    /// <summary>
    ///   A name that this tutorial reacts to by hiding itself
    /// </summary>
    public abstract string ClosedByName { get; }

    public bool Complete => !ShownCurrently && HasBeenShown;

    public bool WantsPaused => Pauses && ShownCurrently;

    public abstract ushort CurrentArchiveVersion { get; }
    public abstract ArchiveObjectType ArchiveObjectType { get; }

    /// <summary>
    ///   Event that is triggered when this tutorial opens
    /// </summary>
    public OnTutorialOpenDelegate? OnOpened { get; set; }

    /// <summary>
    ///   Event that is triggered when this tutorial closes for any reason
    /// </summary>
    public OnTutorialCompleteDelegate? OnClosed { get; set; }

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
        if (!ShownCurrently)
            OnOpened?.Invoke();

        ShownCurrently = true;
        HasBeenShown = true;
        Time = 0;
    }

    public virtual void Hide()
    {
        if (!ShownCurrently)
            return;

        OnClosed?.Invoke();

        ShownCurrently = false;

        // Disable the triggering again by making sure this is marked as shown
        HasBeenShown = true;
        CanTrigger = false;
        HandlesEvents = false;
    }

    /// <summary>
    ///   Enables trigger condition and sets run in the background to true. Should only be used on tutorials
    ///   that properly handle processing while hidden.
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

    public virtual Vector3? GetPositionGuidance()
    {
        throw new NotImplementedException("child class didn't override position guidance");
    }

    /// <summary>
    ///   Called while this is shown
    /// </summary>
    /// <param name="overallState">Access to all the state</param>
    /// <param name="delta">Elapsed time</param>
    public void Process(TutorialState overallState, float delta)
    {
        Time += delta;

        OnProcess(overallState, delta);
    }

    public virtual void WritePropertiesToArchive(ISArchiveWriter writer)
    {
        writer.Write(CanTrigger);
        writer.Write(ShownCurrently);
        writer.Write(HasBeenShown);
        writer.Write(HandlesEvents);
        writer.Write(Pauses);
        writer.Write(ProcessWhileHidden);
        writer.Write(Time);
        writer.Write(UsesPlayerPositionGuidance);
    }

    public virtual void ReadPropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version != 1)
            throw new InvalidArchiveVersionException(version, 1);

        CanTrigger = reader.ReadBool();
        ShownCurrently = reader.ReadBool();
        HasBeenShown = reader.ReadBool();
        HandlesEvents = reader.ReadBool();
        Pauses = reader.ReadBool();
        ProcessWhileHidden = reader.ReadBool();
        Time = reader.ReadFloat();
        UsesPlayerPositionGuidance = reader.ReadBool();
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
