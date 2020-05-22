using System;
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
        var save = CreateSaveObject("MicrobeStage", SaveInformation.SaveType.QuickSave);

        // TODO: save other properties as well
        save.SavedProperties = stage.CurrentGame;

        PerformSave(save, SaveInformation.SaveType.QuickSave);
    }

    /// <summary>
    ///   Loads the save file with the latest write time
    /// </summary>
    public static void QuickLoad()
    {
        throw new NotImplementedException();
    }

    private static Save CreateSaveObject(string gameState, SaveInformation.SaveType type)
    {
        return new Save
        {
            GameStateName = gameState, Info = { Type = type },
            Screenshot = ScreenShotTaker.Instance.TakeScreenshot(),
        };
    }

    private static void PerformSave(Save save, SaveInformation.SaveType type)
    {
        // TODO: implement type naming
        var name = "quick_save.tar.gz";

        save.Name = name;

        // try
        // {
        save.SaveToFile();
        DisplaySaveStatusMessage(true, name);

        // }
        // catch (Exception e)
        // {
        //     DisplaySaveStatusMessage(false, "Error, an exception happened: " + e);
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
    /// <remarks>
    ///   TODO: implement this
    /// </remarks>
    private static void DisplaySaveStatusMessage(bool success, string message)
    {
        GD.Print("save finished, success: ", success, " message: ", message);
    }

    /// <summary>
    ///   Runs a background task for removing excess quick saves
    /// </summary>
    private static void QueueRemoveExcessQuickSaves()
    {
        TaskExecutor.Instance.AddTask(new Task(() => { SaveManager.RemoveExcessQuickSaves(); }));
    }
}
