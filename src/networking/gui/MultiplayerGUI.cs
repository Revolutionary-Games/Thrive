using System;
using System.Globalization;
using System.Text;
using Godot;
using YamlDotNet.Serialization;

public class MultiplayerGUI : CenterContainer
{
    [Export]
    public NodePath NameBoxPath = null!;

    [Export]
    public NodePath AddressBoxPath = null!;

    [Export]
    public NodePath PortBoxPath = null!;

    [Export]
    public NodePath ConnectButtonPath = null!;

    [Export]
    public NodePath LobbyPlayerListPath = null!;

    [Export]
    public NodePath ServerNamePath = null!;

    [Export]
    public NodePath ServerAttributesPath = null!;

    [Export]
    public NodePath StartButtonPath = null!;

    [Export]
    public NodePath KickedDialogPath = null!;

    [Export]
    public NodePath GameModeTitlePath = null!;

    [Export]
    public NodePath GameModeDescriptionPath = null!;

    [Export]
    public NodePath PlayerListTabPath = null!;

    [Export]
    public NodePath GameInfoTabPath = null!;

    [Export]
    public NodePath PlayerListTabButtonPath = null!;

    [Export]
    public NodePath GameInfoTabButtonPath = null!;

    [Export]
    public NodePath LatencyLabelPath = null!;

    [Export]
    public PackedScene NetworkedPlayerLabelScene = null!;

    private readonly string[] ellipsisAnimationSequence = { " ", " .", " ..", " ..." };

    private LineEdit nameBox = null!;
    private LineEdit addressBox = null!;
    private LineEdit portBox = null!;
    private CustomConfirmationDialog generalDialog = null!;
    private CustomConfirmationDialog loadingDialog = null!;
    private Button connectButton = null!;
    private NetworkPlayerList list = null!;
    private ServerSetup serverSetup = null!;
    private Button startButton = null!;
    private CustomConfirmationDialog kickedDialog = null!;
    private Label serverName = null!;
    private Label serverAttributes = null!;
    private Label gameModeTitle = null!;
    private CustomRichTextLabel gameModeDescription = null!;
    private Label latency = null!;

    private Control primaryMenu = null!;
    private Control lobbyMenu = null!;
    private Control playerListTab = null!;
    private Control gameInfoTab = null!;
    private Button playerListTabButton = null!;
    private Button gameInfoTabButton = null!;

    private SubMenu currentSubMenu = SubMenu.Main;
    private LobbyTab selectedLobbyTab = LobbyTab.PlayerList;

    private string loadingDialogTitle = string.Empty;
    private string loadingDialogText = string.Empty;

    private float ellipsisAnimationTimer = 1.0f;
    private int ellipsisAnimationStep;

    private ConnectionJob currentJobStatus = ConnectionJob.None;
    private NetworkManager.RegistrationResult registrationResult;

    [Signal]
    public delegate void OnClosed();

    public enum SubMenu
    {
        Main,
        Lobby,
    }

    public enum LobbyTab
    {
        PlayerList,
        GameInfo,
    }

    private enum ConnectionJob
    {
        None,
        Hosting,
        Connecting,
        SettingUpUpnp,
        PortForwarding,
    }

