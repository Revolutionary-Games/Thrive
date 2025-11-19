using System;
using Godot;
using SharedBase.Archive;

public abstract class Metaball : IReadOnlyMetaball, IArchivable
{
    public const ushort SERIALIZATION_VERSION_BASE = 1;

    public Vector3 Position { get; set; }

    /// <summary>
    ///   The diameter of the metaball
    /// </summary>
    public float Size { get; set; } = 1.0f;

    /// <summary>
    ///   The radius of the metaball
    /// </summary>
    public float Radius => Size * 0.5f;

    /// <summary>
    ///   Volume of the metaball sphere. Do not scale the result returned from this, use <see cref="GetVolume"/>
    ///   instead.
    /// </summary>
    public float Volume => GetVolume();

    /// <summary>
    ///   For animation and convolution surfaces we need to know the structure of metaballs
    /// </summary>
    public Metaball? ModifiableParent { get; set; }

    public IReadOnlyMetaball? Parent => ModifiableParent;

    /// <summary>
    ///   Basic rendering of the metaballs for now just uses a colour
    /// </summary>
    public abstract Color Colour { get; }

    public abstract ushort CurrentArchiveVersion { get; }
    public abstract ArchiveObjectType ArchiveObjectType { get; }
    public virtual bool CanBeReferencedInArchive => false;

    public abstract void WriteToArchive(ISArchiveWriter writer);

    public virtual void WriteBasePropertiesToArchive(ISArchiveWriter writer)
    {
        writer.Write(Position);
        writer.Write(Size);
        writer.WriteObjectOrNull(ModifiableParent);
    }

    public virtual void ReadBasePropertiesFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION_BASE or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION_BASE);

        Position = reader.ReadVector3();
        Size = reader.ReadFloat();
        ModifiableParent = reader.ReadObjectOrNull<Metaball>();
    }

    /// <summary>
    ///   Checks if the data of this ball matches another (parent shouldn't be checked). Used for action replacement
    ///   detection.
    /// </summary>
    /// <param name="other">The other metaball to check against</param>
    /// <returns>True if these are fundamentally the same kind of placed ball</returns>
    public abstract bool MatchesDefinition(Metaball other);

    public float GetVolume(float multiplier = 1)
    {
        return (float)(4.0f * Math.PI * Math.Pow(Radius * multiplier, 3) / 3.0f);
    }

    /// <summary>
    ///   Calculates how many parent links need to be travelled to reach the root
    /// </summary>
    /// <returns>The number of hops to the root metaball</returns>
    public int CalculateTreeDepth()
    {
        if (ModifiableParent == null)
            return 0;

        return 1 + ModifiableParent.CalculateTreeDepth();
    }

    /// <summary>
    ///   Checks the parent tree if the value is an ancestor of this
    /// </summary>
    /// <param name="potentialAncestor">The metaball to look for</param>
    /// <returns>True if the given metaball is this metaball's ancestor</returns>
    public bool HasAncestor(Metaball potentialAncestor)
    {
        if (ModifiableParent == null)
            return false;

        if (ModifiableParent == potentialAncestor)
            return true;

        return ModifiableParent.HasAncestor(potentialAncestor);
    }

    public Vector3? DirectionToParent()
    {
        if (ModifiableParent == null)
            return null;

        var vectorToParent = Position - ModifiableParent.Position;
        return vectorToParent.Normalized();
    }

    public void AdjustPositionToTouchParent(Vector3? precomputedDirection = null)
    {
        Position = CalculatePositionTouchingParent(precomputedDirection);
    }

    public Vector3 CalculatePositionTouchingParent(Vector3? precomputedDirection = null)
    {
        if (ModifiableParent == null)
            throw new InvalidOperationException("Metaball must have a parent to position next to it");

        precomputedDirection ??=
            DirectionToParent() ?? throw new Exception("direction to parent should have returned a value");

        float wantedDistance = ModifiableParent.Radius + Radius;

        var offset = precomputedDirection.Value * wantedDistance;

        return ModifiableParent.Position + offset;
    }

    public override int GetHashCode()
    {
        return Position.GetHashCode() ^ Size.GetHashCode() * 19 ^
            (ModifiableParent?.Position.GetHashCode() ?? 6469) * 23;
    }

    public override string ToString()
    {
        if (ModifiableParent == null)
            return $"Root Metaball at {Position}";

        return $"Metaball at {Position}";
    }
}
