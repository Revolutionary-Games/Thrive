using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using Directory = Godot.Directory;
using File = Godot.File;
using Path = System.IO.Path;

/// <summary>
///   Helper functions for making the places in code dealing with saves shorter
/// </summary>
public static class SaveHelper
{
    /// <summary>
    ///   This is a list of known versions where save compatibility is very broken and loading needs to be prevented
    ///   (unless there exists a version converter)
    /// </summary>
    private static readonly List<string> KnownSaveIncompatibilityPoints = new()
    {
        "0.5.3.0",
        "0.5.3.1",
        "0.5.5.0-alpha",
        "0.5.9.0-alpha",
        "0.6.4.0-alpha",
    };

    private static readonly IReadOnlyList<MainGameState> StagesAllowingPrototypeSaving = new[]
    {
        MainGameState.MicrobeStage,
    };

    private static DateTime? lastSave;

    public enum SaveOrder
    {
        /// <summary>
        ///   The last modified (on disk) save is first
        /// </summary>
        LastModifiedFirst,

        /// <summary>
        ///   The first modified (on disk) save is first (oldest first)
        /// </summary>
        FirstModifiedFirst,

        /// <summary>
        ///   Whatever file the filesystem API gives us is first
        /// </summary>
        FileSystem,
    }

    /// <summary>
    ///   The error that is returned after trying to perform a quick load.
    /// </summary>
    public enum QuickLoadError
    {
        /// <summary>
        ///   No error, quick load is successful (or error is not handled).
        /// </summary>
        None,

        /// <summary>
        ///   The loaded version does not match the current game version.
        /// </summary>
        VersionMismatch,

        /// <summary>
        ///   Quick load is currently prevented by the current state of the game.
        /// </summary>
        NotAllowed,
    }

    /// <summary>
    ///   Checks whether the last save is made within a timespan of set duration.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Used for knowing whether to show confirmation dialog on the pause menu when exiting the game.
    ///   </para>
    /// </remarks>
    /// <returns>True if the last save is still recent, false if otherwise.</returns>
    public static bool SavedRecently => lastSave != null ? DateTime.Now - lastSave < Constants.RecentSaveTime : false;

    /// <summary>
    ///   Determines whether it's allowed to perform quick save and quick load, if set to false they will be disabled.
    /// </summary>
    public static bool AllowQuickSavingAndLoading { get; set; } = true;

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
    ///   Does not load if there is a version difference or if quick load is not allowed.
    /// </summary>
    /// <returns>See <see cref="QuickLoadError"/>.</returns>
    public static QuickLoadError QuickLoad()
    {
        if (!AllowQuickSavingAndLoading)
            return QuickLoadError.NotAllowed;

        // TODO: is there a way to to find the latest modified file without checking them all?
        var save = CreateListOfSaves(SaveOrder.LastModifiedFirst).FirstOrDefault();
        if (save == null)
        {
            GD.Print("No saves exist, can't quick load");
            return QuickLoadError.None;
        }

        SaveInformation info;
        try
        {
            info = global::Save.LoadJustInfoFromSave(save);
        }
        catch (Exception e)
        {
            GD.PrintErr($"Cannot load save information for save {save}: {e}");
            return QuickLoadError.None;
        }

        var versionDiff = VersionUtils.Compare(info.ThriveVersion, Constants.Version);
        if (versionDiff != 0)
            return QuickLoadError.VersionMismatch;

        LoadSave(save);
        return QuickLoadError.None;
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

                // Skip folders
                if (!directory.FileExists(filename))
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
                result = result.OrderByDescending(s =>
                    file.GetModifiedTime(Path.Combine(Constants.SAVE_FOLDER, s))).ToList();

                break;
            }

