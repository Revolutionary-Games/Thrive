using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Helper functions for making the places in code dealing with saves shorter
/// </summary>
public static class SaveHelper
{
    /// <summary>
    ///   Quick save from the microbe stage
    /// </summary>
    /// <param name="stage">Data to include in save</param>
    public static void QuickSave(MicrobeStage stage)
    {
        InternalSaveHelper(SaveInformation.SaveType.QuickSave, MainGameState.MicrobeStage, save =>
        {
            save.SavedProperties = stage.CurrentGame;
            save.MicrobeStage = stage;
        }, () => stage);
    }

    /// <summary>
    ///   Quick save from the microbe editor
    /// </summary>
    /// <param name="editor">Data to include in save</param>
    public static void QuickSave(MicrobeEditor editor)
    {
        InternalSaveHelper(SaveInformation.SaveType.QuickSave, MainGameState.MicrobeEditor, save =>
        {
            save.SavedProperties = editor.CurrentGame;
            save.MicrobeEditor = editor;
        }, () => editor);
    }

    /// <summary>
    ///   Auto save the game (if enabled in settings)
    /// </summary>
    public static void AutoSave(MicrobeStage microbeStage)
    {
        if (!Settings.Instance.AutoSaveEnabled)
            return;

        // TODO: implement
        _ = microbeStage;
    }

    public static void AutoSave(MicrobeEditor editor)
    {
        if (!Settings.Instance.AutoSaveEnabled)
            return;

        // TODO: implement
        _ = editor;
    }

    /// <summary>
    ///   Loads the save file with the latest write time
    /// </summary>
    public static void QuickLoad()
    {
        // TODO: implement name detection
        var name = "quick_save." + Constants.SAVE_EXTENSION;

        LoadSave(name);
    }

    /// <summary>
    ///   Loads save
    /// </summary>
    /// <param name="name">The name of the save to load</param>
    public static void LoadSave(string name)
    {
        // TODO: loading screen while loading save data

        Save save;

        var stopwatch = Stopwatch.StartNew();

        // try
        // {
        save = Save.LoadFromFile(name);

        // }
        // catch (Exception e)
        // {
        //     DisplayLoadFailure("An exception happened while loading the save data", e.ToString(), stopwatch);
        //     return;
        // }

        // Save data loaded, apply the save
        ApplySave(save, stopwatch);
    }

    /// <summary>
    ///   Replaces the current state of the game with what the save has
    /// </summary>
    /// <param name="save">The save to apply data from</param>
    /// <param name="stopwatch">To track how long it took</param>
    public static void ApplySave(Save save, Stopwatch stopwatch)
    {
        PackedScene scene;

        try
        {
            scene = SceneManager.Instance.LoadScene(save.GameState);
        }
        catch (ArgumentException)
        {
            DisplayLoadFailure("Save is invalid", "Save has an unknown game state", stopwatch);
            return;
        }

        var targetState = (ILoadableGameState)scene.Instance();

        FinishMovingToLoadedScene(targetState, save);
        DisplayLoadStatusMessage(true, "Load finished", stopwatch);
    }

    private static void InternalSaveHelper(SaveInformation.SaveType type, MainGameState gameState,
        Action<Save> copyInfoToSave, Func<Node> stateRoot)
    {
        new InProgressSave(type, stateRoot, (data) =>
                CreateSaveObject(gameState, data.Type),
            (inProgress, save) =>
            {
                copyInfoToSave.Invoke(save);

                PerformSave(inProgress, save);
            }).Start();
    }

    private static Save CreateSaveObject(MainGameState gameState, SaveInformation.SaveType type)
    {
        return new Save
        {
            GameState = gameState,
            Info = { Type = type },
            Screenshot = ScreenShotTaker.Instance.TakeScreenshot(),
        };
    }

    private static void PerformSave(InProgressSave inProgress, Save save)
    {
        try
        {
            save.SaveToFile();
            inProgress.ReportStatus(true, "Saving succeeded");
        }
        catch (Exception e)
        {
            inProgress.ReportStatus(false, "Saving failed! An exception happened", e.ToString());
            return;
        }

        if (inProgress.Type == SaveInformation.SaveType.QuickSave)
            QueueRemoveExcessQuickSaves();
    }

    /// <summary>
    ///   Shows a message to the player about a save
    /// </summary>
    /// <param name="success">True on success</param>
    /// <param name="message">Failure reason, or the created save file</param>
    /// <param name="stopwatch">Used to measure how long saving took</param>
    /// <remarks>
    ///   TODO: implement this
    /// </remarks>
    private static void DisplayLoadStatusMessage(bool success, string message, Stopwatch stopwatch)
    {
        stopwatch.Stop();
        GD.Print("load finished, success: ", success, " message: ", message, " elapsed: ", stopwatch.Elapsed);
        SaveStatusOverlay.Instance.ShowMessage(message);
    }

    /// <summary>
    ///   Displays a dismissible dialog saying that loading a save failed
    /// </summary>
    /// <param name="message">Message to show</param>
    /// <param name="error">Error message to include</param>
    /// <param name="stopwatch">Duration tracking</param>
    private static void DisplayLoadFailure(string message, string error, Stopwatch stopwatch)
    {
        stopwatch.Stop();
        GD.Print("loading FAILED, message: ", message, " elapsed: ", stopwatch.Elapsed);
        GD.Print("error related to load fail: ", error);

        // TODO: show the dialog
        // For now at least show something
        SaveStatusOverlay.Instance.ShowMessage("Loading a save failed: " + message);
    }

    private static void FinishMovingToLoadedScene(ILoadableGameState newScene, Save save)
    {
        newScene.IsLoadedFromSave = true;

        SceneManager.Instance.SwitchToScene(newScene.GameStateRoot);

        newScene.OnFinishLoading(save);
    }

    /// <summary>
    ///   Runs a background task for removing excess quick saves
    /// </summary>
    private static void QueueRemoveExcessQuickSaves()
    {
        TaskExecutor.Instance.AddTask(new Task(SaveManager.RemoveExcessQuickSaves));
    }
}
