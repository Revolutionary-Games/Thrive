using System;

/// <summary>
///   An extended version of <see cref="ModInfo"/> that contains non-mod author controlled data
/// </summary>
public class FullModDetails : IEquatable<FullModDetails>
{
    public FullModDetails(string internalName)
    {
        InternalName = internalName;
    }

    public string InternalName { get; }
    public string Folder { get; set; }

    public ModInfo Info { get; set; }

    public bool Equals(FullModDetails other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return InternalName == other.InternalName;
    }

    public override bool Equals(object obj)
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
        return InternalName != null ? InternalName.GetHashCode() : 0;
    }
}
