using Godot;

/// <summary>
///   Autoloads Mods from the Settings and handles the mods that failed to load
///   so that we can show a popup for the player
/// </summary>
public class AutoModLoader : Node
{
    private ModLoader loader = new ModLoader();

    private AutoModLoader()
    {
        var autoloadedModList = loader.LoadModList(false, true, false);
        if (autoloadedModList.Count > 0)
        {
            loader.LoadModFromList(autoloadedModList, false, false, false);
        }

        // Load in the AutoLoadedMods and checks if the AutoLoadedMods List is not empty
        if (loader.LoadReloadedModsList())
        {
            var reloadModList = ModLoader.ReloadedMods;
            loader.LoadModFromList(reloadModList, false, false, false);
        }
    }

    /// <summary>
    ///   This just opens the Error Popup in the main menu
    /// </summary>
    public void OpenModErrorPopup(AcceptDialog errorBox)
    {
        // errorBox.DialogText = TranslationServer.Translate("MOD_LOAD_FAILURE_TEXT") + "\n\n";
        errorBox.DialogText = "There was an error while trying to load the following mods:" + "\n\n";
        if (ModLoader.FailedToLoadMods.Count > 0)
        {
            var failedToLoadMods = ModLoader.FailedToLoadMods;

            foreach (ModInfo currentModInfo in failedToLoadMods)
            {
                errorBox.DialogText += currentModInfo.Name + "\n";
            }

            errorBox.PopupCenteredShrink();
        }
    }
}
