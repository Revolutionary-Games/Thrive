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
    /// <summary>
    ///   This is a list of known versions where save compatibility is very broken and loading needs to be prevented
    ///   (unless there exists a version converter)
    /// </summary>
    private static readonly List<string> KnownSaveIncompatibilityPoints = new List<string>
    {
        "0.5.3.0",
        "0.5.3.1",
        "0.5.5.0-alpha",
    };

    private static DateTime? lastSave;

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
    ///   Checks whether the last save is made within a timespan of set duration.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Used for knowing whether to show confirmation dialog on the pause menu when exiting the game.
    ///     TODO: Implement a method overriding this flag as old in some way.
    ///   </para>
    /// </remarks>
    /// <returns>True if the last save is still recent, false if otherwise.</returns>
    public static bool SavedRecently => lastSave != null ? DateTime.Now - lastSave < Constants.RecentSaveTime : false;

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
        if (InProgressLoad.IsLoading || InProgressSave.IsSaving)
        {
            GD.PrintErr("Can't load a save while a load or save is in progress");
            return;
        }

        GD.Print("Starting load of save: ", name);
        new InProgressLoad(name).Start();
    }

    /// <summary>
    ///   Loads the save file with the latest write time.
    ///   Does not load if there is a version difference.
    /// </summary>
    /// <returns>False if the versions do not match</returns>
    public static bool QuickLoad()
    {
        // TODO: is there a way to to find the latest modified file without checking them all?
        var save = CreateListOfSaves(SaveOrder.LastModifiedFirst).FirstOrDefault();
        if (save == null)
        {
            GD.Print("No saves exist, can't quick load");
            return true;
        }

        SaveInformation info;
        try
        {
            info = global::Save.LoadJustInfoFromSave(save);
        }
        catch (Exception e)
        {
            GD.PrintErr($"Cannot load save information for save {save}: {e}");
            return true;
        }

        var versionDiff = VersionUtils.Compare(info.ThriveVersion, Constants.Version);
        if (versionDiff != 0)
            return false;

        LoadSave(save);
        return true;
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
                using var file = new File();
                result = result.OrderByDescending(item =>
                    file.GetModifiedTime(PathUtils.Join(Constants.SAVE_FOLDER, item))).ToList();

                break;
            }

            case SaveOrder.FirstModifiedFirst:
            {
                using var file = new File();
                result = result.OrderBy(item =>
                    file.GetModifiedTime(PathUtils.Join(Constants.SAVE_FOLDER, item))).ToList();

                break;
            }
        }

        return result;
    }

    /// <summary>
    ///   Counts the total number of saves and how many bytes they take up
    /// </summary>
    public static (int Count, long DiskSpace) CountSaves(string nameStartsWith = null)
    {
        int count = 0;
        long totalSize = 0;

        using var file = new File();
        foreach (var save in CreateListOfSaves())
        {
            if (nameStartsWith == null || save.StartsWith(nameStartsWith, StringComparison.CurrentCulture))
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
        using var directory = new Directory();
        directory.Remove(PathUtils.Join(Constants.SAVE_FOLDER, saveName));
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

    /// <summary>
    ///   Deletes all saves with the given prefix except the latest one and returns the list of saves deleted
    /// </summary>
    public static List<string> CleanUpOldSavesOfType(string nameStartsWith)
    {
        bool isLatestSave = true;
        var savesDeleted = new List<string>();

        foreach (var save in CreateListOfSaves())
        {
            if (save.StartsWith(nameStartsWith, StringComparison.CurrentCulture))
            {
                if (isLatestSave)
                {
                    isLatestSave = false;
                }
                else
                {
                    savesDeleted.Add(save);
                    DeleteSave(save);
                }
            }
        }

        return savesDeleted;
    }

    /// <summary>
    ///   Returns true if the specified version is known to be incompatible
    ///   from list in KnownSaveIncompatibilityPoints
    /// </summary>
    /// <param name="saveVersion">The save's version to check</param>
    /// <returns>True if certainly incompatible</returns>
    public static bool IsKnownIncompatible(string saveVersion)
    {
        int currentVersionPlaceInList = -1;
        int savePlaceInList = -1;

        var current = Constants.Version;

        for (int i = 0; i < KnownSaveIncompatibilityPoints.Count; ++i)
        {
            var version = KnownSaveIncompatibilityPoints[i];

            bool anyMatched = false;

            var currentDifference = VersionUtils.Compare(current, version);
            var saveDifference = VersionUtils.Compare(saveVersion, version);

            if (currentDifference >= 0)
            {
                anyMatched = true;
                currentVersionPlaceInList = i;
            }

            if (saveDifference >= 0)
            {
                anyMatched = true;
                savePlaceInList = i;
            }

            if (!anyMatched)
                break;
        }

        // If the current version and the save version don't fit in the same place in the save breakage points list
        // the save is either older or newer than the closes save breakage point to the current version.
        // Basically if numbers don't match, we know that the save is incompatible.
        return currentVersionPlaceInList != savePlaceInList;
    }

    /// <summary>
    ///   Marks the last save time to the time this method is called in.
    /// </summary>
    public static void MarkLastSaveToCurrentTime()
    {
        lastSave = DateTime.Now;
    }

    private static void InternalSaveHelper(SaveInformation.SaveType type, MainGameState gameState,
        Action<Save> copyInfoToSave, Func<Node> stateRoot, string saveName = null)
    {
        if (InProgressLoad.IsLoading || InProgressSave.IsSaving)
        {
            GD.PrintErr("Can't start save while a load or save is in progress");
            return;
        }

        new InProgressSave(type, stateRoot, data =>
                CreateSaveObject(gameState, data.Type),
            (inProgress, save) =>
            {
                copyInfoToSave.Invoke(save);

                if (PreventSavingIfExtinct(inProgress, save))
                    return;

                PerformSave(inProgress, save);
            }, saveName).Start();
    }

    private static Save CreateSaveObject(MainGameState gameState, SaveInformation.SaveType type)
    {
        return new Save
        {
            GameState = gameState,
            Info = { Type = type },
            Screenshot = ScreenShotTaker.Instance.GetViewportTextureAsImage(),
        };
    }

    private static bool PreventSavingIfExtinct(InProgressSave inProgress, Save save)
    {
        if (!save.SavedProperties.GameWorld.PlayerSpecies.IsExtinct)
            return false;

        inProgress.ReportStatus(false, TranslationServer.Translate("SAVING_NOT_POSSIBLE"),
            TranslationServer.Translate("PLAYER_EXTINCT"), false);
        return true;
    }

    private static void PerformSave(InProgressSave inProgress, Save save)
    {
        try
        {
            save.SaveToFile();
            inProgress.ReportStatus(true, TranslationServer.Translate("SAVING_SUCCEEDED"));
        }
        catch (Exception e)
        {
            // ReSharper disable HeuristicUnreachableCode ConditionIsAlwaysTrueOrFalse
            if (!Constants.CATCH_SAVE_ERRORS)
#pragma warning disable 162
                throw;
#pragma warning restore 162

            inProgress.ReportStatus(false, TranslationServer.Translate("SAVING_FAILED"),
                e.ToString());
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
        TaskExecutor.Instance.AddTask(new Task(() =>
            DeleteExcessSaves("quick_save", Settings.Instance.MaxQuickSaves)));
    }
}
