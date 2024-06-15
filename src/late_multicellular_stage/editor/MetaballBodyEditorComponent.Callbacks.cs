using System.Diagnostics;
using Godot;

/// <summary>
///   Callbacks for the metaball body editor
/// </summary>
[DeserializedCallbackTarget]
public partial class MetaballBodyEditorComponent
{
    [DeserializedCallbackAllowed]
    private void OnMetaballAdded(MulticellularMetaball metaball)
    {
        metaballDisplayDataDirty = true;
    }

    [DeserializedCallbackAllowed]
    private void OnMetaballRemoved(MulticellularMetaball metaball)
    {
        metaballDisplayDataDirty = true;
    }

    [DeserializedCallbackAllowed]
    private void DoMetaballRemoveAction(MetaballRemoveActionData<MulticellularMetaball> data)
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

    [DeserializedCallbackAllowed]
    private void UndoMetaballRemoveAction(MetaballRemoveActionData<MulticellularMetaball> data)
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

    [DeserializedCallbackAllowed]
    private void DoMetaballPlaceAction(MetaballPlacementActionData<MulticellularMetaball> data)
    {
        editedMetaballs.Add(data.PlacedMetaball);
    }

    [DeserializedCallbackAllowed]
    private void UndoMetaballPlaceAction(MetaballPlacementActionData<MulticellularMetaball> data)
    {
        editedMetaballs.Remove(data.PlacedMetaball);
    }

    [DeserializedCallbackAllowed]
    private void DoMetaballMoveAction(MetaballMoveActionData<MulticellularMetaball> data)
    {
        data.MovedMetaball.Position = data.NewPosition;
        data.MovedMetaball.Parent = data.NewParent;

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

    [DeserializedCallbackAllowed]
    private void UndoMetaballMoveAction(MetaballMoveActionData<MulticellularMetaball> data)
    {
        data.MovedMetaball.Position = data.OldPosition;
        data.MovedMetaball.Parent = data.OldParent;

        metaballDisplayDataDirty = true;

        if (data.MovedChildMetaballs != null)
        {
            foreach (var movementAction in data.MovedChildMetaballs)
            {
                UndoMetaballMoveAction(movementAction);
            }
        }
    }
}
