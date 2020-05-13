using System.Collections.Generic;
using Godot;

/// <summary>
///   Manages the screen transitions, usually used for when
///   switching scenes. This singleton class is placed on
///   AutoLoad for global access while still inheriting from Node.
/// </summary>
public class TransitionManager : Node
{
    private static TransitionManager _instance;

    private PackedScene screenFadeScene;
    private PackedScene cutsceneScene;

    /// <summary>
    ///   Sequence of transitions on queue waiting
    ///   to be started.
    /// </summary>
    private Queue<ITransition> queuedTransitions = new Queue<ITransition>();

    private TransitionManager()
    {
        _instance = this;

        screenFadeScene = GD.Load<PackedScene>("res://src/gui_common/Fade.tscn");
        cutsceneScene = GD.Load<PackedScene>("res://src/gui_common/Cutscene.tscn");
    }

    [Signal]
    public delegate void QueuedTransitionsFinished();

    public static TransitionManager Instance
    {
        get { return _instance; }
    }

    /// <summary>
    ///   List of all the existing transitions after calling StartTransitions.
    /// </summary>
    public List<ITransition> TransitionSequence { get; private set; } =
        new List<ITransition>();

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            CancelQueuedTransitions();
        }
    }

    /// <summary>
    ///   Helper function for instantiating
    ///   and queuing a screen fade.
    /// </summary>
    /// <param name="type">
    ///   The type of fade to transition to.
    /// </param>
    /// <param name="fadeDuration">
    ///   How long the fade lasts
    /// </param>
    /// <param name="allowSkipping">
    ///   Allow the user to skip this
    /// </param>
    public void AddScreenFade(Fade.FadeType type, float fadeDuration, bool allowSkipping = true)
    {
        // Instantiate scene
        var screenFade = (Fade)screenFadeScene.Instance();
        AddChild(screenFade);

        screenFade.Skippable = allowSkipping;
        screenFade.FadeTransition = type;
        screenFade.FadeDuration = fadeDuration;

        screenFade.Connect("OnFinishedSignal", this, nameof(StartNextQueuedTransition));

        queuedTransitions.Enqueue(screenFade);
    }

    /// <summary>
    ///   Helper function for instantiating
    ///   and queuing a cutscene.
    /// </summary>
    public void AddCutscene(string path, bool allowSkipping = true)
    {
        // Instantiate scene
        var cutscene = (Cutscene)cutsceneScene.Instance();
        AddChild(cutscene);

        cutscene.Skippable = allowSkipping;

        var stream = GD.Load<VideoStream>(path);

        cutscene.CutsceneVideoPlayer.Stream = stream;

        cutscene.Connect("OnFinishedSignal", this, nameof(StartNextQueuedTransition));

        queuedTransitions.Enqueue(cutscene);
    }

    /// <summary>
    ///   Starts the transitions on the queue.
    ///   Calls a method when all the transition finished.
    /// </summary>
    public void StartTransitions(Object target, string onFinishedMethod)
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
    ///   Skips the running and all the remaining transitions.
    /// </summary>
    public void CancelQueuedTransitions()
    {
        if (TransitionSequence.Count == 0 || TransitionSequence == null)
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
