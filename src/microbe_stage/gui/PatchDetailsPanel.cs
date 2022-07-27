﻿using System;
using System.Text;
using Godot;

public class PatchDetailsPanel : PanelContainer
{
    [Export]
    public NodePath NothingSelectedPath = null!;

    [Export]
    public NodePath DetailsPath = null!;

    [Export]
    public NodePath NamePath = null!;

    [Export]
    public NodePath PlayerHerePath = null!;

    [Export]
    public NodePath BiomePath = null!;

    [Export]
    public NodePath DepthPath = null!;

    [Export]
    public NodePath TemperaturePath = null!;

    [Export]
    public NodePath PressurePath = null!;

    [Export]
    public NodePath LightPath = null!;

    [Export]
    public NodePath OxygenPath = null!;

    [Export]
    public NodePath NitrogenPath = null!;

    [Export]
    public NodePath CO2Path = null!;

    [Export]
    public NodePath HydrogenSulfidePath = null!;

    [Export]
    public NodePath AmmoniaPath = null!;

    [Export]
    public NodePath GlucosePath = null!;

    [Export]
    public NodePath PhosphatePath = null!;

    [Export]
    public NodePath IronPath = null!;

    [Export]
    public NodePath SpeciesListBoxPath = null!;

    [Export]
    public NodePath MoveToPatchButtonPath = null!;

    [Export]
    public NodePath TemperatureSituationPath = null!;

    [Export]
    public NodePath LightSituationPath = null!;

    [Export]
    public NodePath HydrogenSulfideSituationPath = null!;

    [Export]
    public NodePath GlucoseSituationPath = null!;

    [Export]
    public NodePath IronSituationPath = null!;

    [Export]
    public NodePath AmmoniaSituationPath = null!;

    [Export]
    public NodePath PhosphateSituationPath = null!;

    private Control nothingSelected = null!;
    private Control details = null!;
    private Control playerHere = null!;
    private Label name = null!;
    private Label biome = null!;
    private Label depth = null!;
    private Label temperatureLabel = null!;
    private Label pressure = null!;
    private Label light = null!;
    private Label oxygen = null!;
    private Label nitrogen = null!;
    private Label co2 = null!;
    private Label hydrogenSulfide = null!;
    private Label ammonia = null!;
    private Label glucose = null!;
    private Label phosphate = null!;
    private Label iron = null!;
    private CollapsibleList speciesListBox = null!;
    private Button moveToPatchButton = null!;

    private TextureRect temperatureSituation = null!;
    private TextureRect lightSituation = null!;
    private TextureRect hydrogenSulfideSituation = null!;
    private TextureRect glucoseSituation = null!;
    private TextureRect ironSituation = null!;
    private TextureRect ammoniaSituation = null!;
    private TextureRect phosphateSituation = null!;

    private Compound ammoniaCompound = null!;
    private Compound carbondioxideCompound = null!;
    private Compound glucoseCompound = null!;
    private Compound hydrogensulfideCompound = null!;
    private Compound ironCompound = null!;
    private Compound nitrogenCompound = null!;
    private Compound oxygenCompound = null!;
    private Compound phosphatesCompound = null!;
    private Compound sunlightCompound = null!;

    private Texture increaseIcon = null!;
    private Texture decreaseIcon = null!;

    private Patch? targetPatch;
    private Patch? currentPatch;

    public Action<Patch>? OnMoveToPatchClicked { get; set; }

    public Patch? SelectedPatch
    {
        get => targetPatch;
        set
        {
            targetPatch = value;
            UpdateShownPatchDetails();
        }
    }

    public Patch? CurrentPatch
    {
        get => currentPatch;
        set
        {
            currentPatch = value;
            playerHere.Visible = CurrentPatch == SelectedPatch;

            if (SelectedPatch != null)
                UpdateConditionDifferencesBetweenPatches();
        }
    }

    public bool IsPatchMoveValid { get; set; }

