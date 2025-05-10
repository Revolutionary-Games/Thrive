using Godot;

public partial class PlanetCustomizer : Node
{
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
            WorldSize = (WorldGenerationSettings.WorldSizeEnum)planetSettings.worldSizeButton.Selected,
            WorldTemperature =
                (WorldGenerationSettings.WorldTemperatureEnum)planetSettings.worldTemperatureButton.Selected,
            WorldSeaLevel = (WorldGenerationSettings.WorldSeaLevelEnum)planetSettings.worldSeaLevelButton.Selected,
            GeologicalActivity =
                (WorldGenerationSettings.GeologicalActivityEnum)planetSettings.worldGeologicalActivityButton.Selected,
            ClimateInstability =
                (WorldGenerationSettings.ClimateInstabilityEnum)planetSettings.worldClimateInstabilityButton.Selected,
            Origin = (WorldGenerationSettings.LifeOrigin)planetSettings.lifeOriginButton.Selected,
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
