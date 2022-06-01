using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Manages the screen transitions, usually used for when
///   switching scenes. This is autoloaded
/// </summary>
public class TransitionManager : ControlWithInput
{
    private static TransitionManager? instance;

    private static readonly PackedScene screenFadeScene = GD.Load<PackedScene>("res://src/gui_common/ScreenFade.tscn");
    private static readonly PackedScene cutsceneScene = GD.Load<PackedScene>("res://src/gui_common/Cutscene.tscn");

    /// <summary>
    ///   List of all the existing transitions after calling StartTransitions.
    /// </summary>
    private readonly Queue<Sequence> transitionSequences = new();

    private TransitionManager()
    {
        instance = this;
    }

    [Signal]
    public delegate void QueuedTransitionsFinished();

    public static TransitionManager Instance => instance ?? throw new InstanceNotLoadedYetException();

    public bool HasQueuedTransitions => transitionSequences.Count > 0;

    public override void _Process(float delta)
    {
        if (transitionSequences.Count > 0)
        {
            var sequence = transitionSequences.Peek();

            sequence.Process();

            if (sequence.Finished)
            {
                transitionSequences.Dequeue();
                SaveHelper.AllowQuickSavingAndLoading = !HasQueuedTransitions;
            }
        }
    }

    [RunOnKeyDown("ui_cancel", OnlyUnhandled = false)]
    public bool CancelTransitionPressed()
    {
        if (!HasQueuedTransitions)
            return false;

        CancelSequences();
        return true;
    }

    /// <summary>
    ///   Helper method for creating a screen fade.
    /// </summary>
    /// <param name="type">The type of fade to transition to</param>
    /// <param name="fadeDuration">How long the fade lasts</param>
    /// <param name="allowSkipping">Allow the user to skip this</param>
    public static ScreenFade CreateScreenFade(ScreenFade.FadeType type, float fadeDuration)
    {
        // Instantiate scene
        var screenFade = (ScreenFade)screenFadeScene.Instance();

        screenFade.CurrentFadeType = type;
        screenFade.FadeDuration = fadeDuration;

        return screenFade;
    }

    /// <summary>
    ///   Helper method for creating a video cutscene.
    /// </summary>
    /// <param name="path">The video file to play</param>
    /// <param name="volume">The video player's volume in linear value</param>
    /// <param name="allowSkipping">Allow the user to skip this</param>
    public static Cutscene CreateCutscene(string path, float volume = 1.0f)
    {
        // Instantiate scene
        var cutscene = (Cutscene)cutsceneScene.Instance();

        cutscene.Volume = volume;
        cutscene.Stream = GD.Load<VideoStream>(path);

        return cutscene;
    }

    /// <summary>
    ///   Enqueues a new <see cref="Sequence"/> from the given list of transitions.
    ///   Invokes the specified action when the sequence is finished.
    /// </summary>
    /// <param name="onFinishedCallback">The action to invoke when the sequence finished</param>
    public void AddSequence(List<ITransition> transitions, Action? onFinishedCallback = null, bool skippable = true)
    {
        if (transitions.Count <= 0 || transitions == null)
        {
            GD.PrintErr("The given array of transitions are either empty or null, can't add sequence");
            return;
        }

        foreach (var transition in transitions)
        {
            if (transition is Node node)
                AddChild(node);
        }

        var sequence = new Sequence(transitions, onFinishedCallback) { Skippable = skippable };
        transitionSequences.Enqueue(sequence);

        SaveHelper.AllowQuickSavingAndLoading = false;
    }

    /// <summary>
    ///   Skips all the running and remaining transition sequences.
    /// </summary>
    private void CancelSequences()
    {
        foreach (var sequence in transitionSequences)
            sequence.Skip();
    }

    /// <summary>
    ///   A sequence of <see cref="ITransition"/>s. Has its own on finished callback.
    /// </summary>
    public class Sequence
    {
        private Queue<ITransition> queuedTransitions = new();
        private Action? onFinishedCallback;

        public Sequence(List<ITransition> transitions, Action? onFinishedCallback)
        {
            foreach (var transition in transitions)
            {
                queuedTransitions.Enqueue(transition);
            }

            this.onFinishedCallback = onFinishedCallback;
        }

        public bool Skippable { get; set; }

        public bool Finished { get; private set; }

        public bool Running { get; private set; }

        public void Skip()
        {
            if (!Skippable)
                return;

            foreach (var transition in queuedTransitions)
                transition.Skip();
        }

        public void Process()
        {
            if (queuedTransitions.Count > 0)
            {
                if (!Running)
                {
                    Running = true;
                    StartNext();
                    return;
                }

                if (queuedTransitions.Peek().Finished)
                {
                    var previous = queuedTransitions.Dequeue();

                    // Defer call to avoid possible "flickers"
                    Invoke.Instance.Queue(() => previous.Clear());

                    StartNext();
                }
            }
        }

        /// <summary>
        ///   Starts the frontmost transition on the queue.
        /// </summary>
        private void StartNext()
        {
            if (queuedTransitions.Count > 0)
            {
                var front = queuedTransitions.Peek();
                front.Begin();
                return;
            }

            // Assume all transitions are finished if the queue is empty.
            Finished = true;
            Running = false;
            onFinishedCallback?.Invoke();
        }
    }
}
