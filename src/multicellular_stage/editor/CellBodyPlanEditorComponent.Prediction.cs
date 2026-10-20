using System;
using System.Collections.Generic;

/// <summary>
///   Reads the body editor into an isolated, complete design for Auto-Evo.
/// </summary>
public partial class CellBodyPlanEditorComponent
{
    public EditorAutoEvoPredictionPanel PredictionPanel => autoEvoPredictionPanel;

    /// <summary>
    ///   Checks the same live inputs used to create a prediction without cloning the species.
    ///   Moving cells or inputs that have not been prepared are not invalid species designs.
    /// </summary>
    public PredictionInputState GetPredictionInputState()
    {
        if (CanCancelMove || CellTypeVisualsOverride == null)
            return PredictionInputState.NotReady;

        if (editedMicrobeCells.Count < 1)
            return PredictionInputState.Invalid;

        if (ReproductionMethod == MulticellularReproductionMethod.Sporulation && SporeCellType == null)
            return PredictionInputState.Invalid;

        if (ReproductionMethod is MulticellularReproductionMethod.SexualAnisogamy
            or MulticellularReproductionMethod.SexualIsogamy)
        {
            if (GameteACellType == null || editedMicrobeCells.Count < 2)
                return PredictionInputState.Invalid;

            if (ReproductionMethod == MulticellularReproductionMethod.SexualAnisogamy &&
                (GameteBCellType == null || SelectedGameteTypeForPlayer == GameteType.All))
            {
                return PredictionInputState.Invalid;
            }
        }

        var types = new HashSet<CellType>(Editor.EditedSpecies.ModifiableCellTypes,
            ReferenceEqualityComparer.Instance);

        foreach (var original in types)
        {
            if (CellTypeVisualsOverride.GetCellType(original).Organelles.Count < 1)
                return PredictionInputState.Invalid;
        }

        CellType? MapType(CellType? current)
        {
            if (current == null)
                return null;

            var original = CellTypeVisualsOverride.GetOriginalType(current);
            return types.Contains(original) ? original : null;
        }

        var sporeType = MapType(SporeCellType);
        if ((SporeCellType != null && sporeType == null) ||
            (GameteACellType != null && MapType(GameteACellType) == null) ||
            (GameteBCellType != null && MapType(GameteBCellType) == null))
        {
            return PredictionInputState.Invalid;
        }

        foreach (var cell in editedMicrobeCells)
        {
            if (cell.Data == null)
                return PredictionInputState.Invalid;

            var type = MapType(cell.Data.ModifiableCellType);
            if (type == null || ReferenceEquals(type, sporeType))
                return PredictionInputState.Invalid;
        }

        var budCount = GetPredictionMassBuddingCellCount();
        if (ReproductionMethod == MulticellularReproductionMethod.MassBudding &&
            budCount < Constants.MASS_BUDDING_MINIMUM_BUD_SIZE)
        {
            return PredictionInputState.Invalid;
        }

        if (budCount < 1 || budCount > editedMicrobeCells.Count)
            return PredictionInputState.Invalid;

        return PredictionInputState.Valid;
    }

    /// <summary>
    ///   Captures committed body and buffered type edits without applying any changes to the editor or world.
    ///   A temporarily incomplete reproduction design can be retried after the next committed edit.
    /// </summary>
    public bool TryCreatePredictionSpecies(out MulticellularSpecies? result)
    {
        result = null;
        if (GetPredictionInputState() != PredictionInputState.Valid)
            return false;

        var source = Editor.EditedSpecies;
        var species = new MulticellularSpecies(source.ID, source.Genus, source.Epithet);
        source.ClonePropertiesTo(species);
        var types = new Dictionary<CellType, CellType>(ReferenceEqualityComparer.Instance);

        foreach (var original in source.ModifiableCellTypes)
        {
            var copy = CellTypeVisualsOverride!.GetCellType(original).Clone(true);
            types.Add(original, copy);
            species.ModifiableCellTypes.Add(copy);
        }

        CellType? MapType(CellType? original)
        {
            if (original == null)
                return null;

            return types[CellTypeVisualsOverride!.GetOriginalType(original)];
        }

        species.ModifiableSporeCellType = MapType(SporeCellType);
        species.ModifiableGameteTypeA = MapType(GameteACellType);
        species.ModifiableGameteTypeB = MapType(GameteBCellType);

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();
        var workMemory3 = new HashSet<Hex>();
        Hex leader = default;
        bool first = true;
        foreach (var cell in growthOrderGUI.ApplyOrderingToItems(editedMicrobeCells.AsModifiable(), c => c.Data!))
        {
            var type = MapType(cell.Data!.ModifiableCellType)!;
            if (first)
            {
                leader = cell.Position;
                first = false;
            }

            var position = cell.Position - leader;
            var template = new CellTemplate(type, position, cell.Orientation);
            species.ModifiableEditorCells.AddFast(new HexWithData<CellTemplate>(template, position, cell.Orientation),
                workMemory1, workMemory2);
        }

        species.ReproductionMethod = ReproductionMethod;
        species.MassBuddingCellCount = GetPredictionMassBuddingCellCount();

        species.PlayerGamete = ReproductionMethod switch
        {
            MulticellularReproductionMethod.SexualIsogamy => GameteType.A,
            MulticellularReproductionMethod.SexualAnisogamy => SelectedGameteTypeForPlayer,
            _ => GameteType.All,
        };

        if (behaviourEditor.Behaviour != null)
            species.ModifiableBehaviour = new BehaviourDictionary(behaviourEditor.Behaviour);

        species.ModifiableTolerances.CopyFrom(tolerancesEditor.CurrentTolerances);
        species.RepositionCellTypesToOrigin();

        // Auto-Evo evaluates the compact design. Avoid the high-quality gameplay layout work used on final apply.
        MulticellularLayoutHelpers.UpdateGameplayLayoutForAutoEvo(species.ModifiableGameplayCells,
            species.ModifiableEditorCells, workMemory1, workMemory2, workMemory3);
        species.BecomePlayerSpecies();
        species.OnEdited();
        result = species;
        return true;
    }

    private int GetPredictionMassBuddingCellCount()
    {
        if (ReproductionMethod == MulticellularReproductionMethod.MassBudding)
        {
            return Math.Min(DesiredMassBuddingCellCount,
                CellBodyPlanInternalCalculations.MaxBudSize(editedMicrobeCells.Count));
        }

        return Editor.EditedSpecies.MassBuddingCellCount;
    }
}
