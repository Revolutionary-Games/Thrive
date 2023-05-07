using System;

/// <summary>
///   An extended version of <see cref="ModInfo"/> that contains non-mod author controlled data
/// </summary>
public class FullModDetails : IEquatable<FullModDetails>
{
    public FullModDetails(string internalName, string folder, ModInfo info)
    {
        InternalName = internalName;
        Folder = folder;
        Info = info;
    }

    /// <summary>
    ///   The compatibility with the current version of Thrive.
    /// </summary>
    public enum VersionCompatibility
    {
        /// <summary>It has been explicitly stated to be incompatible.</summary>
        Incompatible = -2,

        /// <summary>It might not be compatible as it has not been explicitly stated to be compatible.</summary>
        NotExplicitlyCompatible,

        /// <summary>The variable has not been set or no version compatibility has been stated.</summary>
        Unknown,

        /// <summary>It has been explicitly stated to be compatible.</summary>
        Compatible,
    }

    public string InternalName { get; }
    public string Folder { get; set; }

    /// <summary>
    ///   Is the mod compatible with the current version of thrive?
    /// </summary>
    public VersionCompatibility IsCompatibleVersion { get; set; }

    /// <summary>
    ///   The index of the mod is loaded in the current list it is in.
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
