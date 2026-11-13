using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

/// <content>
///   Builds the current body plan's independent input for Auto-Evo prediction.
/// </content>
public partial class CellBodyPlanEditorComponent
{
    public override void _ExitTree()
    {
        StopAutoEvoPrediction();
        base._ExitTree();
    }

    /// <summary>
    ///   Waits for the prediction to finish when leaving the scene tree. Editing uses independent prediction data
    ///   and does not need to wait.
    /// </summary>
    public void StopAutoEvoPrediction()
    {
        autoEvoPrediction.Stop();
    }

    private MulticellularSpecies? CreateAutoEvoPrediction()
    {
        return TryCreateAutoEvoPredictionSpecies(out var prediction) ? prediction : null;
    }

    private void OnAutoEvoPredictionComplete(AutoEvoRun run, Species originalSpecies, Species predictedSpecies)
    {
        var previousEnergy = Editor.PreviousAutoEvoResults?.GetPatchEnergyResults(Editor.EditedSpecies)
            .Sum(p => p.Value.TotalEnergyGathered);
        autoEvoPredictionPanel.DisplayRun(run, originalSpecies, predictedSpecies,
            previousEnergy, Editor.CurrentPatch);
    }

    private bool TryCreateAutoEvoPredictionSpecies([NotNullWhen(true)] out MulticellularSpecies? species)
    {
        species = null;

        if (!Editor.EditorReady || MovingPlacedHex != null || editedMicrobeCells is not { Count: > 0 })
            return false;

        var behaviour = behaviourEditor.Behaviour;
        if (behaviour == null)
            return false;

        // Desired bud size can exceed the current body size after removing cells, just as when finishing the editor.
        int budSize = Math.Min(DesiredMassBuddingCellCount,
            CellBodyPlanInternalCalculations.MaxBudSize(editedMicrobeCells.Count));
        if (ReproductionMethod == MulticellularReproductionMethod.MassBudding && budSize < 1)
            return false;

        var original = Editor.EditedSpecies;
        var predictionCells = new IndividualHexLayout<CellTemplate>();
        var prediction = new MulticellularSpecies(original.ID, original.Genus, original.Epithet)
        {
            ModifiableEditorCells = predictionCells,
            ReproductionMethod = ReproductionMethod,
            MassBuddingCellCount = ReproductionMethod == MulticellularReproductionMethod.MassBudding ?
                budSize :
                original.MassBuddingCellCount,
            PlayerGamete = ReproductionMethod switch
            {
                MulticellularReproductionMethod.SexualIsogamy => GameteType.A,
                MulticellularReproductionMethod.SexualAnisogamy => SelectedGameteTypeForPlayer,
                _ => GameteType.All,
            },
            UsesManualPlayerLayout = UsesManualPlayerLayout,
            IsReadyForMacroscopic = original.IsReadyForMacroscopic,
        };

        // The original species' layout can still reference a deleted cell type until editing finishes, so copy
        // only its base properties and build the body from the editor's current data.
        original.ClonePropertiesTo(prediction);
        prediction.ModifiableBehaviour = new BehaviourDictionary(behaviour);
        prediction.ModifiableTolerances.CopyFrom(tolerancesEditor.CurrentTolerances);

        if (original.PlayerSpecies)
            prediction.BecomePlayerSpecies();

        var typeMapping = new Dictionary<CellType, CellType>(ReferenceEqualityComparer.Instance);
        foreach (var cellType in original.ModifiableCellTypes)
        {
            var editedType = GetEditedCellDataIfEdited(cellType);
            var clonedType = editedType.Clone(true);
            prediction.ModifiableCellTypes.Add(clonedType);
            typeMapping.Add(editedType, clonedType);
        }

        var cells = new List<(HexWithData<CellTemplate> Source, HexWithData<CellTemplate> Target)>(
            editedMicrobeCells.Count);

        foreach (var cell in editedMicrobeCells.AsModifiable())
        {
            if (cell.Data == null ||
                !typeMapping.TryGetValue(GetEditedCellDataIfEdited(cell.Data.ModifiableCellType), out var clonedType))
            {
                return false;
            }

            var template = new CellTemplate(clonedType, cell.Position, cell.Orientation, cell.Data.IsCavity);
            cells.Add((cell, new HexWithData<CellTemplate>(template, cell.Position, cell.Orientation)));
        }

        PrepareBodyPlanLayout(prediction, predictionCells, cells);

        if (SporeCellType != null)
            prediction.ModifiableSporeCellType = typeMapping[GetEditedCellDataIfEdited(SporeCellType)];

        if (GameteACellType != null)
            prediction.ModifiableGameteTypeA = typeMapping[GetEditedCellDataIfEdited(GameteACellType)];

        if (GameteBCellType != null)
            prediction.ModifiableGameteTypeB = typeMapping[GetEditedCellDataIfEdited(GameteBCellType)];

        // Auto-Evo works with the compact body plan, without generating or validating a gameplay layout.
        prediction.OnAttemptedInAutoEvo(false);
        species = prediction;
        return true;
    }
}
