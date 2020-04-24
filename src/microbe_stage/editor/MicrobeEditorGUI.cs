using System;
using System.Collections.Generic;
using System.Text;
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

    private const string ATP_BALANCE_DEFAULT_TEXT = "ATP Balance";

    private MicrobeEditor editor;
    private LoadingScreen loadingScreen;

    private Godot.Collections.Array organelleSelectionElements;
    private Godot.Collections.Array membraneSelectionElements;
    private Godot.Collections.Array itemTooltipElements;

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

    private bool inEditorTab = false;

    public override void _Ready()
    {
        organelleSelectionElements = GetTree().GetNodesInGroup("OrganelleSelectionElement");
        membraneSelectionElements = GetTree().GetNodesInGroup("MembraneSelectionElement");
        itemTooltipElements = GetTree().GetNodesInGroup("ItemTooltip");

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

    /// <summary>
    ///   Updates the organelle efficiencies in tooltips.
    /// </summary>
    public void UpdateOrganelleEfficiencies(Dictionary<string, OrganelleEfficiency> organelleEfficiency)
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

    private void OnConditionClicked(string tab)
    {
        // I couldn't make these slide
        if (tab == "physical")
        {
            var minusButton = physicalConditionsButton.GetNode<TextureButton>("minusButton");
            var plusButton = physicalConditionsButton.GetNode<TextureButton>("plusButton");

            if (!physicalConditionsBox.Visible)
            {
                physicalConditionsBox.Show();
                minusButton.Show();
                plusButton.Hide();
            }
            else
            {
                physicalConditionsBox.Hide();
                minusButton.Hide();
                plusButton.Show();
            }
        }

        if (tab == "atmospheric")
        {
            var minusButton = atmosphericConditionsButton.GetNode<TextureButton>("minusButton");
            var plusButton = atmosphericConditionsButton.GetNode<TextureButton>("plusButton");

            if (!atmosphericConditionsBox.Visible)
            {
                atmosphericConditionsBox.Show();
                minusButton.Show();
                plusButton.Hide();
            }
            else
            {
                atmosphericConditionsBox.Hide();
                minusButton.Hide();
                plusButton.Show();
            }
        }

        if (tab == "compounds")
        {
            var minusButton = compoundsButton.GetNode<TextureButton>("minusButton");
            var plusButton = compoundsButton.GetNode<TextureButton>("plusButton");

            if (!compoundsBox.Visible)
            {
                compoundsBox.Show();
                minusButton.Show();
                plusButton.Hide();
            }
            else
            {
                compoundsBox.Hide();
                minusButton.Hide();
                plusButton.Show();
            }
        }

        if (tab == "species")
        {
            var minusButton = speciesListButton.GetNode<TextureButton>("minusButton");
            var plusButton = speciesListButton.GetNode<TextureButton>("plusButton");

            if (!speciesList.Visible)
            {
                speciesList.Show();
                minusButton.Show();
                plusButton.Hide();
            }
            else
            {
                speciesList.Hide();
                minusButton.Hide();
                plusButton.Show();
            }
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

    private TextureRect CreateCompoundIcon(string compoundName)
    {
        var element = new TextureRect();
        element.Expand = true;
        element.RectMinSize = new Vector2(20, 20);

        var icon = GD.Load<Texture>("res://assets/textures/gui/bevel/" + compoundName.ReplaceN(
            " ", string.Empty) + ".png");

        element.Texture = icon;

        return element;
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

            var usePlus = true;

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
                    amountLabel.Text = Math.Round(inputCompound.Amount, 2) + " ";
                    processBody.AddChild(amountLabel);
                    processBody.AddChild(CreateCompoundIcon(inputCompound.Compound.Name));
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

                var stringBuilder = new StringBuilder("", 150);

                if (usePlus)
                {
                    stringBuilder.Append(outputCompound.Amount >= 0 ? "+" : "");
                }

                stringBuilder.Append(Math.Round(outputCompound.Amount, 2) + " ");

                amountLabel.Text = stringBuilder.ToString();

                processBody.AddChild(amountLabel);
                processBody.AddChild(CreateCompoundIcon(outputCompound.Compound.Name));
            }

            var perSecondLabel = new Label();
            perSecondLabel.Text = "/second";

            processBody.AddChild(perSecondLabel);

            // Environment conditions
            if (process.EnvironmentInputs.Count > 0)
            {
                var atSymbol = new Label();
                var separator = new HSeparator();

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
                    percentageLabel.Text = Math.Round((environmentCompound.AvailableAmount * 100), 1) + "%";

                    processBody.AddChild(percentageLabel);
                    processBody.AddChild(CreateCompoundIcon(environmentCompound.Compound.Name));
                }
            }

            processContainer.AddChild(processBody);
        }
    }

    private float GetPatchChunkTotalCompoundAmount(Patch patch, string compoundName)
    {
        var result = 0.0f;

        foreach (var chunkKey in patch.Biome.Chunks.Keys)
        {
            var chunk = patch.Biome.Chunks[chunkKey];

            if (chunk.Density != 0 && chunk.Compounds.ContainsKey(compoundName))
            {
                result += chunk.Density * chunk.Compounds[compoundName].Amount;
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
        patchBiome.Text = "Biome: " + patch.Biome.Name;
        patchPlayerHere.Visible = editor.CurrentPatch == patch;

        // Atmospheric gasses
        patchTemperature.Text = patch.Biome.AverageTemperature + " °C";
        patchPressure.Text = "20 bar";
        patchLight.Text = (patch.Biome.Compounds["sunlight"].Dissolved * 100) + "% lux";
        patchOxygen.Text = (patch.Biome.Compounds["oxygen"].Dissolved * 100) + "%";
        patchNitrogen.Text = (patch.Biome.Compounds["nitrogen"].Dissolved * 100) + "% ppm";
        patchCO2.Text = (patch.Biome.Compounds["carbondioxide"].Dissolved * 100) + "% ppm";

        // Compounds
        patchHydrogenSulfide.Text = Math.Round(patch.Biome.Compounds["hydrogensulfide"].Density *
            patch.Biome.Compounds["hydrogensulfide"].Amount + GetPatchChunkTotalCompoundAmount(
            patch, "hydrogensulfide"), 3) + "%";

        patchAmmonia.Text = Math.Round(patch.Biome.Compounds["ammonia"].Density *
            patch.Biome.Compounds["ammonia"].Amount + GetPatchChunkTotalCompoundAmount(
            patch, "ammonia"), 3) + "%";

        patchGlucose.Text = Math.Round(patch.Biome.Compounds["glucose"].Density *
            patch.Biome.Compounds["glucose"].Amount + GetPatchChunkTotalCompoundAmount(
            patch, "glucose"), 3) + "%";

        patchPhosphate.Text = Math.Round(patch.Biome.Compounds["phosphates"].Density *
            patch.Biome.Compounds["phosphates"].Amount + GetPatchChunkTotalCompoundAmount(
            patch, "phosphates"), 3) + "%";

        patchIron.Text = GetPatchChunkTotalCompoundAmount(patch, "iron") + "%";

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
            speciesLabel.Text = species.FormattedName + " with population: " + species.Population;
            speciesList.AddChild(speciesLabel);
        }

        // Enable move to patch button if this is a valid move
        moveToPatchButton.Disabled = !editor.IsPatchMoveValid(patch);
    }
}
