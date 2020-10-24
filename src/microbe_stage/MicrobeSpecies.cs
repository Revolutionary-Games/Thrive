using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
///   Represents a microbial species with microbe stage specific species things.
/// </summary>
[JsonObject(IsReference = true)]
[TypeConverter(typeof(ThriveTypeConverter))]
[JSONDynamicTypeAllowedAttribute]
[UseThriveConverter]
public class MicrobeSpecies : Species
{
    public bool IsBacteria;
    public MembraneType MembraneType;
    public float MembraneRigidity;

    public MicrobeSpecies(uint id)
        : base(id)
    {
        Organelles = new OrganelleLayout<OrganelleTemplate>();
    }

    public OrganelleLayout<OrganelleTemplate> Organelles { get; set; }

    [JsonIgnore]
    public override string StringCode
    {
        get => ThriveJsonConverter.Instance.SerializeObject(this);

        // TODO: allow replacing Organelles from value
        set => throw new NotImplementedException();
    }

    public override bool IsStructureValid
    {
        get
        {
            return Organelles.Any() && GetIslandHexes().Any();
        }
    }

    public void SetInitialCompoundsForDefault()
    {
        InitialCompounds.Clear();
        InitialCompounds.Add(SimulationParameters.Instance.GetCompound("atp"), 30);
        InitialCompounds.Add(SimulationParameters.Instance.GetCompound("glucose"), 10);
    }

    public void SetInitialCompoundsForIron()
    {
        SetInitialCompoundsForDefault();
        InitialCompounds.Add(SimulationParameters.Instance.GetCompound("iron"), 10);
    }

    public void SetInitialCompoundsForChemo()
    {
        SetInitialCompoundsForDefault();
        InitialCompounds.Add(SimulationParameters.Instance.GetCompound("hydrogensulfide"), 10);
    }

    public void UpdateInitialCompounds()
    {
        var simulation = SimulationParameters.Instance;

        var rusticyanin = simulation.GetOrganelleType("rusticyanin");
        var chemo = simulation.GetOrganelleType("chemoplast");
        var chemoProtein = simulation.GetOrganelleType("chemoSynthesizingProteins");

        if (Organelles.Any(o => o.Definition == rusticyanin))
        {
            SetInitialCompoundsForIron();
        }
        else if (Organelles.Any(o => o.Definition == chemo ||
            o.Definition == chemoProtein))
        {
            SetInitialCompoundsForChemo();
        }
        else
        {
            SetInitialCompoundsForDefault();
        }
    }

    public override void ApplyMutation(Species mutation)
    {
        base.ApplyMutation(mutation);

        var casted = (MicrobeSpecies)mutation;

        Organelles.Clear();

        foreach (var organelle in casted.Organelles)
        {
            Organelles.Add((OrganelleTemplate)organelle.Clone());
        }

        IsBacteria = casted.IsBacteria;
        MembraneType = casted.MembraneType;
        MembraneRigidity = casted.MembraneRigidity;
    }

    public override object Clone()
    {
        var result = new MicrobeSpecies(ID);

        ClonePropertiesTo(result);

        result.IsBacteria = IsBacteria;
        result.MembraneType = MembraneType;
        result.MembraneRigidity = MembraneRigidity;

        foreach (var organelle in Organelles)
        {
            result.Organelles.Add((OrganelleTemplate)organelle.Clone());
        }

        return result;
    }

    /// <summary>
    ///   Recursively loops though all hexes and checks if there any without connection to the rest.
    /// </summary>
    /// <returns>
    ///   Returns a list of hexes that are not connected to the rest
    /// </returns>
    internal List<Hex> GetIslandHexes()
    {
        // The hex to start the recursion with
        var initHex = Organelles[0].Position;

        // These are the hexes have neighbours and aren't islands
        var hexesWithNeighbours = new List<Hex> { initHex };

        // These are all of the existing hexes, that if there are no islands will all be visited
        var shouldBeVisited = Organelles.Select(p => p.Position).ToList();

        CheckmarkNeighbors(hexesWithNeighbours, initHex);

        // Return the difference of the lists (hexes that were not visited)
        return shouldBeVisited.Except(hexesWithNeighbours).ToList();
    }

    /// <summary>
    ///   A recursive function that adds the neighbours of current hex that contain organelles to the checked list and
    ///   recurses to them to find more connected organelles
    /// </summary>
    /// <param name="checked">The list of already visited hexes. Will be filled up with found hexes.</param>
    /// <param name="currentHex">The hex to visit the neighbours of.</param>
    private void CheckmarkNeighbors(List<Hex> @checked, Hex currentHex)
    {
        // Get all neighbors not already visited
        var myNeighbors = GetNeighborHexes(currentHex).Where(p => !@checked.Contains(p)).ToArray();

        // Add the new neighbors to the list to not visit them again
        @checked.AddRange(myNeighbors);

        // Recurse to all neighbours to find more connected hexes
        foreach (var neighbor in myNeighbors)
        {
            CheckmarkNeighbors(@checked, neighbor);
        }
    }

    /// <summary>Gets all neighboring hexes where there is an organelle</summary>
    /// <param name="hex">The hex to get the neighbours for</param>
    /// <returns>Returns a list of neighbors that are part of an organelle</returns>
    private IEnumerable<Hex> GetNeighborHexes(Hex hex)
    {
        return Hex.HexNeighbourOffset
                  .Select(p => hex + p.Value)
                  .Where(p => Organelles.GetOrganelleAt(p) != null);
    }
}
