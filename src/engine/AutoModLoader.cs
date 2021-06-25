using System;
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
        // Load in the AutoLoadedMods and checks if the AutoLoadedMods List is not empty
        if (loader.LoadAutoLoadedModsList())
        {
            var autoLoadedModList = ModLoader.AutoLoadedMods;
            loader.LoadModFromList(autoLoadedModList, false, false, false);
        }
    }

    public void OpenModErrorPopup(AcceptDialog errorBox)
    {
        errorBox.DialogText = TranslationServer.Translate("MOD_LOAD_FAILURE_TEXT") + "\n\n";

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
