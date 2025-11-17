using System;
using System.Collections.Generic;
using Godot;

/// <summary>
///   Base facade class for species data with edits on top
/// </summary>
public abstract class SpeciesEditsFacade : IReadOnlySpecies
{
    protected readonly List<EditorCombinableActionData> activeActions = new();

    protected bool overrideColour;
    protected Color newColour;

    private readonly IReadOnlySpecies baseSpecies;

    /// <summary>
    ///   If <see cref="activeActions"/> contains a history reset action, this is the index of that action.
    ///   When applying changes, this is used to quickly skip over useless things.
    /// </summary>
    private int lastHistoryReset = -1;

    private bool dirty;

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

    /// <summary>
    ///   Applies a new set of actions replacing the old set
    /// </summary>
    /// <returns>A checkpoint number that can be used to revert another set of actions</returns>
    public int SetActiveActions(List<EditorCombinableActionData> actions)
    {
        activeActions.Clear();
        activeActions.AddRange(actions);

        RefreshHistoryResetPoint();

        MarkDirty();
        return activeActions.Count;
    }

    public int AppendActions(IEnumerable<EditorCombinableActionData> data)
    {
        foreach (var actionData in data)
        {
            activeActions.Add(actionData);

            if (actionData.ResetsHistory)
            {
                lastHistoryReset = activeActions.Count - 1;
            }
        }

        MarkDirty();
        return activeActions.Count;
    }

    public int AppendAction(EditorCombinableActionData singleAction)
    {
        activeActions.Add(singleAction);

        if (singleAction.ResetsHistory)
        {
            lastHistoryReset = activeActions.Count - 1;
        }

        MarkDirty();
        return activeActions.Count;
    }

    /// <summary>
    ///   Returns to a checkpoint created by <see cref="SetActiveActions"/>, for example, to revert
    ///   <see cref="AppendActions"/> call.
    /// </summary>
    /// <param name="checkpoint">Checkpoint to return to</param>
    /// <returns>True if the checkpoint is valid</returns>
    public bool RevertAppend(int checkpoint)
    {
        if (checkpoint < 0 || checkpoint > activeActions.Count)
            return false;

        while (activeActions.Count >= checkpoint)
        {
            activeActions.RemoveAt(activeActions.Count - 1);
            MarkDirty();
        }

        if (lastHistoryReset >= activeActions.Count)
        {
            RefreshHistoryResetPoint();
        }

        return true;
    }

    public void ResolveDataIfDirty()
    {
        if (!dirty)
            return;

        OnStartApplyChanges();

        // We need to only process after the latest history reset action as nothing from-before-that can affect things
        int count = activeActions.Count;
        for (int i = lastHistoryReset + 1; i < count; ++i)
        {
            if (!ApplyAction(activeActions[i]))
            {
                throw new InvalidOperationException(
                    $"Could not apply action data at index {i}: {activeActions[i]} ({activeActions[i].GetType()})");
            }
        }

        dirty = false;
    }

    protected virtual bool ApplyAction(EditorCombinableActionData actionData)
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
    protected virtual void OnStartApplyChanges()
    {
        overrideBehaviour = false;
        overrideTolerances = false;
        overrideColour = false;
    }

    protected void MarkDirty()
    {
        dirty = true;
    }

    protected void RefreshHistoryResetPoint()
    {
        lastHistoryReset = -1;
        int count = activeActions.Count;
        for (int i = 0; i < count; ++i)
        {
            if (activeActions[i].ResetsHistory)
                lastHistoryReset = i;
        }
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
