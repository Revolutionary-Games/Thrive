/// <summary>
///   The <see cref="ReproductionOrderEditor{THex,TEditor,TCombinedAction,TAction,THexMove,TContext}"/> for
///   <see cref="EarlyMulticellularSpecies"/>. This mostly exists because nodes in the Godot editor can't bind to
///   generic classes.
/// </summary>
public class EarlyMulticellularReproductionOrderEditor : ReproductionOrderEditor<CellTemplate, EarlyMulticellularEditor,
    CombinedEditorAction, EditorAction, HexWithData<CellTemplate>, EarlyMulticellularSpecies>
{
}
