using System.Collections;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Represents a view of a microbe species with a set of actions applied on top of the base data
/// </summary>
public class MicrobeEditsFacade : SpeciesEditsFacade, IReadOnlyMicrobeSpecies,
    IReadOnlyOrganelleLayout<IReadOnlyOrganelleTemplate>
{
    private readonly IReadOnlyMicrobeSpecies microbeSpecies;

    private readonly CellTypeEditsFacade organelleEditsFacade;

    public MicrobeEditsFacade(IReadOnlyMicrobeSpecies microbeSpecies, OrganelleDefinition? nucleusDefinition = null) :
        base(microbeSpecies)
    {
        organelleEditsFacade = new CellTypeEditsFacade(microbeSpecies, nucleusDefinition);
        this.microbeSpecies = microbeSpecies;
    }

    public IReadOnlyOrganelleLayout<IReadOnlyOrganelleTemplate> Organelles => organelleEditsFacade.Organelles;
    public MembraneType MembraneType => organelleEditsFacade.MembraneType;
    public float MembraneRigidity => organelleEditsFacade.MembraneRigidity;

    public Color Colour => SpeciesColour;

    /// <summary>
    ///   Note that this is a very inefficient check
    /// </summary>
    public bool IsBacteria => organelleEditsFacade.IsBacteria;

    public int MPCost => microbeSpecies.MPCost;
    public string CellTypeName => microbeSpecies.CellTypeName;

    public int Count => organelleEditsFacade.Count;

    public IReadOnlyOrganelleTemplate? GetElementAt(Hex location, List<Hex> temporaryHexesStorage)
    {
        return organelleEditsFacade.GetElementAt(location, temporaryHexesStorage);
    }

    public IReadOnlyOrganelleTemplate? GetByExactElementRootPosition(Hex location)
    {
        return organelleEditsFacade.GetByExactElementRootPosition(location);
    }

    public IEnumerator<IReadOnlyOrganelleTemplate> GetEnumerator()
    {
        return organelleEditsFacade.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public MicrobeSpecies Clone(bool cloneOrganelles)
    {
        ResolveDataIfDirty();

        // Do a base copy
        var result = microbeSpecies.Clone(false);

        // And then override properties
        result.IsBacteria = IsBacteria;
        result.MembraneType = MembraneType;
        result.MembraneRigidity = MembraneRigidity;
        result.Colour = SpeciesColour;

        CopyBaseEdits(result);

        return result;
    }

    internal override void OnStartApplyChanges()
    {
        base.OnStartApplyChanges();
        organelleEditsFacade.OnStartApplyChanges();
    }

    internal override bool ApplyAction(EditorCombinableActionData actionData)
    {
        if (actionData is NewMicrobeActionData)
        {
            // We separately apply just the colour change portion, the internal facade does everything else
            SetNewColour(Colors.White);
        }

        // Forward some actions to the base
        if (actionData is BehaviourActionData or ToleranceActionData)
        {
            return base.ApplyAction(actionData);
        }

        if (actionData is ColourActionData)
            base.ApplyAction(actionData);

        // And everything else should be handled by the organelle facade
        return organelleEditsFacade.ApplyAction(actionData);
    }
}
