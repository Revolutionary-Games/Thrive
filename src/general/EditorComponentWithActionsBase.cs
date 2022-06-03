using Godot;
using Newtonsoft.Json;

/// <summary>
///   Editor component that contains an MP bar, undo / redo buttons etc. related functions
/// </summary>
/// <typeparam name="TEditor">The type of editor this component is contained in</typeparam>
/// <typeparam name="TAction">Editor action type the editor this will be used with will use</typeparam>
public abstract class EditorComponentWithActionsBase<TEditor, TAction> : EditorComponentBase<TEditor>
    where TEditor : IEditorWithActions
    where TAction : EditorAction
{
    [Export]
    public NodePath MutationPointsBarPath = null!;

    [Export]
    public NodePath ComponentBottomLeftButtonsPath = null!;

    [Export]
    public NodePath CancelButtonPath = null!;

    protected EditorComponentBottomLeftButtons componentBottomLeftButtons = null!;

    private MutationPointsBar mutationPointsBar = null!;

    private Button cancelButton = null!;

    /// <summary>
    ///   If true an editor (component) action is active and can be cancelled. By default just checks for moves
    /// </summary>
    [JsonIgnore]
    public abstract bool CanCancelAction { get; }

    public override void _Ready()
    {
        base._Ready();

        mutationPointsBar = GetNode<MutationPointsBar>(MutationPointsBarPath);
        componentBottomLeftButtons = GetNode<EditorComponentBottomLeftButtons>(ComponentBottomLeftButtonsPath);

        cancelButton = GetNode<Button>(CancelButtonPath);
    }

    public override void OnActionBlockedWhileAnotherIsInProgress()
    {
        PlayInvalidActionSound();
    }

    public override void OnInsufficientMP(bool playSound = true)
    {
        mutationPointsBar.PlayFlashAnimation();

        if (playSound)
            PlayInvalidActionSound();
    }

    public void UpdateMutationPointsBar(bool tween = true)
    {
        float possibleMutationPoints = Editor.FreeBuilding ?
            Constants.BASE_MUTATION_POINTS :
            Editor.MutationPoints - CalculateCurrentActionCost();

        mutationPointsBar.UpdateBar(Editor.MutationPoints, possibleMutationPoints, tween);

        mutationPointsBar.UpdateMutationPoints(Editor.FreeBuilding, Editor.ShowHover && Editor.MutationPoints > 0,
            Editor.MutationPoints, possibleMutationPoints);
    }

    public override void OnMutationPointsChanged(int mutationPoints)
    {
        UpdateMutationPointsBar(true);
    }

    public override void NotifyFreebuild(bool freebuilding)
    {
        base.NotifyFreebuild(freebuilding);
        componentBottomLeftButtons.ShowNewButton = freebuilding;
        UpdateMutationPointsBar(false);
    }

    /// <summary>
    ///   Updates the visibility of the current action cancel button.
    /// </summary>
    public void UpdateCancelButtonVisibility()
    {
        cancelButton.Visible = Editor.CanCancelAction;
    }

    public override void UpdateUndoRedoButtons(bool canUndo, bool canRedo)
    {
        SetUndoButtonStatus(canUndo && !Editor.CanCancelAction);
        SetRedoButtonStatus(canRedo && !Editor.CanCancelAction);
    }

    protected void Undo()
    {
        Editor.Undo();
    }

    protected void Redo()
    {
        Editor.Redo();
    }

    protected virtual void OnCancelActionClicked()
    {
        GUICommon.Instance.PlayButtonPressSound();
        Editor.CancelCurrentAction();
    }

    protected void SetUndoButtonStatus(bool enabled)
    {
        componentBottomLeftButtons.UndoEnabled = enabled;
    }

    protected void SetRedoButtonStatus(bool enabled)
    {
        componentBottomLeftButtons.RedoEnabled = enabled;
    }

    /// <summary>
    ///   Calculates the cost of the current editor action (may be 0 if free or no active action)
    /// </summary>
    protected abstract int CalculateCurrentActionCost();

    /// <summary>
    ///   Special enqueue that can have special logic in specific components to pre-process actions before passing
    ///   them to the editor
    /// </summary>
    protected virtual bool EnqueueAction(TAction action)
    {
        return Editor.EnqueueAction(action);
    }

    /// <summary>
    ///   Registers tooltip for the already existing Controls in the editor GUI
    /// </summary>
    protected override void RegisterTooltips()
    {
        base.RegisterTooltips();

        componentBottomLeftButtons.RegisterTooltips();

        cancelButton.RegisterToolTipForControl("cancelButton", "editor");
    }
}
