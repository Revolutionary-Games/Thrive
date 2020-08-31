using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Godot;
using Array = Godot.Collections.Array;

/// <summary>
///   Main class managing the microbe editor GUI
/// </summary>
public class MicrobeEditorGUI : Node
{
    // The labels to update are at really long relative paths, so they are set in the Godot editor
    [Export]
    public NodePath MenuPath;

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
    public NodePath MutationPointsSubtractBarPath;

    [Export]
    public NodePath SpeciesNameEditPath;

    [Export]
    public NodePath MembraneColorPickerPath;

    [Export]
    public NodePath NewCellButtonPath;

    [Export]
    public NodePath UndoButtonPath;

    [Export]
    public NodePath RedoButtonPath;

    [Export]
    public NodePath FinishButtonPath;

    [Export]
    public NodePath SymmetryButtonPath;

    [Export]
    public NodePath ATPBalanceLabelPath;

    [Export]
    public NodePath ATPBarContainerPath;

    [Export]
    public NodePath ATPProductionBarPath;

    [Export]
    public NodePath ATPConsumptionBarPath;

    [Export]
    public NodePath GlucoseReductionLabelPath;

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
    public NodePath PatchDepthPath;

    [Export]
    public NodePath PatchTemperaturePath;

    [Export]
    public NodePath PatchPressurePath;

    [Export]
    public NodePath PatchLightPath;

    [Export]
    public NodePath PatchOxygenPath;

    [Export]
    public NodePath PatchNitrogenPath;

    [Export]
    public NodePath PatchCO2Path;

    [Export]
    public NodePath PatchHydrogenSulfidePath;

    [Export]
    public NodePath PatchAmmoniaPath;

    [Export]
    public NodePath PatchGlucosePath;

    [Export]
    public NodePath PatchPhosphatePath;

    [Export]
    public NodePath PatchIronPath;

    [Export]
    public NodePath SpeciesListPath;

    [Export]
    public NodePath PhysicalConditionsButtonPath;

    [Export]
    public NodePath PhysicalConditionsBoxPath;

    [Export]
    public NodePath AtmosphericConditionsButtonPath;

    [Export]
    public NodePath AtmosphericConditionsBoxPath;

    [Export]
    public NodePath CompoundsBoxButtonPath;

    [Export]
    public NodePath CompoundsBoxPath;

    [Export]
    public NodePath SpeciesListButtonPath;

    [Export]
    public NodePath MoveToPatchButtonPath;

    [Export]
    public NodePath PatchTemperatureSituationPath;

    [Export]
    public NodePath PatchLightSituationPath;

    [Export]
    public NodePath PatchHydrogenSulfideSituationPath;

    [Export]
    public NodePath PatchGlucoseSituationPath;

    [Export]
    public NodePath PatchIronSituationPath;

    [Export]
    public NodePath PatchAmmoniaSituationPath;

    [Export]
    public NodePath PatchPhosphateSituationPath;

    [Export]
    public NodePath RigiditySliderPath;

    [Export]
    public NodePath RigiditySliderTooltipHealthLabelPath;

    [Export]
    public NodePath RigiditySliderTooltipSpeedLabelPath;

    [Export]
    public NodePath SymmetryIconPath;

    [Export]
    public Texture SymmetryIcon2x;

    [Export]
    public Texture SymmetryIcon4x;

    [Export]
    public Texture SymmetryIcon6x;

    [Export]
    public Texture IncreaseIcon;

    [Export]
    public Texture DecreaseIcon;

    private const string ATP_BALANCE_DEFAULT_TEXT = "ATP Balance";

    private readonly Compound ammonia = SimulationParameters.Instance.GetCompound("ammonia");
    private readonly Compound carbondioxide = SimulationParameters.Instance.GetCompound("carbondioxide");
    private readonly Compound glucose = SimulationParameters.Instance.GetCompound("glucose");
    private readonly Compound hydrogensulfide = SimulationParameters.Instance.GetCompound("hydrogensulfide");
    private readonly Compound iron = SimulationParameters.Instance.GetCompound("iron");
    private readonly Compound nitrogen = SimulationParameters.Instance.GetCompound("nitrogen");
    private readonly Compound oxygen = SimulationParameters.Instance.GetCompound("oxygen");
    private readonly Compound phosphates = SimulationParameters.Instance.GetCompound("phosphates");
    private readonly Compound sunlight = SimulationParameters.Instance.GetCompound("sunlight");

    private MicrobeEditor editor;

    private Array organelleSelectionElements;
    private Array membraneSelectionElements;
    private Array itemTooltipElements;

    private Control menu;
    private Label sizeLabel;
    private Label speedLabel;
    private Label generationLabel;
    private Label mutationPointsLabel;
    private TextureProgress mutationPointsBar;
    private TextureProgress mutationPointsSubtractBar;
    private LineEdit speciesNameEdit;
    private ColorPicker membraneColorPicker;
    private TextureButton newCellButton;
    private TextureButton undoButton;
    private TextureButton redoButton;
    private Button finishButton;

