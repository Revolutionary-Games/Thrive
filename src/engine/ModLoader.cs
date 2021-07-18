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
    ///   The number that corresponds to the number returned by the isValidModList/isValidModArray function
    /// </summary>
    public enum CheckErrorStatus
    {
        InvalidLoadOrderAfter = -6,
        InvalidLoadOrderBefore = -5,
        IncompatibleMod = -4,
        InvalidDependencyOrder = -3,
        DependencyNotFound = -2,
        IncompatibleVersion = -1,
        Unknown = 0,
        Valid = 1,
        EmptyList = 2,
    }

    /// <summary>
    ///   Mods that are loaded in by the auto mod loader
    ///   so the player don't have to reload them every time they start the game
    /// </summary>
    public static List<ModInfo> ReloadedMods { get; set; } = new List<ModInfo>();

    /// <summary>
    ///   Mods that need to be loaded in on the game next start up
    ///   useful for mods that need to alter settings before everything else
    /// </summary>
    public static List<ModInfo> StartupMods { get; set; } = new List<ModInfo>();

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
    public List<ModInfo> LoadModList(bool loadRegularMods = true, bool loadAutoload = true, bool loadStartupMod = true)
    {
        FileHelpers.MakeSureDirectoryExists(Constants.MOD_FOLDER);
        var modFolder = new Directory();

        modFolder.Open(Constants.MOD_FOLDER);
        modFolder.ListDirBegin(true, true);

        List<ModInfo> modList = new List<ModInfo>();
        var currentMod = modFolder.GetNext();

        // Loops until it went through all the folders
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
            // or if it is an Startup Mod and loadStartupMod is true
            if ((loadRegularMods && !currentModInfo.AutoLoad && !currentModInfo.StartupMod) ||
                (currentModInfo.AutoLoad && loadAutoload) || (currentModInfo.StartupMod && loadStartupMod))
            {
                modList.Add(currentModInfo);
            }

            currentMod = modFolder.GetNext();
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
    public int LoadMod(ModInfo currentMod, bool addToReloadedModList = false, bool clearReloadedModList = true,
        bool clearFailedToLoadModsList = true)
    {
        var file = new File();

        if (clearReloadedModList)
        {
            ReloadedMods.Clear();
        }

        if (clearFailedToLoadModsList)
        {
            FailedToLoadMods.Clear();
        }

        // If we want the mod to be reloaded then add it if it not already being reloaded
        if (addToReloadedModList && !ReloadedMods.Contains(currentMod))
        {
            ReloadedMods.Add(currentMod);
        }

        // If it already been loaded then skip it
        if (LoadedMods.Contains(currentMod))
        {
            currentMod.Status = (int)ModStatus.ModAlreadyBeenLoaded;
            return currentMod.Status;
        }

        if (!file.FileExists(currentMod.Location + "/mod.pck"))
        {
            GD.Print("Fail to find mod file: " + currentMod.Name);
            currentMod.Status = (int)ModStatus.ModFileCanNotBeFound;
            FailedToLoadMods.Add(currentMod);
            return currentMod.Status;
        }

        // Checks if a Dll file needs to be loaded
        if (!string.IsNullOrEmpty(currentMod.Dll))
        {
            if (file.FileExists(ProjectSettings.GlobalizePath(currentMod.Location + "/" + currentMod.Dll)))
            {
                Assembly.LoadFile(ProjectSettings.GlobalizePath(currentMod.Location + "/" + currentMod.Dll));
                GD.Print("ADJ_DLL: " + currentMod.Dll);
            }
        }

        if (ProjectSettings.LoadResourcePack(currentMod.Location + "/mod.pck", true))
        {
            GD.Print("Loaded mod: " + currentMod.Name);
            LoadedMods.Add(currentMod);
            currentMod.Status = (int)ModStatus.ModLoadedSuccessfully;
            return currentMod.Status;
        }

        GD.Print("Failed to load mod: " + currentMod.Name);
        currentMod.Status = (int)ModStatus.FailedModLoading;
        FailedToLoadMods.Add(currentMod);
        return currentMod.Status;
    }

    /// <summary>
    ///   This checks if all of the mod in the array is compatible with each other
    /// </summary>
    /// <returns>
    ///  The 1st index returns the error type (Look at CheckErrorStatus enum for more explanation)
    ///  Tht 2nd index returns the mod that is causing the error
    ///  The 3rd index returns the other mod that is causing the error, if there is one
    /// </returns>
    public int[] IsValidModArray(ModInfo[] modsToCheck)
    {
        int[] isValidList = { (int)CheckErrorStatus.Unknown };
        Dictionary<string, ModInfo> tempModDictionary;

        if (modsToCheck.Length < 1)
        {
            return new[] { (int)CheckErrorStatus.EmptyList };
        }

        // Store the mod in a dictionary for faster look-up when actually checking
        tempModDictionary = ModArrayToModDictioanry(modsToCheck);

        for (int index = 0; index < modsToCheck.Length; index++)
        {
            var currentMod = modsToCheck[index];
            var validMod = IsModValid(currentMod, tempModDictionary);

            if (validMod.Length > 1)
            {
                isValidList = new[] { validMod[0], index, validMod[1] };
            }
            else if (validMod.Length > 0)
            {
                isValidList = new[] { validMod[0], index };
            }
        }

        // If there were no errors then the list is valid
        if (isValidList[0] == (int)CheckErrorStatus.Unknown)
        {
            isValidList = new[] { (int)CheckErrorStatus.Valid };
        }

        return isValidList;
    }

    /// <summary>
    ///   This checks if all of the mod in the list is compatible with each other
    /// </summary>
    /// <returns>
    ///  The 1st index returns the error type (Look at CheckErrorStatus enum for more explanation)
    ///  Tht 2nd index returns the mod that is causing the error
    ///  The 3rd index returns the other mod that is causing the error, if there is one
    /// </returns>
    public int[] IsValidModList(List<ModInfo> modsToCheck)
    {
        int[] isValidList = { (int)CheckErrorStatus.Unknown };
        Dictionary<string, ModInfo> tempModDictionary;

        if (modsToCheck.Count < 1)
        {
            return new[] { (int)CheckErrorStatus.EmptyList };
        }

        // Store the mod in a dictionary for faster look-up when actually checking
        tempModDictionary = ModArrayToModDictioanry(modsToCheck.ToArray());

        for (int index = 0; index < modsToCheck.Count; index++)
        {
            ModInfo currentMod = modsToCheck[index];
            int[] validMod = IsModValid(currentMod, tempModDictionary);
            if (validMod.Length > 0)
            {
                isValidList = new[] { validMod[0], index };
            }
            else if (validMod.Length > 1)
            {
                isValidList = new[] { validMod[0], index, validMod[1] };
            }
        }

        // If there were no errors then the list is valid
        if (isValidList[0] == (int)CheckErrorStatus.Unknown)
        {
            isValidList = new[] { (int)CheckErrorStatus.Valid };
        }

        return isValidList;
    }

    /// <summary>
    ///   This checks if all of the mod in the list is compatible with each other
    /// </summary>
    /// <returns>
    ///  The 1st index returns the error type (Look at CheckErrorStatus enum for more explanation)
    ///  Tht 2nd index returns the mod that is causing the error
    ///  The 3rd index returns the other mod that is causing the error, if there is one
    /// </returns>
    public int[] IsValidModList(ItemList modsToCheck)
    {
        var modListCount = modsToCheck.GetItemCount();
        int[] isValidList = { (int)CheckErrorStatus.Unknown };
        Dictionary<string, ModInfo> tempModDictionary = new Dictionary<string, ModInfo>();

        if (modListCount < 1)
        {
            return new[] { (int)CheckErrorStatus.EmptyList };
        }

        // Store the mod in a dictionary for faster look-up when actually checking
        for (int index = 0; index < modListCount; index++)
        {
            var currentMod = (ModInfo)modsToCheck.GetItemMetadata(index);
            currentMod.LoadPosition = index;
            tempModDictionary.Add(currentMod.Name, currentMod);
        }

        for (int index = 0; index < modListCount; index++)
        {
            var currentMod = (ModInfo)modsToCheck.GetItemMetadata(index);
            var validMod = IsModValid(currentMod, tempModDictionary);
            if (validMod[0] != (int)CheckErrorStatus.Valid)
            {
                if (validMod.Length > 1)
                {
                    isValidList = new[] { validMod[0], index, validMod[1] };
                }
                else if (validMod.Length > 0)
                {
                    isValidList = new[] { validMod[0], index };
                }
            }
        }

        // If there were no errors then the list is valid
        if (isValidList[0] == (int)CheckErrorStatus.Unknown)
        {
            isValidList = new[] { (int)CheckErrorStatus.Valid };
        }

        return isValidList;
    }

    /// <summary>
    ///   This checks if the mod is valid in dictionary of mods
    ///   If you want to check if a mod is valid in a list use of the above functions
    /// </summary>
    /// <returns>
    ///  The 1st index returns the error type (Look at CheckErrorStatus enum for more explanation)
    ///  The 2nd index returns the mod that is causing the error, if there is one
    /// </returns>
    public int[] IsModValid(ModInfo currentMod, Dictionary<string, ModInfo> modDictionary)
    {
        if (currentMod.IsCompatibleVersion < -1)
        {
            return new[]
            {
                (int)CheckErrorStatus.IncompatibleVersion,
            };
        }

        if (currentMod.Dependencies != null)
        {
            var dependencyIndex = 0;
            foreach (string dependencyName in currentMod.Dependencies)
            {
                if (modDictionary.ContainsKey(dependencyName))
                {
                    if (currentMod.LoadPosition < modDictionary[dependencyName].LoadPosition)
                    {
                        return new[]
                        {
                            (int)CheckErrorStatus.InvalidDependencyOrder, modDictionary[dependencyName].LoadPosition,
                        };
                    }
                }
                else
                {
                    return new[]
                    {
                        (int)CheckErrorStatus.DependencyNotFound, dependencyIndex,
                    };
                }

                dependencyIndex++;
            }
        }

        if (currentMod.IncompatibleMods != null)
        {
            foreach (string incompatibleName in currentMod.IncompatibleMods)
            {
                if (modDictionary.ContainsKey(incompatibleName))
                {
                    return new[]
                    {
                        (int)CheckErrorStatus.IncompatibleMod, modDictionary[incompatibleName].LoadPosition,
                    };
                }
            }
        }

        if (currentMod.LoadBefore != null)
        {
            foreach (string loadBeforeName in currentMod.LoadBefore)
            {
                if (modDictionary.ContainsKey(loadBeforeName))
                {
                    if (currentMod.LoadPosition > modDictionary[loadBeforeName].LoadPosition)
                    {
                        return new[]
                        {
                            (int)CheckErrorStatus.InvalidLoadOrderBefore, modDictionary[loadBeforeName].LoadPosition,
                        };
                    }
                }
            }
        }

        if (currentMod.LoadAfter != null)
        {
            foreach (string loadAfterName in currentMod.LoadAfter)
            {
                if (modDictionary.ContainsKey(loadAfterName))
                {
                    if (currentMod.LoadPosition < modDictionary[loadAfterName].LoadPosition)
                    {
                        return new[]
                        {
                            (int)CheckErrorStatus.InvalidLoadOrderAfter, modDictionary[loadAfterName].LoadPosition,
                        };
                    }
                }
            }
        }

        return new[] { (int)CheckErrorStatus.Valid };
    }

    /// <summary>
    ///   This loads multiple mods from a array
    /// </summary>
    public List<ModInfo> LoadModFromArray(ModInfo[] modsToLoad, bool ignoreStartupMods = true,
        bool addToReloadedModList = false, bool clearReloadedModList = true, bool clearFailedToLoadModsList = true)
    {
        var failedModList = new List<ModInfo>();

        if (clearReloadedModList)
        {
            ReloadedMods.Clear();
        }

        if (clearFailedToLoadModsList)
        {
            FailedToLoadMods.Clear();
        }

        foreach (ModInfo currentMod in modsToLoad)
        {
            // If we should ignore Startup mods then skip it
            if (currentMod.StartupMod && ignoreStartupMods)
            {
                // Add to the autoloader, delaying the loading until next launch
                if (addToReloadedModList)
                {
                    ReloadedMods.Add(currentMod);
                    StartupMods.Add(currentMod);
                }

                continue;
            }

            // Loads the mod, checks if it fails and if so then add it to the fail list
            if (LoadMod(currentMod, addToReloadedModList, false, false) < 0)
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
        bool addToAutoLoader = false, bool clearReloadedModList = true, bool clearFailedToLoadModsList = true)
    {
        return LoadModFromArray(modsToLoad.ToArray(), ignoreAutoloaded, addToAutoLoader, clearReloadedModList,
            clearFailedToLoadModsList);
    }

    /// <summary>
    ///   This loads multiple mods from a ItemList, Mostly use for the ModManagerUI
    /// </summary>
    public List<ModInfo> LoadModFromList(ItemList modsToLoad, bool ignoreStartupMods = true,
        bool addToReloadedModList = false, bool clearReloadedModList = true, bool clearFailedToLoadModsList = true)
    {
        var modListCount = modsToLoad.GetItemCount();
        var failedModList = new List<ModInfo>();

        if (clearReloadedModList)
        {
            ReloadedMods.Clear();
        }

        if (clearFailedToLoadModsList)
        {
            FailedToLoadMods.Clear();
        }

        for (int index = 0; index < modListCount; index++)
        {
            var currentMod = (ModInfo)modsToLoad.GetItemMetadata(index);

            // If we should ignore Startup mods then skip it
            if (currentMod.StartupMod && ignoreStartupMods)
            {
                // Add to the autoloader, delaying the loading until next launch
                if (addToReloadedModList)
                {
                    ReloadedMods.Add(currentMod);
                    StartupMods.Add(currentMod);
                }

                continue;
            }

            // Loads the mod, checks if it fails and if so then add it to the fail list
            if (LoadMod(currentMod, addToReloadedModList, false, false) < 0)
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
    ///   This saves the 'ReloadedMods' list to a file
    /// </summary>
    /// <returns>True on success, false if the file can't be written.</returns>
    /// <remarks>
    ///   This was based on the Save method from Settings.cs
    /// </remarks>
    public bool SaveReloadedModsList()
    {
        using var file = new File();
        var error = file.Open(Constants.MOD_CONFIGURATION, File.ModeFlags.Write);

        if (error != Error.Ok)
        {
            GD.PrintErr("Couldn't open mod configuration file for writing.");
            return false;
        }

        file.StoreString(JsonConvert.SerializeObject(ReloadedMods, Formatting.Indented));

        file.Close();

        return true;
    }

    /// <summary>
    ///   This loads the 'ReloadedMods' list from a file
    /// </summary>
    /// <returns>True on success, false if the file can't be loaded.</returns>
    public bool LoadReloadedModsList()
    {
        var modFileContent = ReadJSONFile(Constants.MOD_CONFIGURATION);
        var autoModList =
            JsonConvert.DeserializeObject<List<ModInfo>>(modFileContent);

        if (autoModList != null)
        {
            ReloadedMods = autoModList;
            return true;
        }

        return false;
    }

    /// <summary>
    ///   This resets the game by clearing the list in the settings file
    /// </summary>
    public void ResetGame()
    {
        ReloadedMods.Clear();
        SaveReloadedModsList();
    }

    private static string ReadJSONFile(string path)
    {
        using var file = new File();

        file.Open(path, File.ModeFlags.Read);
        var result = file.GetAsText();

        // This might be completely unnecessary
        file.Close();

        return result;
    }

    private Dictionary<string, ModInfo> ModArrayToModDictioanry(ModInfo[] modArray)
    {
        Dictionary<string, ModInfo> returnValue = new Dictionary<string, ModInfo>();
        for (int index = 0; index < modArray.Length; ++index)
        {
            var currentMod = modArray[index];
            currentMod.LoadPosition = index;
            returnValue.Add(currentMod.Name, currentMod);
        }

        return returnValue;
    }
}
