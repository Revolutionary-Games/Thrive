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

    /// <summary>
    ///   Action specific data
    /// </summary>
    [JsonProperty]
    public IMicrobeEditorActionData Data;

    [JsonProperty]
    private readonly Action<MicrobeEditorAction, MicrobeSpecies> redo;

    [JsonProperty]
    private readonly Action<MicrobeEditorAction, MicrobeSpecies> undo;

    [JsonProperty]
    private readonly MicrobeEditor editor;

    public MicrobeEditorAction(MicrobeEditor editor, int cost,
        Action<MicrobeEditorAction, MicrobeSpecies> redo,
        Action<MicrobeEditorAction, MicrobeSpecies> undo, IMicrobeEditorActionData data = null)
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
        redo(this, editor.CurrentSpecies);
        editor.UpdateMutationPoints();
        editor.UpdateGUI();
    }

    public override void UndoAction()
    {
        undo(this, editor.CurrentSpecies);
        editor.UpdateMutationPoints();
        editor.UpdateGUI();
    }

    public int Forcast()
    {
        var prospectiveSpecies = (MicrobeSpecies)editor.CurrentSpecies.Clone();
        redo(this, prospectiveSpecies);
        return editor.MutationPointsAfterChange(prospectiveSpecies);
    }
}
