using System;
using Godot;

/// <summary>
///   Main class managing the microbe editor GUI
/// </summary>
public class MicrobeEditorGUI : Node
{
    // The labels to update are at really long relative paths, so they are set in the Godot editor
    [Export]
    public NodePath SizeLabelPath;
    [Export]
    public NodePath SpeedLabelPath;
    [Export]
    public NodePath GenerationLabelPath;
    [Export]
    public NodePath MutationPointsLabelPath;
    [Export]
    public NodePath MutationPointsBarPath;
    [Export]
    public NodePath SpeciesNameEditPath;
    [Export]
    public NodePath UndoButtonPath;
    [Export]
    public NodePath RedoButtonPath;
    [Export]
    public NodePath SymmetryButtonPath;
    [Export]
    public NodePath ATPBalanceLabelPath;
    [Export]
    public NodePath ATPProductionBarPath;
    [Export]
    public NodePath ATPConsumptionBarPath;

    private const string ATP_BALANCE_DEFAULT_TEXT = "ATP Balance";

    private MicrobeEditor editor;

    private Godot.Collections.Array organelleSelectionElements;
    private Godot.Collections.Array membraneSelectionElements;

    private Label sizeLabel;
    private Label speedLabel;
    private Label generationLabel;
    private Label mutationPointsLabel;
    private TextureProgress mutationPointsBar;
    private LineEdit speciesNameEdit;
    private TextureButton undoButton;
    private TextureButton redoButton;
    private TextureButton symmetryButton;
    private Label aTPBalanceLabel;
    private ProgressBar aTPProductionBar;
    private ProgressBar aTPConsumptionBar;

