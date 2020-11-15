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

    /// <summary>
    ///   True when a save is currently being loaded
    ///   Used to stop quick load starting while a load is in progress already
    /// </summary>
    public static bool IsLoading { get; private set; }

    public void ReportStatus(bool success, string message, string exception = "")
    {
        this.success = success;
        this.message = message;
        this.exception = exception;
    }

    public void Start()
    {
        IsLoading = true;
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
                LoadingScreen.Instance.Show(TranslationServer.Translate("LOADING_GAME"),
                    TranslationServer.Translate("READING_SAVE_DATA"));

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
                        LoadingScreen.Instance.Show(TranslationServer.Translate("LOADING_GAME"),
                            TranslationServer.Translate("CREATING_OBJECTS_FROM_SAVE"))));

                    state = State.CreatingScene;
                }
                catch (Exception e)
                {
                    ReportStatus(false,
                        TranslationServer.Translate("AN_EXCEPTION_HAPPENED_WHILE_LOADING"),
                        e.ToString());
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
                    ReportStatus(false, TranslationServer.Translate("SAVE_IS_INVALID"),
                        TranslationServer.Translate("SAVE_HAS_INVALID_GAME_STATE"));
                    state = State.Finished;
                    break;
                }

                state = State.ProcessingLoadedObjects;
                break;
            }

            case State.ProcessingLoadedObjects:
            {
                LoadingScreen.Instance.Show(TranslationServer.Translate("LOADING_GAME"),
                    TranslationServer.Translate("PROCESSING_LOADED_OBJECTS"));

                if (loadedState.IsLoadedFromSave != true)
                    throw new Exception("Game load logic not working correctly, IsLoadedFromSave was not set");

                try
                {
                    SceneManager.Instance.SwitchToScene(loadedState.GameStateRoot);
                    loadedState.OnFinishLoading(save);
                }
                catch (Exception e)
                {
                    ReportStatus(false,
                        TranslationServer.Translate("AN_EXCEPTION_HAPPENED_WHILE_PROCESSING"),
                        e.ToString());
                    state = State.Finished;
                    break;
                }

                ReportStatus(true, TranslationServer.Translate("LOAD_FINISHED"), string.Empty);
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
                    SaveStatusOverlay.Instance.ShowError(TranslationServer.Translate("ERROR_LOADING"),
                        message, exception, true,
                        () => LoadingScreen.Instance.Hide());
                }

                IsLoading = false;
                return;
            }

            default:
                throw new InvalidOperationException();
        }

        Invoke.Instance.Queue(Step);
    }
}
