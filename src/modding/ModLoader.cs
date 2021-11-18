using System;
using System.Reflection;
using Godot;
using File = System.IO.File;

/// <summary>
///   Handles loading mods, and auto-loading mods, and also showing related errors etc. popups
/// </summary>
public class ModLoader : Node
{
    private void LoadPckFile(string path)
    {
        GD.Print("Loading mod .pck file: ", path);
        ProjectSettings.LoadResourcePack(path);
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
