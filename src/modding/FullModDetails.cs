using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   An extended version of <see cref="ModInfo"/> that contains non-mod author controlled data
/// </summary>
public class FullModDetails : IEquatable<FullModDetails>
{
    [JsonConstructor]
    public FullModDetails(string internalName, string folder, ModInfo info)
    {
        InternalName = internalName;
        Folder = folder;
        Info = info;
    }

    public FullModDetails(string internalName)
    {
        InternalName = internalName;
        Info = new ModInfo();
        Folder = string.Empty;
    }

    public string InternalName { get; }
    public string Folder { get; set; }

    /// <summary>
    ///   Is the mod compatible with the current version of thrive?
    /// </summary>

    // 1 = Compatible, 0 = Unknown, -1 = Might Not Be Compatible, -2 = Not Compatible
    public int IsCompatibleVersion { get; set; }

    /// <summary>
    ///   The index of the mod is loaded in
    /// </summary>
    public int LoadPosition { get; set; }

    /// <summary>
    ///   List of all the configuration options there are
    /// </summary>
    public ModConfigItemInfo[]? ConfigurationInfoList { get; set; }

    public Dictionary<string, object>? CurrentConfiguration { get; set; }

    public ModInfo Info { get; set; }

    public Control? ConfigNodes { get; set; }

    /// <summary>
    ///   Mod is from the workshop / downloaded
    /// </summary>
    public bool Workshop { get; set; }

    public bool Equals(FullModDetails? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return InternalName == other.InternalName;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;

        return Equals((FullModDetails)obj);
    }

    public override int GetHashCode()
    {
        return InternalName.GetHashCode();
    }
}
