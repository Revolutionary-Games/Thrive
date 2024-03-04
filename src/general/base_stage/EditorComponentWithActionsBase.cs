using Godot;
using Newtonsoft.Json;

/// <summary>
///   Editor component that contains an MP bar, undo / redo buttons etc. related functions
/// </summary>
/// <typeparam name="TEditor">The type of editor this component is contained in</typeparam>
/// <typeparam name="TAction">Editor action type the editor this will be used with will use</typeparam>
[GodotAbstract]
public partial class EditorComponentWithActionsBase<TEditor, TAction> : EditorComponentBase<TEditor>
    where TEditor : IEditorWithActions
    where TAction : EditorAction
{
    [Export]
    public NodePath? MutationPointsBarPath;

    [Export]
    public NodePath ComponentBottomLeftButtonsPath = null!;

    [Export]
    public NodePath CancelButtonPath = null!;

    [Export]
    public NodePath FinishWarningBadgePath = null!;

#pragma warning disable CA2213
    protected EditorComponentBottomLeftButtons componentBottomLeftButtons = null!;

    private MutationPointsBar mutationPointsBar = null!;

    private Button cancelButton = null!;
    private TextureRect finishButtonWarningBadge = null!;
#pragma warning restore CA2213

    protected EditorComponentWithActionsBase()
    {
    }

    /// <summary>
    ///   If true an editor (component) action is active and can be cancelled. By default just checks for moves
    /// </summary>
    [JsonIgnore]
    public virtual bool CanCancelAction => throw new GodotAbstractPropertyNotOverriddenException();

    /// <summary>
    ///   If true, the finish button will have a warning badge shown on the top right to indicate that something
    ///   requires player attention before finishing.
    /// </summary>
    [JsonIgnore]
    public virtual bool ShowFinishButtonWarning => CanCancelAction;

    public override void _Ready()
    {
        base._Ready();

        mutationPointsBar = GetNode<MutationPointsBar>(MutationPointsBarPath);
        componentBottomLeftButtons = GetNode<EditorComponentBottomLeftButtons>(ComponentBottomLeftButtonsPath);

        cancelButton = GetNode<Button>(CancelButtonPath);
        finishButtonWarningBadge = GetNode<TextureRect>(FinishWarningBadgePath);
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

        // Show warning when finishing is blocked due to an in-progress action or vice versa
        UpdateFinishButtonWarningVisibility();
    }

    public override void UpdateUndoRedoButtons(bool canUndo, bool canRedo)
    {
        SetUndoButtonStatus(canUndo && !Editor.CanCancelAction);
        SetRedoButtonStatus(canRedo && !Editor.CanCancelAction);

        // Update the finish badge warning visibility after updating action history
        // because undoing and redoing actions can cause or fix warnings
        UpdateFinishButtonWarningVisibility();
    }

    /// <summary>
    ///   Hides the finish button warning badge after the hide animation plays.
    /// </summary>
    /// <param name="animation">The name of the animation that just finished playing.</param>
    public void HideFinishButtonWarningAfterAnimation(string animation)
    {
        // Before hiding the warning badge, make sure it's still supposed to be hidden
        if (animation == "hide" && !ShowFinishButtonWarning)
            finishButtonWarningBadge.Hide();
    }

    /// <summary>
    ///   Updates the visibility of the finish button's warning badge based on <see cref="ShowFinishButtonWarning"/>.
    /// </summary>
    protected void UpdateFinishButtonWarningVisibility()
    {
        var animator = finishButtonWarningBadge.GetChild<AnimationPlayer>(0);

        var animation = ShowFinishButtonWarning ? "show" : "hide";
        if (animator.AssignedAnimation == animation)
            return;

        if (ShowFinishButtonWarning)
            finishButtonWarningBadge.Show();

        animator.Play(animation);
    }

    protected virtual void OnCancelActionClicked()
    {
        GUICommon.Instance.PlayButtonPressSound();
        Editor.CancelCurrentAction();
    }

    protected void Undo()
    {
        Editor.Undo();
    }

    protected void Redo()
    {
        Editor.Redo();
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
    protected virtual int CalculateCurrentActionCost()
    {
        throw new GodotAbstractMethodNotOverriddenException();
    }

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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (MutationPointsBarPath != null)
            {
                MutationPointsBarPath.Dispose();
                ComponentBottomLeftButtonsPath.Dispose();
                CancelButtonPath.Dispose();
                FinishWarningBadgePath.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