    public override void _Ready()
    {
        organelleSelectionElements = GetTree().GetNodesInGroup("OrganelleSelectionElement");
        membraneSelectionElements = GetTree().GetNodesInGroup("MembraneSelectionElement");

        sizeLabel = GetNode<Label>(SizeLabelPath);
        speedLabel = GetNode<Label>(SpeedLabelPath);
        generationLabel = GetNode<Label>(GenerationLabelPath);
        mutationPointsLabel = GetNode<Label>(MutationPointsLabelPath);
        mutationPointsBar = GetNode<TextureProgress>(MutationPointsBarPath);
        speciesNameEdit = GetNode<LineEdit>(SpeciesNameEditPath);
        undoButton = GetNode<TextureButton>(UndoButtonPath);
        redoButton = GetNode<TextureButton>(RedoButtonPath);
        symmetryButton = GetNode<TextureButton>(SymmetryButtonPath);
        aTPBalanceLabel = GetNode<Label>(ATPBalanceLabelPath);
        aTPProductionBar = GetNode<ProgressBar>(ATPProductionBarPath);
        aTPConsumptionBar = GetNode<ProgressBar>(ATPConsumptionBarPath);

        // Fade out for that smooth satisfying transition
        TransitionManager.Instance.AddScreenFade(Fade.FadeType.FadeOut, 0.5f);
        TransitionManager.Instance.StartTransitions(null, string.Empty);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            MenuButtonPressed();
        }
    }

    public void Init(MicrobeEditor editor)
    {
        this.editor = editor ?? throw new ArgumentNullException(nameof(editor));
    }

    public override void _Process(float delta)
    {
        // Update mutation points
        mutationPointsBar.MaxValue = Constants.BASE_MUTATION_POINTS;
        mutationPointsBar.Value = editor.MutationPoints;
        mutationPointsLabel.Text = string.Format("{0:F0} / {1:F0}", editor.MutationPoints,
            Constants.BASE_MUTATION_POINTS);
    }

    public void UpdateSize(int size)
    {
        sizeLabel.Text = "Size " + size.ToString();
    }

    public void UpdateGeneration(int generation)
    {
        generationLabel.Text = "Generation " + generation.ToString();
    }

    public void UpdateSpeed(float speed)
    {
        speedLabel.Text = "Speed " + string.Format("{0:F1}", speed);
    }

    public void UpdateEnergyBalance(EnergyBalanceInfo energyBalance)
    {
        if (energyBalance.FinalBalance > 0)
        {
            aTPBalanceLabel.Text = ATP_BALANCE_DEFAULT_TEXT;
            aTPBalanceLabel.AddColorOverride("font_color", new Color(1.0f, 1.0f, 1.0f, 1.0f));
        }
        else
        {
            aTPBalanceLabel.Text = ATP_BALANCE_DEFAULT_TEXT + " - ATP PRODUCTION TOO LOW!";
            aTPBalanceLabel.AddColorOverride("font_color", new Color(1.0f, 0.2f, 0.2f, 1.0f));
        }

        float maxValue = Math.Max(energyBalance.TotalConsumption, energyBalance.TotalProduction);

        aTPProductionBar.MaxValue = maxValue;
        aTPProductionBar.Value = energyBalance.TotalProduction;

        aTPConsumptionBar.MaxValue = maxValue;
        aTPConsumptionBar.Value = energyBalance.TotalConsumption;
    }

    /// <summary>
    ///   Called once when the mouse enters the editor GUI.
    /// </summary>
    internal void OnMouseEnter()
    {
        editor.ShowHover = false;
    }

    /// <summary>
    ///   Called when the mouse is no longer hovering
    //    the editor GUI.
    /// </summary>
    internal void OnMouseExit()
    {
        editor.ShowHover = true;
    }

    internal void SetUndoButtonStatus(bool enabled)
    {
        undoButton.Disabled = !enabled;
    }

    internal void SetRedoButtonStatus(bool enabled)
    {
        redoButton.Disabled = !enabled;
    }

    internal void NotifyFreebuild(object freebuilding)
    {
        // TODO: fix
        // throw new NotImplementedException();
    }

    /// <summary>
    ///   lock / unlock the organelles  that need a nuclues
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: rename to something more sensible
    ///   </para>
    /// </remarks>
    internal void UpdateGuiButtonStatus(bool hasNucleus)
    {
        // TODO: fix
        // throw new NotImplementedException();
    }

    internal void OnOrganelleToPlaceSelected(string organelle)
    {
        editor.ActiveActionName = organelle;

        // Make all buttons unselected except the one that is now selected
        foreach (Button element in organelleSelectionElements)
        {
            var selectedLabel = element.GetNode<Label>(
                "MarginContainer/VBoxContainer/SelectedLabelMargin/SelectedLabel");

            if (element.Name == organelle)
            {
                if (!element.Pressed)
                    element.Pressed = true;

                selectedLabel.Show();
            }
            else
            {
                selectedLabel.Hide();
            }
        }

        GD.Print("Editor action is now: " + editor.ActiveActionName);
    }

    internal void OnFinishEditingClicked()
    {
        TransitionManager.Instance.AddScreenFade(Fade.FadeType.FadeIn, 0.3f, false);
        TransitionManager.Instance.StartTransitions(editor, nameof(MicrobeEditor.OnFinishEditing));
    }

    internal void OnSymmetryClicked()
    {
        // TODO: fix
    }

    internal void OnMembraneSelected(string membrane)
    {
        // todo: Send selected membrane to the editor script

        // Updates the GUI buttons based on current membrane
        foreach (Button element in membraneSelectionElements)
        {
            var selectedLabel = element.GetNode<Label>(
                "MarginContainer/VBoxContainer/SelectedLabelMargin/SelectedLabel");

            if (element.Name == membrane)
            {
                if (!element.Pressed)
                    element.Pressed = true;

                selectedLabel.Show();
            }
            else
            {
                selectedLabel.Hide();
            }
        }
    }

    internal void SetSpeciesInfo(string name, MembraneType membrane, Color colour,
        float rigidity)
    {
        // TODO: fix
        // throw new NotImplementedException();

        speciesNameEdit.Text = name;
    }

    private void SetCellTab(string tab)
    {
        var structureTab = GetNode<Control>("CellEditor/LeftPanel/Panel/Structure");
        var membraneTab = GetNode<Control>("CellEditor/LeftPanel/Panel/Membrane");

        // Hide all
        structureTab.Hide();
        membraneTab.Hide();

        // Show selected
        if (tab == "structure")
        {
            structureTab.Show();
        }
        else if (tab == "membrane")
        {
            membraneTab.Show();
        }
        else
        {
            GD.PrintErr("Invalid tab");
        }
    }

    private void MenuButtonPressed()
    {
        var menu = GetNode<Control>("PauseMenu");

        if (menu.Visible)
        {
            menu.Hide();
            GetTree().Paused = false;
        }
        else
        {
            menu.Show();
            GetTree().Paused = true;
        }

        GUICommon.Instance.PlayButtonPressSound();
    }

    private void ExitPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        GetTree().Quit();
    }
}
