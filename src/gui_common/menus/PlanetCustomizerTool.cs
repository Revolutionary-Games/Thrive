using Godot;

/// <summary>
///   Customizes planet settings and world generation.
/// </summary>
public partial class PlanetCustomizerTool : Node
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

    private WorldGenerationSettings worldSettings = null!;

    private GameProperties gameProperties = null!;

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
        var planetGenerationSettings = planetSettings.GetPlanetSettings();
        worldSettings = new WorldGenerationSettings
        {
            AutoEvoConfiguration = configuration,
            WorldSize = planetGenerationSettings.WorldSize,
            WorldTemperature = planetGenerationSettings.WorldTemperature,
            WorldSeaLevel = planetGenerationSettings.WorldSeaLevel,
            GeologicalActivity = planetGenerationSettings.GeologicalActivity,
            ClimateInstability = planetGenerationSettings.ClimateInstability,
            Origin = planetGenerationSettings.Origin,
            DayNightCycleEnabled = planetGenerationSettings.DayNightCycleEnabled,
            DayLength = planetGenerationSettings.DayLength,
            LAWK = planetGenerationSettings.LAWK,
        };

        gameProperties = GameProperties.StartNewMicrobeGame(worldSettings);
        gameProperties.GameWorld.Map.RevealAllPatches();
        patchMapDrawer.PlayerPatch = null;
        patchMapDrawer.Map = gameProperties.GameWorld.Map;
        patchMapDrawer.SelectedPatch = patchMapDrawer.PlayerPatch;
        patchDetailsPanel.SelectedPatch = patchMapDrawer.PlayerPatch;
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
