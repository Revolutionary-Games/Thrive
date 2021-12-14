using Godot;

/// <summary>
///   Provides extra level of abstraction to allow simultaneous switching between 3D positional and non positional
///   audio players in a single Node-derived class. Though setting this can only be done in instantiation.
/// </summary>
/// <remarks>
///   <para>
///     Useful in cases where the playing of an audio stream must be able to fulfill both of these conditions.
///   </para>
/// </remarks>
public class HybridAudioPlayer : Node
{
    private AudioStreamPlayer3D player3D;
    private AudioStreamPlayer playerNonPositional;

    private AudioStream stream;
    private float volume;
    private string bus;

    public HybridAudioPlayer(bool positional)
    {
        Positional = positional;
        Volume = 1.0f;
    }

    public bool Positional { get; }

    public AudioStream Stream
    {
        get => stream;
        set
        {
            stream = value;
            ApplyStream();
        }
    }

    public bool Playing => Positional ? player3D.Playing : playerNonPositional.Playing;

    /// <summary>
    ///   Volume in linear scale.
    /// </summary>
    public float Volume
    {
        get => volume;
        set
        {
            volume = Mathf.Clamp(value, 0, 1);
            ApplyVolume();
        }
    }

    public string Bus
    {
        get => bus;
        set
        {
            bus = value;
            ApplyBus();
        }
    }

    public override void _Ready()
    {
        if (Positional)
        {
            player3D = new AudioStreamPlayer3D();
            AddChild(player3D);
        }
        else
        {
            playerNonPositional = new AudioStreamPlayer();
            AddChild(playerNonPositional);
        }

        ApplyAudioPlayerSettings();
    }

    public void Play(float fromPosition = 0)
    {
        if (Positional)
        {
            player3D.Play(fromPosition);
        }
        else
        {
            playerNonPositional.Play(fromPosition);
        }
    }

    public void Stop()
    {
        if (Positional)
        {
            player3D.Stop();
        }
        else
        {
            playerNonPositional.Stop();
        }
    }

    private void ApplyAudioPlayerSettings()
    {
        ApplyStream();
        ApplyVolume();
        ApplyBus();
    }

    private void ApplyStream()
    {
        if (Positional && player3D != null)
        {
            player3D.Stream = stream;
        }
        else if (!Positional && playerNonPositional != null)
        {
            playerNonPositional.Stream = stream;
        }
    }

    private void ApplyVolume()
    {
        if (Positional && player3D != null)
        {
            player3D.UnitDb = GD.Linear2Db(volume);
        }
        else if (!Positional && playerNonPositional != null)
        {
            playerNonPositional.VolumeDb = GD.Linear2Db(volume);
        }
    }

    private void ApplyBus()
    {
        if (Positional && player3D != null)
        {
            player3D.Bus = bus;
        }
        else if (!Positional && playerNonPositional != null)
        {
            playerNonPositional.Bus = bus;
        }
    }
}
