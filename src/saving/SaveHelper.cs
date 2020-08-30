using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Helper functions for making the places in code dealing with saves shorter
/// </summary>
public static class SaveHelper
{
    public enum SaveOrder
    {
        /// <summary>
        ///   The last modified (on disk) save is first
        /// </summary>
        LastModifiedFirst,

        /// <summary>
        ///   The first modified (on disk) save is first
        /// </summary>
        FirstModifiedFirst,

        /// <summary>
        ///   Whatever file the filesystem API gives us is first
        /// </summary>
        FileSystem,
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
    ///   Auto save the game (if enabled in settings)
    /// </summary>
    public static void AutoSave(MicrobeStage state)
    {
        if (!Settings.Instance.AutoSaveEnabled)
            return;

        InternalSaveHelper(SaveInformation.SaveType.AutoSave, MainGameState.MicrobeStage, save =>
        {
            save.SavedProperties = state.CurrentGame;
            save.MicrobeStage = state;
        }, () => state);
    }

    public static void AutoSave(MicrobeEditor state)
    {
        if (!Settings.Instance.AutoSaveEnabled)
            return;

        InternalSaveHelper(SaveInformation.SaveType.AutoSave, MainGameState.MicrobeEditor, save =>
        {
            save.SavedProperties = state.CurrentGame;
            save.MicrobeEditor = state;
        }, () => state);
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
    ///   Loads the save file with the latest write time
    /// </summary>
    public static void QuickLoad()
    {
        // TODO: is there a way to to find the latest modified file without checking them all?
        var save = CreateListOfSaves(SaveOrder.LastModifiedFirst).FirstOrDefault();

        if (save == null)
        {
            GD.Print("No saves exist, can't quick load");
            return;
        }

        LoadSave(save);
    }

    /// <summary>
    ///   Returns a list of all saves
    /// </summary>
    /// <returns>The list of save names</returns>
    public static List<string> CreateListOfSaves(SaveOrder order = SaveOrder.LastModifiedFirst)
    {
        var result = new List<string>();

        using (var directory = new Directory())
        {
            if (!directory.DirExists(Constants.SAVE_FOLDER))
                return result;

            directory.Open(Constants.SAVE_FOLDER);
            directory.ListDirBegin(true, true);

            while (true)
            {
                var filename = directory.GetNext();

                if (string.IsNullOrEmpty(filename))
                    break;

                if (!filename.EndsWith(Constants.SAVE_EXTENSION, StringComparison.Ordinal))
                    continue;

                result.Add(filename);
            }

            directory.ListDirEnd();
        }

        switch (order)
        {
            case SaveOrder.LastModifiedFirst:
            {
                using (var file = new File())
                {
                    result = result.OrderByDescending(item =>
                        file.GetModifiedTime(PathUtils.Join(Constants.SAVE_FOLDER, item))).ToList();
                }

                break;
            }

            case SaveOrder.FirstModifiedFirst:
            {
                using (var file = new File())
                {
                    result = result.OrderBy(item =>
                        file.GetModifiedTime(PathUtils.Join(Constants.SAVE_FOLDER, item))).ToList();
                }

                break;
            }
        }

        return result;
    }

    /// <summary>
    ///   Counts the total number of saves and how many bytes they take up
    /// </summary>
    public static (int count, long diskSpace) CountSaves()
    {
        int count = 0;
        long totalSize = 0;

        using (var file = new File())
        {
            foreach (var save in CreateListOfSaves())
            {
                file.Open(PathUtils.Join(Constants.SAVE_FOLDER, save), File.ModeFlags.Read);
                ++count;
                totalSize += file.GetLen();
            }
        }

        return (count, totalSize);
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

    public static void DeleteExcessSaves(string nameStartsWith, int maximumCount)
    {
        var currentSaveNames = new List<string>();
        var allSaveNames = CreateListOfSaves(SaveOrder.FirstModifiedFirst);

        foreach (var save in allSaveNames)
        {
            if (save.StartsWith(nameStartsWith, StringComparison.CurrentCulture))
                currentSaveNames.Add(save);

            if (currentSaveNames.Count > maximumCount && currentSaveNames.Count > 0)
            {
                GD.Print("Found more ", nameStartsWith, " files than specified in settings; ",
                    "deleting current oldest ", nameStartsWith, " file: ", currentSaveNames[0]);
                DeleteSave(currentSaveNames[0]);
                currentSaveNames.RemoveAt(0);
            }
        }
    }

    private static void InternalSaveHelper(SaveInformation.SaveType type, MainGameState gameState,
        Action<Save> copyInfoToSave, Func<Node> stateRoot, string saveName = null)
    {
        new InProgressSave(type, stateRoot, data =>
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

        if (inProgress.Type == SaveInformation.SaveType.AutoSave)
            QueueRemoveExcessAutoSaves();

        if (inProgress.Type == SaveInformation.SaveType.QuickSave)
            QueueRemoveExcessQuickSaves();
    }

    /// <summary>
    ///   Runs a background task for removing excess auto saves
    /// </summary>
    private static void QueueRemoveExcessAutoSaves()
    {
        TaskExecutor.Instance.AddTask(new Task(() => DeleteExcessSaves("auto_save", Settings.Instance.MaxAutoSaves)));
    }

    /// <summary>
    ///   Runs a background task for removing excess quick saves
    /// </summary>
    private static void QueueRemoveExcessQuickSaves()
    {
        TaskExecutor.Instance.AddTask(new Task(() => DeleteExcessSaves("quick_save", Settings.Instance.MaxQuickSaves)));
    }
}
