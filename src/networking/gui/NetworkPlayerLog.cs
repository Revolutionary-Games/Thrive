using System.Globalization;
using System.Text;
using Godot;

public class NetworkPlayerLog : PanelContainer
{
    [Export]
    public NodePath AvatarPath = null;

    [Export]
    public NodePath NamePath = null!;

    [Export]
    public NodePath ScorePath = null!;

    [Export]
    public NodePath KillsPath = null!;

    [Export]
    public NodePath DeathsPath = null!;

    [Export]
    public NodePath KickButtonPath = null!;

    [Export]
    public NodePath CrossPath = null!;

    [Export]
    public NodePath SpacerPath = null!;

    [Export]
    public NodePath PingBarPath = null!;

    private TextureRect avatarRect = null!;
    private CustomRichTextLabel? nameLabel;
    private Label scoreLabel = null!;
    private Label killsLabel = null!;
    private Label deathsLabel = null!;
    private Button kickButton = null!;
    private Control spacer = null!;
    private PingBar pingBar = null!;

    private Texture? avatar;
    private string playerName = string.Empty;
    private bool highlight;

    [Signal]
    public delegate void KickRequested(int id);

    public int PeerID { get; set; } = -1;

    public Texture? PlayerAvatar
    {
        get => avatar;
        set
        {
            avatar = value;

            if (avatarRect != null)
                UpdateAvatar();
        }
    }

    public string PlayerName
    {
        get => playerName;
        set
        {
            playerName = value;

            if (nameLabel != null)
                UpdateName();
        }
    }

    public bool Highlight
    {
        get => highlight;
        set
        {
            if (highlight == value)
                return;

            highlight = value;
            UpdateReadyState();
        }
    }

    public override void _Ready()
    {
        avatarRect = GetNode<TextureRect>(AvatarPath);
        nameLabel = GetNode<CustomRichTextLabel>(NamePath);
        scoreLabel = GetNode<Label>(ScorePath);
        killsLabel = GetNode<Label>(KillsPath);
        deathsLabel = GetNode<Label>(DeathsPath);
        kickButton = GetNode<Button>(KickButtonPath);
        spacer = GetNode<Control>(SpacerPath);
        pingBar = GetNode<PingBar>(PingBarPath);

        NetworkManager.Instance.Connect(
            nameof(NetworkManager.PlayerStatusChanged), this, nameof(OnPlayerStatusChanged));

        pingBar.PeerId = PeerID;

        UpdateAvatar();
        UpdateName();
        UpdateKickButton();
        UpdateReadyState();
    }

    public override void _Process(float delta)
    {
        var info = NetworkManager.Instance.GetPlayerInfo(PeerID);
        if (info == null)
            return;

        info.TryGetVar("score", out int score);
        info.TryGetVar("kills", out int kills);
        info.TryGetVar("deaths", out int deaths);

        scoreLabel.Text = score.ToString(CultureInfo.CurrentCulture);
        killsLabel.Text = kills.ToString(CultureInfo.CurrentCulture);
        deathsLabel.Text = deaths.ToString(CultureInfo.CurrentCulture);
    }

    private void UpdateAvatar()
    {
        if (avatarRect == null)
            throw new SceneTreeAttachRequired();

        var marginContainer = nameLabel?.GetParent<MarginContainer>();
        marginContainer?.AddConstantOverride("margin_left", avatar != null ? 0 : 10);

        avatarRect.Visible = avatar != null;
        avatarRect.Texture = avatar;
    }

    private void UpdateName()
    {
        if (nameLabel == null)
            throw new SceneTreeAttachRequired();

        var builder = new StringBuilder(50);

        if (PeerID == NetworkManager.DEFAULT_SERVER_ID)
        {
            builder.Append($"[color=#fe82ff]{PlayerName}[/color]");
        }
        else
        {
            builder.Append(PlayerName);
        }

        if (PeerID == NetworkManager.DEFAULT_SERVER_ID)
        {
            builder.Append(' ');
            builder.Append(TranslationServer.Translate("PLAYER_LOG_HOST_ATTRIBUTE"));
        }

        var network = NetworkManager.Instance;

        var player = network.GetPlayerInfo(PeerID);
        if (player != null && player.Status != network.LocalPlayer?.Status)
        {
            builder.Append(' ');
            builder.Append($"[{player.GetStatusReadable()}]");
        }

        nameLabel.ExtendedBbcode = builder.ToString();
    }

    private void UpdateKickButton()
    {
        kickButton.Visible = NetworkManager.Instance.IsServer && PeerID != NetworkManager.Instance.PeerId;
        spacer.Visible = !kickButton.Visible;
    }

    private void UpdateReadyState()
    {
        var stylebox = GetStylebox("panel").Duplicate(true) as StyleBoxFlat;
        stylebox!.BgColor = Highlight ? new Color(0.07f, 0.51f, 0.84f, 0.39f) : new Color(Colors.Black, 0.39f);
        AddStyleboxOverride("panel", stylebox);
    }

    private void OnPlayerStatusChanged(int peerId, NetworkPlayerStatus status)
    {
        _ = peerId;
        _ = status;
        UpdateName();
    }

    private void OnKickPressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        EmitSignal(nameof(KickRequested), PeerID);
    }
}
