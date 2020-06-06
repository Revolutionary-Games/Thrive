using System;
using System.Collections.Generic;
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
    /// <param name="stage">Data to include in save</param>
    public static void QuickSave(MicrobeStage stage)
    {
        var stopwatch = Stopwatch.StartNew();
        var save = CreateSaveObject(MainGameState.MicrobeStage, SaveInformation.SaveType.QuickSave);

        save.SavedProperties = stage.CurrentGame;
        save.MicrobeStage = stage;

        PerformSave(save, SaveInformation.SaveType.QuickSave, stopwatch);
    }

    /// <summary>
    ///   Quick save from the microbe editor
    /// </summary>
    /// <param name="editor">Data to include in save</param>
    public static void QuickSave(MicrobeEditor editor)
    {
        var stopwatch = Stopwatch.StartNew();
        var save = CreateSaveObject(MainGameState.MicrobeEditor, SaveInformation.SaveType.QuickSave);

        save.SavedProperties = editor.CurrentGame;
        save.MicrobeEditor = editor;

        PerformSave(save, SaveInformation.SaveType.QuickSave, stopwatch);
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
        LoadSave(GetLastSavegame(false));
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
        DisplaySaveStatusMessage(true, "Load finished", stopwatch);
    }

    private static Save CreateSaveObject(MainGameState gameState, SaveInformation.SaveType type)
    {
        return new Save
        {
            GameState = gameState, Info = { Type = type },
            Screenshot = ScreenShotTaker.Instance.TakeScreenshot(),
        };
    }

    private static string BuildQuicksaveFilename(uint quicksaveId)
    {
        return $"quick_save_{quicksaveId}.{Constants.SAVE_EXTENSION}";
    }

    /// <summary>
    ///   Returns the next quicksave-id to be used
    /// </summary>
    private static uint GetNextQuicksaveId()
    {
        var files = GetSaves();
        for (uint i = 0; i < Constants.SAVE_MAX_QUICKSAVES_BEFORE_OVERRIDING_OLD_ONES; i++)
        {
            if (!files.Contains(BuildQuicksaveFilename(i)))
            {
                // Not all 5 quicksaves have been used
                return i;
            }
        }

        // All 5 quicksaves have been used
        var filename = GetLastSavegame(true);
        return (uint.Parse(
                        filename.Substr("quick_save_".Length,
                        filename.Length - Constants.SAVE_EXTENSION.Length - "quick_save_".Length - 1),
                        System.Globalization.CultureInfo.InvariantCulture)
                    + 1) % (Constants.SAVE_MAX_QUICKSAVES_BEFORE_OVERRIDING_OLD_ONES+1);
    }

    private static string GetLastModifiedFile(List<string> filesToCheck)
    {
        return filesToCheck.ToDictionary(p => p, p =>
        {
            using (var file = new File())
            {
                return file.GetModifiedTime(PathUtils.Join(Constants.SAVE_FOLDER, p));
            }
        }).Aggregate((a, b) => a.Value > b.Value ? a : b).Key;
    }

    /// <summary>
    ///   Get the latest modified save in the saves folder
    /// </summary>
    private static string GetLastSavegame(bool onlyQuicksaves)
    {
        var saves = GetSaves().ToList();
        if (onlyQuicksaves)
        {
            saves = saves.FindAll(p => p.StartsWith("quick_save_", StringComparison.Ordinal));
        }

        return GetLastModifiedFile(saves);
    }

    /// <summary>
    ///   Gets all savegames in the saves folder
    /// </summary>
    private static IEnumerable<string> GetSaves()
    {
        using (var directory = new Directory())
        {
            directory.Open(Constants.SAVE_FOLDER);
            directory.ListDirBegin();
            string filename = string.Empty;

            while (true)
            {
                filename = directory.GetNext();
                if (string.IsNullOrEmpty(filename))
                {
                    directory.ListDirEnd();
                    break;
                }

                yield return filename;
            }
        }
    }

    private static void PerformSave(Save save, SaveInformation.SaveType type, Stopwatch stopwatch)
    {
        // TODO: implement type naming
        var name = BuildQuicksaveFilename(GetNextQuicksaveId());
        save.Name = name;

        try
        {
            save.SaveToFile();
            DisplaySaveStatusMessage(true, name, stopwatch);
        }
        catch (Exception e)
        {
            DisplaySaveStatusMessage(false, "Error, an exception happened: " + e, stopwatch);
            return;
        }

        if (type == SaveInformation.SaveType.QuickSave)
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
    private static void DisplaySaveStatusMessage(bool success, string message, Stopwatch stopwatch)
    {
        stopwatch.Stop();
        GD.Print("save/load finished, success: ", success, " message: ", message, " elapsed: ", stopwatch.Elapsed);
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