    public override void _Ready()
    {
        nameBox = GetNode<LineEdit>(NameBoxPath);
        addressBox = GetNode<LineEdit>(AddressBoxPath);
        portBox = GetNode<LineEdit>(PortBoxPath);
        connectButton = GetNode<Button>(ConnectButtonPath);
        list = GetNode<NetworkPlayerList>(LobbyPlayerListPath);
        startButton = GetNode<Button>(StartButtonPath);
        kickedDialog = GetNode<CustomConfirmationDialog>(KickedDialogPath);
        serverName = GetNode<Label>(ServerNamePath);
        serverAttributes = GetNode<Label>(ServerAttributesPath);
        gameModeTitle = GetNode<Label>(GameModeTitlePath);
        gameModeDescription = GetNode<CustomRichTextLabel>(GameModeDescriptionPath);
        playerListTab = GetNode<Control>(PlayerListTabPath);
        gameInfoTab = GetNode<Control>(GameInfoTabPath);
        playerListTabButton = GetNode<Button>(PlayerListTabButtonPath);
        gameInfoTabButton = GetNode<Button>(GameInfoTabButtonPath);
        latency = GetNode<Label>(LatencyLabelPath);

        generalDialog = GetNode<CustomConfirmationDialog>("GeneralDialog");
        loadingDialog = GetNode<CustomConfirmationDialog>("LoadingDialog");
        primaryMenu = GetNode<Control>("PrimaryMenu");
        lobbyMenu = GetNode<Control>("Lobby");
        serverSetup = GetNode<ServerSetup>("ServerSetup");

        GetTree().Connect("server_disconnected", this, nameof(OnServerDisconnected));

        NetworkManager.Instance.Connect(
            nameof(NetworkManager.RegistrationResultReceived), this, nameof(OnServerRegistrationReceived));
        NetworkManager.Instance.Connect(nameof(NetworkManager.ConnectionFailed), this, nameof(OnConnectionFailed));
        NetworkManager.Instance.Connect(nameof(NetworkManager.ServerStateUpdated), this, nameof(UpdateLobby));
        NetworkManager.Instance.Connect(nameof(NetworkManager.Kicked), this, nameof(OnKicked));
        NetworkManager.Instance.Connect(
            nameof(NetworkManager.LobbyReadyStateReceived), this, nameof(UpdateReadyStatus));
        NetworkManager.Instance.Connect(
            nameof(NetworkManager.UpnpCallResultReceived), this, nameof(OnUPNPCallResultReceived));
        NetworkManager.Instance.Connect(nameof(NetworkManager.LatencyUpdated), this, nameof(OnLatencyUpdated));

        ApplySubMenu();
        ApplyLobbyTab();
        ResetFields();
        ValidateFields();
        UpdateLatencyIndicator();
    }

    public override void _Process(float delta)
    {
        if (loadingDialog.Visible)
            UpdateLoadingDialog(delta);

        var peer = NetworkManager.Instance;

        // Display game time
        var builder = new StringBuilder(100);
        builder.Append(" - ");
        builder.Append(peer.GameInSession ?
            TranslationServer.Translate("LOBBY_ATTRIBUTE_IN_PROGRESS").FormatSafe(peer.GameTimeHumanized,
                peer.ServerSettings.GetVar<uint>("SessionLength")) :
            TranslationServer.Translate("LOBBY_ATTRIBUTE_PENDING"));

        serverAttributes.Text = builder.ToString();
    }

    public void ShowKickedDialog(string reason)
    {
        kickedDialog.DialogText = TranslationServer.Translate("MULTIPLAYER_PLAYER_IS_KICKED").FormatSafe(
            string.IsNullOrEmpty(reason) ? TranslationServer.Translate("UNSPECIFIED_LOWERCASE") : reason);
        kickedDialog.PopupCenteredShrink();
    }

    public void ShowDisconnectedDialog()
    {
        switch (registrationResult)
        {
            case NetworkManager.RegistrationResult.ServerFull:
                ShowGeneralDialog(
                    TranslationServer.Translate("SERVER_DISCONNECTED"), TranslationServer.Translate("SERVER_IS_FULL"));
                return;
            case NetworkManager.RegistrationResult.DuplicateName:
                ShowGeneralDialog(TranslationServer.Translate("SERVER_DISCONNECTED"),
                    TranslationServer.Translate("NAME_IS_ALREADY_TAKEN"));
                return;
            default:
                ShowGeneralDialog(TranslationServer.Translate("SERVER_DISCONNECTED"),
                    TranslationServer.Translate("CLOSED_BY_REMOTE_HOST"));
                break;
        }
    }

    public void SetSubMenu(SubMenu menu)
    {
        currentSubMenu = menu;
        ApplySubMenu();
    }

    public void SetLobbyTab(LobbyTab tab)
    {
        selectedLobbyTab = tab;
        ApplyLobbyTab();
    }