            case SaveOrder.FirstModifiedFirst:
            {
                using var file = new File();
                result = result.OrderBy(s =>
                    file.GetModifiedTime(Path.Combine(Constants.SAVE_FOLDER, s))).ToList();

                break;
            }
        }

        return result;
    }

    /// <summary>
    ///   Counts the total number of saves matching the given regular expression and how many bytes they take up
    /// </summary>
    public static (int Count, ulong DiskSpace) CountSaves(Regex? nameMatches = null)
    {
        int count = 0;
        ulong totalSize = 0;

        using var file = new File();
        foreach (var save in CreateListOfSaves())
        {
            if (nameMatches?.IsMatch(save) != false)
            {
                if (file.Open(Path.Combine(Constants.SAVE_FOLDER, save), File.ModeFlags.Read) != Error.Ok)
                {
                    GD.PrintErr("Can't read size of save file: ", save);
                    continue;
                }

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
        var finalPath = Path.Combine(Constants.SAVE_FOLDER, saveName);
        directory.Remove(finalPath);

        if (directory.FileExists(finalPath))
            throw new IOException($"Failed to delete: {finalPath}");
    }

    public static void DeleteExcessSaves(string nameStartsWith, int maximumCount)
    {
        var currentSaveNames = new List<string>();
        var allSaveNames = CreateListOfSaves(SaveOrder.FirstModifiedFirst);

        foreach (var save in allSaveNames)
        {
            if (!save.StartsWith(nameStartsWith, StringComparison.CurrentCulture))
                continue;

            currentSaveNames.Add(save);

            if (currentSaveNames.Count > maximumCount && currentSaveNames.Count > 0)
            {
                GD.Print("Found more ", nameStartsWith, " files than specified in settings; ",
                    "deleting current oldest ", nameStartsWith, " file: ", currentSaveNames[0]);
                try
                {
                    DeleteSave(currentSaveNames[0]);
                }
                catch (IOException e)
                {
                    GD.PrintErr(e.Message);
                }

                currentSaveNames.RemoveAt(0);
            }
        }
    }

    /// <summary>
    ///   Deletes all saves matching the given regex expression except the latest one if deleteLatest is false
    /// </summary>
    /// <returns>the list of saves deleted</returns>
    public static List<string> CleanUpOldSavesOfType(Regex nameMatches, bool deleteLatest = false)
    {
        var savesDeleted = new List<string>();

        foreach (var save in CreateListOfSaves())
        {
            if (nameMatches.IsMatch(save))
            {
                if (!deleteLatest)
                {
                    deleteLatest = true;
                }
                else
                {
                    savesDeleted.Add(save);
                    try
                    {
                        DeleteSave(save);
                    }
                    catch (IOException e)
                    {
                        GD.PrintErr(e.Message);
                    }
                }
            }
        }

        return savesDeleted;
    }

    public static void ShowErrorAboutPrototypeSaving(Node currentNode)
    {
        if (InProgressLoad.IsLoading || InProgressSave.IsSaving)
        {
            GD.PrintErr("Can't show message about being in a prototype while loading or saving");
            return;
        }

        new InProgressSave(SaveInformation.SaveType.Invalid, () => currentNode, _ =>
                new Save(),
            (inProgress, _) => { SetMessageAboutPrototypeSaving(inProgress); }, "invalid_prototype").Start();
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
    ///   Marks the last save time to the time this method is called at.
    /// </summary>
    public static void MarkLastSaveToCurrentTime()
    {
        lastSave = DateTime.Now;
    }

    /// <summary>
    ///   Sets the stored lastSave time value to null. Can be used to override
    ///   <see cref="SavedRecently"/> flag to false.
    /// </summary>
    public static void ClearLastSaveTime()
    {
        lastSave = null;
    }

    private static void InternalSaveHelper(SaveInformation.SaveType type, MainGameState gameState,
        Action<Save> copyInfoToSave, Func<Node> stateRoot, string? saveName = null)
    {
        if (type == SaveInformation.SaveType.QuickSave && !AllowQuickSavingAndLoading)
        {
            GD.Print("Can't save due to quick save being currently suppressed");
            return;
        }

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

                if (PreventSavingIfInPrototype(inProgress, save))
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
        if (save.SavedProperties == null)
        {
            GD.PrintErr("Can't check extinction before saving because save is missing game properties");
            try
            {
                throw new NullReferenceException();
            }
            catch (NullReferenceException e)
            {
                inProgress.ReportStatus(false, TranslationServer.Translate("SAVING_FAILED_WITH_EXCEPTION"),
                    e.ToString(), true);
                return true;
            }
        }

        if (!save.SavedProperties.GameWorld.PlayerSpecies.IsExtinct)
            return false;

        inProgress.ReportStatus(false, TranslationServer.Translate("SAVING_NOT_POSSIBLE"),
            TranslationServer.Translate("PLAYER_EXTINCT"), false);
        return true;
    }

    private static bool PreventSavingIfInPrototype(InProgressSave inProgress, Save save)
    {
        if (!save.SavedProperties!.InPrototypes)
            return false;

        if (StagesAllowingPrototypeSaving.Contains(save.GameState))
            return false;

        SetMessageAboutPrototypeSaving(inProgress);
        return true;
    }

    private static void SetMessageAboutPrototypeSaving(InProgressSave inProgressSave)
    {
        inProgressSave.ReportStatus(false, TranslationServer.Translate("SAVING_NOT_POSSIBLE"),
            TranslationServer.Translate("IN_PROTOTYPE"), false);
    }

    private static void PerformSave(InProgressSave inProgress, Save save)
    {
        // Ensure prototype state flag is also in the info data for use by the save list
        save.Info.IsPrototype = save.SavedProperties?.InPrototypes ??
            throw new InvalidOperationException("Saved properties of a save to write to disk is unset");

        try
        {
            save.SaveToFile();
            inProgress.ReportStatus(true, TranslationServer.Translate("SAVING_SUCCEEDED"));
        }
        catch (Exception e)
        {
#if DEBUG
            if (Debugger.IsAttached)
                Debugger.Break();
#endif

            // ReSharper disable HeuristicUnreachableCode ConditionIsAlwaysTrueOrFalse
            if (!Constants.CATCH_SAVE_ERRORS)
#pragma warning disable 162
                throw;
#pragma warning restore 162

            inProgress.ReportStatus(false, TranslationServer.Translate("SAVING_FAILED_WITH_EXCEPTION"),
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
