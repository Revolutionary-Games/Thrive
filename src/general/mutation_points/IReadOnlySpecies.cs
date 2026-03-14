using Godot;

/// <summary>
///   Readonly access to a species. Useful for comparisons and correctly made copies (in auto-evo).
/// </summary>
public interface IReadOnlySpecies
{
    public uint ID { get; }

    public string Genus { get; }
    public string Epithet { get; }

    public string FormattedName { get; }

    public Color SpeciesColour { get; }

    public IReadOnlyBehaviourDictionary Behaviour { get; }

    public long Population { get; }

    public int Generation { get; }

    public bool PlayerSpecies { get; }

    public IReadOnlyEnvironmentalTolerances Tolerances { get; }
}
