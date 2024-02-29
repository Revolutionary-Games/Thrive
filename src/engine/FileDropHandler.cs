using System;
using Godot;

/// <summary>
///   Handles signal from Godot when files are dragged and dropped onto the game window
/// </summary>
public partial class FileDropHandler : Node
{
    public override void _Ready()
    {
        GetTree().Root.Connect(Window.SignalName.FilesDropped, new Callable(this, nameof(OnFilesDropped)));
    }

    private void OnFilesDropped(string[] files, int screen)
    {
        foreach (var file in files)
        {
            GD.Print("Detected file drop \"", file, "\" on screen ", screen);
            HandleFileDrop(file, screen);
        }
    }

    private void HandleFileDrop(string file, int screen)
    {
        // Currently nothing depends on this (this might be the game window index, at least on Linux this doesn't seem
        // to change when the monitor is changed the drag happens on...)
        _ = screen;

        // For now just the load save functionality is done, but in the future this might be extended to allow
        // other code to dynamically register listeners here

        if (file.EndsWith(Constants.SAVE_EXTENSION, StringComparison.InvariantCulture))
        {
            if (!file.StartsWith(Constants.EXPLICIT_PATH_PREFIX, StringComparison.InvariantCulture))
                file = Constants.EXPLICIT_PATH_PREFIX + file;

            // TODO: could add a dialog to ask if the save should be loaded now or copied to the saves folder
            HandleLoadSaveFromDrop(file);
        }
        else
        {
            GD.Print("Unknown file type to handle on drop");
        }
    }

    private void HandleLoadSaveFromDrop(string file)
    {
        GD.Print("Trying to load dropped save file...");

        // TODO: would be nice to have some kind of popup box showing the errors from here to the user
        try
        {
            var info = Save.LoadJustInfoFromSave(file);

            if (info.Type == SaveInformation.SaveType.Invalid)
            {
                GD.PrintErr("Given file (", file, ") is not a valid Thrive save");
                return;
            }

            if (SaveHelper.IsKnownIncompatible(info.ThriveVersion))
            {
                GD.Print("Dropped save file is known incompatible, not loading it");
                return;
            }

            if (info.ThriveVersion != Constants.Version)
            {
                // TODO: would be nice to show a confirmation dialog here
                GD.Print("The dropped save version is not exactly the same as current game version");
            }

            SaveHelper.LoadSave(file);
        }
        catch (Exception e)
        {
            GD.PrintErr("Exception while trying to load dropped save: ", e);
        }
    }
}
