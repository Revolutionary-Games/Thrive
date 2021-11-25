using System.Collections.Generic;
using Godot;

/// <summary>
///   Manages the screen transitions, usually used for when
///   switching scenes. This is autoloaded
/// </summary>
public class TransitionManager : NodeWithInput
{
    private static TransitionManager instance;

    private readonly PackedScene screenFadeScene;
    private readonly PackedScene cutsceneScene;

    /// <summary>
    ///   Transitions waiting to be executed.
    /// </summary>
    private Queue<ITransition> queuedTransitions = new Queue<ITransition>();

    private TransitionManager()
    {
        instance = this;

        screenFadeScene = GD.Load<PackedScene>("res://src/gui_common/ScreenFade.tscn");
        cutsceneScene = GD.Load<PackedScene>("res://src/gui_common/Cutscene.tscn");
    }

    [Signal]
    public delegate void QueuedTransitionsFinished();

    public static TransitionManager Instance => instance;

    /// <summary>
    ///   List of all the existing transitions after calling StartTransitions.
    /// </summary>
    public List<ITransition> TransitionSequence { get; } = new List<ITransition>();

    public bool HasQueuedTransitions => TransitionSequence.Count > 0;

    [RunOnKeyDown("ui_cancel", OnlyUnhandled = false)]
    public bool CancelTransitionPressed()
    {
        if (!HasQueuedTransitions)
            return false;

        CancelQueuedTransitions();
        return true;
    }

    /// <summary>
    ///   Creates and queues a screen fade.
    /// </summary>
    /// <param name="type">The type of fade to transition to</param>
    /// <param name="fadeDuration">How long the fade lasts</param>
    /// <param name="allowSkipping">Allow the user to skip this</param>
    public void AddScreenFade(ScreenFade.FadeType type, float fadeDuration, bool allowSkipping = true)
    {
        // Instantiate scene
        var screenFade = (ScreenFade)screenFadeScene.Instance();
        AddChild(screenFade);

        screenFade.Skippable = allowSkipping;
        screenFade.CurrentFadeType = type;
        screenFade.FadeDuration = fadeDuration;

        screenFade.Connect("OnFinishedSignal", this, nameof(StartNextQueuedTransition));

        queuedTransitions.Enqueue(screenFade);
    }

    /// <summary>
    ///   Creates and queues a video cutscene.
    /// </summary>
    /// <param name="path">The video file to play</param>
    /// <param name="allowSkipping">Allow the user to skip this</param>
    public void AddCutscene(string path, float volume = 1.0f, bool allowSkipping = true)
    {
        // Instantiate scene
        var cutscene = (Cutscene)cutsceneScene.Instance();
        AddChild(cutscene);

        cutscene.Skippable = allowSkipping;
        cutscene.Volume = volume;
        cutscene.Stream = GD.Load<VideoStream>(path);

        cutscene.Connect("OnFinishedSignal", this, nameof(StartNextQueuedTransition));

        queuedTransitions.Enqueue(cutscene);
    }

    /// <summary>
    ///   Executes queued transitions.
    ///   Calls the specified method when all transitions are finished.
    /// </summary>
    /// <param name="target">The target object to connect to</param>
    /// <param name="onFinishedMethod">The name of the method on the target object</param>
    public void StartTransitions(Object target = null, string onFinishedMethod = null)
    {
        if (queuedTransitions.Count == 0 || queuedTransitions == null)
        {
            GD.PrintErr("No transitions found in the queue, can't execute transitions");
            return;
        }

        if (!string.IsNullOrEmpty(onFinishedMethod))
        {
            if (!IsConnected(nameof(QueuedTransitionsFinished), target, onFinishedMethod))
            {
                Connect(nameof(QueuedTransitionsFinished), target, onFinishedMethod, null,
                    (uint)ConnectFlags.Oneshot);
            }
        }

        foreach (var entry in queuedTransitions)
        {
            entry.Visible = false;

            // Add the transitions to the list for reference
            TransitionSequence.Add(entry);
        }

        // Begin the first transition in the queue
        StartNextQueuedTransition();
    }

    /// <summary>
    ///   Skips all the running and remaining transitions.
    /// </summary>
    private void CancelQueuedTransitions()
    {
        if (!HasQueuedTransitions)
            return;

        var transitions = new List<ITransition>(TransitionSequence);

        foreach (var entry in transitions)
        {
            if (IsInstanceValid((Node)entry))
            {
                if (entry.Skippable)
                    entry.OnFinished();
            }
        }
    }

    /// <summary>
    ///   Starts the next queued transition when the previous ends.
    /// </summary>
    private void StartNextQueuedTransition()
    {
        // Assume all transitions are finished if the queue is empty.
        if (queuedTransitions.Count == 0)
        {
            EmitSignal(nameof(QueuedTransitionsFinished));
            TransitionSequence.Clear();
            return;
        }

        var currentTransition = queuedTransitions.Dequeue();
        currentTransition.Visible = true;
        currentTransition.OnStarted();
    }
}
