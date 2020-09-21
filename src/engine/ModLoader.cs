using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Godot;
using Newtonsoft.Json;
using Directory = System.IO.Directory;
using File = System.IO.File;

public class ModLoader
{
    public static List<ModInfo> LoadedMods = new List<ModInfo>();

    public List<ModInfo> LoadModList(bool ignoreAutoload = true)
    {
        DirectoryInfo modFolder = Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}\\mods");

        List<ModInfo> modList = new List<ModInfo>();
        foreach (DirectoryInfo currentMod in modFolder.EnumerateDirectories())
        {
            var currentModInfo = GetModInfo(currentMod.FullName);

            if (currentModInfo.Name == null)
            {
                continue;
            }

            if (currentModInfo.AutoLoad && ignoreAutoload)
            {
                continue;
            }

            modList.Add(currentModInfo);
        }

        return modList;
    }

    public ModInfo GetModInfo(string location)
    {
        if (!File.Exists(location + "/mod_info.json"))
        {
            return new ModInfo();
        }

        var currentModInfo =
            JsonConvert.DeserializeObject<ModInfo>(ReadJSONFile(location + "/mod_info.json"));

        currentModInfo.Location = location;
        return currentModInfo;
    }

    public int LoadMod(ModInfo currentMod, bool addToAutoLoader = false, bool clearAutoloaderModList = true)
    {
        var modSettingList = Settings.Instance.AutoLoadedMods;
        if (clearAutoloaderModList)
        {
            modSettingList.Clear();
        }

        if (addToAutoLoader && !modSettingList.Contains(currentMod))
        {
            modSettingList.Add(currentMod);
        }

        if (LoadedMods.Contains(currentMod))
        {
             return 0;
        }

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
            LoadedMods.Add(currentMod);
            return 1;
        }
        else
        {
            GD.Print("Failed to load mod: " + currentMod.Name);
            return -1;
        }
    }

    public List<ModInfo> LoadModFromList(ModInfo[] modsToLoad, bool ignoreAutoloaded = true, bool addToAutoLoader = false, bool clearAutoloaderModList = true)
    {
        var modSettingList = Settings.Instance.AutoLoadedMods;
        var failedModList = new List<ModInfo>();

        if (clearAutoloaderModList)
        {
            modSettingList.Clear();
        }

        foreach (ModInfo currentMod in modsToLoad)
        {
            if (currentMod.AutoLoad && ignoreAutoloaded)
            {
                if (addToAutoLoader)
                {
                    modSettingList.Add(currentMod);
                }

                continue;
            }

            if (LoadMod(currentMod, addToAutoLoader, false) < 0)
            {
                failedModList.Add(currentMod);
            }
        }

        return failedModList;
    }

    public List<ModInfo> LoadModFromList(ItemList modsToLoad, bool ignoreAutoloaded = true, bool addToAutoLoader = false, bool clearAutoloaderModList = true)
    {
        var modListCount = modsToLoad.GetItemCount();
        var modSettingList = Settings.Instance.AutoLoadedMods;
        var failedModList = new List<ModInfo>();

        if (clearAutoloaderModList)
        {
            modSettingList.Clear();
        }

        for (int index = 0; index < modListCount; index++)
        {
            var currentMod = (ModInfo)modsToLoad.GetItemMetadata(index);
            if (currentMod.AutoLoad && ignoreAutoloaded)
            {
                if (addToAutoLoader)
                {
                    modSettingList.Add(currentMod);
                }

                continue;
            }

            if (LoadMod(currentMod, addToAutoLoader, false) < 0)
            {
                failedModList.Add(currentMod);
            }
        }

        return failedModList;
    }

    public void ResetGame()
    {
        var modSettingList = Settings.Instance.AutoLoadedMods;
        modSettingList.Clear();
        Settings.Instance.Save();
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
