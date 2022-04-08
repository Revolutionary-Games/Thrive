using System;
using Newtonsoft.Json;

public class CellEditorAction : ReversibleAction
{
    // TODO: split this to a separate class / interface (I'm not doing it here due to the open dynamic MP PR
    // -hhyyrylainen)
    [JsonProperty]
    public readonly int Cost;

    /// <summary>
    ///   Action specific data
    /// </summary>
    [JsonProperty]
    public IMicrobeEditorActionData? Data;

    [JsonProperty]
    private readonly Action<CellEditorAction> redo;

    [JsonProperty]
    private readonly Action<CellEditorAction> undo;

    [JsonProperty]
    private readonly ICellEditorData editor;

    public CellEditorAction(ICellEditorData editor, int cost,
        Action<CellEditorAction> redo,
        Action<CellEditorAction> undo, IMicrobeEditorActionData? data = null)
    {
        this.editor = editor;
        Cost = cost;
        this.redo = redo;
        this.undo = undo;
        Data = data;
    }

    [JsonIgnore]
    public bool IsMoveAction => Data is MoveActionData;

    public override void DoAction()
    {
        editor.ChangeMutationPoints(-Cost);
        redo(this);
    }

    public override void UndoAction()
    {
        editor.ChangeMutationPoints(Cost);
        undo(this);
    }
}
