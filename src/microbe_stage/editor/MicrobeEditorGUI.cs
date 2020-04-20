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
    [Export]
    public NodePath AutoEvoLabelPath;
    [Export]
    public NodePath ExternalEffectsLabelPath;
    [Export]
    public NodePath MapDrawerPath;
    [Export]
    public NodePath PatchNothingSelectedPath;
    [Export]
    public NodePath PatchDetailsPath;
    [Export]
    public NodePath PatchNamePath;
    [Export]
    public NodePath PatchPlayerHerePath;
    [Export]
    public NodePath PatchBiomePath;
    [Export]
    public NodePath MoveToPatchButtonPath;

    private const string ATP_BALANCE_DEFAULT_TEXT = "ATP Balance";

    private MicrobeEditor editor;
    private LoadingScreen loadingScreen;

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
    private Label atpBalanceLabel;
    private ProgressBar atpProductionBar;
    private ProgressBar atpConsumptionBar;
    private Label autoEvoLabel;
    private Label externalEffectsLabel;
    private PatchMapDrawer mapDrawer;
    private Control patchNothingSelected;
    private Control patchDetails;
    private Label patchName;
    private Control patchPlayerHere;
    private Label patchBiome;
    private Button moveToPatchButton;

    private bool inEditorTab = false;

    public override void _Ready()
    {
        organelleSelectionElements = GetTree().GetNodesInGroup("OrganelleSelectionElement");
        membraneSelectionElements = GetTree().GetNodesInGroup("MembraneSelectionElement");

        loadingScreen = GetNode<LoadingScreen>("LoadingScreen");

        sizeLabel = GetNode<Label>(SizeLabelPath);
        speedLabel = GetNode<Label>(SpeedLabelPath);
        generationLabel = GetNode<Label>(GenerationLabelPath);
        mutationPointsLabel = GetNode<Label>(MutationPointsLabelPath);
        mutationPointsBar = GetNode<TextureProgress>(MutationPointsBarPath);
        speciesNameEdit = GetNode<LineEdit>(SpeciesNameEditPath);
        undoButton = GetNode<TextureButton>(UndoButtonPath);
        redoButton = GetNode<TextureButton>(RedoButtonPath);
        symmetryButton = GetNode<TextureButton>(SymmetryButtonPath);
        atpBalanceLabel = GetNode<Label>(ATPBalanceLabelPath);
        atpProductionBar = GetNode<ProgressBar>(ATPProductionBarPath);
        atpConsumptionBar = GetNode<ProgressBar>(ATPConsumptionBarPath);
        autoEvoLabel = GetNode<Label>(AutoEvoLabelPath);
        externalEffectsLabel = GetNode<Label>(ExternalEffectsLabelPath);
        mapDrawer = GetNode<PatchMapDrawer>(MapDrawerPath);
        patchNothingSelected = GetNode<Control>(PatchNothingSelectedPath);
        patchDetails = GetNode<Control>(PatchDetailsPath);
        patchName = GetNode<Label>(PatchNamePath);
        patchPlayerHere = GetNode<Control>(PatchPlayerHerePath);
        patchBiome = GetNode<Label>(PatchBiomePath);
        moveToPatchButton = GetNode<Button>(MoveToPatchButtonPath);

        mapDrawer.OnSelectedPatchChanged = (drawer) =>
        {
            UpdateShownPatchDetails();
        };

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

    public void SetMap(PatchMap map)
    {
        mapDrawer.Map = map;
    }

    public void UpdatePlayerPatch(Patch patch)
    {
        if (patch == null)
        {
            mapDrawer.PlayerPatch = editor.CurrentPatch;
        }
        else
        {
            mapDrawer.PlayerPatch = patch;
        }

        // Just in case this didn't get called already. Note that this may result in duplicate calls here
        UpdateShownPatchDetails();
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
            atpBalanceLabel.Text = ATP_BALANCE_DEFAULT_TEXT;
            atpBalanceLabel.AddColorOverride("font_color", new Color(1.0f, 1.0f, 1.0f, 1.0f));
        }
        else
        {
            atpBalanceLabel.Text = ATP_BALANCE_DEFAULT_TEXT + " - ATP PRODUCTION TOO LOW!";
            atpBalanceLabel.AddColorOverride("font_color", new Color(1.0f, 0.2f, 0.2f, 1.0f));
        }

        float maxValue = Math.Max(energyBalance.TotalConsumption, energyBalance.TotalProduction);

        atpProductionBar.MaxValue = maxValue;
        atpProductionBar.Value = energyBalance.TotalProduction;

        atpConsumptionBar.MaxValue = maxValue;
        atpConsumptionBar.Value = energyBalance.TotalConsumption;
    }

    public void SetLoadingStatus(bool loading)
    {
        loadingScreen.Visible = loading;
    }

    public void SetLoadingText(string status, string description = "")
    {
        loadingScreen.LoadingMessage = status;
        loadingScreen.LoadingDescription = description;
    }

    public void UpdateAutoEvoResults(string results, string external)
    {
        autoEvoLabel.Text = results;
        externalEffectsLabel.Text = external;
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
        editor.ShowHover = true && inEditorTab;
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

    private void MoveToPatchClicked()
    {
        var target = mapDrawer.SelectedPatch;

        if (editor.IsPatchMoveValid(target))
            editor.SetPlayerPatch(target);
    }

    private void SetEditorTab(string tab)
    {
        // Hide all
        var cellEditor = GetNode<Control>("CellEditor");
        var report = GetNode<Control>("Report");
        var patchMap = GetNode<Control>("PatchMap");

        report.Hide();
        patchMap.Hide();
        cellEditor.Hide();

        inEditorTab = false;

        // Show selected
        if (tab == "report")
        {
            report.Show();
        }
        else if (tab == "patch")
        {
            patchMap.Show();
        }
        else if (tab == "editor")
        {
            cellEditor.Show();
            inEditorTab = true;
        }
        else
        {
            GD.PrintErr("Invalid tab");
        }
    }

    private void GoToPatchTab()
    {
        var button = GetNode<Button>("LeftTopBar/HBoxContainer/PatchMapButton");
        button.Pressed = true;
        SetEditorTab("patch");
    }

    private void GoToEditorTab()
    {
        var button = GetNode<Button>("LeftTopBar/HBoxContainer/CellEditorButton");
        button.Pressed = true;
        SetEditorTab("editor");
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

    private void UpdateShownPatchDetails()
    {
        var patch = mapDrawer.SelectedPatch;

        if (patch == null)
        {
            patchDetails.Visible = false;
            patchNothingSelected.Visible = true;

            return;
        }

        patchDetails.Visible = true;
        patchNothingSelected.Visible = false;

        patchName.Text = patch.Name;
        patchBiome.Text = "Biome: " + patch.Biome.Name;
        patchPlayerHere.Visible = editor.CurrentPatch == patch;

        // TODO: fix (show patch details like before)

        // Enable move to patch button if this is a valid move
        moveToPatchButton.Disabled = !editor.IsPatchMoveValid(patch);
    }
}
