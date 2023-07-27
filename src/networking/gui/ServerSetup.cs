using Godot;

public class ServerSetup : CustomWindow
{
    [Export]
    public NodePath NamePath = null!;

    [Export]
    public NodePath MaxPlayerPath = null!;

    [Export]
    public NodePath SessionLengthPath = null!;

    [Export]
    public NodePath GameModePath = null!;

    [Export]
    public NodePath UseUpnpPath = null!;

    [Export]
    public NodePath UseUpnpHintPath = null!;

    [Export]
    public NodePath GameModeSpecificSettingsPath = null!;

    private LineEdit name = null!;
    private SpinBox maxPlayers = null!;
    private SpinBox sessionLength = null!;
    private OptionButton gameMode = null!;
    private CustomCheckBox useUpnp = null!;
    private TextureButton useUpnpHint = null!;
    private Container gameModeSpecificOptions = null!;

    private Vars? settings;
    private string playerName = "unnamed";
    private string address = string.Empty;
    private int port;

    private MultiplayerGameMode? currentGameMode;
    private IGameModeOptionsMenu? gameModeOptions;

    [Signal]
    public delegate void Confirmed(string settings);

    public override void _Ready()
    {
        name = GetNode<LineEdit>(NamePath);
        maxPlayers = GetNode<SpinBox>(MaxPlayerPath);
        sessionLength = GetNode<SpinBox>(SessionLengthPath);
        gameMode = GetNode<OptionButton>(GameModePath);
        useUpnp = GetNode<CustomCheckBox>(UseUpnpPath);
        useUpnpHint = GetNode<TextureButton>(UseUpnpHintPath);
        gameModeSpecificOptions = GetNode<Container>(GameModeSpecificSettingsPath);

        useUpnpHint.RegisterToolTipForControl("upnp", "serverSetup");
    }

    public void Open(string playerName, string address, int port)
    {
        this.playerName = playerName;
        this.address = address;
        this.port = port;

        ResetForm();

        this.PopupCenteredShrink();
    }

    private void Cancel()
    {
        GUICommon.Instance.PlayButtonPressSound();
        Hide();
    }

    private void Confirm()
    {
        GUICommon.Instance.PlayButtonPressSound();

        if (settings == null)
        {
            GD.PrintErr("Server setup confirmed with settings being null");
            return;
        }

        ReadControlsToSettings();

        Hide();

        EmitSignal(nameof(Confirmed), ThriveJsonConverter.Instance.SerializeObject(settings));
    }

    private void ResetForm()
    {
        settings = new Vars();
        settings.SetVar("Name", $"{playerName}'s server");
        settings.SetVar("Address", address);
        settings.SetVar("Port", port);
        settings.SetVar("MaxPlayers", Constants.MULTIPLAYER_DEFAULT_MAX_PLAYERS);
        settings.SetVar("SessionLength", Constants.MULTIPLAYER_DEFAULT_SESSION_LENGTH);
        settings.SetVar("UseUpnp", false);
        settings.SetVar("GameMode", SimulationParameters.Instance.GetMultiplayerGameMode("MicrobeArena").InternalName);

        ApplySettingsToControls();
    }

    private void ApplySettingsToControls()
    {
        var simulationParameters = SimulationParameters.Instance;

        name.Text = settings!.GetVar<string>("Name");
        maxPlayers.Value = settings.GetVar<int>("MaxPlayers");
        sessionLength.Value = settings.GetVar<uint>("SessionLength");
        gameMode.Selected = simulationParameters.GetMultiplayerGameMode(settings.GetVar<string>("GameMode")).Index;
        useUpnp.Pressed = settings.GetVar<bool>("UseUpnp");

        gameMode.Clear();

        foreach (var mode in simulationParameters.GetAllMultiplayerGameMode())
        {
            gameMode.AddItem(mode.Name);
        }

        // Teasers
        gameMode.AddItem(TranslationServer.Translate("MICROBE_STAGE"), 100);
        gameMode.AddItem(TranslationServer.Translate("OPEN_WORLD"), 101);
        gameMode.SetItemDisabled(gameMode.GetItemIndex(100), true);
        gameMode.SetItemDisabled(gameMode.GetItemIndex(101), true);

        maxPlayers.MinValue = 1;
        maxPlayers.MaxValue = Constants.MULTIPLAYER_DEFAULT_MAX_PLAYERS;

        OnGameModeSelected(gameMode.Selected);

        sessionLength.Suffix = TranslationServer.Translate("MINUTES_LOWERCASE");
    }

    private void ReadControlsToSettings()
    {
        settings!.SetVar("Name", name.Text);
        settings.SetVar("MaxPlayers", (int)maxPlayers.Value);
        settings.SetVar("SessionLength", (uint)sessionLength.Value);
        settings.SetVar("UseUpnp", useUpnp.Pressed);
        settings.SetVar("GameMode", currentGameMode?.InternalName ?? string.Empty);
        settings.SetVar("GameModeSettings", gameModeOptions?.ReadSettings() ?? new Vars());
    }

    private void CreateGameModeSpecificOptions(int index)
    {
        _ = index;

        gameModeSpecificOptions.FreeChildren();

        if (currentGameMode == null)
            return;

        if (string.IsNullOrEmpty(currentGameMode.SettingsGUI))
        {
            gameModeSpecificOptions.Hide();
            return;
        }

        gameModeSpecificOptions.Show();

        var scene = GD.Load<PackedScene>(currentGameMode.SettingsGUI);

        if (scene == null)
        {
            GD.PrintErr($"Failed to load options scene for game mode {currentGameMode.InternalName}");
            return;
        }

        var instance = scene.Instance();
        gameModeOptions = (IGameModeOptionsMenu)instance;

        gameModeSpecificOptions.AddChild(instance);
    }

    private void OnGameModeSelected(int index)
    {
        if (index == currentGameMode?.Index)
            return;

        currentGameMode = SimulationParameters.Instance.GetMultiplayerGameModeByIndex(index);
        CreateGameModeSpecificOptions(index);
    }
}
