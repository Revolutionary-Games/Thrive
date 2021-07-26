using System;
using Newtonsoft.Json;

/// <summary>
///   Done actions are stored here to provide undo/redo functionality
/// </summary>
/// <remarks>
///   TODO: this probably needs to be split into separate classes to make saving work for these
/// </remarks>
public class MicrobeEditorAction : ReversibleAction
{
    [JsonProperty]
    public readonly int Cost;

    [JsonProperty]
    public readonly Action<MicrobeEditorAction> Redo;

    [JsonProperty]
    public readonly Action<MicrobeEditorAction> Undo;

    [JsonProperty]
    public readonly MicrobeEditor Editor;

    /// <summary>
    ///   Action specific data
    /// </summary>
    [JsonProperty]
    public IMicrobeEditorActionData Data;

    public MicrobeEditorAction(MicrobeEditor editor, int cost,
        Action<MicrobeEditorAction> redo,
        Action<MicrobeEditorAction> undo, IMicrobeEditorActionData data = null)
    {
        Editor = editor;
        Cost = cost;
        Redo = redo;
        Undo = undo;
        Data = data;
    }

    [JsonIgnore]
    public bool IsMoveAction => Data is MoveActionData;

    public override void DoAction()
    {
        Editor.ChangeMutationPoints(-Cost);
        Redo(this);
    }

    public override void UndoAction()
    {
        Editor.ChangeMutationPoints(Cost);
        Undo(this);
    }
}
