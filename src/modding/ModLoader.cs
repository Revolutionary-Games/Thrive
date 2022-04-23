using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Godot;
using File = System.IO.File;
using Path = System.IO.Path;

/// <summary>
///   Handles loading mods, and auto-loading mods, and also showing related errors etc. popups
/// </summary>
public class ModLoader : Node
{
    private static ModLoader? instance;
    private static ModInterface? modInterface;

    private readonly List<string> loadedMods = new();

    private readonly Dictionary<string, IMod> loadedModAssemblies = new();

    private readonly List<(FullModDetails, string)> modErrors = new();

    private List<FullModDetails>? workshopMods;

    private bool firstExecute = true;

    private ModLoader()
    {
        instance = this;

        // The reason why mods aren't loaded here already is that this object can't be attached to the scene here yet
        // so we delay mod loading until this has been attached to the main scene tree
    }

    /// <summary>
    ///   The number that corresponds to the number returned by the isValidModList function
    /// </summary>
    public enum CheckErrorStatus
    {
        RequiredModsNotFound = -7,
        InvalidLoadOrderAfter,
        InvalidLoadOrderBefore,
        IncompatibleMod,
        InvalidDependencyOrder,
        DependencyNotFound,
        IncompatibleVersion,
        Unknown = 0,
        Valid,
        EmptyList,
    }

    public static ModLoader Instance => instance!;

    /// <summary>
    ///   The mod interface the game uses to trigger events that mods can react to
    /// </summary>
    public static ModInterface ModInterface => modInterface ?? throw new InstanceNotLoadedYetException();

    /// <summary>
    ///   Set to true if a mod that requires restart is loaded or unloaded
    /// </summary>
    public bool RequiresRestart { get; private set; }

    /// <summary>
    ///   Mod assembnlies for all of the loaded mods used to communicate with the mod
    /// </summary>
    public Dictionary<string, IMod> LoadedModAssemblies => loadedModAssemblies;

    /// <summary>
    ///   Errors that occurred when loading or unloading mods
    /// </summary>
    public IEnumerable<(FullModDetails Mod, string ErrorMessage)> ModErrors => modErrors;

    /// <summary>
    ///   Finds a mod and loads its info
    /// </summary>
    /// <param name="name">The internal (folder) name of the mod</param>
    /// <param name="failureIsError">If true, failure to find a mod is printed out</param>
    /// <returns>The mod details if the mod could be loaded</returns>
    public static FullModDetails? LoadModInfo(string name, bool failureIsError = true)
    {
        using var currentDirectory = new Directory();

        foreach (var location in Constants.ModLocations)
        {
            var modsFolder = Path.Combine(location, name);

            if (!currentDirectory.DirExists(modsFolder))
                continue;

            if (currentDirectory.FileExists(Path.Combine(modsFolder, Constants.MOD_INFO_FILE_NAME)))
            {
                var info = ModManager.LoadModInfo(modsFolder);

                if (info == null)
                {
                    GD.PrintErr("Failed to load info for mod \"", name, "\" from: ", modsFolder);
                    return null;
                }

                if (info.InternalName != name)
                {
                    GD.PrintErr("Mod internal name (", info.InternalName, ") doesn't match name of folder (", name,
                        ")");
                    return null;
                }

                return new FullModDetails(name, modsFolder, info);
            }
        }

        if (failureIsError)
            GD.PrintErr("No folder found for mod: ", name);
        return null;
    }

    public static List<FullModDetails> LoadWorkshopModsList()
    {
        var steamHandler = SteamHandler.Instance;
        if (!steamHandler.IsLoaded)
            return new List<FullModDetails>();

        var result = new List<FullModDetails>();

        using var directory = new Directory();

        foreach (var location in steamHandler.GetWorkshopItemFolders())
        {
            if (!directory.DirExists(location))
            {
                GD.PrintErr("Workshop item folder doesn't exist: ", location);
                continue;
            }

            if (directory.FileExists(Path.Combine(location, Constants.MOD_INFO_FILE_NAME)))
            {
                var info = ModManager.LoadModInfo(location);

                if (info == null)
                {
                    GD.PrintErr("Failed to load info for workshop mod at: ", location);
                    continue;
                }

                result.Add(new FullModDetails(info.InternalName, location, info)
                    { Workshop = true });
            }
            else
            {
                GD.PrintErr("Workshop item folder is missing mod info JSON at: ", location);
            }
        }

        return result;
    }

