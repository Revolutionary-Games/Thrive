using Godot;

/// <summary>
/// Customizes planet settings and world generation.
/// </summary>
public partial class PlanetCustomizer : Node
{
    public WorldGenerationSettings WorldSettings = null!;

    /// <summary>
    ///   The game itself
    /// </summary>
    public GameProperties GameProperties = null!;

    [Export]
    private PatchMapDrawer patchMapDrawer = null!;

    [Export]
    private PatchDetailsPanel patchDetailsPanel = null!;

    [Export]
    private PanelContainer settingsPanel = null!;

    [Export]
    private Control patchMapPanel = null!;

    [Export]
    private Control patchMapButtons = null!;

    [Export]
    private Button generateButton = null!;

    [Export]
    private PlanetSettings planetSettings = null!;

    public override void _Ready()
    {
        base._Ready();
        InitNewWorld(SimulationParameters.Instance.AutoEvoConfiguration);
        patchMapDrawer.OnSelectedPatchChanged += UpdatePatchDetailPanel;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            patchMapDrawer.Dispose();
            patchDetailsPanel.Dispose();
            settingsPanel.Dispose();
            patchMapPanel.Dispose();
            patchMapButtons.Dispose();
            generateButton.Dispose();
            planetSettings.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitNewWorld(IAutoEvoConfiguration configuration)
    {
        WorldSettings = new WorldGenerationSettings
        {
            AutoEvoConfiguration = configuration,
            WorldSize = (WorldGenerationSettings.WorldSizeEnum)planetSettings.WorldSizeButton.Selected,
            WorldTemperature =
                (WorldGenerationSettings.WorldTemperatureEnum)planetSettings.WorldTemperatureButton.Selected,
            WorldSeaLevel = (WorldGenerationSettings.WorldSeaLevelEnum)planetSettings.WorldSeaLevelButton.Selected,
            GeologicalActivity =
                (WorldGenerationSettings.GeologicalActivityEnum)planetSettings.WorldGeologicalActivityButton.Selected,
            ClimateInstability =
                (WorldGenerationSettings.ClimateInstabilityEnum)planetSettings.WorldClimateInstabilityButton.Selected,
            Origin = (WorldGenerationSettings.LifeOrigin)planetSettings.LifeOriginButton.Selected,
        };

        GameProperties = GameProperties.StartNewMicrobeGame(WorldSettings);
        GameProperties.GameWorld.Map.RevealAllPatches();
        patchMapDrawer.PlayerPatch = null;
        patchMapDrawer.Map = GameProperties.GameWorld.Map;
        patchMapDrawer.SelectedPatch = patchMapDrawer.PlayerPatch;
        patchDetailsPanel.SelectedPatch = patchMapDrawer.PlayerPatch;
        // UpdatePatchDetailPanel(patchMapDrawer);
    }

    private void UpdatePatchDetailPanel(PatchMapDrawer drawer)
    {
        var selectedPatch = drawer.SelectedPatch;

        if (selectedPatch == null)
            return;

        patchDetailsPanel.SelectedPatch = selectedPatch;
    }

    private void OnBackButtonPressed()
    {
        TransitionManager.Instance.AddSequence(ScreenFade.FadeType.FadeOut, 0.1f,
            SceneManager.Instance.ReturnToMenu, false);
    }

    private void OnGeneratePressed()
    {
        if (settingsPanel.Visible)
        {
            InitNewWorld(SimulationParameters.Instance.AutoEvoConfiguration);
            generateButton.Text = Localization.Translate("BACK_TO_SETTINGS");
        }
        else
        {
            generateButton.Text = Localization.Translate("GENERATE_BUTTON");
        }

        settingsPanel.Visible = !settingsPanel.Visible;
        patchMapPanel.Visible = !patchMapPanel.Visible;
        patchMapButtons.Visible = !patchMapButtons.Visible;
    }

    private void OnRegeneratePressed()
    {
        InitNewWorld(SimulationParameters.Instance.AutoEvoConfiguration);
    }
}