    private void UpdateLobby()
    {
        var peer = NetworkManager.Instance;
        if (!peer.IsMultiplayer)
            return;

        list.RefreshPlayers();
        list.SortHighestScoreFirst();

        peer.ServerSettings.TryGetVar("Name", out string name);
        peer.ServerSettings.TryGetVar("GameMode", out string gameModeName);

        serverName.Text = name;

        var gameMode = SimulationParameters.Instance.GetMultiplayerGameMode(gameModeName);
        gameModeTitle.Text = gameMode.Name;
        gameModeDescription.ExtendedBbcode = gameMode.Description;

        foreach (var player in peer.ConnectedPlayers)
            UpdateReadyStatus(player.Key, player.Value.LobbyReady);

        UpdateStartButton();
    }

    private void ResetFields()
    {
        nameBox.PlaceholderText = Settings.EnvironmentUserName;
        portBox.Text = Constants.MULTIPLAYER_DEFAULT_PORT.ToString(CultureInfo.CurrentCulture);
        addressBox.Text = Constants.MULTIPLAYER_DEFAULT_HOST_ADDRESS;
    }

    private void ValidateFields()
    {
        connectButton.Disabled = string.IsNullOrEmpty(addressBox.Text);
    }

    private void UpdateStartButton()
    {
        var network = NetworkManager.Instance;

        if (NetworkManager.Instance.IsServer)
        {
            startButton.Text = TranslationServer.Translate("START");

            // Disable start game button if one or more player is not ready
            // NOTE: For now, we let the host start the game regardless other players' ready status to avoid potential
            //       annoying long wait time due to uncooperative players. We keep this in case this behavior is
            //       otherwise preferred.
            // startButton.Disabled = network.ConnectedPlayers.Any(
            //    p => p.Key != NetworkManager.DEFAULT_SERVER_ID && !p.Value.ReadyForSession);

            startButton.ToggleMode = false;
        }
        else if (NetworkManager.Instance.IsClient)
        {
            startButton.Text = network.GameInSession ?
                TranslationServer.Translate("JOIN") :
                TranslationServer.Translate("READY");
            startButton.Disabled = false;
            startButton.ToggleMode = !network.GameInSession;

            if (network.LocalPlayer != null)
                startButton.SetPressedNoSignal(network.LocalPlayer.LobbyReady);
        }
    }

    private void UpdateReadyStatus(int peerId, bool ready)
    {
        var log = list.GetPlayer(peerId);

        if (log != null)
        {
            log.Highlight = ready;
            UpdateStartButton();
        }
    }

    private void ShowGeneralDialog(string title, string text)
    {
        generalDialog.WindowTitle = title;
        generalDialog.DialogText = text;
        generalDialog.PopupCenteredShrink();
    }

    private void ShowLoadingDialog(string title, string text, bool allowClosing = true)
    {
        loadingDialogTitle = title;
        loadingDialogText = text;
        loadingDialog.ShowCloseButton = allowClosing;
        loadingDialog.PopupCenteredShrink();
    }

    private void UpdateLoadingDialog(float delta)
    {
        // One whitespace and three trailing dots loading animation (  . .. ...)
        ellipsisAnimationTimer += delta;
        if (ellipsisAnimationTimer >= 1.0f)
        {
            ellipsisAnimationStep = (ellipsisAnimationStep + 1) % ellipsisAnimationSequence.Length;
            ellipsisAnimationTimer = 0;
        }

        loadingDialog.WindowTitle = loadingDialogTitle;
        loadingDialog.DialogText = loadingDialogText + ellipsisAnimationSequence[ellipsisAnimationStep];

        if (NetworkManager.Instance.Status == NetworkedMultiplayerPeer.ConnectionStatus.Connecting)
        {
            loadingDialog.WindowTitle += " (" + Mathf.RoundToInt(NetworkManager.Instance.TimePassedConnecting) + "s)";
        }
    }

    private void UpdateLatencyIndicator(int? miliseconds = null)
    {
        var player = NetworkManager.Instance.GetPlayerInfo(NetworkManager.Instance.PeerId);
        miliseconds ??= player?.Latency;
        latency.Text = TranslationServer.Translate("PING_VALUE_MILISECONDS").FormatSafe(miliseconds);
    }

    private void ApplySubMenu()
    {
        primaryMenu.Hide();
        lobbyMenu.Hide();

        switch (currentSubMenu)
        {
            case SubMenu.Main:
                primaryMenu.Show();
                break;
            case SubMenu.Lobby:
                lobbyMenu.Show();
                UpdateLobby();
                break;
            default:
                throw new Exception("Invalid submenu");
        }
    }

