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
        Stopwatch stopwatch = Stopwatch.StartNew();
        var save = CreateSaveObject(SaveGameState.MicrobeStage, SaveInformation.SaveType.QuickSave);

        // TODO: save other properties as well
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
        Stopwatch stopwatch = Stopwatch.StartNew();
        var save = CreateSaveObject(SaveGameState.MicrobeEditor, SaveInformation.SaveType.QuickSave);

        // TODO: save other properties as well
        save.SavedProperties = editor.CurrentGame;
        save.MicrobeEditor = editor;
        save.MicrobeStage = editor.ReturnToStage;

        PerformSave(save, SaveInformation.SaveType.QuickSave, stopwatch);
    }

    /// <summary>
    ///   Loads the save file with the latest write time
    /// </summary>
    public static void QuickLoad()
    {
        throw new NotImplementedException();
    }

    private static Save CreateSaveObject(SaveGameState gameState, SaveInformation.SaveType type)
    {
        return new Save
        {
            GameState = gameState, Info = { Type = type },
            Screenshot = ScreenShotTaker.Instance.TakeScreenshot(),
        };
    }

    private static void PerformSave(Save save, SaveInformation.SaveType type, Stopwatch stopwatch)
    {
        // TODO: implement type naming
        var name = "quick_save.tar.gz";

        save.Name = name;

        // try
        // {
        save.SaveToFile();
        DisplaySaveStatusMessage(true, name, stopwatch);

        // }
        // catch (Exception e)
        // {
        //     DisplaySaveStatusMessage(false, "Error, an exception happened: " + e, stopwatch);
        //     return;
        // }

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
        GD.Print("save finished, success: ", success, " message: ", message, " elapsed: ", stopwatch.Elapsed);
    }

    /// <summary>
    ///   Runs a background task for removing excess quick saves
    /// </summary>
    private static void QueueRemoveExcessQuickSaves()
    {
        TaskExecutor.Instance.AddTask(new Task(() => { SaveManager.RemoveExcessQuickSaves(); }));
    }
}
