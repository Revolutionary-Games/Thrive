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
        new InProgressLoad(name).Start();
    }

    /// <summary>
    ///   Deletes a save with the given name
    /// </summary>
    public static void DeleteSave(string saveName)
    {
        using (var directory = new Directory())
        {
            directory.Remove(PathUtils.Join(Constants.SAVE_FOLDER, saveName));
        }
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
    ///   Runs a background task for removing excess quick saves
    /// </summary>
    private static void QueueRemoveExcessQuickSaves()
    {
        TaskExecutor.Instance.AddTask(new Task(SaveManager.RemoveExcessQuickSaves));
    }
}
