﻿using System;
using System.Diagnostics;
using System.Globalization;
using Godot;
using Saving;

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

    private readonly Stopwatch stopwatch;

    private State state = State.Initial;
    private Save? save;

    private ILoadableGameState? loadedState;

    private bool success;
    private string message = "Error: message not set";
    private string exception = string.Empty;

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
        CheatManager.OnCheatsDisabled();

        IsLoading = true;
        SceneManager.Instance.DetachCurrentScene();
        PauseManager.Instance.AddPause(nameof(InProgressLoad));

        Invoke.Instance.Perform(Step);
    }

    private void Step()
    {
        switch (state)
        {
            case State.Initial:
                state = State.ReadingData;

                // Invalid is given as the target state here, because it's unknown yet.
                // TODO: See #1847
                LoadingScreen.Instance.Show(TranslationServer.Translate("LOADING_GAME"),
                    MainGameState.Invalid,
                    TranslationServer.Translate("READING_SAVE_DATA"));

                // Let all suppressed deletions happen
                TemporaryLoadedNodeDeleter.Instance.ReleaseAllHolds();
                JSONDebug.FlushJSONTracesOut();

                break;
            case State.ReadingData:
            {
                // Start suppressing loaded node deletion
                TemporaryLoadedNodeDeleter.Instance.AddDeletionHold(Constants.DELETION_HOLD_LOAD);

                // TODO: do this in a background thread if possible
                try
                {
                    // Invalid is given as the target state here, because it's unknown yet.
                    // TODO: See #1847
                    save = Save.LoadFromFile(saveName, () => Invoke.Instance.Perform(() =>
                        LoadingScreen.Instance.Show(TranslationServer.Translate("LOADING_GAME"),
                            MainGameState.Invalid,
                            TranslationServer.Translate("CREATING_OBJECTS_FROM_SAVE"))));

                    state = State.CreatingScene;
                }
                catch (Exception e)
                {
                    var extraProblem = TryFreeAlreadyLoadedData();

                    ReportStatus(false,
                        TranslationServer.Translate("EXCEPTION_HAPPENED_WHILE_LOADING"),
                        e + extraProblem);
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
                    loadedState = save!.TargetScene ?? throw new Exception("Save has no target scene");
                }
                catch (Exception)
                {
                    var extraProblem = TryFreeAlreadyLoadedData();

                    ReportStatus(false, TranslationServer.Translate("SAVE_IS_INVALID"),
                        TranslationServer.Translate("SAVE_HAS_INVALID_GAME_STATE") + extraProblem);
                    state = State.Finished;
                    break;
                }

                state = State.ProcessingLoadedObjects;
                break;
            }

            case State.ProcessingLoadedObjects:
            {
                LoadingScreen.Instance.Show(TranslationServer.Translate("LOADING_GAME"),
                    save!.GameState,
                    TranslationServer.Translate("PROCESSING_LOADED_OBJECTS"));

                if (loadedState!.IsLoadedFromSave != true)
                    throw new Exception("Game load logic not working correctly, IsLoadedFromSave was not set");

                try
                {
                    SceneManager.Instance.SwitchToScene(loadedState.GameStateRoot);
                    loadedState.OnFinishLoading(save);
                }
                catch (Exception e)
                {
                    var extraProblem = TryFreeAlreadyLoadedData();

                    ReportStatus(false,
                        TranslationServer.Translate("EXCEPTION_HAPPENED_PROCESSING_SAVE"),
                        e + extraProblem);
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

                JSONDebug.FlushJSONTracesOut();

                PauseManager.Instance.Resume(nameof(InProgressLoad));

                if (success)
                {
                    LoadingScreen.Instance.Hide();
                    SaveStatusOverlay.Instance.ShowMessage(message);
                }
                else
                {
                    SaveStatusOverlay.Instance.ShowError(TranslationServer.Translate("ERROR_LOADING"),
                        message, exception, true,
                        () => LoadingScreen.Instance.Hide());
                }

                IsLoading = false;

                SaveHelper.MarkLastSaveToCurrentTime();

                // Make certain that if some game element paused and we unloaded it without it realizing that, we
                // don't get stuck in paused mode
                PauseManager.Instance.ForceClear();

                return;
            }

            default:
                throw new InvalidOperationException();
        }

        Invoke.Instance.Queue(Step);
    }

    private string TryFreeAlreadyLoadedData()
    {
        if (save == null)
            return string.Empty;

        try
        {
            // Free up the loaded Godot resources
            save.DestroyGameStates();
        }
        catch (Exception e2)
        {
            return string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("SAVE_LOAD_ALREADY_LOADED_FREE_FAILURE"), e2);
        }

        return string.Empty;
    }
}