    public override void _Ready()
    {
        nothingSelected = GetNode<Control>(NothingSelectedPath);
        details = GetNode<Control>(DetailsPath);
        playerHere = GetNode<Control>(PlayerHerePath);
        name = GetNode<Label>(NamePath);
        biome = GetNode<Label>(BiomePath);
        depth = GetNode<Label>(DepthPath);
        temperatureLabel = GetNode<Label>(TemperaturePath);
        pressure = GetNode<Label>(PressurePath);
        light = GetNode<Label>(LightPath);
        oxygen = GetNode<Label>(OxygenPath);
        nitrogen = GetNode<Label>(NitrogenPath);
        co2 = GetNode<Label>(CO2Path);
        hydrogenSulfide = GetNode<Label>(HydrogenSulfidePath);
        ammonia = GetNode<Label>(AmmoniaPath);
        glucose = GetNode<Label>(GlucosePath);
        phosphate = GetNode<Label>(PhosphatePath);
        iron = GetNode<Label>(IronPath);
        speciesListBox = GetNode<CollapsibleList>(SpeciesListBoxPath);
        moveToPatchButton = GetNode<Button>(MoveToPatchButtonPath);

        temperatureSituation = GetNode<TextureRect>(TemperatureSituationPath);
        lightSituation = GetNode<TextureRect>(LightSituationPath);
        hydrogenSulfideSituation = GetNode<TextureRect>(HydrogenSulfideSituationPath);
        glucoseSituation = GetNode<TextureRect>(GlucoseSituationPath);
        ironSituation = GetNode<TextureRect>(IronSituationPath);
        ammoniaSituation = GetNode<TextureRect>(AmmoniaSituationPath);
        phosphateSituation = GetNode<TextureRect>(PhosphateSituationPath);

        ammoniaCompound = SimulationParameters.Instance.GetCompound("ammonia");
        carbondioxideCompound = SimulationParameters.Instance.GetCompound("carbondioxide");
        glucoseCompound = SimulationParameters.Instance.GetCompound("glucose");
        hydrogensulfideCompound = SimulationParameters.Instance.GetCompound("hydrogensulfide");
        ironCompound = SimulationParameters.Instance.GetCompound("iron");
        nitrogenCompound = SimulationParameters.Instance.GetCompound("nitrogen");
        oxygenCompound = SimulationParameters.Instance.GetCompound("oxygen");
        phosphatesCompound = SimulationParameters.Instance.GetCompound("phosphates");
        sunlightCompound = SimulationParameters.Instance.GetCompound("sunlight");

        increaseIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/increase.png");
        decreaseIcon = GD.Load<Texture>("res://assets/textures/gui/bevel/decrease.png");
    }

    public void UpdateShownPatchDetails()
    {
        if (SelectedPatch == null)
        {
            details.Visible = false;
            nothingSelected.Visible = true;

            return;
        }

        details.Visible = true;
        nothingSelected.Visible = false;

        UpdatePatchDetails();

        // Enable move to patch button if this is a valid move
        moveToPatchButton.Disabled = !IsPatchMoveValid;
    }

    /// <summary>
    ///   Updates patch-specific GUI elements with data from a patch
    /// </summary>
    private void UpdatePatchDetails()
    {
        name.Text = SelectedPatch!.Name.ToString();

        // Biome: {0}
        biome.Text = TranslationServer.Translate("BIOME_LABEL").FormatSafe(SelectedPatch.BiomeTemplate.Name);

        // {0}-{1}m below sea level
        depth.Text = new LocalizedString("BELOW_SEA_LEVEL", SelectedPatch.Depth[0], SelectedPatch.Depth[1]).ToString();
        playerHere.Visible = CurrentPatch == SelectedPatch;

        var percentageFormat = TranslationServer.Translate("PERCENTAGE_VALUE");
        var unitFormat = TranslationServer.Translate("VALUE_WITH_UNIT");

        // Atmospheric gasses
        var temperature = SimulationParameters.Instance.GetCompound("temperature");
        temperatureLabel.Text =
            unitFormat.FormatSafe(SelectedPatch.Biome.Compounds[temperature].Ambient, temperature.Unit);
        pressure.Text = unitFormat.FormatSafe(20, "bar");
        light.Text =
            unitFormat.FormatSafe(
                percentageFormat.FormatSafe(GetCompoundAmount(SelectedPatch, sunlightCompound.InternalName)), "lx");
        oxygen.Text = percentageFormat.FormatSafe(GetCompoundAmount(SelectedPatch, oxygenCompound.InternalName));
        nitrogen.Text = percentageFormat.FormatSafe(GetCompoundAmount(SelectedPatch, nitrogenCompound.InternalName));
        co2.Text = percentageFormat.FormatSafe(GetCompoundAmount(SelectedPatch, carbondioxideCompound.InternalName));

        // Compounds
        hydrogenSulfide.Text =
            percentageFormat.FormatSafe(
                Math.Round(GetCompoundAmount(SelectedPatch, hydrogensulfideCompound.InternalName), 3));
        ammonia.Text =
            percentageFormat.FormatSafe(Math.Round(GetCompoundAmount(SelectedPatch, ammoniaCompound.InternalName), 3));
        glucose.Text =
            percentageFormat.FormatSafe(Math.Round(GetCompoundAmount(SelectedPatch, glucoseCompound.InternalName), 3));
        phosphate.Text =
            percentageFormat.FormatSafe(
                Math.Round(GetCompoundAmount(SelectedPatch, phosphatesCompound.InternalName), 3));
        iron.Text = percentageFormat.FormatSafe(GetCompoundAmount(SelectedPatch, ironCompound.InternalName));

        var label = speciesListBox.GetItem<CustomRichTextLabel>("SpeciesList");
        var speciesList = new StringBuilder(100);

        foreach (var species in SelectedPatch.SpeciesInPatch.Keys)
        {
            speciesList.AppendLine(TranslationServer.Translate("SPECIES_WITH_POPULATION").FormatSafe(
                species.FormattedNameBbCode, SelectedPatch.GetSpeciesSimulationPopulation(species)));
        }

        label.ExtendedBbcode = speciesList.ToString();

        UpdateConditionDifferencesBetweenPatches();
    }

