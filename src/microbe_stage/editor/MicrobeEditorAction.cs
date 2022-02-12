/// <summary>
///   Done actions are stored here to provide undo/redo functionality
/// </summary>
/// <remarks>
///   TODO: this probably needs to be split into separate classes to make saving work for these
/// </remarks>
public abstract class MicrobeEditorAction : ReversibleAction
{
    public abstract MicrobeEditorActionData MicrobeData { get; set; }
    public override ActionData Data
    {
        get => MicrobeData;
        set
        {
            MicrobeData = (MicrobeEditorActionData)value;
            MicrobeData.CalculateCost();
        }
    }
}
