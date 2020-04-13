using System;
using System.Threading.Tasks;

/// <summary>
///   A single run of the auto-evo system happening in a background thread
/// </summary>
public class AutoEvoRun
{
    private readonly AutoEvo.RunParameters parameters;

    private bool started = false;
    private volatile bool running = false;
    private volatile bool finished = false;
    private volatile bool aborted = false;

    private AutoEvo.RunResults results = new AutoEvo.RunResults();

    public AutoEvoRun(GameWorld world)
    {
        parameters = new AutoEvo.RunParameters(world);
    }

    public bool Running { get => running; private set => running = value; }
    public bool Finished { get => finished; private set => finished = value; }
    public bool Aborted { get => aborted; set => aborted = value; }

    /// <summary>
    ///   Starts this run if not started already
    /// </summary>
    public void Start()
    {
        if (started)
            return;

        var task = new Task(Run);

        TaskExecutor.Instance.AddTask(task);
        started = true;
    }

    /// <summary>
    ///   Returns true when this run is finished
    /// </summary>
    /// <param name="autostart">If set to <c>true</c> start the run if not already.</param>
    /// <returns>True when the run is complete or aborted</returns>
    public bool IsFinished(bool autostart = true)
    {
        if (autostart && !started)
            Start();

        return Finished;
    }

    public void ApplyResults()
    {
        if (!Finished || Running)
        {
            throw new InvalidOperationException("Can't apply run results before it is done");
        }

        results.ApplyResults(parameters.World, false);
    }

    /// <summary>
    ///   Run this instance. Should only be called in a background thread
    /// </summary>
    private void Run()
    {
        Running = true;

        bool complete = false;

        while (!Aborted && !complete)
        {
            // TODO: run the steps
            complete = true;
        }

        Running = false;
        Finished = true;
    }
}