    private float GetCompoundAmount(Patch patch, string compoundName)
    {
        return patch.GetCompoundAmount(compoundName);
    }

    /// <remarks>
    ///   TODO: this function should be cleaned up by generalizing the adding
    ///   the increase or decrease icons in order to remove the duplicated
    ///   logic here
    /// </remarks>
    private void UpdateConditionDifferencesBetweenPatches()
    {
        if (SelectedPatch == null || CurrentPatch == null)
            return;

        var temperature = SimulationParameters.Instance.GetCompound("temperature");
        var nextCompound = SelectedPatch.Biome.Compounds[temperature].Ambient;

        if (nextCompound > CurrentPatch.Biome.Compounds[temperature].Ambient)
        {
            temperatureSituation.Texture = increaseIcon;
        }
        else if (nextCompound < CurrentPatch.Biome.Compounds[temperature].Ambient)
        {
            temperatureSituation.Texture = decreaseIcon;
        }
        else
        {
            temperatureSituation.Texture = null;
        }

        nextCompound = SelectedPatch.Biome.Compounds[sunlightCompound].Ambient;

        if (nextCompound > CurrentPatch.Biome.Compounds[sunlightCompound].Ambient)
        {
            lightSituation.Texture = increaseIcon;
        }
        else if (nextCompound < CurrentPatch.Biome.Compounds[sunlightCompound].Ambient)
        {
            lightSituation.Texture = decreaseIcon;
        }
        else
        {
            lightSituation.Texture = null;
        }

        nextCompound = GetCompoundAmount(SelectedPatch, hydrogensulfideCompound.InternalName);

        if (nextCompound > GetCompoundAmount(CurrentPatch, hydrogensulfideCompound.InternalName))
        {
            hydrogenSulfideSituation.Texture = increaseIcon;
        }
        else if (nextCompound < GetCompoundAmount(CurrentPatch, hydrogensulfideCompound.InternalName))
        {
            hydrogenSulfideSituation.Texture = decreaseIcon;
        }
        else
        {
            hydrogenSulfideSituation.Texture = null;
        }

        nextCompound = GetCompoundAmount(SelectedPatch, glucoseCompound.InternalName);

        if (nextCompound > GetCompoundAmount(CurrentPatch, glucoseCompound.InternalName))
        {
            glucoseSituation.Texture = increaseIcon;
        }
        else if (nextCompound < GetCompoundAmount(CurrentPatch, glucoseCompound.InternalName))
        {
            glucoseSituation.Texture = decreaseIcon;
        }
        else
        {
            glucoseSituation.Texture = null;
        }

        nextCompound = GetCompoundAmount(SelectedPatch, ironCompound.InternalName);

        if (nextCompound > GetCompoundAmount(CurrentPatch, ironCompound.InternalName))
        {
            ironSituation.Texture = increaseIcon;
        }
        else if (nextCompound < GetCompoundAmount(CurrentPatch, ironCompound.InternalName))
        {
            ironSituation.Texture = decreaseIcon;
        }
        else
        {
            ironSituation.Texture = null;
        }

        nextCompound = GetCompoundAmount(SelectedPatch, ammoniaCompound.InternalName);

        if (nextCompound > GetCompoundAmount(CurrentPatch, ammoniaCompound.InternalName))
        {
            ammoniaSituation.Texture = increaseIcon;
        }
        else if (nextCompound < GetCompoundAmount(CurrentPatch, ammoniaCompound.InternalName))
        {
            ammoniaSituation.Texture = decreaseIcon;
        }
        else
        {
            ammoniaSituation.Texture = null;
        }

        nextCompound = GetCompoundAmount(SelectedPatch, phosphatesCompound.InternalName);

        if (nextCompound > GetCompoundAmount(CurrentPatch, phosphatesCompound.InternalName))
        {
            phosphateSituation.Texture = increaseIcon;
        }
        else if (nextCompound < GetCompoundAmount(CurrentPatch, phosphatesCompound.InternalName))
        {
            phosphateSituation.Texture = decreaseIcon;
        }
        else
        {
            phosphateSituation.Texture = null;
        }
    }

    private void MoveToPatchClicked()
    {
        if (SelectedPatch == null)
            return;

        if (OnMoveToPatchClicked == null)
        {
            GD.PrintErr("No move to patch callback set, probably nothing is going to happen");
            return;
        }

        OnMoveToPatchClicked.Invoke(SelectedPatch);
    }
}
