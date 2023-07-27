using Godot;

/// <summary>
///   A UI element displaying a peer's network delay (lag) in 4 levels from lowest to highest.
/// </summary>
public class PingBar : TextureRect
{
    private Texture level1 = null!;
    private Texture level2 = null!;
    private Texture level3 = null!;
    private Texture level4 = null!;

    private int peerId;

    public int PeerId
    {
        get => peerId;
        set
        {
            peerId = value;
            UpdateLevel();
        }
    }

    public override void _Ready()
    {
        level1 = GD.Load<Texture>("res://assets/textures/gui/bevel/pingBar1.png");
        level2 = GD.Load<Texture>("res://assets/textures/gui/bevel/pingBar2.png");
        level3 = GD.Load<Texture>("res://assets/textures/gui/bevel/pingBar3.png");
        level4 = GD.Load<Texture>("res://assets/textures/gui/bevel/pingBar4.png");

        NetworkManager.Instance.Connect(nameof(NetworkManager.LatencyUpdated), this, nameof(OnLatencyUpdated));

        UpdateLevel();
    }

    private void UpdateLevel(int? miliseconds = null)
    {
        var player = NetworkManager.Instance.GetPlayerInfo(PeerId);
        miliseconds ??= player?.Latency;

        if (miliseconds >= 0 && miliseconds <= 100)
        {
            Texture = level4;
        }
        else if (miliseconds > 100 && miliseconds <= 150)
        {
            Texture = level3;
        }
        else if (miliseconds > 150 && miliseconds <= 300)
        {
            Texture = level2;
        }
        else if (miliseconds > 300)
        {
            Texture = level1;
        }

        HintTooltip = TranslationServer.Translate("PING_VALUE_MILISECONDS").FormatSafe(miliseconds);
    }

    private void OnLatencyUpdated(int peerId, int miliseconds)
    {
        if (peerId != PeerId)
            return;

        UpdateLevel(miliseconds);
    }
}
