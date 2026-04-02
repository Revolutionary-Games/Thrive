using System;
using Godot;

/// <summary>
///   Base facade class for species data with edits on top
/// </summary>
public abstract class SpeciesEditsFacade : EditsFacadeBase, IReadOnlySpecies
{
    protected bool overrideColour;
    protected Color newColour;

    private readonly IReadOnlySpecies baseSpecies;

    private BehaviourDictionary? newBehaviour;
    private bool overrideBehaviour;

    private EnvironmentalTolerances? newTolerances;
    private bool overrideTolerances;

    protected SpeciesEditsFacade(IReadOnlySpecies baseSpecies)
    {
        this.baseSpecies = baseSpecies;
    }

    public Color SpeciesColour
    {
        get
        {
            ResolveDataIfDirty();
            return overrideColour ? newColour : baseSpecies.SpeciesColour;
        }
    }

    public IReadOnlyBehaviourDictionary Behaviour
    {
        get
        {
            ResolveDataIfDirty();
            return overrideBehaviour && newBehaviour != null ? newBehaviour : baseSpecies.Behaviour;
        }
    }

    public IReadOnlyEnvironmentalTolerances Tolerances
    {
        get
        {
            ResolveDataIfDirty();
            return overrideTolerances && newTolerances != null ? newTolerances : baseSpecies.Tolerances;
        }
    }

    // Passthroughs
    public uint ID => baseSpecies.ID;
    public string Genus => baseSpecies.Genus;
    public string Epithet => baseSpecies.Epithet;
    public long Population => baseSpecies.Population;
    public int Generation => baseSpecies.Generation;
    public bool PlayerSpecies => baseSpecies.PlayerSpecies;

    // TODO: should this show it is an edited variant?
    public string FormattedName => baseSpecies.FormattedName + " (facade)";
    public string ReadableName => FormattedName;

    internal override bool ApplyAction(EditorCombinableActionData actionData)
    {
        if (actionData is BehaviourActionData behaviourActionData)
        {
            if (!overrideBehaviour || newBehaviour == null)
            {
                newBehaviour ??= new BehaviourDictionary();
                newBehaviour.CopyFrom(baseSpecies.Behaviour);
                overrideBehaviour = true;
            }

            newBehaviour[behaviourActionData.Type] = behaviourActionData.NewValue;
            return true;
        }

        if (actionData is ToleranceActionData toleranceActionData)
        {
            if (!overrideTolerances || newTolerances == null)
            {
                newTolerances = new EnvironmentalTolerances();
                newTolerances.CopyFrom(baseSpecies.Tolerances);
                overrideTolerances = true;
            }

            newTolerances.CopyFrom(toleranceActionData.NewTolerances);
            return true;
        }

        if (actionData is ColourActionData colourActionData)
        {
            SetNewColour(colourActionData.NewColour);
            return true;
        }

        throw new NotSupportedException($"Base species facade doesn't know how to handle: {actionData.GetType()}");
    }

    /// <summary>
    ///   Clears old override data to prepare for a new set of override data
    /// </summary>
    internal override void OnStartApplyChanges()
    {
        overrideBehaviour = false;
        overrideTolerances = false;
        overrideColour = false;
    }

    protected void SetNewColour(Color color)
    {
        overrideColour = true;
        newColour = color;
    }

    protected void CopyBaseEdits(Species target)
    {
        target.ModifiableBehaviour = Behaviour.Clone();
        target.ModifiableTolerances.CopyFrom(Tolerances);
    }
}