    // ReSharper disable once NotAccessedField.Local
    private TextureButton symmetryButton;
    private TextureRect symmetryIcon;
    private Label atpBalanceLabel;
    private SegmentedBar atpProductionBar;
    private SegmentedBar atpConsumptionBar;
    private Label glucoseReductionLabel;
    private Label autoEvoLabel;
    private Label externalEffectsLabel;
    private PatchMapDrawer mapDrawer;
    private Control patchNothingSelected;
    private Control patchDetails;
    private Label patchName;
    private Control patchPlayerHere;
    private Label patchBiome;
    private Label patchDepth;
    private Label patchTemperature;
    private Label patchPressure;
    private Label patchLight;
    private Label patchOxygen;
    private Label patchNitrogen;
    private Label patchCO2;
    private Label patchHydrogenSulfide;
    private Label patchAmmonia;
    private Label patchGlucose;
    private Label patchPhosphate;
    private Label patchIron;
    private VBoxContainer speciesList;
    private Control physicalConditionsButton;
    private Control physicalConditionsBox;
    private Control atmosphericConditionsButton;
    private Control atmosphericConditionsBox;
    private Control compoundsButton;
    private Control compoundsBox;
    private Control speciesListButton;
    private Button moveToPatchButton;
    private TextureRect patchTemperatureSituation;
    private TextureRect patchLightSituation;
    private TextureRect patchHydrogenSulfideSituation;
    private TextureRect patchGlucoseSituation;
    private TextureRect patchIronSituation;
    private TextureRect patchAmmoniaSituation;
    private TextureRect patchPhosphateSituation;
    private Slider rigiditySlider;

    private bool inEditorTab;
    private MicrobeEditor.MicrobeSymmetry symmetry = MicrobeEditor.MicrobeSymmetry.None;

    /// <summary>
    ///   For toggling purposes
    /// </summary>
    private bool speciesListIsHidden;

    private Texture invalidBarTexture;
    private Texture subractBarTexture;

    public override void _Ready()
    {
        organelleSelectionElements = GetTree().GetNodesInGroup("OrganelleSelectionElement");
        membraneSelectionElements = GetTree().GetNodesInGroup("MembraneSelectionElement");
        itemTooltipElements = GetTree().GetNodesInGroup("ItemTooltip");

        menu = GetNode<Control>(MenuPath);
        sizeLabel = GetNode<Label>(SizeLabelPath);
        speedLabel = GetNode<Label>(SpeedLabelPath);
        generationLabel = GetNode<Label>(GenerationLabelPath);
        mutationPointsLabel = GetNode<Label>(MutationPointsLabelPath);
        mutationPointsBar = GetNode<TextureProgress>(MutationPointsBarPath);
        mutationPointsSubtractBar = GetNode<TextureProgress>(MutationPointsSubtractBarPath);
        speciesNameEdit = GetNode<LineEdit>(SpeciesNameEditPath);
        membraneColorPicker = GetNode<ColorPicker>(MembraneColorPickerPath);
        newCellButton = GetNode<TextureButton>(NewCellButtonPath);
        undoButton = GetNode<TextureButton>(UndoButtonPath);
        redoButton = GetNode<TextureButton>(RedoButtonPath);
        symmetryButton = GetNode<TextureButton>(SymmetryButtonPath);
        finishButton = GetNode<Button>(FinishButtonPath);
        atpBalanceLabel = GetNode<Label>(ATPBalanceLabelPath);
        atpProductionBar = GetNode<SegmentedBar>(ATPProductionBarPath);
        atpConsumptionBar = GetNode<SegmentedBar>(ATPConsumptionBarPath);
        glucoseReductionLabel = GetNode<Label>(GlucoseReductionLabelPath);
        autoEvoLabel = GetNode<Label>(AutoEvoLabelPath);
        externalEffectsLabel = GetNode<Label>(ExternalEffectsLabelPath);
        mapDrawer = GetNode<PatchMapDrawer>(MapDrawerPath);
        patchNothingSelected = GetNode<Control>(PatchNothingSelectedPath);
        patchDetails = GetNode<Control>(PatchDetailsPath);
        patchName = GetNode<Label>(PatchNamePath);
        patchPlayerHere = GetNode<Control>(PatchPlayerHerePath);
        patchBiome = GetNode<Label>(PatchBiomePath);
        patchDepth = GetNode<Label>(PatchDepthPath);
        patchTemperature = GetNode<Label>(PatchTemperaturePath);
        patchPressure = GetNode<Label>(PatchPressurePath);
        patchLight = GetNode<Label>(PatchLightPath);
        patchOxygen = GetNode<Label>(PatchOxygenPath);
        patchNitrogen = GetNode<Label>(PatchNitrogenPath);
        patchCO2 = GetNode<Label>(PatchCO2Path);
        patchHydrogenSulfide = GetNode<Label>(PatchHydrogenSulfidePath);
        patchAmmonia = GetNode<Label>(PatchAmmoniaPath);
        patchGlucose = GetNode<Label>(PatchGlucosePath);
        patchPhosphate = GetNode<Label>(PatchPhosphatePath);
        patchIron = GetNode<Label>(PatchIronPath);
        speciesList = GetNode<VBoxContainer>(SpeciesListPath);
        physicalConditionsBox = GetNode<Control>(PhysicalConditionsBoxPath);
        atmosphericConditionsBox = GetNode<Control>(AtmosphericConditionsBoxPath);
        compoundsBox = GetNode<Control>(CompoundsBoxPath);
        moveToPatchButton = GetNode<Button>(MoveToPatchButtonPath);
        physicalConditionsButton = GetNode<Control>(PhysicalConditionsButtonPath);
        atmosphericConditionsButton = GetNode<Control>(AtmosphericConditionsButtonPath);
        compoundsButton = GetNode<Control>(CompoundsBoxButtonPath);
        speciesListButton = GetNode<Control>(SpeciesListButtonPath);
        symmetryIcon = GetNode<TextureRect>(SymmetryIconPath);
        patchTemperatureSituation = GetNode<TextureRect>(PatchTemperatureSituationPath);
        patchLightSituation = GetNode<TextureRect>(PatchLightSituationPath);
        patchHydrogenSulfideSituation = GetNode<TextureRect>(PatchHydrogenSulfideSituationPath);
        patchGlucoseSituation = GetNode<TextureRect>(PatchGlucoseSituationPath);
        patchIronSituation = GetNode<TextureRect>(PatchIronSituationPath);
        patchAmmoniaSituation = GetNode<TextureRect>(PatchAmmoniaSituationPath);
        patchPhosphateSituation = GetNode<TextureRect>(PatchPhosphateSituationPath);
        rigiditySlider = GetNode<Slider>(RigiditySliderPath);

        invalidBarTexture = GD.Load<Texture>("res://assets/textures/gui/bevel/MpBarInvalid.png");
        subractBarTexture = GD.Load<Texture>("res://assets/textures/gui/bevel/MpBarSubtract.png");

        mapDrawer.OnSelectedPatchChanged = drawer => { UpdateShownPatchDetails(); };

        atpProductionBar.SelectedType = SegmentedBar.Type.ATP;
        atpProductionBar.IsProduction = true;
        atpConsumptionBar.SelectedType = SegmentedBar.Type.ATP;

        // Fade out for that smooth satisfying transition
        TransitionManager.Instance.AddScreenFade(Fade.FadeType.FadeOut, 0.5f);
        TransitionManager.Instance.StartTransitions(null, string.Empty);
    }

