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
    private readonly Action<MicrobeEditorAction> redo;

    [JsonProperty]
    private readonly Action<MicrobeEditorAction> undo;

    [JsonProperty]
    private readonly MicrobeEditor editor;

    private MicrobeEditorActionData data;

    public MicrobeEditorAction(MicrobeEditor editor,
        Action<MicrobeEditorAction> redo,
        Action<MicrobeEditorAction> undo, MicrobeEditorActionData data = null)
    {
        this.editor = editor;
        this.redo = redo;
        this.undo = undo;
        Data = data;
    }

    /// <summary>
    ///   Action specific data
    /// </summary>
    [JsonProperty]
    public MicrobeEditorActionData Data
    {
        get => data;
        set
        {
            data = value;
            value.CalculateCost();
        }
    }

    [JsonIgnore]
    public bool IsMoveAction => Data is MoveActionData;

    public override bool IsSubAction => Data.IsSubAction;

    public override void DoAction()
    {
        redo(this);
    }

    public override void UndoAction()
    {
        undo(this);
    }
}
