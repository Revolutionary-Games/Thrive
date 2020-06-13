using System;
using System.CodeDom;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Holds data needed for an in-progress save action. And manages stepping through all the actions that need to happen
/// </summary>
public class InProgressSave : IDisposable
{
    private readonly Func<Node> currentGameRoot;
    private readonly Func<InProgressSave, Save> createSaveData;
    private readonly Action<InProgressSave, Save> performSave;
    private readonly bool returnToPauseState;

    private State state = State.Initial;
    private Save save;

    private Task<string> saveNameTask;

    private Stopwatch stopwatch;

    private bool success;
    private string message;
    private string exception;

    private bool disposed;

    public InProgressSave(SaveInformation.SaveType type, Func<Node> currentGameRoot,
        Func<InProgressSave, Save> createSaveData, Action<InProgressSave, Save> performSave)
    {
        this.currentGameRoot = currentGameRoot;
        this.createSaveData = createSaveData;
        this.performSave = performSave;
        Type = type;
        returnToPauseState = currentGameRoot.Invoke().GetTree().Paused;

        stopwatch = Stopwatch.StartNew();

        // Start calculating the save name here to save some time
        saveNameTask = new Task<string>(CalculateNameForSave);
        TaskExecutor.Instance.AddTask(saveNameTask);
    }

    private enum State
    {
        Initial,
        Screenshot,
        SaveData,
        Finished,
    }

    public SaveInformation.SaveType Type { get; }

    public void Start()
    {
        currentGameRoot.Invoke().GetTree().Paused = true;

        Invoke.Instance.Perform(Step);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    internal void ReportStatus(bool success, string message, string exception = "")
    {
        this.success = success;
        this.message = message;
        this.exception = exception;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                saveNameTask.Dispose();
            }

            disposed = true;
        }
    }

    private void Step()
    {
        switch (state)
        {
            case State.Initial:
                // TODO: if there is a pause menu open, close that here before moving onto the screenshot
                state = State.Screenshot;
                break;
            case State.Screenshot:
            {
                save = createSaveData.Invoke(this);
                SaveStatusOverlay.Instance.ShowMessage("Saving...", Mathf.Inf);

                state = State.SaveData;
                break;
            }

            case State.SaveData:
            {
                save.Name = saveNameTask.Result;
                performSave.Invoke(this, save);
                state = State.Finished;
                break;
            }

            case State.Finished:
            {
                stopwatch.Stop();
                GD.Print("save finished, success: ", success, " message: ", message, " elapsed: ", stopwatch.Elapsed);

                if (success)
                {
                    SaveStatusOverlay.Instance.ShowMessage(message);

                    currentGameRoot.Invoke().GetTree().Paused = returnToPauseState;
                }
                else
                {
                    SaveStatusOverlay.Instance.ShowMessage("Save failed");
                    SaveStatusOverlay.Instance.ShowError("Error Saving", message, exception);
                }

                return;
            }

            default:
                throw new InvalidOperationException();
        }

        Invoke.Instance.Queue(Step);
    }

    private string CalculateNameForSave()
    {
        // TODO: implement type naming
        var name = "quick_save." + Constants.SAVE_EXTENSION;

        return name;
    }
}