    /// <summary>
    ///   This checks if all of the mod in the list is compatible with each other
    /// </summary>
    /// <returns>
    ///  The 1st index returns the error type (Look at CheckErrorStatus enum for more explanation)
    ///  Tht 2nd index returns the mod that is causing the error
    ///  The 3rd index returns the other mod that is causing the error, if there is one
    /// </returns>
    public static (int ErrorType, int ModIndex, int OtherModIndex) IsValidModList(List<FullModDetails> modsToCheck)
    {
        var isValidList = (ErrorType: (int)CheckErrorStatus.Unknown, ModIndex: -1, OtherModIndex: -1);

        // Make sure the list is not empty
        if (modsToCheck.Count < 1)
        {
            return ((int)CheckErrorStatus.EmptyList, -1, -1);
        }

        // Store the mod in a dictionary for faster look-up when actually checking
        Dictionary<string, FullModDetails> tempModDictionary = ModArrayToModDictioanry(modsToCheck.ToArray());

        for (int index = 0; index < modsToCheck.Count; ++index)
        {
            FullModDetails currentMod = modsToCheck[index];
            int[] validMod = IsModValid(currentMod, tempModDictionary);
            isValidList = (validMod[0], index, validMod[1]);

            // TODO: allow for multiple mod errors to show up
            if (isValidList.ErrorType <= 0)
            {
                break;
            }
        }

        // If there were no errors then the list is valid
        if (isValidList.ErrorType == (int)CheckErrorStatus.Unknown)
        {
            isValidList = ((int)CheckErrorStatus.Valid, -1, -1);
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
    public static int[] IsModValid(FullModDetails currentMod, Dictionary<string, FullModDetails> modDictionary)
    {
        var currentModInfo = currentMod.Info;

        if (!currentMod.IsCompatibleVersion.IsCompatible())
        {
            return new[]
            {
                (int)CheckErrorStatus.IncompatibleVersion, -1,
            };
        }

        if (currentModInfo.Dependencies != null)
        {
            var dependencyIndex = 0;
            foreach (string dependencyName in currentModInfo.Dependencies)
            {
                if (!string.IsNullOrWhiteSpace(dependencyName))
                {
                    if (modDictionary.ContainsKey(dependencyName))
                    {
                        // See if the dependency is loaded before this mod
                        if (currentMod.LoadPosition < modDictionary[dependencyName].LoadPosition)
                        {
                            return new[]
                            {
                                (int)CheckErrorStatus.InvalidDependencyOrder,
                                modDictionary[dependencyName].LoadPosition,
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
                }

                ++dependencyIndex;
            }
        }

        if (currentModInfo.RequiredMods != null)
        {
            var requiredModsIndex = 0;
            foreach (string requiredModsName in currentModInfo.RequiredMods)
            {
                if (!string.IsNullOrWhiteSpace(requiredModsName))
                {
                    if (!modDictionary.ContainsKey(requiredModsName))
                    {
                        return new[]
                        {
                            (int)CheckErrorStatus.RequiredModsNotFound, requiredModsIndex,
                        };
                    }
                }

                ++requiredModsIndex;
            }
        }

        if (currentModInfo.IncompatibleMods != null)
        {
            foreach (string incompatibleName in currentModInfo.IncompatibleMods)
            {
                if (!string.IsNullOrWhiteSpace(incompatibleName))
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
        }

        if (currentModInfo.LoadBefore != null)
        {
            foreach (string loadBeforeName in currentModInfo.LoadBefore)
            {
                if (!string.IsNullOrWhiteSpace(loadBeforeName))
                {
                    if (modDictionary.ContainsKey(loadBeforeName))
                    {
                        if (currentMod.LoadPosition > modDictionary[loadBeforeName].LoadPosition)
                        {
                            return new[]
                            {
                                (int)CheckErrorStatus.InvalidLoadOrderBefore,
                                modDictionary[loadBeforeName].LoadPosition,
                            };
                        }
                    }
                }
            }
        }

        if (currentModInfo.LoadAfter != null)
        {
            foreach (string loadAfterName in currentModInfo.LoadAfter)
            {
                if (!string.IsNullOrWhiteSpace(loadAfterName))
                {
                    if (modDictionary.ContainsKey(loadAfterName))
                    {
                        if (currentMod.LoadPosition < modDictionary[loadAfterName].LoadPosition)
                        {
                            return new[]
                            {
                                (int)CheckErrorStatus.InvalidLoadOrderAfter,
                                modDictionary[loadAfterName].LoadPosition,
                            };
                        }
                    }
                }
            }
        }

        return new[] { (int)CheckErrorStatus.Valid, -1 };
    }

    public override void _Ready()
    {
        base._Ready();

        if (modInterface != null)
            throw new InvalidOperationException("ModInterface has been created already");

        modInterface = new ModInterface(GetTree());

        LoadMods();
        RequiresRestart = false;
    }

    public override void _Process(float delta)
    {
        base._Process(delta);

        if (firstExecute)
        {
            GD.Print("Loading mod Nodes into the scene tree");

            foreach (var tuple in loadedModAssemblies)
            {
                RunCodeModFirstRunCallbacks(tuple.Value);
            }

            firstExecute = false;
        }
    }

    public void LoadMods()
    {
        var newMods = Settings.Instance.EnabledMods.Value.ToHashSet();

        foreach (var unload in loadedMods.ToList())
        {
            GD.Print("Unloading mod: ", unload);

            UnLoadMod(unload);
            loadedMods.Remove(unload);
        }

        if (newMods.Count < 1)
            return;

        foreach (var load in newMods.ToList())
        {
            GD.Print("Loading mod: ", load);
            LoadMod(load);
            loadedMods.Add(load);
        }

        GD.Print("Mod loading finished");
    }

    public void OnNewWorkshopModsInstalled()
    {
        workshopMods = null;
    }

    public List<(FullModDetails Mod, string ErrorMessage)> GetAndClearModErrors()
    {
        var result = ModErrors.ToList();

        modErrors.Clear();

        return result;
    }

    public List<(FullModDetails Mod, string ErrorMessage)> GetModErrors()
    {
        var result = ModErrors.ToList();

        return result;
    }

    private static Dictionary<string, FullModDetails> ModArrayToModDictioanry(FullModDetails[] modArray)
    {
        var returnValue = new Dictionary<string, FullModDetails>();
        for (int index = 0; index < modArray.Length; ++index)
        {
            var currentMod = modArray[index];
            currentMod.LoadPosition = index;
            returnValue.Add(currentMod.InternalName, currentMod);
        }

        return returnValue;
    }

    private void LoadMod(string name)
    {
        var info = FindMod(name);

        if (info == null)
        {
            GD.PrintErr("Can't load mod due to failed info reading: ", name);

            var tempMod = new FullModDetails(name);

            modErrors.Add((tempMod, string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("CANT_LOAD_MOD_INFO"),
                name)));
            return;
        }

        bool loadedSomething = false;

        if (!string.IsNullOrEmpty(info.Info.PckToLoad) &&
            FileHelpers.ExistsCaseSensitive(Path.Combine(info.Folder, info.Info.PckToLoad ?? string.Empty)))
        {
            LoadPckFile(Path.Combine(info.Folder, info.Info.PckToLoad ?? string.Empty));
            loadedSomething = true;
        }

        if (!string.IsNullOrEmpty(info.Info.ModAssembly))
        {
            Assembly modAssembly;
            try
            {
                modAssembly = LoadCodeAssembly(Path.Combine(info.Folder, info.Info.ModAssembly!));
            }
            catch (Exception e)
            {
                GD.PrintErr("Could not load mod assembly due to exception: ", e);
                modErrors.Add((info, string.Format(CultureInfo.CurrentCulture,
                    TranslationServer.Translate("MOD_ASSEMBLY_LOAD_EXCEPTION"),
                    name, e)));
                return;
            }

            if (!CreateModInstance(name, info, modAssembly))
                return;

            loadedSomething = true;
        }

        CheckAndMarkIfModRequiresRestart(info);

        if (!loadedSomething)
        {
            GD.Print("A mod contained no loadable resources");
            modErrors.Add((info, string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("MOD_HAS_NO_LOADABLE_RESOURCES"),
                name)));
        }
    }

    private void UnLoadMod(string name)
    {
        var info = FindMod(name);

        if (info == null)
        {
            GD.PrintErr("Can't unload mod due to failed info reading: ", name);
            var tempMod = new FullModDetails(name);

            modErrors.Add((tempMod, string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("CANT_LOAD_MOD_INFO"),
                name)));
            return;
        }

        if (loadedModAssemblies.TryGetValue(name, out var mod))
        {
            GD.Print("Unloaded mod contained an assembly, sending it the unload method call");

            try
            {
                if (!mod.Unload())
                {
                    GD.PrintErr("Mod's (", name, ") assembly unload method call failed");
                    modErrors.Add((info, string.Format(CultureInfo.CurrentCulture,
                        TranslationServer.Translate("MOD_ASSEMBLY_UNLOAD_CALL_FAILED"),
                        name)));
                }
            }
            catch (Exception e)
            {
                GD.PrintErr("Mod's (", name, ") assembly unload method call failed with an exception: ", e);
                modErrors.Add((info, string.Format(CultureInfo.CurrentCulture,
                    TranslationServer.Translate("MOD_ASSEMBLY_UNLOAD_CALL_FAILED_EXCEPTION"),
                    name, e)));
            }

            loadedModAssemblies.Remove(name);
        }

        CheckAndMarkIfModRequiresRestart(info);
    }

    private void CheckAndMarkIfModRequiresRestart(FullModDetails mod)
    {
        if (mod.Info.RequiresRestart)
        {
            GD.Print(mod.InternalName, " requires a restart");
            RequiresRestart = true;
        }
    }

    /// <summary>
    ///   Finds a mod by name to load. Also checks workshop mods
    /// </summary>
    /// <param name="name">The name of the mod</param>
    /// <returns>The loaded mod info or null if not found</returns>
    private FullModDetails? FindMod(string name)
    {
        var info = LoadModInfo(name);

        if (info == null)
        {
            if (workshopMods == null)
            {
                GD.Print("Checking for potentially installed workshop mods");
                workshopMods = LoadWorkshopModsList();
            }

            info = workshopMods.FirstOrDefault(m => m.InternalName == name);

            if (info != null)
            {
                GD.Print("Mod folder found as a workshop item");
            }
        }

        return info;
    }

    private void RunCodeModFirstRunCallbacks(IMod mod)
    {
        var scene = modInterface!.CurrentScene;

        mod.CanAttachNodes(scene);
    }

    private bool CreateModInstance(string name, FullModDetails info, Assembly assembly)
    {
        if (info.Info.AssemblyModClass == null)
        {
            return false;
        }

        var className = info.Info.AssemblyModClass;

        try
        {
            var type = assembly.GetTypes().FirstOrDefault(t => t.Name == className);
            if (type == null)
            {
                GD.Print("No class with name \"", className, "\" found, can't finish loading mod assembly");
                modErrors.Add((info, string.Format(CultureInfo.CurrentCulture,
                    TranslationServer.Translate("MOD_ASSEMBLY_CLASS_NOT_FOUND"),
                    name, className)));
                return false;
            }

            var mod = (IMod)Activator.CreateInstance(type);

            if (!mod.Initialize(modInterface!, info))
            {
                GD.PrintErr("Mod's (", name, ") initialize method call failed");
                modErrors.Add((info, string.Format(CultureInfo.CurrentCulture,
                    TranslationServer.Translate("MOD_ASSEMBLY_INIT_CALL_FAILED"),
                    name)));
            }

            loadedModAssemblies.Add(name, mod);

            if (!firstExecute)
            {
                // We need to do the actions that would normally be delayed if this mod was loaded after game startup
                RunCodeModFirstRunCallbacks(mod);
            }
        }
        catch (Exception e)
        {
            GD.PrintErr("Mod's (", name, ") initialization failed with an exception: ", e);
            modErrors.Add((info, string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("MOD_ASSEMBLY_LOAD_CALL_FAILED_EXCEPTION"),
                name, e)));
        }

        return true;
    }

    private void LoadPckFile(string path)
    {
        GD.Print("Loading mod .pck file: ", path);

        if (!ProjectSettings.LoadResourcePack(path))
        {
            GD.PrintErr(".pck loading failed");
            var tempMod = new FullModDetails(Path.GetFileName(path));
            modErrors.Add((tempMod, string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("PCK_LOAD_FAILED"),
                Path.GetFileName(path))));
        }
    }

    private Assembly LoadCodeAssembly(string path)
    {
        path = ProjectSettings.GlobalizePath(path);

        if (!File.Exists(path))
        {
            GD.PrintErr("Can't load assembly from non-existent path: ", path);
            throw new ArgumentException("Invalid given assembly path");
        }

        GD.Print("Loading mod C# assembly from: ", path);

        // This version doesn't load the classes into the current assembly find path (or whatever it is properly called
        // in C#)
        var result = Assembly.LoadFrom(path);

        // This version should load like that, however this still results in classes not being found from Godot
        // so special workaround is needed
        // TODO: now that upper call is changed to LoadFrom this might not work as a replacement anymore
        // var result = AppDomain.CurrentDomain.Load(File.ReadAllBytes(path));

        GD.Print("Assembly load succeeded");

        return result;
    }
}
