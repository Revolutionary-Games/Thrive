using System;
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

    public enum VersionCompatibility
    {
        Incompatible = -2,
        NotExplicitlyCompatible,
        Unknown,
        Compatible,
    }

    public string InternalName { get; }
    public string Folder { get; set; }

    /// <summary>
    ///   Is the mod compatible with the current version of thrive?
    /// </summary>
    /// <remarks>
    ///    <para>
    ///        1 = Compatible - It has been explicitly stated to be compatible.
    ///    </para>
    ///    <para>
    ///        0 = Unknown - The variable has not been set or no version compatibility has been stated.
    ///    </para>
    ///    <para>
    ///        -1 = Might Not Be Compatible - It has not been explicitly stated to be compatible.
    ///    </para>
    ///    <para>
    ///        -2 = Not Compatible - It has been explicitly stated to be incompatible.
    ///    </para>
    /// </remarks>
    public VersionCompatibility IsCompatibleVersion { get; set; }

    /// <summary>
    ///   The index of the mod is loaded in
    /// </summary>
    public int LoadPosition { get; set; }
    public ModInfo Info { get; set; }

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
