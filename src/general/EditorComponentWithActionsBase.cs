using System;
using System.Drawing;
using Godot;
using Color = Godot.Color;

/// <summary>
///   Editor component that contains an MP bar, undo / redo buttons etc. related functions
/// </summary>
/// <typeparam name="TEditor">The type of editor this component is contained in</typeparam>
/// <typeparam name="TAction">Editor action type the editor this will be used with will use</typeparam>
public abstract class EditorComponentWithActionsBase<TEditor, TAction> : EditorComponentBase<TEditor>
    where TEditor : Godot.Object, IEditorWithActions
    where TAction : MicrobeEditorAction
{
    [Export]
    public NodePath CurrentMutationPointsLabelPath = null!;

    [Export]
    public NodePath MutationPointsArrowPath = null!;

    [Export]
    public NodePath ResultingMutationPointsLabelPath = null!;

    [Export]
    public NodePath BaseMutationPointsLabelPath = null!;

    [Export]
    public NodePath MutationPointsBarPath = null!;

    [Export]
    public NodePath MutationPointsSubtractBarPath = null!;

    [Export]
    public NodePath UndoButtonPath = null!;

    [Export]
    public NodePath RedoButtonPath = null!;

    [Export]
    public NodePath CancelButtonPath = null!;

    protected TextureButton undoButton = null!;
    protected TextureButton redoButton = null!;

    private Label currentMutationPointsLabel = null!;
    private TextureRect mutationPointsArrow = null!;
    private Label resultingMutationPointsLabel = null!;
    private Label baseMutationPointsLabel = null!;
    private ProgressBar mutationPointsBar = null!;
    private ProgressBar mutationPointsSubtractBar = null!;

    private Button cancelButton = null!;

    public override void _Ready()
    {
        base._Ready();

        currentMutationPointsLabel = GetNode<Label>(CurrentMutationPointsLabelPath);
        mutationPointsArrow = GetNode<TextureRect>(MutationPointsArrowPath);
        resultingMutationPointsLabel = GetNode<Label>(ResultingMutationPointsLabelPath);
        baseMutationPointsLabel = GetNode<Label>(BaseMutationPointsLabelPath);
        mutationPointsBar = GetNode<ProgressBar>(MutationPointsBarPath);
        mutationPointsSubtractBar = GetNode<ProgressBar>(MutationPointsSubtractBarPath);

        undoButton = GetNode<TextureButton>(UndoButtonPath);
        redoButton = GetNode<TextureButton>(RedoButtonPath);
        cancelButton = GetNode<Button>(CancelButtonPath);
    }

    public void UpdateMutationPointsBar(bool tween = true)
    {
        // Update mutation points
        float possibleMutationPoints = Editor.FreeBuilding ?
            Constants.BASE_MUTATION_POINTS :
            Editor.MutationPoints - CalculateCurrentActionCost();

        if (tween)
        {
            GUICommon.Instance.TweenBarValue(
                mutationPointsBar, possibleMutationPoints, Constants.BASE_MUTATION_POINTS, 0.5f);
            GUICommon.Instance.TweenBarValue(
                mutationPointsSubtractBar, Editor.MutationPoints, Constants.BASE_MUTATION_POINTS, 0.7f);
        }
        else
        {
            mutationPointsBar.Value = possibleMutationPoints;
            mutationPointsBar.MaxValue = Constants.BASE_MUTATION_POINTS;
            mutationPointsSubtractBar.Value = Editor.MutationPoints;
            mutationPointsSubtractBar.MaxValue = Constants.BASE_MUTATION_POINTS;
        }

        if (Editor.FreeBuilding)
        {
            mutationPointsArrow.Hide();
            resultingMutationPointsLabel.Hide();
            baseMutationPointsLabel.Hide();

            currentMutationPointsLabel.Text = TranslationServer.Translate("FREEBUILDING");
        }
        else
        {
            // TODO: check if we truly need ShowHover here
            // if (editor.ShowHover && editor.MutationPoints > 0)
            if (Editor.MutationPoints > 0 && Editor.MutationPoints != possibleMutationPoints)
            {
                mutationPointsArrow.Show();
                resultingMutationPointsLabel.Show();

                currentMutationPointsLabel.Text = $"({Editor.MutationPoints:F0}";
                resultingMutationPointsLabel.Text = $"{possibleMutationPoints:F0})";
                baseMutationPointsLabel.Text = $"/ {Constants.BASE_MUTATION_POINTS:F0}";
            }
            else
            {
                mutationPointsArrow.Hide();
                resultingMutationPointsLabel.Hide();

                currentMutationPointsLabel.Text = $"{Editor.MutationPoints:F0}";
                baseMutationPointsLabel.Text = $"/ {Constants.BASE_MUTATION_POINTS:F0}";
            }
        }

        mutationPointsSubtractBar.SelfModulate = possibleMutationPoints < 0 ?
            new Color(0.72f, 0.19f, 0.19f) :
            new Color(0.72f, 0.72f, 0.72f);
    }

    public override void OnMutationPointsChanged(int mutationPoints)
    {
        UpdateMutationPointsBar(true);
    }

    /// <summary>
    ///   Updates the visibility of the current action cancel button.
    /// </summary>
    public void UpdateCancelButtonVisibility()
    {
        cancelButton.Visible = Editor.CanCancelAction;
    }

    protected void Undo()
    {
        Editor.Undo();
    }

    protected void Redo()
    {
        Editor.Redo();
    }

    protected void OnCancelActionClicked()
    {
        GUICommon.Instance.PlayButtonPressSound();
        Editor.CancelCurrentAction();
    }

    internal void SetUndoButtonStatus(bool enabled)
    {
        undoButton.Disabled = !enabled;
    }

    internal void SetRedoButtonStatus(bool enabled)
    {
        redoButton.Disabled = !enabled;
    }

    public override void OnActionBlockedWhileAnotherIsInProgress()
    {
        PlayInvalidActionSound();
    }

    public override void OnInsufficientMP(bool playSound = true)
    {
        var animationPlayer = mutationPointsBar.GetNode<AnimationPlayer>("FlashAnimation");
        animationPlayer.Play("FlashBar");

        if (playSound)
            PlayInvalidActionSound();
    }

    /// <summary>
    ///   Calculates the cost of the current editor action (may be 0 if free or no active action)
    /// </summary>
    protected abstract int CalculateCurrentActionCost();

    /// <summary>
    ///   Special enqueue that can have special logic in specific components to pre-process actions before passing
    ///   them to the editor
    /// </summary>
    protected virtual void EnqueueAction(TAction action)
    {
        Editor.EnqueueAction(action);
    }

    /// <summary>
    ///   Registers tooltip for the already existing Controls in the editor GUI
    /// </summary>
    protected override void RegisterTooltips()
    {
        base.RegisterTooltips();

        undoButton.RegisterToolTipForControl("undoButton", "editor");
        redoButton.RegisterToolTipForControl("redoButton", "editor");
        cancelButton.RegisterToolTipForControl("cancelButton", "editor");
    }
}
