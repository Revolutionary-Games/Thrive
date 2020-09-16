using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Godot;
using Newtonsoft.Json;
using Directory = System.IO.Directory;
using File = System.IO.File;

public class ModLoader
{
    public List<ModInfo> LoadModList(bool ignoreAutoloaded = true)
    {
        DirectoryInfo modFolder = Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}\\mods");

        List<ModInfo> modList = new List<ModInfo>();
        foreach (DirectoryInfo currentMod in modFolder.EnumerateDirectories())
        {
            if (!File.Exists(currentMod.FullName + "/mod_info.json"))
            {
                continue;
            }

            var currentModInfo =
                JsonConvert.DeserializeObject<ModInfo>(ReadJSONFile(currentMod.FullName + "/mod_info.json"));

            if (currentModInfo.AutoLoad && ignoreAutoloaded)
            {
                continue;
            }

            currentModInfo.Location = currentMod.FullName;
            modList.Add(currentModInfo);
        }

        return modList;
    }

    public int LoadMod(ModInfo currentMod)
    {
            if (string.IsNullOrEmpty(currentMod.Dll))
            {
                if (File.Exists(ProjectSettings.GlobalizePath(currentMod.Dll)))
                {
                    Assembly.LoadFile(ProjectSettings.GlobalizePath(currentMod.Dll));
                }
            }

            if (!File.Exists(currentMod.Location + "/mod.pck"))
            {
                GD.Print("Fail to find mod file: " + currentMod.Name);
                return -2;
            }

            if (ProjectSettings.LoadResourcePack(currentMod.Location + "/mod.pck", true))
            {
                GD.Print("Loaded mod: " + currentMod.Name);
                return 1;
            }
            else
            {
                GD.Print("Failed to load mod: " + currentMod.Name);
                return -1;
            }
    }

    public void LoadModFromList(ModInfo[] modsToLoad)
    {
        foreach (ModInfo currentMod in modsToLoad)
        {
            LoadMod(currentMod);
        }
    }

    public void LoadModFromList(ItemList modsToLoad)
    {
        int modListCount = modsToLoad.GetItemCount();
        for (int index = 0; index < modListCount; index++)
        {
            LoadMod((ModInfo)modsToLoad.GetItemMetadata(index));
        }
    }

    public void ResetGame()
    {
        GUICommon.Instance.PlayButtonPressSound();
        if (!File.Exists(Directory.GetCurrentDirectory() + "/Thrive.pck"))
        {
            GD.Print("Fail to find Thrive");
            return;
        }

        if (ProjectSettings.LoadResourcePack(Directory.GetCurrentDirectory() + "/Thrive.pck", true))
        {
            GD.Print("Reset successful");
        }
        else
        {
            GD.Print("Reset failed");
        }

        SceneManager.Instance.ReturnToMenu();
    }

    // Copied From The PauseMenu.cs
    private static string ReadJSONFile(string path)
    {
        using (var file = new Godot.File())
        {
            file.Open(path, Godot.File.ModeFlags.Read);
            var result = file.GetAsText();

            // This might be completely unnecessary
            file.Close();

            return result;
        }
    }
}
