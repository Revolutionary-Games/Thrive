using System;
using System.Collections.Generic;
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
    private static ModLoader instance;

    private readonly List<string> loadedMods = new();

    private readonly Dictionary<string, IMod> loadedModAssemblies = new();

    private ModLoader()
    {
        instance = this;

        // The reason why mods aren't loaded here already is that this object can't be attached to the scene here yet
        // so we delay mod loading until this has been attached to the main scene tree
    }

    public static ModLoader Instance => instance;

    public override void _Ready()
    {
        base._Ready();

        LoadMods();
    }

    public void LoadMods()
    {
        var newMods = Settings.Instance.EnabledMods.Value.ToHashSet();

        foreach (var unload in loadedMods.Where(m => !newMods.Contains(m)).ToList())
        {
            GD.Print("Unloading mod: ", unload);
            UnLoadMod(unload);
            loadedMods.Remove(unload);
        }

        if (newMods.Count < 1)
            return;

        foreach (var load in newMods.Except(loadedMods).ToList())
        {
            GD.Print("Loading mod: ", load);
            LoadMod(load);
            loadedMods.Add(load);
        }

        GD.Print("Mod loading finished");
    }

    private FullModDetails LoadModInfo(string name)
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

                return new FullModDetails(name) { Folder = modsFolder, Info = info };
            }
        }

        GD.PrintErr("No folder found for mod: ", name);
        return null;
    }

    private void LoadMod(string name)
    {
        var info = LoadModInfo(name);

        if (info == null)
        {
            GD.PrintErr("Can't load mod due to failed info reading: ", name);
            return;
        }

        bool loadedSomething = false;

        if (!string.IsNullOrEmpty(info.Info.PckToLoad))
        {
            LoadPckFile(Path.Combine(info.Folder, info.Info.PckToLoad));
            loadedSomething = true;
        }

        if (!string.IsNullOrEmpty(info.Info.ModAssembly))
        {
            Assembly assembly;
            try
            {
                assembly = Assembly.GetExecutingAssembly();
            }
            catch (Exception e)
            {
                GD.PrintErr("Could not get executing assembly due to: ", e, " can't load mod with an assembly");
                return;
            }

            LoadCodeAssembly(Path.Combine(info.Folder, info.Info.ModAssembly));

            if (!CreateModInstance(name, info, assembly))
                return;

            loadedSomething = true;
        }

        if (!loadedSomething)
        {
            GD.Print("A mod contained no loadable resources");
        }
    }

    private void UnLoadMod(string name)
    {
        var info = LoadModInfo(name);

        if (info == null)
        {
            GD.PrintErr("Can't load mod due to failed info reading: ", name);
            return;
        }

        if (loadedModAssemblies.TryGetValue(name, out var mod))
        {
            GD.Print("Unloaded mod contained an assembly, sending it the unload method call");
            if (!mod.Unload())
            {
                GD.PrintErr("Mod's (", name, ") assembly unload method call failed");
            }

            loadedModAssemblies.Remove(name);
        }
    }

    private bool CreateModInstance(string name, FullModDetails info, Assembly assembly)
    {
        var className = info.Info.AssemblyModClass;

        var type = assembly.GetTypes().FirstOrDefault(t => t.Name == className);

        if (type == null)
        {
            GD.Print("No class with name \"", className, "\" found, can't finish loading mod assembly");
            return false;
        }

        try
        {
            var mod = (IMod)Activator.CreateInstance(type);

            // TODO: ModInterface class
            if (!mod.Initialize(null, info.Info))
            {
                GD.PrintErr("Mod's (", name, ") initialize method call failed");
            }

            loadedModAssemblies.Add(name, mod);
        }
        catch (Exception e)
        {
            GD.PrintErr("Mod's (", name, ") initialization failed with an exception: ", e);
        }

        return true;
    }

    private void LoadPckFile(string path)
    {
        GD.Print("Loading mod .pck file: ", path);

        if (!ProjectSettings.LoadResourcePack(path))
        {
            GD.PrintErr(".pck loading failed");
        }
    }

    private void LoadCodeAssembly(string path)
    {
        path = ProjectSettings.GlobalizePath(path);

        if (!File.Exists(path))
        {
            GD.PrintErr("Can't load assembly from non-existent path: ", path);
            throw new ArgumentException("Invalid given assembly path");
        }

        GD.Print("Loading mod C# assembly from: ", path);

        Assembly.LoadFile(path);

        GD.Print("Assembly load succeeded");
    }
}
