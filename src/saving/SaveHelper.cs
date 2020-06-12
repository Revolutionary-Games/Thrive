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
        LoadSave(GetLastSaveGame(false));
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

    private static string BuildQuickSaveFilename(int quickSaveId)
    {
        return $"quick_save_{quickSaveId}.{Constants.SAVE_EXTENSION}";
    }

    /// <summary>
    ///   Returns the next quick save id to be used
    /// </summary>
    private static int GetNextQuickSaveId()
    {
        var files = GetSaves();
        var enumerable = files as string[] ?? files.ToArray();
        for (var i = 0; i < Settings.Instance.MaxQuickSavesBeforeOverriding; i++)
        {
            if (!enumerable.Contains(BuildQuickSaveFilename(i)))
            {
                // Not all 5 quick saves have been used
                return i;
            }
        }

        // All 5 quick saves have been used
        var filename = GetLastSaveGame(true);

        // Get the id out of the quick save file name.
        filename = filename.Substr("quick_save_".Length,
            filename.Length - Constants.SAVE_EXTENSION.Length - "quick_save_".Length - 1);

        var id = int.Parse(filename, System.Globalization.CultureInfo.InvariantCulture);

        // Get the next id. Overflow if MaxQuickSavesBeforeOverriding is reached.
        return (id + 1) % (Settings.Instance.MaxQuickSavesBeforeOverriding + 1);
    }

    /// <summary>
    ///   Get the latest modified save in the saves folder
    /// </summary>
    /// <returns>
    ///   Only filename, without path
    /// </returns>
    private static string GetLastSaveGame(bool onlyQuickSaves)
    {
        var saves = GetSaves();

        if (onlyQuickSaves)
        {
            saves = saves.Where(p => p.StartsWith("quick_save_", StringComparison.Ordinal))
                .Select(p => PathUtils.Join(Constants.SAVE_FOLDER,p));
        }

        var fullPath = FileHelpers.GetLastModifiedFile(saves);

        // Get filename of fullPath
        return fullPath.Substring(fullPath.LastIndexOf(PathUtils.PATH_SEPARATOR) + 1);
    }

    /// <summary>
    ///   Gets all save games in the saves folder
    /// </summary>
    private static IEnumerable<string> GetSaves()
    {
        using (var directory = new Directory())
        {
            if (!directory.DirExists(Constants.SAVE_FOLDER))
                yield break;

            directory.Open(Constants.SAVE_FOLDER);
            directory.ListDirBegin();
            while (true)
            {
                var filename = directory.GetNext();
                if (string.IsNullOrEmpty(filename))
                {
                    directory.ListDirEnd();
                    break;
                }

                yield return filename;
            }

            directory.ListDirEnd();
        }
    }

    private static void PerformSave(Save save, SaveInformation.SaveType type, Stopwatch stopwatch)
    {
        // TODO: implement type naming
        var name = BuildQuickSaveFilename(GetNextQuickSaveId());
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
