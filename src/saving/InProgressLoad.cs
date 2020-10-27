using System;
using System.Diagnostics;
using Godot;

/// <summary>
///   Holds data needed for an in-progress load action.
///   And manages stepping through all the actions that need to happen
/// </summary>
/// <remarks>
///   <para>
///     This is very similar to InProgressSave
///   </para>
/// </remarks>
public class InProgressLoad
{
    private readonly string saveName;
    private State state = State.Initial;
    private Save save;

    private ILoadableGameState loadedState;

    private Stopwatch stopwatch;

    private bool success;
    private string message;
    private string exception;

    public InProgressLoad(string saveName)
    {
        this.saveName = saveName;
        stopwatch = Stopwatch.StartNew();
    }

    private enum State
    {
        Initial,
        ReadingData,
        ProcessingLoadedObjects,
        CreatingScene,
        Finished,
    }

    public void ReportStatus(bool success, string message, string exception = "")
    {
        this.success = success;
        this.message = message;
        this.exception = exception;
    }

    public void Start()
    {
        SceneManager.Instance.DetachCurrentScene();
        SceneManager.Instance.GetTree().Paused = true;

        Invoke.Instance.Perform(Step);
    }

    private void Step()
    {
        switch (state)
        {
            case State.Initial:
                state = State.ReadingData;
                LoadingScreen.Instance.Show("Loading Game", "Reading save data");

                // Let all suppressed deletions happen
                TemporaryLoadedNodeDeleter.Instance.ReleaseAllHolds();

                break;
            case State.ReadingData:
            {
                // Start suppressing loaded node deletion
                TemporaryLoadedNodeDeleter.Instance.AddDeletionHold(Constants.DELETION_HOLD_LOAD);

                // TODO: do this in a background thread if possible
                try
                {
                    save = Save.LoadFromFile(saveName, () => Invoke.Instance.Perform(() =>
                        LoadingScreen.Instance.Show("Loading Game", "Creating objects from save")));

                    state = State.CreatingScene;
                }
                catch (Exception e)
                {
                    ReportStatus(false, "An exception happened while loading the save data", e.ToString());
                    state = State.Finished;

                    // ReSharper disable HeuristicUnreachableCode ConditionIsAlwaysTrueOrFalse
                    if (!Constants.CATCH_SAVE_ERRORS)
#pragma warning disable 162
                        throw;
#pragma warning restore 162

                    // ReSharper restore HeuristicUnreachableCode ConditionIsAlwaysTrueOrFalse
                }

                break;
            }

            case State.CreatingScene:
            {
                try
                {
                    loadedState = save.TargetScene;
                }
                catch (Exception)
                {
                    ReportStatus(false, "Save is invalid", "Save has invalid game state scene");
                    state = State.Finished;
                    break;
                }

                state = State.ProcessingLoadedObjects;
                break;
            }

            case State.ProcessingLoadedObjects:
            {
                LoadingScreen.Instance.Show("Loading Game", "Processing loaded objects");

                if (loadedState.IsLoadedFromSave != true)
                    throw new Exception("Game load logic not working correctly, IsLoadedFromSave was not set");

                try
                {
                    SceneManager.Instance.SwitchToScene(loadedState.GameStateRoot);
                    loadedState.OnFinishLoading(save);
                }
                catch (Exception e)
                {
                    ReportStatus(false, "An exception happened while processing loaded objects", e.ToString());
                    state = State.Finished;
                    break;
                }

                ReportStatus(true, "Load finished", string.Empty);
                state = State.Finished;
                break;
            }

            case State.Finished:
            {
                stopwatch.Stop();
                GD.Print("load finished, success: ", success, " message: ", message, " elapsed: ", stopwatch.Elapsed);

                // Stop suppressing loaded node deletion
                TemporaryLoadedNodeDeleter.Instance.RemoveDeletionHold(Constants.DELETION_HOLD_LOAD);

                if (success)
                {
                    LoadingScreen.Instance.Hide();
                    SaveStatusOverlay.Instance.ShowMessage(message);

                    // TODO: does this cause problems if the game was paused when saving?
                    loadedState.GameStateRoot.GetTree().Paused = false;
                }
                else
                {
                    SaveStatusOverlay.Instance.ShowError("Error Loading", message, exception, true,
                        () => LoadingScreen.Instance.Hide());
                }

                return;
            }

            default:
                throw new InvalidOperationException();
        }

        Invoke.Instance.Queue(Step);
    }
}