    public void Init(MicrobeEditor editor)
    {
        this.editor = editor ?? throw new ArgumentNullException(nameof(editor));
    }

    public override void _Process(float delta)
    {
        // Update mutation points
        float possibleMutationPoints = editor.FreeBuilding ?
            Constants.BASE_MUTATION_POINTS :
            editor.MutationPoints - editor.CurrentOrganelleCost;
        mutationPointsBar.MaxValue = Constants.BASE_MUTATION_POINTS;
        mutationPointsBar.Value = possibleMutationPoints;
        mutationPointsSubtractBar.MaxValue = Constants.BASE_MUTATION_POINTS;
        mutationPointsSubtractBar.Value = editor.MutationPoints;
        if (possibleMutationPoints != editor.MutationPoints)
        {
            mutationPointsLabel.Text =
                $"({editor.MutationPoints:F0} -> {possibleMutationPoints:F0}) / {Constants.BASE_MUTATION_POINTS:F0}";
        }
        else
        {
            mutationPointsLabel.Text = $"{editor.MutationPoints:F0} / {Constants.BASE_MUTATION_POINTS:F0}";
        }

        if (possibleMutationPoints < 0)
        {
            mutationPointsSubtractBar.TextureProgress_ = invalidBarTexture;
        }
        else
        {
            mutationPointsSubtractBar.TextureProgress_ = subractBarTexture;
        }
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

    public void UpdateGlucoseReduction(float value)
    {
        var percentage = value * 100 + "%";

        glucoseReductionLabel.Text = "The amount of glucose has been reduced to " + percentage +
            " of the previous amount.";
    }

    public void UpdateSize(int size)
    {
        sizeLabel.Text = "Size " + size.ToString(CultureInfo.CurrentCulture);
    }

    public void UpdateGeneration(int generation)
    {
        generationLabel.Text = "Generation " + generation.ToString(CultureInfo.CurrentCulture);
    }

    public void UpdateSpeed(float speed)
    {
        speedLabel.Text = "Speed " + string.Format(CultureInfo.CurrentCulture, "{0:F1}", speed);
    }

    public void UpdateEnergyBalance(EnergyBalanceInfo energyBalance)
    {
        if (energyBalance.FinalBalance > 0)
        {
            atpBalanceLabel.Text = ATP_BALANCE_DEFAULT_TEXT;
            atpBalanceLabel.AddColorOverride("font_color", new Color(1.0f, 1.0f, 1.0f));
        }
        else
        {
            atpBalanceLabel.Text = ATP_BALANCE_DEFAULT_TEXT + " - ATP PRODUCTION TOO LOW!";
            atpBalanceLabel.AddColorOverride("font_color", new Color(1.0f, 0.2f, 0.2f));
        }

        float maxValue = Math.Max(energyBalance.TotalConsumption, energyBalance.TotalProduction);
        atpProductionBar.MaxValue = maxValue;
        atpConsumptionBar.MaxValue = maxValue;

        atpProductionBar.UpdateAndMoveBars(SortBarData(energyBalance.Production));
        atpConsumptionBar.UpdateAndMoveBars(SortBarData(energyBalance.Consumption));
    }

    // Disable this because the cleanup and inspections disagree
    // ReSharper disable once RedundantNameQualifier
    /// <summary>
    ///   Updates the organelle efficiencies in tooltips.
    /// </summary>
    public void UpdateOrganelleEfficiencies(
        System.Collections.Generic.Dictionary<string, OrganelleEfficiency> organelleEfficiency)
    {
        foreach (var organelleName in organelleEfficiency.Keys)
        {
            foreach (Node tooltip in itemTooltipElements)
            {
                if (tooltip.Name == organelleName)
                {
                    var processList = tooltip.GetNode<VBoxContainer>("MarginContainer/VBoxContainer/ProcessList");

                    WriteOrganelleProcessList(organelleEfficiency[organelleName].Processes,
                        processList);
                }
            }
        }
    }

    /// <summary>
    ///   Updates the fluidity / rigidity slider tooltip
    /// </summary>
    public void SetRigiditySliderTooltip(int rigidity)
    {
        float convertedRigidity = rigidity / Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO;

        var healthChangeLabel = GetNode<Label>(RigiditySliderTooltipHealthLabelPath);
        var mobilityChangeLabel = GetNode<Label>(RigiditySliderTooltipSpeedLabelPath);

        float healthChange = convertedRigidity * Constants.MEMBRANE_RIGIDITY_HITPOINTS_MODIFIER;
        float mobilityChange = -1 * convertedRigidity * Constants.MEMBRANE_RIGIDITY_MOBILITY_MODIFIER;

        healthChangeLabel.Text = ((healthChange > 0) ? "+" : string.Empty)
            + healthChange.ToString("F2", CultureInfo.CurrentCulture);
        mobilityChangeLabel.Text = ((mobilityChange > 0) ? "+" : string.Empty)
            + mobilityChange.ToString("F2", CultureInfo.CurrentCulture);

        if (healthChange >= 0)
        {
            healthChangeLabel.AddColorOverride("font_color", new Color(0, 1, 0));
        }
        else
        {
            healthChangeLabel.AddColorOverride("font_color", new Color(1, 0.3f, 0.3f));
        }

        if (mobilityChange >= 0)
        {
            mobilityChangeLabel.AddColorOverride("font_color", new Color(0, 1, 0));
        }
        else
        {
            mobilityChangeLabel.AddColorOverride("font_color", new Color(1, 0.3f, 0.3f));
        }
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
    ///   the editor GUI.
    /// </summary>
    internal void OnMouseExit()
    {
        editor.ShowHover = inEditorTab;
    }

    internal void OnItemMouseHover(string itemName)
    {
        foreach (PanelContainer tooltip in itemTooltipElements)
        {
            tooltip.Hide();

            if (tooltip.Name == itemName)
            {
                tooltip.Show();
            }
        }
    }

    internal void OnItemMouseExit()
    {
        foreach (PanelContainer tooltip in itemTooltipElements)
        {
            tooltip.Hide();
        }
    }

    internal void SetUndoButtonStatus(bool enabled)
    {
        undoButton.Disabled = !enabled;
    }

    internal void SetRedoButtonStatus(bool enabled)
    {
        redoButton.Disabled = !enabled;
    }

    internal void NotifyFreebuild(bool freebuilding)
    {
        if (freebuilding)
        {
            newCellButton.Disabled = false;
        }
        else
        {
            newCellButton.Disabled = true;
        }
    }

    internal void OnNewCellClicked()
    {
        GUICommon.Instance.PlayButtonPressSound();

        editor.CreateNewMicrobe();
    }

    /// <summary>
    ///   Lock / unlock the organelles  that need a nuclues
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     TODO: rename to something more sensible
    ///     and maybe also improve how this is implemented
    ///     to be not cluttered
    ///   </para>
    /// </remarks>
    internal void UpdateGuiButtonStatus(bool hasNucleus)
    {
        foreach (Button organelleItem in organelleSelectionElements)
        {
            SetOrganelleButtonStatus(organelleItem, hasNucleus);
        }
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
        // To prevent being clicked twice
        finishButton.MouseFilter = Control.MouseFilterEnum.Ignore;

        TransitionManager.Instance.AddScreenFade(Fade.FadeType.FadeIn, 0.3f, false);
        TransitionManager.Instance.StartTransitions(editor, nameof(MicrobeEditor.OnFinishEditing));
    }

    internal void OnSymmetryClicked()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (symmetry == MicrobeEditor.MicrobeSymmetry.SixWaySymmetry)
        {
            ResetSymmetryButton();
        }
        else if (symmetry == MicrobeEditor.MicrobeSymmetry.None)
        {
            symmetryIcon.Texture = SymmetryIcon2x;
            symmetry = MicrobeEditor.MicrobeSymmetry.XAxisSymmetry;
        }
        else if (symmetry == MicrobeEditor.MicrobeSymmetry.XAxisSymmetry)
        {
            symmetryIcon.Texture = SymmetryIcon4x;
            symmetry = MicrobeEditor.MicrobeSymmetry.FourWaySymmetry;
        }
        else if (symmetry == MicrobeEditor.MicrobeSymmetry.FourWaySymmetry)
        {
            symmetryIcon.Texture = SymmetryIcon6x;
            symmetry = MicrobeEditor.MicrobeSymmetry.SixWaySymmetry;
        }

        editor.Symmetry = symmetry;
    }

    internal void ResetSymmetryButton()
    {
        symmetryIcon.Texture = null;
        symmetry = 0;
    }

    internal void OnMembraneSelected(string membrane)
    {
        editor.SetMembrane(membrane);
    }

    internal void SetSpeciesInfo(string name, MembraneType membrane, Color colour,
        float rigidity)
    {
        speciesNameEdit.Text = name;
        membraneColorPicker.Color = colour;

        // Callback is manually called because the function isn't called automatically here
        OnSpeciesNameTextChanged(name);

        UpdateMembraneButtons(membrane.InternalName);

        UpdateRigiditySlider((int)Math.Round(rigidity * Constants.MEMBRANE_RIGIDITY_SLIDER_TO_VALUE_RATIO),
            editor.MutationPoints);
    }

    internal void UpdateMembraneButtons(string membrane)
    {
        // Updates the GUI buttons based on current membrane
        foreach (Button element in membraneSelectionElements)
        {
            // This is required so that the button press state won't be
            // updated incorrectly when we don't have enough MP to change the membrane
            element.Pressed = false;

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

    internal void UpdateRigiditySlider(int value, int mutationPoints)
    {
        if (mutationPoints >= Constants.MEMBRANE_RIGIDITY_COST_PER_STEP)
        {
            rigiditySlider.Editable = true;
        }
        else
        {
            rigiditySlider.Editable = false;
        }

        rigiditySlider.Value = value;
        SetRigiditySliderTooltip(value);
    }

    private static void SetOrganelleButtonStatus(Button organelleItem, bool nucleus)
    {
        if (organelleItem.Name == "nucleus")
        {
            organelleItem.Disabled = nucleus;
        }
        else if (organelleItem.Name == "mitochondrion")
        {
            organelleItem.Disabled = !nucleus;
        }
        else if (organelleItem.Name == "chloroplast")
        {
            organelleItem.Disabled = !nucleus;
        }
        else if (organelleItem.Name == "chemoplast")
        {
            organelleItem.Disabled = !nucleus;
        }
        else if (organelleItem.Name == "nitrogenfixingplastid")
        {
            organelleItem.Disabled = !nucleus;
        }
        else if (organelleItem.Name == "vacuole")
        {
            organelleItem.Disabled = !nucleus;
        }
        else if (organelleItem.Name == "oxytoxy")
        {
            organelleItem.Disabled = !nucleus;
        }
    }

    private void OnRigidityChanged(int value)
    {
        editor.SetRigidity(value);
    }

    private void OnColorChanged(Color color)
    {
        editor.Colour = color;
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

    private void ToggleConditionsTab(string tab)
    {
        var slideAnimations = patchDetails.GetNode<AnimationPlayer>("SlideAnimations");

        if (tab == "physical")
        {
            var minusButton = physicalConditionsButton.GetNode<TextureButton>("minusButton");
            var plusButton = physicalConditionsButton.GetNode<TextureButton>("plusButton");

            if (!physicalConditionsBox.Visible)
            {
                slideAnimations.Play("physicalSlideDown");
                minusButton.Show();
                plusButton.Hide();
            }
            else
            {
                slideAnimations.Play("physicalSlideUp");
                minusButton.Hide();
                plusButton.Show();
            }
        }
        else if (tab == "atmospheric")
        {
            var minusButton = atmosphericConditionsButton.GetNode<TextureButton>("minusButton");
            var plusButton = atmosphericConditionsButton.GetNode<TextureButton>("plusButton");

            if (!atmosphericConditionsBox.Visible)
            {
                slideAnimations.Play("atmosphericSlideDown");
                minusButton.Show();
                plusButton.Hide();
            }
            else
            {
                slideAnimations.Play("atmosphericSlideUp");
                minusButton.Hide();
                plusButton.Show();
            }
        }
        else if (tab == "compounds")
        {
            var minusButton = compoundsButton.GetNode<TextureButton>("minusButton");
            var plusButton = compoundsButton.GetNode<TextureButton>("plusButton");

            if (!compoundsBox.Visible)
            {
                slideAnimations.Play("compoundsSlideDown");
                minusButton.Show();
                plusButton.Hide();
            }
            else
            {
                slideAnimations.Play("compoundsSlideUp");
                minusButton.Hide();
                plusButton.Show();
            }
        }
        else if (tab == "species")
        {
            var minusButton = speciesListButton.GetNode<TextureButton>("minusButton");
            var plusButton = speciesListButton.GetNode<TextureButton>("plusButton");

            var clip = speciesList.GetParent<MarginContainer>();
            var tween = clip.GetNode<Tween>("Tween");

            if (speciesListIsHidden)
            {
                tween.InterpolateProperty(clip, "custom_constants/margin_top", -speciesList.RectSize.y, 20, 0.3f,
                    Tween.TransitionType.Sine, Tween.EaseType.Out);
                tween.Start();

                minusButton.Show();
                plusButton.Hide();

                speciesListIsHidden = false;
            }
            else
            {
                tween.InterpolateProperty(clip, "custom_constants/margin_top", 20, -speciesList.RectSize.y, 0.3f,
                    Tween.TransitionType.Sine, Tween.EaseType.Out);
                tween.Start();

                minusButton.Hide();
                plusButton.Show();

                speciesListIsHidden = true;
            }
        }
    }

    private void MenuButtonPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        OpenMenu();
    }

    private void OpenMenu()
    {
        menu.Show();
        GetTree().Paused = true;
    }

    private void CloseMenu()
    {
        menu.Hide();
        GetTree().Paused = false;
    }

    private void ExitPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        GetTree().Quit();
    }

    private void WriteOrganelleProcessList(List<ProcessSpeedInformation> processList,
        VBoxContainer targetElement)
    {
        // Remove previous process list
        if (targetElement.GetChildCount() > 0)
        {
            foreach (Node children in targetElement.GetChildren())
            {
                children.QueueFree();
            }
        }

        if (processList == null)
        {
            var noProcesslabel = new Label();
            noProcesslabel.Text = "No processes";
            targetElement.AddChild(noProcesslabel);
            return;
        }

        foreach (var process in processList)
        {
            var processContainer = new VBoxContainer();
            targetElement.AddChild(processContainer);

            var processTitle = new Label();
            processTitle.AddColorOverride("font_color", new Color(1.0f, 0.84f, 0.0f));
            processTitle.Text = process.Process.Name;
            processContainer.AddChild(processTitle);

            var processBody = new HBoxContainer();

            bool usePlus;

            if (process.OtherInputs.Count == 0)
            {
                // Just environmental stuff
                usePlus = true;
            }
            else
            {
                // Something turns into something else, uses the arrow notation
                usePlus = false;

                // Show the inputs
                // TODO: add commas or maybe pluses for multiple inputs
                foreach (var key in process.OtherInputs.Keys)
                {
                    var inputCompound = process.OtherInputs[key];

                    var amountLabel = new Label();
                    amountLabel.Text = Math.Round(inputCompound.Amount, 3) + " ";
                    processBody.AddChild(amountLabel);
                    processBody.AddChild(GUICommon.Instance.CreateCompoundIcon(inputCompound.Compound.Name));
                }

                // And the arrow
                var arrow = new TextureRect();
                arrow.Expand = true;
                arrow.RectMinSize = new Vector2(20, 20);
                arrow.Texture = GD.Load<Texture>("res://assets/textures/gui/bevel/WhiteArrow.png");
                processBody.AddChild(arrow);
            }

            // Outputs of the process. It's assumed that every process has outputs
            foreach (var key in process.Outputs.Keys)
            {
                var outputCompound = process.Outputs[key];

                var amountLabel = new Label();

                var stringBuilder = new StringBuilder(string.Empty, 150);

                // Changes process title and process# to red if process has 0 output
                if (outputCompound.Amount == 0)
                {
                    processTitle.AddColorOverride("font_color", new Color(1.0f, 0.1f, 0.1f));
                    amountLabel.AddColorOverride("font_color", new Color(1.0f, 0.1f, 0.1f));
                }

                if (usePlus)
                {
                    stringBuilder.Append(outputCompound.Amount >= 0 ? "+" : string.Empty);
                }

                stringBuilder.Append(Math.Round(outputCompound.Amount, 3) + " ");

                amountLabel.Text = stringBuilder.ToString();

                processBody.AddChild(amountLabel);
                processBody.AddChild(GUICommon.Instance.CreateCompoundIcon(outputCompound.Compound.Name));
            }

            var perSecondLabel = new Label();
            perSecondLabel.Text = "/second";

            processBody.AddChild(perSecondLabel);

            // Environment conditions
            if (process.EnvironmentInputs.Count > 0)
            {
                var atSymbol = new Label();

                atSymbol.Text = "@";
                atSymbol.RectMinSize = new Vector2(30, 20);
                atSymbol.Align = Label.AlignEnum.Center;
                processBody.AddChild(atSymbol);

                var first = true;

                foreach (var key in process.EnvironmentInputs.Keys)
                {
                    if (!first)
                    {
                        var commaLabel = new Label();
                        commaLabel.Text = ", ";
                        processBody.AddChild(commaLabel);
                    }

                    first = false;

                    var environmentCompound = process.EnvironmentInputs[key];

                    // To percentage
                    var percentageLabel = new Label();

                    // TODO: sunlight needs some special handling (it used to say the lux amount)
                    percentageLabel.Text = Math.Round(environmentCompound.AvailableAmount * 100, 1) + "%";

                    processBody.AddChild(percentageLabel);
                    processBody.AddChild(GUICommon.Instance.CreateCompoundIcon(environmentCompound.Compound.Name));
                }
            }

            processContainer.AddChild(processBody);
        }
    }

    /// <remarks>
    ///   TODO: this function should be cleaned up by generalizing the adding
    ///   the increase or decrease icons in order to remove the duplicated
    ///   logic here
    /// </remarks>
    private void UpdateConditionDifferencesBetweenPatches(Patch selectedPatch, Patch currentPatch)
    {
        var nextCompound = selectedPatch.Biome.AverageTemperature;

        if (nextCompound > currentPatch.Biome.AverageTemperature)
        {
            patchTemperatureSituation.Texture = IncreaseIcon;
        }
        else if (nextCompound < currentPatch.Biome.AverageTemperature)
        {
            patchTemperatureSituation.Texture = DecreaseIcon;
        }
        else
        {
            patchTemperatureSituation.Texture = null;
        }

        nextCompound = selectedPatch.Biome.Compounds[sunlight].Dissolved;

        if (nextCompound > currentPatch.Biome.Compounds[sunlight].Dissolved)
        {
            patchLightSituation.Texture = IncreaseIcon;
        }
        else if (nextCompound < currentPatch.Biome.Compounds[sunlight].Dissolved)
        {
            patchLightSituation.Texture = DecreaseIcon;
        }
        else
        {
            patchLightSituation.Texture = null;
        }

        nextCompound = selectedPatch.Biome.Compounds[hydrogensulfide].Density *
            selectedPatch.Biome.Compounds[hydrogensulfide].Amount + GetPatchChunkTotalCompoundAmount(
                selectedPatch, hydrogensulfide);

        if (nextCompound > currentPatch.Biome.Compounds[hydrogensulfide].Density *
            currentPatch.Biome.Compounds[hydrogensulfide].Amount + GetPatchChunkTotalCompoundAmount(
                currentPatch, hydrogensulfide))
        {
            patchHydrogenSulfideSituation.Texture = IncreaseIcon;
        }
        else if (nextCompound < currentPatch.Biome.Compounds[hydrogensulfide].Density *
            currentPatch.Biome.Compounds[hydrogensulfide].Amount + GetPatchChunkTotalCompoundAmount(
                currentPatch, hydrogensulfide))
        {
            patchHydrogenSulfideSituation.Texture = DecreaseIcon;
        }
        else
        {
            patchHydrogenSulfideSituation.Texture = null;
        }

        nextCompound = selectedPatch.Biome.Compounds[glucose].Density *
            selectedPatch.Biome.Compounds[glucose].Amount + GetPatchChunkTotalCompoundAmount(
                selectedPatch, glucose);

        if (nextCompound > currentPatch.Biome.Compounds[glucose].Density *
            currentPatch.Biome.Compounds[glucose].Amount + GetPatchChunkTotalCompoundAmount(
                currentPatch, glucose))
        {
            patchGlucoseSituation.Texture = IncreaseIcon;
        }
        else if (nextCompound < currentPatch.Biome.Compounds[glucose].Density *
            currentPatch.Biome.Compounds[glucose].Amount + GetPatchChunkTotalCompoundAmount(
                currentPatch, glucose))
        {
            patchGlucoseSituation.Texture = DecreaseIcon;
        }
        else
        {
            patchGlucoseSituation.Texture = null;
        }

        nextCompound = GetPatchChunkTotalCompoundAmount(selectedPatch, iron);

        if (nextCompound > GetPatchChunkTotalCompoundAmount(currentPatch, iron))
        {
            patchIronSituation.Texture = IncreaseIcon;
        }
        else if (nextCompound < GetPatchChunkTotalCompoundAmount(currentPatch, iron))
        {
            patchIronSituation.Texture = DecreaseIcon;
        }
        else
        {
            patchIronSituation.Texture = null;
        }

        nextCompound = selectedPatch.Biome.Compounds[ammonia].Density *
            selectedPatch.Biome.Compounds[ammonia].Amount + GetPatchChunkTotalCompoundAmount(
                selectedPatch, ammonia);

        if (nextCompound > currentPatch.Biome.Compounds[ammonia].Density *
            currentPatch.Biome.Compounds[ammonia].Amount + GetPatchChunkTotalCompoundAmount(
                currentPatch, ammonia))
        {
            patchAmmoniaSituation.Texture = IncreaseIcon;
        }
        else if (nextCompound < currentPatch.Biome.Compounds[ammonia].Density *
            currentPatch.Biome.Compounds[ammonia].Amount + GetPatchChunkTotalCompoundAmount(
                currentPatch, ammonia))
        {
            patchAmmoniaSituation.Texture = DecreaseIcon;
        }
        else
        {
            patchAmmoniaSituation.Texture = null;
        }

        nextCompound = selectedPatch.Biome.Compounds[phosphates].Density *
            selectedPatch.Biome.Compounds[phosphates].Amount + GetPatchChunkTotalCompoundAmount(
                selectedPatch, phosphates);

        if (nextCompound > currentPatch.Biome.Compounds[phosphates].Density *
            currentPatch.Biome.Compounds[phosphates].Amount + GetPatchChunkTotalCompoundAmount(
                currentPatch, phosphates))
        {
            patchPhosphateSituation.Texture = IncreaseIcon;
        }
        else if (nextCompound < currentPatch.Biome.Compounds[phosphates].Density *
            currentPatch.Biome.Compounds[phosphates].Amount + GetPatchChunkTotalCompoundAmount(
                currentPatch, phosphates))
        {
            patchPhosphateSituation.Texture = DecreaseIcon;
        }
        else
        {
            patchPhosphateSituation.Texture = null;
        }
    }

    private float GetPatchChunkTotalCompoundAmount(Patch patch, Compound compound)
    {
        var result = 0.0f;

        foreach (var chunkKey in patch.Biome.Chunks.Keys)
        {
            var chunk = patch.Biome.Chunks[chunkKey];

            if (chunk.Density > 0 && chunk.Compounds.ContainsKey(compound))
            {
                result += chunk.Density * chunk.Compounds[compound].Amount;
            }
        }

        return result;
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
        patchBiome.Text = "Biome: " + patch.BiomeTemplate.Name;
        patchDepth.Text = patch.Depth[0] + "-" + patch.Depth[1] + "m below sea level";
        patchPlayerHere.Visible = editor.CurrentPatch == patch;

        // Atmospheric gasses
        patchTemperature.Text = patch.Biome.AverageTemperature + " °C";
        patchPressure.Text = "20 bar";
        patchLight.Text = (patch.Biome.Compounds[sunlight].Dissolved * 100) + "% lux";
        patchOxygen.Text = (patch.Biome.Compounds[oxygen].Dissolved * 100) + "%";
        patchNitrogen.Text = (patch.Biome.Compounds[nitrogen].Dissolved * 100) + "% ppm";
        patchCO2.Text = (patch.Biome.Compounds[carbondioxide].Dissolved * 100) + "% ppm";

        // Compounds
        patchHydrogenSulfide.Text = Math.Round(patch.Biome.Compounds[hydrogensulfide].Density *
            patch.Biome.Compounds[hydrogensulfide].Amount + GetPatchChunkTotalCompoundAmount(
                patch, hydrogensulfide), 3) + "%";

        patchAmmonia.Text = Math.Round(patch.Biome.Compounds[ammonia].Density *
            patch.Biome.Compounds[ammonia].Amount + GetPatchChunkTotalCompoundAmount(
                patch, ammonia), 3) + "%";

        patchGlucose.Text = Math.Round(patch.Biome.Compounds[glucose].Density *
            patch.Biome.Compounds[glucose].Amount + GetPatchChunkTotalCompoundAmount(
                patch, glucose), 3) + "%";

        patchPhosphate.Text = Math.Round(patch.Biome.Compounds[phosphates].Density *
            patch.Biome.Compounds[phosphates].Amount + GetPatchChunkTotalCompoundAmount(
                patch, phosphates), 3) + "%";

        patchIron.Text = GetPatchChunkTotalCompoundAmount(patch, iron) + "%";

        // Delete previous species list
        if (speciesList.GetChildCount() > 0)
        {
            foreach (Node child in speciesList.GetChildren())
            {
                child.QueueFree();
            }
        }

        foreach (var species in patch.SpeciesInPatch.Keys)
        {
            var speciesLabel = new Label();
            speciesLabel.Text = species.FormattedName + " with population: " + patch.GetSpeciesPopulation(species);
            speciesList.AddChild(speciesLabel);

            // Yes, apparently this has to be done so that the rect size is updated immediately
            speciesList.RectSize = speciesList.RectSize;

            if (speciesListIsHidden)
            {
                // Adjust the species list's clipping area's "height" value
                var clip = speciesList.GetParent<MarginContainer>();
                clip.AddConstantOverride("margin_top", -(int)speciesList.RectSize.y);
            }
        }

        // Enable move to patch button if this is a valid move
        moveToPatchButton.Disabled = !editor.IsPatchMoveValid(patch);

        UpdateConditionDifferencesBetweenPatches(patch, editor.CurrentPatch);
    }

    private void OnSpeciesNameTextChanged(string newText)
    {
        if (newText.Split(" ").Length != 2)
        {
            speciesNameEdit.Set("custom_colors/font_color", new Color(1, 0, 0));
        }
        else
        {
            speciesNameEdit.Set("custom_colors/font_color", new Color(1, 1, 1));
        }

        editor.NewName = newText;
    }

    // ReSharper disable once RedundantNameQualifier
    private List<KeyValuePair<string, float>> SortBarData(System.Collections.Generic.Dictionary<string, float> bar)
    {
        var comparer = new ATPComparer();

        var result = bar.OrderBy(
                i => i.Key, comparer)
            .ToList();

        return result;
    }

    private class ATPComparer : IComparer<string>
    {
        /// <summary>
        ///   Compares ATP production / consumption items
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Only works if there aren't duplicate entries of osmoregulation or baseMovement.
        ///   </para>
        /// </remarks>
        public int Compare(string stringA, string stringB)
        {
            if (stringA == "osmoregulation")
            {
                return -1;
            }

            if (stringB == "osmoregulation")
            {
                return 1;
            }

            if (stringA == "baseMovement")
            {
                return -1;
            }

            if (stringB == "baseMovement")
            {
                return 1;
            }

            return string.Compare(stringA, stringB, StringComparison.InvariantCulture);
        }
    }
}
