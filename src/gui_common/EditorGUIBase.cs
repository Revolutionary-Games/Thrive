using System;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Base class for all editor GUI classes providing a few common operations
/// </summary>
public abstract class EditorGUIBase<TEditor> : Control, ISaveLoadedTracked, IEditorGUI
    where TEditor : Godot.Object, IEditor
{
    [Export]
    public NodePath MenuPath = null!;

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
    public NodePath FinishButtonPath = null!;

    [Export]
    public NodePath CancelButtonPath = null!;

    protected TextureButton undoButton = null!;
    protected TextureButton redoButton = null!;

    protected Texture increaseIcon = null!;
    protected Texture decreaseIcon = null!;
    protected Texture questionIcon = null!;
    protected Texture temperatureIcon = null!;

    protected TEditor? editor;

    protected Compound atp = null!;
    protected Compound ammonia = null!;
    protected Compound carbondioxide = null!;
    protected Compound glucose = null!;
    protected Compound hydrogensulfide = null!;
    protected Compound iron = null!;
    protected Compound nitrogen = null!;
    protected Compound oxygen = null!;
    protected Compound phosphates = null!;
    protected Compound sunlight = null!;

    [JsonProperty]
    protected EditorTab selectedEditorTab = EditorTab.Report;

    [JsonProperty]
    protected bool? autoEvoRunSuccessful;

    private PauseMenu menu = null!;

    private Label currentMutationPointsLabel = null!;
    private TextureRect mutationPointsArrow = null!;
    private Label resultingMutationPointsLabel = null!;
    private Label baseMutationPointsLabel = null!;
    private ProgressBar mutationPointsBar = null!;
    private ProgressBar mutationPointsSubtractBar = null!;

    private Button finishButton = null!;
    private Button cancelButton = null!;

    private AudioStream unableToPlaceHexSound = null!;

    public bool IsLoadedFromSave { get; set; }

    public override void _Ready()
    {
        base._Ready();

        menu = GetNode<PauseMenu>(MenuPath);

        currentMutationPointsLabel = GetNode<Label>(CurrentMutationPointsLabelPath);
        mutationPointsArrow = GetNode<TextureRect>(MutationPointsArrowPath);
        resultingMutationPointsLabel = GetNode<Label>(ResultingMutationPointsLabelPath);
        baseMutationPointsLabel = GetNode<Label>(BaseMutationPointsLabelPath);
        mutationPointsBar = GetNode<ProgressBar>(MutationPointsBarPath);
        mutationPointsSubtractBar = GetNode<ProgressBar>(MutationPointsSubtractBarPath);

        undoButton = GetNode<TextureButton>(UndoButtonPath);
        redoButton = GetNode<TextureButton>(RedoButtonPath);
        finishButton = GetNode<Button>(FinishButtonPath);
        cancelButton = GetNode<Button>(CancelButtonPath);

        questionIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/helpButton.png");
        temperatureIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/Temperature.png");
        increaseIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/increase.png");
        decreaseIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/decrease.png");
        unableToPlaceHexSound = GD.Load<AudioStream>("res://assets/sounds/soundeffects/gui/click_place_blocked.ogg");

        atp = SimulationParameters.Instance.GetCompound("atp");
        ammonia = SimulationParameters.Instance.GetCompound("ammonia");
        carbondioxide = SimulationParameters.Instance.GetCompound("carbondioxide");
        glucose = SimulationParameters.Instance.GetCompound("glucose");
        hydrogensulfide = SimulationParameters.Instance.GetCompound("hydrogensulfide");
        iron = SimulationParameters.Instance.GetCompound("iron");
        nitrogen = SimulationParameters.Instance.GetCompound("nitrogen");
        oxygen = SimulationParameters.Instance.GetCompound("oxygen");
        phosphates = SimulationParameters.Instance.GetCompound("phosphates");
        sunlight = SimulationParameters.Instance.GetCompound("sunlight");
    }

    public virtual void Init(TEditor editor)
    {
        this.editor = editor ?? throw new ArgumentNullException(nameof(editor));
    }

    public void UpdateMutationPointsBar(bool tween = true)
    {
        if (editor == null)
            throw new InvalidOperationException("GUI not initialized");

        // Update mutation points
        float possibleMutationPoints = editor.FreeBuilding ?
            Constants.BASE_MUTATION_POINTS :
            editor.MutationPoints - editor.CalculateCurrentActionCost();

        if (tween)
        {
            GUICommon.Instance.TweenBarValue(
                mutationPointsBar, possibleMutationPoints, Constants.BASE_MUTATION_POINTS, 0.5f);
            GUICommon.Instance.TweenBarValue(
                mutationPointsSubtractBar, editor.MutationPoints, Constants.BASE_MUTATION_POINTS, 0.7f);
        }
        else
        {
            mutationPointsBar.Value = possibleMutationPoints;
            mutationPointsBar.MaxValue = Constants.BASE_MUTATION_POINTS;
            mutationPointsSubtractBar.Value = editor.MutationPoints;
            mutationPointsSubtractBar.MaxValue = Constants.BASE_MUTATION_POINTS;
        }

        if (editor.FreeBuilding)
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
            if (editor.MutationPoints > 0 && editor.MutationPoints != possibleMutationPoints)
            {
                mutationPointsArrow.Show();
                resultingMutationPointsLabel.Show();

                currentMutationPointsLabel.Text = $"({editor.MutationPoints:F0}";
                resultingMutationPointsLabel.Text = $"{possibleMutationPoints:F0})";
                baseMutationPointsLabel.Text = $"/ {Constants.BASE_MUTATION_POINTS:F0}";
            }
            else
            {
                mutationPointsArrow.Hide();
                resultingMutationPointsLabel.Hide();

                currentMutationPointsLabel.Text = $"{editor.MutationPoints:F0}";
                baseMutationPointsLabel.Text = $"/ {Constants.BASE_MUTATION_POINTS:F0}";
            }
        }

        mutationPointsSubtractBar.SelfModulate = possibleMutationPoints < 0 ?
            new Color(0.72f, 0.19f, 0.19f) :
            new Color(0.72f, 0.72f, 0.72f);
    }

    /// <summary>
    ///   Updates the visibility of the current action cancel button.
    /// </summary>
    public void UpdateCancelButtonVisibility()
    {
        if (editor == null)
            throw new InvalidOperationException("GUI not initialized");

        cancelButton.Visible = editor.CanCancelAction;
    }

    internal void SetUndoButtonStatus(bool enabled)
    {
        undoButton.Disabled = !enabled;
    }

    internal void SetRedoButtonStatus(bool enabled)
    {
        redoButton.Disabled = !enabled;
    }

    internal void OnInsufficientMp(bool playSound = true)
    {
        if (selectedEditorTab != EditorTab.CellEditor)
            return;

        var animationPlayer = mutationPointsBar.GetNode<AnimationPlayer>("FlashAnimation");
        animationPlayer.Play("FlashBar");

        if (playSound)
            PlayInvalidActionSound();
    }

    internal void OnActionBlockedWhileMoving()
    {
        PlayInvalidActionSound();
    }

    internal void PlayInvalidActionSound()
    {
        GUICommon.Instance.PlayCustomSound(unableToPlaceHexSound, 0.4f);
    }

    internal void OnInvalidHexLocationSelected()
    {
        if (selectedEditorTab != EditorTab.CellEditor)
            return;

        GUICommon.Instance.PlayCustomSound(unableToPlaceHexSound, 0.4f);
    }

    internal virtual void NotifyFreebuild(bool freebuilding)
    {
    }

    /// <summary>
    ///   Registers tooltip for the already existing Controls in the editor GUI
    /// </summary>
    protected virtual void RegisterTooltips()
    {
        undoButton.RegisterToolTipForControl("undoButton", "editor");
        redoButton.RegisterToolTipForControl("redoButton", "editor");
        finishButton.RegisterToolTipForControl("finishButton", "editor");
        cancelButton.RegisterToolTipForControl("cancelButton", "editor");
    }

    protected void OnCancelActionClicked()
    {
        GUICommon.Instance.PlayButtonPressSound();
        editor!.CancelCurrentAction();
    }

    // TODO: rename the next three methods
    /// <summary>
    ///   Called once when the mouse enters the background.
    /// </summary>
    protected void OnCellEditorMouseEntered()
    {
        editor!.ShowHover = selectedEditorTab == EditorTab.CellEditor;
        UpdateMutationPointsBar();
    }

    /// <summary>
    ///   Called when the mouse is no longer hovering the background.
    /// </summary>
    protected void OnCellEditorMouseExited()
    {
        editor!.ShowHover = false;
        UpdateMutationPointsBar();
    }

    /// <summary>
    ///   To get MouseEnter/Exit the CellEditor needs MouseFilter != Ignore.
    ///   Controls with MouseFilter != Ignore always handle mouse events.
    ///   So to get MouseClicks via the normal InputManager, this must be forwarded.
    ///   This is needed to respect the current Key Settings.
    /// </summary>
    /// <param name="inputEvent">The event the user fired</param>
    protected void OnCellEditorGuiInput(InputEvent inputEvent)
    {
        if (!editor!.ShowHover)
            return;

        InputManager.ForwardInput(inputEvent);
    }

    protected virtual void OnFinishEditingClicked()
    {
        if (!EditorCanFinishEditingEarly())
            return;

        GUICommon.Instance.PlayButtonPressSound();

        if (!EditorCanFinishEditingLate())
            return;

        // To prevent being clicked twice
        finishButton.MouseFilter = MouseFilterEnum.Ignore;

        TransitionManager.Instance.AddScreenFade(ScreenFade.FadeType.FadeOut, 0.3f, false);
        TransitionManager.Instance.StartTransitions(editor, nameof(MicrobeEditor.OnFinishEditing));
    }

    protected virtual bool EditorCanFinishEditingEarly()
    {
        // Prevent exiting when the transition hasn't finished
        if (!editor!.TransitionFinished)
            return false;

        // Can't finish an organism edit if an action is in progress
        if (editor.CanCancelAction)
        {
            OnActionBlockedWhileMoving();
            return false;
        }

        return true;
    }

    protected virtual bool EditorCanFinishEditingLate()
    {
        return true;
    }

    protected void ConfirmFinishEditingPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        TransitionManager.Instance.AddScreenFade(ScreenFade.FadeType.FadeOut, 0.3f, false);
        TransitionManager.Instance.StartTransitions(editor, nameof(MicrobeEditor.OnFinishEditing));
    }

    protected virtual void SetEditorTab(EditorTab tab)
    {
        selectedEditorTab = tab;

        ApplyEditorTab();
    }

    protected abstract void ApplyEditorTab();

    protected void ExitPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        GetTree().Quit();
    }
}
