using Godot;

/// <summary>
///   Provides extra level of abstraction to allow simultaneous switching between 3D positional and non positional
///   audio players in a single Node-derived class.
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

    private float volume;

    public HybridAudioPlayer()
    {
        player3D = new AudioStreamPlayer3D();
        playerNonPositional = new AudioStreamPlayer();

        AddChild(player3D);
        AddChild(playerNonPositional);

        Volume = 1.0f;
    }

    public bool Positional { get; set; }

    public AudioStream Stream
    {
        get => Positional ? player3D.Stream : playerNonPositional.Stream;
        set
        {
            if (Positional)
            {
                player3D.Stream = value;
            }
            else
            {
                playerNonPositional.Stream = value;
            }
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

            if (Positional)
            {
                player3D.UnitDb = GD.Linear2Db(volume);
            }
            else
            {
                playerNonPositional.VolumeDb = GD.Linear2Db(volume);
            }
        }
    }

    public string Bus
    {
        get => Positional ? player3D.Bus : playerNonPositional.Bus;
        set
        {
            if (Positional)
            {
                player3D.Bus = value;
            }
            else
            {
                playerNonPositional.Bus = value;
            }
        }
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
}
