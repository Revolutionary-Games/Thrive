using System;
using System.Collections.Generic;

/// <summary>
///   Reads the body editor into an isolated, complete design for Auto-Evo.
/// </summary>
public partial class CellBodyPlanEditorComponent
{
    public EditorAutoEvoPredictionPanel PredictionPanel =>
        GetNode<EditorAutoEvoPredictionPanel>("RightPanel/VBoxContainer/AutoEvoInfoPanel");

    /// <summary>
    ///   Captures committed body and buffered type edits without applying any changes to the editor or world.
    ///   The optional live type is already detached and becomes owned by this snapshot.
    ///   A temporarily incomplete reproduction design can be retried after the next committed edit.
    /// </summary>
    public bool TryCreatePredictionSpecies(out MulticellularSpecies? result, CellType? originalLiveType = null,
        CellType? liveType = null)
    {
        result = null;
        if (CanCancelMove || editedMicrobeCells.Count < 1 || CellTypeVisualsOverride == null)
            return false;

        if (ReproductionMethod == MulticellularReproductionMethod.Sporulation && SporeCellType == null)
            return false;

        if (ReproductionMethod is MulticellularReproductionMethod.SexualAnisogamy
            or MulticellularReproductionMethod.SexualIsogamy)
        {
            if (GameteACellType == null || editedMicrobeCells.Count < 2)
                return false;

            if (ReproductionMethod == MulticellularReproductionMethod.SexualAnisogamy &&
                (GameteBCellType == null || SelectedGameteTypeForPlayer == GameteType.All))
            {
                return false;
            }
        }

        var source = Editor.EditedSpecies;
        var species = new MulticellularSpecies(source.ID, source.Genus, source.Epithet);
        source.ClonePropertiesTo(species);
        var types = new Dictionary<CellType, CellType>(ReferenceEqualityComparer.Instance);

        foreach (var original in source.ModifiableCellTypes)
        {
            var current = ReferenceEquals(original, originalLiveType) ?
                liveType! :
                CellTypeVisualsOverride.GetCellType(original);
            if (current.Organelles.Count < 1)
                return false;

            var copy = ReferenceEquals(original, originalLiveType) ? current : current.Clone(true);
            types.Add(original, copy);
            species.ModifiableCellTypes.Add(copy);
        }

        CellType? MapType(CellType? original)
        {
            if (original == null)
                return null;

            return types.GetValueOrDefault(CellTypeVisualsOverride.GetOriginalType(original));
        }

        species.ModifiableSporeCellType = MapType(SporeCellType);
        species.ModifiableGameteTypeA = MapType(GameteACellType);
        species.ModifiableGameteTypeB = MapType(GameteBCellType);
        if ((SporeCellType != null && species.ModifiableSporeCellType == null) ||
            (GameteACellType != null && species.ModifiableGameteTypeA == null) ||
            (GameteBCellType != null && species.ModifiableGameteTypeB == null))
        {
            return false;
        }

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();
        var workMemory3 = new HashSet<Hex>();
        Hex leader = default;
        bool first = true;
        foreach (var cell in growthOrderGUI.ApplyOrderingToItems(editedMicrobeCells.AsModifiable(), c => c.Data!))
        {
            if (cell.Data == null)
                return false;

            var type = MapType(cell.Data.ModifiableCellType);
            if (type == null || ReferenceEquals(type, species.ModifiableSporeCellType))
                return false;

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
        species.MassBuddingCellCount = source.MassBuddingCellCount;
        if (ReproductionMethod == MulticellularReproductionMethod.MassBudding)
        {
            species.MassBuddingCellCount = Math.Min(DesiredMassBuddingCellCount,
                CellBodyPlanInternalCalculations.MaxBudSize(editedMicrobeCells.Count));
            if (species.MassBuddingCellCount < Constants.MASS_BUDDING_MINIMUM_BUD_SIZE)
                return false;
        }

        if (species.MassBuddingCellCount < 1 || species.MassBuddingCellCount > editedMicrobeCells.Count)
            return false;

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
}
