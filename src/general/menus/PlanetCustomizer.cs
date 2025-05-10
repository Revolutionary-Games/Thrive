using Godot;

public partial class PlanetCustomizer : Node
{
    [Export]
    private PatchMapDrawer patchMapDrawer = null!;

    [Export]
    private PatchDetailsPanel patchDetailsPanel = null!;

    [Export]
    private PlanetSettings settingsPanel = null!;

    [Export]
    private Control patchMapPanel = null!;

    [Export]
    private Control patchMapButtons = null!;

    [Export]
    private Button generateButton = null!;

    public WorldGenerationSettings WorldSettings;

    /// <summary>
    ///   The game itself
    /// </summary>
    public GameProperties GameProperties;

    public override void _Ready()
    {
        base._Ready();
        InitNewWorld(SimulationParameters.Instance.AutoEvoConfiguration);
        patchMapDrawer.OnSelectedPatchChanged += UpdatePatchDetailPanel;
    }

    private void InitNewWorld(IAutoEvoConfiguration configuration)
    {
        WorldSettings = new WorldGenerationSettings
        {
            AutoEvoConfiguration = configuration,
            WorldSize = (WorldGenerationSettings.WorldSizeEnum)settingsPanel.worldSizeButton.Selected,
            WorldTemperature =
                (WorldGenerationSettings.WorldTemperatureEnum)settingsPanel.worldTemperatureButton.Selected,
            WorldSeaLevel = (WorldGenerationSettings.WorldSeaLevelEnum)settingsPanel.worldSeaLevelButton.Selected,
            GeologicalActivity =
                (WorldGenerationSettings.GeologicalActivityEnum)settingsPanel.worldGeologicalActivityButton.Selected,
            ClimateInstability =
                (WorldGenerationSettings.ClimateInstabilityEnum)settingsPanel.worldClimateInstabilityButton.Selected,
            Origin = (WorldGenerationSettings.LifeOrigin)settingsPanel.lifeOriginButton.Selected,
        };

        GameProperties = GameProperties.StartNewMicrobeGame(WorldSettings);
        GameProperties.GameWorld.Map.RevealAllPatches();
        patchMapDrawer.PlayerPatch = null;
        patchMapDrawer.Map = GameProperties.GameWorld.Map;
        patchMapDrawer.SelectedPatch = patchMapDrawer.PlayerPatch;
        patchDetailsPanel.SelectedPatch = patchMapDrawer.PlayerPatch;
        UpdatePatchDetailPanel(patchMapDrawer);

        // patchMapDrawer.CenterToCurrentPatch();
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
