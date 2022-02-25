/// <summary>
///   Interface extracted to make GUI generic parameters work
/// </summary>
public interface IEditor
{
    /// <summary>
    ///   The number of mutation points left
    /// </summary>
    public int MutationPoints { get; }

    /// <summary>
    ///   When true nothing costs MP
    /// </summary>
    public bool FreeBuilding { get; }

    /// <summary>
    ///   True when there is an action that can be canceled
    /// </summary>
    public bool CanCancelAction { get; }

    /// <summary>
    ///   True once fade transition is finished when entering editor
    /// </summary>
    public bool TransitionFinished { get; }

    /// <summary>
    ///   True when the editor view is active and the user can perform an action (for example place an organelle)
    /// </summary>
    public bool ShowHover { get; set; }

    /// <summary>
    ///   Calculates the cost of the current editor action (may be 0 if free or no active action)
    /// </summary>
    public float CalculateCurrentActionCost();

    /// <summary>
    ///   Cancels the current editor action if possible
    /// </summary>
    /// <returns>True if canceled</returns>
    public bool CancelCurrentAction();
}
