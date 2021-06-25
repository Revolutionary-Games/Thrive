using System.Collections.Generic;
using System.Reflection;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Class that manages all the loading, getting the mods from the directory, and other things
///   relating to mods
/// </summary>
public class ModLoader : Reference
{
    /// <summary>
    ///   The number that corresponds to the number returned by the loadMod function
    /// </summary>
    public enum ModStatus
    {
        ModFileCanNotBeFound = -2,
        FailedModLoading = -1,
        ModAlreadyBeenLoaded = 0,
        ModLoadedSuccessfully = 1,
    }

    /// <summary>
    ///   Mods that are loaded in by the auto mod loader
    ///   so the player don't have to reload them every time they start the game
    /// </summary>
    public static List<ModInfo> AutoLoadedMods { get; set; } = new List<ModInfo>();

    /// <summary>
    ///   Mods that have already been loaded including autoloaded mods too
    /// </summary>
    public static List<ModInfo> LoadedMods { get; private set; } = new List<ModInfo>();

    /// <summary>
    ///   Mods that have failed to load
    /// </summary>
    public static List<ModInfo> FailedToLoadMods { get; set; } = new List<ModInfo>();

    /// <summary>
    ///   This fetches the mods from the mod directory and then returns it
    /// </summary>
    public List<ModInfo> LoadModList(bool loadNonAutoloadedMods = true, bool loadAutoload = true)
    {
        FileHelpers.MakeSureDirectoryExists(Constants.MOD_FOLDER);
        var modFolder = new Directory();

        modFolder.Open(Constants.MOD_FOLDER);
        modFolder.ListDirBegin(true, true);

        List<ModInfo> modList = new List<ModInfo>();
        var currentMod = modFolder.GetNext();

        while (true)
        {
            if (string.IsNullOrEmpty(currentMod))
            {
                break;
            }

            var currentModInfo = GetModInfo(PathUtils.Join(Constants.MOD_FOLDER, currentMod));

            // Checks if the retrieval of ModInfo was valid
            if (currentModInfo.Name == null)
            {
                continue;
            }

            // It will load the mod if it not a autoloaded mod and loadNonAutoloadedMods is true
            // or if it is an autoloaded and loadAutoload is true
            if ((loadNonAutoloadedMods && !currentModInfo.AutoLoad) || (currentModInfo.AutoLoad && loadAutoload))
            {
                modList.Add(currentModInfo);
                currentMod = modFolder.GetNext();
            }
            else
            {
                currentMod = modFolder.GetNext();
            }
        }

        return modList;
    }

    /// <summary>
    ///   This is the method that actually loads the mod after verifying it and updating all the other variables
    /// </summary>
    /// <returns>
    ///   Returns a postive integer when successful and negative when it fails
    ///   -2: Returns when the mod file can not be found
    ///   -1: Returns when the mod file fails to be loaded
    ///    0: Returns when the mod is already loaded
    ///    1: Returns when the mod is successfully loaded
    /// </returns>
    public int LoadMod(ModInfo currentMod, bool addToAutoLoader = false, bool clearAutoloaderModList = true, bool clearFailedToLoadModsList = true)
    {
        var file = new File();

        if (clearAutoloaderModList)
        {
            AutoLoadedMods.Clear();
        }

        if (clearFailedToLoadModsList)
        {
            FailedToLoadMods.Clear();
        }

        if (addToAutoLoader && !AutoLoadedMods.Contains(currentMod))
        {
            AutoLoadedMods.Add(currentMod);
        }

        if (LoadedMods.Contains(currentMod))
        {
            currentMod.Status = 0;
            return 0;
        }

        if (!file.FileExists(currentMod.Location + "/mod.pck"))
        {
            GD.Print("Fail to find mod file: " + currentMod.Name);
            currentMod.Status = -2;
            FailedToLoadMods.Add(currentMod);
            return -2;
        }

        // Checks if a Dll file needs to be loaded
        if (!string.IsNullOrEmpty(currentMod.Dll))
        {
            if (file.FileExists(ProjectSettings.GlobalizePath(currentMod.Dll)))
            {
                GD.Print("NEW JERSEY: (" + currentMod.Dll + ").");
                // GD.Print("Loading...: " + Assembly.LoadFile(ProjectSettings.GlobalizePath(currentMod.Dll)).Location);
                GD.Print("DLL LOADED YES: (" + currentMod.Dll + ").");
            }
        }

        if (ProjectSettings.LoadResourcePack(currentMod.Location + "/mod.pck", true))
        {
            GD.Print("Loaded mod: " + currentMod.Name);
            LoadedMods.Add(currentMod);
            currentMod.Status = 1;
            return 1;
        }

        GD.Print("Failed to load mod: " + currentMod.Name);
        currentMod.Status = -1;
        FailedToLoadMods.Add(currentMod);
        return -1;
    }

    /// <summary>
    ///   This loads multiple mods from a array
    /// </summary>
    public List<ModInfo> LoadModFromArray(ModInfo[] modsToLoad, bool ignoreAutoloaded = true,
        bool addToAutoLoader = false, bool clearAutoloaderModList = true, bool clearFailedToLoadModsList = true)
    {
        var failedModList = new List<ModInfo>();

        if (clearAutoloaderModList)
        {
            AutoLoadedMods.Clear();
        }

        if (clearFailedToLoadModsList)
        {
            FailedToLoadMods.Clear();
        }

        foreach (ModInfo currentMod in modsToLoad)
        {
            if (currentMod.AutoLoad && ignoreAutoloaded)
            {
                if (addToAutoLoader)
                {
                    AutoLoadedMods.Add(currentMod);
                }

                continue;
            }

            if (LoadMod(currentMod, addToAutoLoader, false, false) < 0)
            {
                failedModList.Add(currentMod);
            }
        }

        // Returns the mods that failed to load from the list
        return failedModList;
    }

