using System.Collections.Generic;
using Godot;

/// <summary>
///   Autoloads Mods from the Settings and handles the mods that failed to load
///   so that we can show a popup for the player
/// </summary>
public class AutoModLoader : Node
{
    private List<ModInfo> failedToLoadMods;

    private AutoModLoader()
    {
        ModLoader loader = new ModLoader();

        var autoLoadedModList = Settings.Instance.AutoLoadedMods.ToArray();
        failedToLoadMods = loader.LoadModFromList(autoLoadedModList, false, false, false);
    }

    public void OpenModErrorPopup(AcceptDialog errorBox)
    {
        if (failedToLoadMods.Count != 0)
        {
            errorBox.DialogText = GetFailedMods();
            errorBox.PopupCenteredMinsize();
        }
    }

    public string GetFailedMods()
    {
        if (failedToLoadMods.Count != 0)
        {
            string text = "The Following Mods Failed To Load: \n \n";
            foreach (ModInfo currentModInfo in failedToLoadMods)
            {
                text += currentModInfo.Name + "\n";
            }

            return text;
        }

        return string.Empty;
    }
}
