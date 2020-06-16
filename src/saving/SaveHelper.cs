using System;
using System.Diagnostics;
using System.Linq;
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
    /// <param name="state">Data to include in save</param>
    public static void QuickSave(MicrobeStage state)
    {
        InternalSaveHelper(SaveInformation.SaveType.QuickSave, MainGameState.MicrobeStage, save =>
        {
            save.SavedProperties = state.CurrentGame;
            save.MicrobeStage = state;
        }, () => state);
    }

    /// <summary>
    ///   Quick save from the microbe editor
    /// </summary>
    /// <param name="state">Data to include in save</param>
    public static void QuickSave(MicrobeEditor state)
    {
        InternalSaveHelper(SaveInformation.SaveType.QuickSave, MainGameState.MicrobeEditor, save =>
        {
            save.SavedProperties = state.CurrentGame;
            save.MicrobeEditor = state;
        }, () => state);
    }

    /// <summary>
    ///   A save (and not a quick save) that the user triggered
    /// </summary>
    /// <param name="name">Save name to use, or blank</param>
    /// <param name="state">The current game state to make the save with</param>
    public static void Save(string name, MicrobeStage state)
    {
        InternalSaveHelper(SaveInformation.SaveType.Manual, MainGameState.MicrobeStage, save =>
        {
            save.SavedProperties = state.CurrentGame;
            save.MicrobeStage = state;
        }, () => state, name);
    }

    public static void Save(string name, MicrobeEditor state)
    {
        InternalSaveHelper(SaveInformation.SaveType.Manual, MainGameState.MicrobeEditor, save =>
        {
            save.SavedProperties = state.CurrentGame;
            save.MicrobeEditor = state;
        }, () => state, name);
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
        // TODO: is there a way to to find the latest modified file without checking them all?
        var save = SaveManager.CreateListOfSaves(SaveManager.SaveOrder.LastModifiedFirst).FirstOrDefault();

        if (save == null)
        {
            GD.Print("No saves exist, can't quick load");
            return;
        }

        LoadSave(save);
    }

    /// <summary>
    ///   Loads save
    /// </summary>
    /// <param name="name">The name of the save to load</param>
    public static void LoadSave(string name)
    {
        GD.Print("Starting load of save: ", name);
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
        Action<Save> copyInfoToSave, Func<Node> stateRoot, string saveName = null)
    {
        new InProgressSave(type, stateRoot, (data) =>
                CreateSaveObject(gameState, data.Type),
            (inProgress, save) =>
            {
                copyInfoToSave.Invoke(save);

                PerformSave(inProgress, save);
            }, saveName).Start();
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