    private void OnLobbyTabPressed(string tab)
    {
        var selection = (LobbyTab)Enum.Parse(typeof(LobbyTab), tab);

        if (selection == selectedLobbyTab)
            return;

        GUICommon.Instance.PlayButtonPressSound();

        SetLobbyTab(selection);
    }

    private void ApplyLobbyTab()
    {
        playerListTab.Hide();
        gameInfoTab.Hide();

        switch (selectedLobbyTab)
        {
            case LobbyTab.PlayerList:
                playerListTab.Show();
                playerListTabButton.Pressed = true;
                break;
            case LobbyTab.GameInfo:
                gameInfoTab.Show();
                gameInfoTabButton.Pressed = true;
                break;
            default:
                throw new Exception("Invalid lobby tab");
        }
    }

    private void ReadNameAndPort(out string name, out int port)
    {
        name = string.IsNullOrEmpty(nameBox.Text) ? Settings.Instance.ActiveUsername : nameBox.Text;
        port = string.IsNullOrEmpty(portBox.Text) || !int.TryParse(portBox.Text, out int parsedPort) ?
            Constants.MULTIPLAYER_DEFAULT_PORT :
            parsedPort;
    }

    private void OnConnectPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();

        ShowLoadingDialog(
            TranslationServer.Translate("CONNECTING"),
            TranslationServer.Translate("ESTABLISHING_CONNECTION_TO").FormatSafe(addressBox.Text, portBox.Text));

        ReadNameAndPort(out string name, out int port);

        var error = NetworkManager.Instance.ConnectToServer(addressBox.Text, port, name);
        if (error != Error.Ok)
        {
            loadingDialog.Hide();
            ShowGeneralDialog(
                TranslationServer.Translate("CONNECTION_FAILED"),
                TranslationServer.Translate("FAILED_TO_ESTABLISH_CONNECTION").FormatSafe(error));
            return;
        }

