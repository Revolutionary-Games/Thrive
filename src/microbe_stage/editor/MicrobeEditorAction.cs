using System;
using Newtonsoft.Json;

/// <summary>
///   Done actions are stored here to provide undo/redo functionality
/// </summary>
public class MicrobeEditorAction : ReversibleAction
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
    private readonly Action<MicrobeEditorAction> redo;

    [JsonProperty]
    private readonly Action<MicrobeEditorAction> undo;

    [JsonProperty]
    private readonly MicrobeEditor editor;

    public MicrobeEditorAction(MicrobeEditor editor, int cost,
        Action<MicrobeEditorAction> redo,
        Action<MicrobeEditorAction> undo, IMicrobeEditorActionData? data = null)
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
