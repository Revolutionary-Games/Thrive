using System.Collections.Generic;
using Godot;

public class AutoModLoader : Node
{
    private List<ModInfo> failedToLoadMods = new List<ModInfo>();

    private AutoModLoader()
    {
        ModLoader loader = new ModLoader();

        var modList = loader.LoadModList(false);
        foreach (ModInfo currentModInfo in modList)
        {
            if (currentModInfo.AutoLoad)
            {
                if (loader.LoadMod(currentModInfo) < 0)
                {
                   failedToLoadMods.Add(currentModInfo);
                }
            }
        }
    }

    public void OpenModErrorPopup(AcceptDialog errorBox)
    {
        if (failedToLoadMods.Count != 0)
        {
            errorBox.DialogText = GetFailedMods();
            errorBox.PopupCentered();
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