        currentJobStatus = ConnectionJob.Connecting;
    }

    private void OnCreatePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        ReadNameAndPort(out string name, out int port);
        serverSetup.Open(name, addressBox.Text, port);
    }

    private void OnServerSetupConfirmed(string serialized)
    {
        currentJobStatus = ConnectionJob.Hosting;

        Vars settings;

        try
        {
            settings = ThriveJsonConverter.Instance.DeserializeObject<Vars>(serialized) ??
                throw new Exception("deserialized value is null");
        }
        catch (Exception e)
        {
            ShowGeneralDialog(TranslationServer.Translate("HOSTING_FAILED"), TranslationServer.Translate(
                "FAILED_TO_CREATE_SERVER").FormatSafe(e));
            GD.PrintErr("Can't setup server due to parse failure on data: ", e);
            return;
        }

        // Preset settings that players probably won't care (thus not present in the server setup window, yet)
        settings.SetVar("TickRate", Constants.NETWORK_DEFAULT_TICK_RATE);
        settings.SetVar("SendRate", Constants.NETWORK_DEFAULT_SEND_RATE);

        var playerName = string.IsNullOrEmpty(nameBox.Text) ? Settings.Instance.ActiveUsername : nameBox.Text;

        var error = NetworkManager.Instance.CreatePlayerHostedServer(playerName, settings);
        if (error != Error.Ok)
        {
            loadingDialog.Hide();
            ShowGeneralDialog(TranslationServer.Translate("HOSTING_FAILED"), TranslationServer.Translate(
                "FAILED_TO_CREATE_SERVER").FormatSafe(error));
            return;
        }

        if (settings.GetVar<bool>("UseUpnp"))
        {
            ShowLoadingDialog(
                TranslationServer.Translate("UPNP_SETUP"),
                TranslationServer.Translate("UPNP_DISCOVERING_DEVICES"), false);

            currentJobStatus = ConnectionJob.SettingUpUpnp;
        }
        else
        {
            SetSubMenu(SubMenu.Lobby);
        }
    }

    private void OnBackPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(OnClosed));
    }

    private void OnDisconnectPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        NetworkManager.Instance.Disconnect();
        SetSubMenu(SubMenu.Main);
    }

    private void OnLoadingCancelled()
    {
        switch (currentJobStatus)
        {
            case ConnectionJob.Connecting:
                NetworkManager.Instance.Print("Cancelling connection");
                NetworkManager.Instance.Disconnect();
                break;

            // TODO: handle upnp cancellations, currently you can't cancel these
        }

        currentJobStatus = ConnectionJob.None;
    }

    private void OnServerRegistrationReceived(int peerId, NetworkManager.RegistrationResult result)
    {
        if (peerId != NetworkManager.Instance.PeerId || currentJobStatus == ConnectionJob.Hosting)
            return;

        loadingDialog.Hide();

        registrationResult = result;
        currentJobStatus = ConnectionJob.None;

        if (result != NetworkManager.RegistrationResult.Success)
            return;

        SetSubMenu(SubMenu.Lobby);

        NetworkManager.Instance.Print(
            "Connection to ", addressBox.Text, ":", portBox.Text, " established," +
            " using network ID (", NetworkManager.Instance.PeerId, ")");
    }

    private void OnConnectionFailed(string reason)
    {
        if (!loadingDialog.Visible)
            return;

        loadingDialog.Hide();

        ShowGeneralDialog(
            TranslationServer.Translate("CONNECTION_FAILED"),
            TranslationServer.Translate("FAILED_TO_ESTABLISH_CONNECTION").FormatSafe(reason));

        currentJobStatus = ConnectionJob.None;

        NetworkManager.Instance.PrintError(
            "Connection to ", addressBox.Text, ":", portBox.Text, " failed: ", reason);
    }

    private void OnServerDisconnected()
    {
        loadingDialog.Hide();
        ShowDisconnectedDialog();
        SetSubMenu(SubMenu.Main);
    }

    private void OnKicked(string reason)
    {
        ShowKickedDialog(reason);
        SetSubMenu(SubMenu.Main);
    }

    private void OnStartPressed()
    {
        // Client shouldn't join before the host/before the server even starts the game session
        if (NetworkManager.Instance.IsClient && !NetworkManager.Instance.GameInSession)
            return;

        NetworkManager.Instance.Join();
        startButton.Disabled = true;
    }

    private void OnReadyToggled(bool active)
    {
        NetworkManager.Instance.SetLobbyReadyState(active);
    }

    private void OnUPNPCallResultReceived(UPNP.UPNPResult result, NetworkManager.UpnpJobStep step)
    {
        switch (step)
        {
            case NetworkManager.UpnpJobStep.Discovery:
            {
                if (result != UPNP.UPNPResult.Success)
                {
                    loadingDialog.Hide();

                    ShowGeneralDialog(TranslationServer.Translate("UPNP_SETUP"), TranslationServer.Translate(
                        "UPNP_ERROR_WHILE_SETTING_UP").FormatSafe(result.ToString()));

                    currentJobStatus = ConnectionJob.None;

                    NetworkManager.Instance.Disconnect();
                }
                else
                {
                    ShowLoadingDialog(TranslationServer.Translate("PORT_FORWARDING"), TranslationServer.Translate(
                        "UPNP_ATTEMPTING_TO_FORWARD_PORT").FormatSafe(portBox.Text), false);

                    currentJobStatus = ConnectionJob.PortForwarding;
                }

                break;
            }

            case NetworkManager.UpnpJobStep.PortMapping:
            {
                loadingDialog.Hide();

                currentJobStatus = ConnectionJob.None;

                if (result != UPNP.UPNPResult.Success)
                {
                    ShowGeneralDialog(TranslationServer.Translate("PORT_FORWARDING"), TranslationServer.Translate(
                        "UPNP_ATTEMPTING_TO_FORWARD_PORT_FAILED").FormatSafe(result.ToString()));

                    NetworkManager.Instance.Disconnect();
                }
                else
                {
                    SetSubMenu(SubMenu.Lobby);
                }

                break;
            }
        }
    }

    private void OnLatencyUpdated(int peerId, int milliseconds)
    {
        if (peerId != NetworkManager.Instance.PeerId)
            return;

        UpdateLatencyIndicator(milliseconds);
    }
}