    /// <summary>
    ///   This loads multiple mods from a List
    /// </summary>
    public List<ModInfo> LoadModFromList(List<ModInfo> modsToLoad, bool ignoreAutoloaded = true,
        bool addToAutoLoader = false, bool clearAutoloaderModList = true, bool clearFailedToLoadModsList = true)
    {
        var failedModList = new List<ModInfo>();

        if (clearAutoloaderModList)
        {
            AutoLoadedMods.Clear();
        }

        if (clearFailedToLoadModsList)
        {
            FailedToLoadMods.Clear();
        }

        foreach (ModInfo currentMod in modsToLoad)
        {
            if (currentMod.AutoLoad && ignoreAutoloaded)
            {
                if (addToAutoLoader)
                {
                    AutoLoadedMods.Add(currentMod);
                }

                continue;
            }

            if (LoadMod(currentMod, addToAutoLoader, false, false) < 0)
            {
                failedModList.Add(currentMod);
            }
        }

        // Returns the mods that failed to load from the list
        return failedModList;
    }

    /// <summary>
    ///   This loads multiple mods from a ItemList, Mostly use for the ModManagerUI
    /// </summary>
    public List<ModInfo> LoadModFromList(ItemList modsToLoad, bool ignoreAutoloaded = true,
        bool addToAutoLoader = false, bool clearAutoloaderModList = true, bool clearFailedToLoadModsList = true)
    {
        var modListCount = modsToLoad.GetItemCount();
        var failedModList = new List<ModInfo>();

        if (clearAutoloaderModList)
        {
            AutoLoadedMods.Clear();
        }

        if (clearFailedToLoadModsList)
        {
            FailedToLoadMods.Clear();
        }

        for (int index = 0; index < modListCount; index++)
        {
            var currentMod = (ModInfo)modsToLoad.GetItemMetadata(index);
            if (currentMod.AutoLoad && ignoreAutoloaded)
            {
                if (addToAutoLoader)
                {
                    AutoLoadedMods.Add(currentMod);
                }

                continue;
            }

            if (LoadMod(currentMod, addToAutoLoader, false, false) < 0)
            {
                failedModList.Add(currentMod);
            }
        }

        // Returns the mods that failed to load from the list
        return failedModList;
    }

    /// <summary>
    ///   This get a ModInfo from a directory and then returns it
    /// </summary>
    public ModInfo GetModInfo(string location)
    {
        var file = new File();
        if (!file.FileExists(location + "/mod_info.json"))
        {
            return new ModInfo();
        }

        var currentModInfo =
            JsonConvert.DeserializeObject<ModInfo>(ReadJSONFile(location + "/mod_info.json"));

        currentModInfo.Location = location;
        if (file.FileExists(location + "/icon.png") || file.FileExists(location + "/icon.jpg"))
        {
            var iconTexture = new ImageTexture();
            var iconImage = new Image();
            if (iconImage.Load(location + "/icon.png") != 0)
            {
                iconImage.Load(location + "/icon.jpg");
            }

            iconTexture.CreateFromImage(iconImage);
            currentModInfo.IconImage = iconTexture;
        }

        if (file.FileExists(location + "/preview.png") || file.FileExists(location + "/preview.jpg"))
        {
            var previewTexture = new ImageTexture();
            var previewImage = new Image();
            if (previewImage.Load(location + "/preview.png") != 0)
            {
                previewImage.Load(location + "/preview.jpg");
            }

            previewTexture.CreateFromImage(previewImage);
            currentModInfo.PreviewImage = previewTexture;
        }

        return currentModInfo;
    }

    /// <summary>
    ///   This saves the 'AutoLoadedMods' list to a file
    /// </summary>
    /// <returns>True on success, false if the file can't be written.</returns>
    /// <remarks>
    ///   This was based on the Save method from Settings.cs
    /// </remarks>
    public bool SaveAutoLoadedModsList()
    {
        using var file = new File();
        var error = file.Open(Constants.MOD_CONFIGURATION, File.ModeFlags.Write);

        if (error != Error.Ok)
        {
            GD.PrintErr("Couldn't open mod configuration file for writing.");
            return false;
        }

        file.StoreString(JsonConvert.SerializeObject(AutoLoadedMods, Formatting.Indented));

        file.Close();

        return true;
    }

    /// <summary>
    ///   This loads the 'AutoLoadedMods' list from a file
    /// </summary>
    /// <returns>True on success, false if the file can't be loaded.</returns>
    public bool LoadAutoLoadedModsList()
    {
        var modFileContent = ReadJSONFile(Constants.MOD_CONFIGURATION);
        var autoModList =
            JsonConvert.DeserializeObject<List<ModInfo>>(modFileContent);

        if (autoModList != null)
        {
            AutoLoadedMods = autoModList;
            return true;
        }

        return false;
    }

    /// <summary>
    ///   This resets the game by clearing the mod list in the settings file
    /// </summary>
    public void ResetGame()
    {
        AutoLoadedMods.Clear();
        SaveAutoLoadedModsList();
    }

    private static string ReadJSONFile(string path)
    {
        using (var file = new File())
        {
            file.Open(path, File.ModeFlags.Read);
            var result = file.GetAsText();

            // This might be completely unnecessary
            file.Close();

            return result;
        }
    }
}
