using System.Diagnostics;
using Godot;
using SharedBase.Archive;

/// <summary>
///   Callbacks for the metaball body editor
/// </summary>
public partial class MetaballBodyEditorComponent
{
    [ArchiveAllowedMethod]
    private void OnMetaballAdded(MacroscopicMetaball metaball)
    {
        metaballDisplayDataDirty = true;
    }

    [ArchiveAllowedMethod]
    private void OnMetaballRemoved(MacroscopicMetaball metaball)
    {
        metaballDisplayDataDirty = true;
    }

    [ArchiveAllowedMethod]
    private void DoMetaballRemoveAction(MetaballRemoveActionData<MacroscopicMetaball> data)
    {
        editedMetaballs.Remove(data.RemovedMetaball);

        // If there are any metaballs that were the children of the removed metaball, we need to fix those
        if (data.ReParentedMetaballs != null)
        {
            foreach (var movementAction in data.ReParentedMetaballs)
            {
                DoMetaballMoveAction(movementAction);
            }
        }
    }

    [ArchiveAllowedMethod]
    private void UndoMetaballRemoveAction(MetaballRemoveActionData<MacroscopicMetaball> data)
    {
        if (data.ReParentedMetaballs != null)
        {
            foreach (var movementAction in data.ReParentedMetaballs)
            {
                UndoMetaballMoveAction(movementAction);
            }
        }

        editedMetaballs.Add(data.RemovedMetaball);
    }

    [ArchiveAllowedMethod]
    private void DoMetaballPlaceAction(MetaballPlacementActionData<MacroscopicMetaball> data)
    {
        editedMetaballs.Add(data.PlacedMetaball);
    }

    [ArchiveAllowedMethod]
    private void UndoMetaballPlaceAction(MetaballPlacementActionData<MacroscopicMetaball> data)
    {
        editedMetaballs.Remove(data.PlacedMetaball);
    }

    [ArchiveAllowedMethod]
    private void DoMetaballMoveAction(MetaballMoveActionData<MacroscopicMetaball> data)
    {
        data.MovedMetaball.Position = data.NewPosition;
        data.MovedMetaball.ModifiableParent = data.NewParent;

        if (editedMetaballs.Contains(data.MovedMetaball))
        {
            metaballDisplayDataDirty = true;

            // TODO: notify auto-evo prediction once that is done
        }
        else
        {
            editedMetaballs.Add(data.MovedMetaball);
        }

        if (data.MovedChildMetaballs != null)
        {
            foreach (var movementAction in data.MovedChildMetaballs)
            {
#if DEBUG
                if (ReferenceEquals(data.MovedMetaball, movementAction.MovedMetaball))
                {
                    GD.PrintErr("Child metaball move references the primary metaball");
                    if (Debugger.IsAttached)
                        Debugger.Break();

                    return;
                }

#endif

                DoMetaballMoveAction(movementAction);
            }
        }
    }

    [ArchiveAllowedMethod]
    private void UndoMetaballMoveAction(MetaballMoveActionData<MacroscopicMetaball> data)
    {
        data.MovedMetaball.Position = data.OldPosition;
        data.MovedMetaball.ModifiableParent = data.OldParent;

        metaballDisplayDataDirty = true;

        if (data.MovedChildMetaballs != null)
        {
            foreach (var movementAction in data.MovedChildMetaballs)
            {
                UndoMetaballMoveAction(movementAction);
            }
        }
    }
    
    [ArchiveAllowedMethod]
    private void DuplicateCellType(DuplicateDeleteCellTypeData data)
    {
        var originalName = data.CellType.CellTypeName;
        var count = 1;

        // Lógica de segurança: garante que o nome seja único ao refazer a ação,
        // caso o jogador tenha criado outro tipo com o mesmo nome enquanto essa ação estava desfeita.
        // Usa o método auxiliar existente na classe principal.
        while (!IsNewCellTypeNameValid(data.CellType.CellTypeName))
        {
            data.CellType.CellTypeName = $"{originalName} {count++}";
        }

        Editor.EditedSpecies.ModifiableCellTypes.Add(data.CellType);
        GD.Print("New cell type created: ", data.CellType.CellTypeName);

        UpdateCellTypeSelections();

        // Seleciona o novo tipo automaticamente (equivalente ao OnCellToPlaceSelected)
        activeActionName = data.CellType.CellTypeName;
        OnCurrentActionChanged();

        // Emite sinal para outros componentes saberem que este tipo foi selecionado
        EmitSignal(SignalName.OnCellTypeToEditSelected, data.CellType.CellTypeName);
    }

    [ArchiveAllowedMethod]
    private void DeleteCellType(DuplicateDeleteCellTypeData data)
    {
        if (!Editor.EditedSpecies.ModifiableCellTypes.Remove(data.CellType))
        {
            GD.PrintErr("Failed to delete cell type from species");
        }

        // Se o tipo deletado estava selecionado, limpa a seleção
        if (activeActionName == data.CellType.CellTypeName)
        {
            ClearSelectedAction();
        }

        UpdateCellTypeSelections();
    }
}
