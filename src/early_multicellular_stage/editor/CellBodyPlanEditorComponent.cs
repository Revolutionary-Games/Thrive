/// <summary>
///   Body plan editor component for making body plans from hexes (that represent cells)
/// </summary>
[SceneLoadedClass("res://src/early_multicellular_stage/editor/CellBodyPlanEditorComponent.tscn")]
public class CellBodyPlanEditorComponent :
    HexEditorComponentBase<EarlyMulticellularEditor, CellEditorAction, CellTemplate>,
    IGodotEarlyNodeResolve
{
    public override bool HasIslands { get; }

    public bool NodeReferencesResolved { get; }

    protected override bool ForceHideHover { get; }

    public override void OnFinishEditing()
    {
        throw new System.NotImplementedException();
    }

    protected override void OnTranslationsChanged()
    {
        throw new System.NotImplementedException();
    }

    protected override int CalculateCurrentActionCost()
    {
        throw new System.NotImplementedException();
    }

    protected override void PerformActiveAction()
    {
        throw new System.NotImplementedException();
    }

    protected override bool DoesActionEndInProgressAction(CellEditorAction action)
    {
        throw new System.NotImplementedException();
    }

    protected override bool IsMoveTargetValid(Hex position, int rotation, CellTemplate hex)
    {
        throw new System.NotImplementedException();
    }

    protected override void OnCurrentActionCanceled()
    {
        throw new System.NotImplementedException();
    }

    protected override void OnMoveActionStarted()
    {
        throw new System.NotImplementedException();
    }

    protected override void PerformMove(int q, int r)
    {
        throw new System.NotImplementedException();
    }

    protected override CellTemplate? GetHexAt(Hex position)
    {
        throw new System.NotImplementedException();
    }

    protected override void TryRemoveHexAt(Hex location)
    {
        throw new System.NotImplementedException();
    }

    protected override void UpdateCancelState()
    {
        throw new System.NotImplementedException();
    }
}
