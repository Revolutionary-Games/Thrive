using System.Collections.Generic;
using Godot;

/// <summary>
///   Manages the transitions.
///   This singleton class is placed on AutoLoad.
/// </summary>
public class TransitionManager : Node
{
    public PackedScene ScreenFadeScene;
    public PackedScene CutsceneScene;

    /// <summary>
    ///   Sequence of transitions on queue waiting
    ///   to be started.
    /// </summary>
    private Queue<ITransition> queuedTransitions = new Queue<ITransition>();

    [Signal]
    public delegate void QueuedTransitionsFinished();

    /// <summary>
    ///   List of all the existing transitions after calling StartTransitions.
    /// </summary>
    public List<ITransition> TransitionSequence { get; private set; } =
        new List<ITransition>();

    public override void _Ready()
    {
        ScreenFadeScene = GD.Load<PackedScene>("res://scripts/gui/Fade.tscn");
        CutsceneScene = GD.Load<PackedScene>("res://scripts/gui/Cutscene.tscn");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            CancelQueuedTransitions();
        }
    }

    /// <summary>
    ///   Helper function for instantiating
    ///   and queues a screen fade.
    /// </summary>
    /// <param name="type">
    ///   The type of fade to transition to.
    /// </param>
    public void AddFade(Fade.FadeType type, float fadeDuration, bool allowSkipping = true)
    {
        // Instantiate scene
        var screenFade = (Fade)ScreenFadeScene.Instance();
        AddChild(screenFade);

        screenFade.Skippable = allowSkipping;
        screenFade.FadeTransition = type;
        screenFade.FadeDuration = fadeDuration;

        screenFade.Connect("OnFinishedSignal", this, nameof(StartNextQueuedTransition));

        queuedTransitions.Enqueue(screenFade);
    }

    /// <summary>
    ///   Helper function for instantiating
    ///   and queues a cutscene.
    /// </summary>
    public void AddCutscene(string path, bool allowSkipping = true)
    {
        // Instantiate scene
        var cutscene = (Cutscene)CutsceneScene.Instance();
        AddChild(cutscene);

        cutscene.Skippable = allowSkipping;

        var stream = GD.Load<VideoStream>(path);

        // Play the video stream
        cutscene.CutsceneVideoPlayer.Stream = stream;

        cutscene.Connect("OnFinishedSignal", this, nameof(StartNextQueuedTransition));

        queuedTransitions.Enqueue(cutscene);
    }

    /// <summary>
    ///   Starts the transitions on the queue.
    ///   Calls a method when all the transition finished.
    /// </summary>
    public void StartTransitions(Godot.Object target, string onFinishedMethod)
    {
        if (queuedTransitions.Count == 0 || queuedTransitions == null)
        {
            GD.PrintErr("Queued transitions is either empty or null");
            return;
        }

        if (onFinishedMethod != string.Empty)
        {
            if (!IsConnected(nameof(QueuedTransitionsFinished), target, onFinishedMethod))
            {
                Connect(nameof(QueuedTransitionsFinished), target, onFinishedMethod, null,
                    (uint)ConnectFlags.Oneshot);
            }
        }

        // Keep the queued transitions as a reference, so that
        // we can cancel the remaining transitions anytime
        // Todo: is this hackish?
        foreach (var entry in queuedTransitions)
        {
            TransitionSequence.Add(entry);
        }

        // Begin the first transition on the queue
        StartNextQueuedTransition();
    }

    /// <summary>
    ///   Skips all the transitions, and emits finished signal.
    /// </summary>
    public void CancelQueuedTransitions()
    {
        if (TransitionSequence.Count == 0 || TransitionSequence == null)
            return;

        var transitions = new List<ITransition>(TransitionSequence);

        foreach (var entry in transitions)
        {
            if (Godot.Object.IsInstanceValid((Node)entry))
            {
                if (entry.Skippable)
                    entry.OnFinished();
            }
        }
    }

    /// <summary>
    ///   Starts the next transition on the
    ///   queue when the previous ends.
    /// </summary>
    private void StartNextQueuedTransition()
    {
        // Assume it's finished when the queue list is empty.
        if (queuedTransitions.Count == 0)
        {
            EmitSignal(nameof(QueuedTransitionsFinished));
            TransitionSequence.Clear();
            return;
        }

        var currentTransition = queuedTransitions.Dequeue();
        currentTransition.OnStarted();
    }
}
