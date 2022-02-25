using System;
using System.Globalization;
using Godot;

/// <summary>
///   Editor GUI base that adds patch map functionality
/// </summary>
public abstract class EditorWithPatchesGUIBase<TEditor> : EditorGUIBase<TEditor>
    where TEditor : Godot.Object, IEditorWithPatches
{
    [Export]
    public NodePath MapDrawerPath = null!;

    [Export]
    public NodePath PatchNothingSelectedPath = null!;

    [Export]
    public NodePath PatchDetailsPath = null!;

    [Export]
    public NodePath PatchNamePath = null!;

    [Export]
    public NodePath PatchPlayerHerePath = null!;

    [Export]
    public NodePath PatchBiomePath = null!;

    [Export]
    public NodePath PatchDepthPath = null!;

    [Export]
    public NodePath PatchTemperaturePath = null!;

    [Export]
    public NodePath PatchPressurePath = null!;

    [Export]
    public NodePath PatchLightPath = null!;

    [Export]
    public NodePath PatchOxygenPath = null!;

    [Export]
    public NodePath PatchNitrogenPath = null!;

    [Export]
    public NodePath PatchCO2Path = null!;

    [Export]
    public NodePath PatchHydrogenSulfidePath = null!;

    [Export]
    public NodePath PatchAmmoniaPath = null!;

    [Export]
    public NodePath PatchGlucosePath = null!;

    [Export]
    public NodePath PatchPhosphatePath = null!;

    [Export]
    public NodePath PatchIronPath = null!;

    [Export]
    public NodePath SpeciesCollapsibleBoxPath = null!;

    [Export]
    public NodePath MoveToPatchButtonPath = null!;

    [Export]
    public NodePath PatchTemperatureSituationPath = null!;

    [Export]
    public NodePath PatchLightSituationPath = null!;

    [Export]
    public NodePath PatchHydrogenSulfideSituationPath = null!;

    [Export]
    public NodePath PatchGlucoseSituationPath = null!;

    [Export]
    public NodePath PatchIronSituationPath = null!;

    [Export]
    public NodePath PatchAmmoniaSituationPath = null!;

    [Export]
    public NodePath PatchPhosphateSituationPath = null!;

    protected PatchMapDrawer mapDrawer = null!;

    private Control patchNothingSelected = null!;
    private Control patchDetails = null!;
    private Control patchPlayerHere = null!;
    private Label patchName = null!;
    private Label patchBiome = null!;
    private Label patchDepth = null!;
    private Label patchTemperature = null!;
    private Label patchPressure = null!;
    private Label patchLight = null!;
    private Label patchOxygen = null!;
    private Label patchNitrogen = null!;
    private Label patchCO2 = null!;
    private Label patchHydrogenSulfide = null!;
    private Label patchAmmonia = null!;
    private Label patchGlucose = null!;
    private Label patchPhosphate = null!;
    private Label patchIron = null!;
    private CollapsibleList speciesListBox = null!;
    private Button moveToPatchButton = null!;

    private TextureRect patchTemperatureSituation = null!;
    private TextureRect patchLightSituation = null!;
    private TextureRect patchHydrogenSulfideSituation = null!;
    private TextureRect patchGlucoseSituation = null!;
    private TextureRect patchIronSituation = null!;
    private TextureRect patchAmmoniaSituation = null!;
    private TextureRect patchPhosphateSituation = null!;

    public override void _Ready()
    {
        base._Ready();

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
        speciesListBox = GetNode<CollapsibleList>(SpeciesCollapsibleBoxPath);
        moveToPatchButton = GetNode<Button>(MoveToPatchButtonPath);

        patchTemperatureSituation = GetNode<TextureRect>(PatchTemperatureSituationPath);
        patchLightSituation = GetNode<TextureRect>(PatchLightSituationPath);
        patchHydrogenSulfideSituation = GetNode<TextureRect>(PatchHydrogenSulfideSituationPath);
        patchGlucoseSituation = GetNode<TextureRect>(PatchGlucoseSituationPath);
        patchIronSituation = GetNode<TextureRect>(PatchIronSituationPath);
        patchAmmoniaSituation = GetNode<TextureRect>(PatchAmmoniaSituationPath);
        patchPhosphateSituation = GetNode<TextureRect>(PatchPhosphateSituationPath);

        mapDrawer.OnSelectedPatchChanged = _ => { UpdateShownPatchDetails(); };
    }

    public void SetMap(PatchMap map)
    {
        mapDrawer.Map = map;
    }

    public void UpdatePlayerPatch(Patch? patch)
    {
        if (editor == null)
            throw new InvalidOperationException("GUI not initialized");

        mapDrawer.PlayerPatch = patch ?? editor.CurrentPatch;

        // Just in case this didn't get called already. Note that this may result in duplicate calls here
        UpdateShownPatchDetails();
    }

    /// <summary>
    ///   Updates patch-specific GUI elements with data from a patch
    /// </summary>
    public virtual void UpdatePatchDetails(Patch patch)
    {
        if (editor == null)
            throw new InvalidOperationException("GUI not initialized");

        patchName.Text = TranslationServer.Translate(patch.Name);

        // Biome: {0}
        patchBiome.Text = string.Format(CultureInfo.CurrentCulture,
            TranslationServer.Translate("BIOME_LABEL"),
            patch.BiomeTemplate.Name);

        // {0}-{1}m below sea level
        patchDepth.Text = string.Format(CultureInfo.CurrentCulture,
            TranslationServer.Translate("BELOW_SEA_LEVEL"),
            patch.Depth[0], patch.Depth[1]);
        patchPlayerHere.Visible = editor.CurrentPatch == patch;

        var percentageFormat = TranslationServer.Translate("PERCENTAGE_VALUE");

        // Atmospheric gasses
        patchTemperature.Text = patch.Biome.AverageTemperature + " °C";
        patchPressure.Text = "20 bar";
        patchLight.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            GetCompoundAmount(patch, sunlight.InternalName)) + " lx";
        patchOxygen.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            GetCompoundAmount(patch, oxygen.InternalName));
        patchNitrogen.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            GetCompoundAmount(patch, nitrogen.InternalName));
        patchCO2.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            GetCompoundAmount(patch, carbondioxide.InternalName));

        // Compounds
        patchHydrogenSulfide.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            Math.Round(GetCompoundAmount(patch, hydrogensulfide.InternalName), 3));
        patchAmmonia.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            Math.Round(GetCompoundAmount(patch, ammonia.InternalName), 3));
        patchGlucose.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            Math.Round(GetCompoundAmount(patch, glucose.InternalName), 3));
        patchPhosphate.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            Math.Round(GetCompoundAmount(patch, phosphates.InternalName), 3));
        patchIron.Text = string.Format(CultureInfo.CurrentCulture, percentageFormat,
            GetCompoundAmount(patch, iron.InternalName));

        // Refresh species list
        speciesListBox.ClearItems();

        foreach (var species in patch.SpeciesInPatch.Keys)
        {
            var speciesLabel = new Label();
            speciesLabel.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            speciesLabel.Autowrap = true;
            speciesLabel.Text = string.Format(CultureInfo.CurrentCulture,
                TranslationServer.Translate("SPECIES_WITH_POPULATION"), species.FormattedName,
                patch.GetSpeciesPopulation(species));
            speciesListBox.AddItem(speciesLabel);
        }

        UpdateConditionDifferencesBetweenPatches(patch, editor.CurrentPatch);
    }

    protected void MoveToPatchClicked()
    {
        var target = mapDrawer.SelectedPatch;

        if (editor!.IsPatchMoveValid(target))
            editor.SetPlayerPatch(target);
    }

    /// <remarks>
    ///   TODO: this function should be cleaned up by generalizing the adding the increase or decrease icons in order
    ///   to remove the duplicated logic here
    /// </remarks>
    private void UpdateConditionDifferencesBetweenPatches(Patch selectedPatch, Patch currentPatch)
    {
        var nextCompound = selectedPatch.Biome.AverageTemperature;

        if (nextCompound > currentPatch.Biome.AverageTemperature)
        {
            patchTemperatureSituation.Texture = increaseIcon;
        }
        else if (nextCompound < currentPatch.Biome.AverageTemperature)
        {
            patchTemperatureSituation.Texture = decreaseIcon;
        }
        else
        {
            patchTemperatureSituation.Texture = null;
        }

        nextCompound = selectedPatch.Biome.Compounds[sunlight].Dissolved;

        if (nextCompound > currentPatch.Biome.Compounds[sunlight].Dissolved)
        {
            patchLightSituation.Texture = increaseIcon;
        }
        else if (nextCompound < currentPatch.Biome.Compounds[sunlight].Dissolved)
        {
            patchLightSituation.Texture = decreaseIcon;
        }
        else
        {
            patchLightSituation.Texture = null;
        }

        nextCompound = GetCompoundAmount(selectedPatch, hydrogensulfide.InternalName);

        if (nextCompound > GetCompoundAmount(currentPatch, hydrogensulfide.InternalName))
        {
            patchHydrogenSulfideSituation.Texture = increaseIcon;
        }
        else if (nextCompound < GetCompoundAmount(currentPatch, hydrogensulfide.InternalName))
        {
            patchHydrogenSulfideSituation.Texture = decreaseIcon;
        }
        else
        {
            patchHydrogenSulfideSituation.Texture = null;
        }

        nextCompound = GetCompoundAmount(selectedPatch, glucose.InternalName);

        if (nextCompound > GetCompoundAmount(currentPatch, glucose.InternalName))
        {
            patchGlucoseSituation.Texture = increaseIcon;
        }
        else if (nextCompound < GetCompoundAmount(currentPatch, glucose.InternalName))
        {
            patchGlucoseSituation.Texture = decreaseIcon;
        }
        else
        {
            patchGlucoseSituation.Texture = null;
        }

        nextCompound = GetCompoundAmount(selectedPatch, iron.InternalName);

        if (nextCompound > GetCompoundAmount(currentPatch, iron.InternalName))
        {
            patchIronSituation.Texture = increaseIcon;
        }
        else if (nextCompound < GetCompoundAmount(currentPatch, iron.InternalName))
        {
            patchIronSituation.Texture = decreaseIcon;
        }
        else
        {
            patchIronSituation.Texture = null;
        }

        nextCompound = GetCompoundAmount(selectedPatch, ammonia.InternalName);

        if (nextCompound > GetCompoundAmount(currentPatch, ammonia.InternalName))
        {
            patchAmmoniaSituation.Texture = increaseIcon;
        }
        else if (nextCompound < GetCompoundAmount(currentPatch, ammonia.InternalName))
        {
            patchAmmoniaSituation.Texture = decreaseIcon;
        }
        else
        {
            patchAmmoniaSituation.Texture = null;
        }

        nextCompound = GetCompoundAmount(selectedPatch, phosphates.InternalName);

        if (nextCompound > GetCompoundAmount(currentPatch, phosphates.InternalName))
        {
            patchPhosphateSituation.Texture = increaseIcon;
        }
        else if (nextCompound < GetCompoundAmount(currentPatch, phosphates.InternalName))
        {
            patchPhosphateSituation.Texture = decreaseIcon;
        }
        else
        {
            patchPhosphateSituation.Texture = null;
        }
    }

    protected virtual void UpdateShownPatchDetails()
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

        UpdatePatchDetails(patch);

        // Enable move to patch button if this is a valid move
        moveToPatchButton.Disabled = !editor!.IsPatchMoveValid(patch);
    }

    protected float GetCompoundAmount(Patch patch, string compoundName)
    {
        return GetCompoundAmount(patch, patch.Biome, compoundName);
    }

    protected float GetCompoundAmount(Patch patch, BiomeConditions biome, string compoundName)
    {
        var compound = SimulationParameters.Instance.GetCompound(compoundName);

        switch (compoundName)
        {
            case "sunlight":
                return biome.Compounds[compound].Dissolved * 100;
            case "oxygen":
                return biome.Compounds[compound].Dissolved * 100;
            case "carbondioxide":
                return biome.Compounds[compound].Dissolved * 100;
            case "nitrogen":
                return biome.Compounds[compound].Dissolved * 100;
            case "iron":
                return patch.GetTotalChunkCompoundAmount(compound);
            default:
                return biome.Compounds[compound].Density *
                    biome.Compounds[compound].Amount + patch.GetTotalChunkCompoundAmount(
                        compound);
        }
    }
}
