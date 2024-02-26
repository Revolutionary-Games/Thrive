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
public partial class HybridAudioPlayer : Node3D
{
#pragma warning disable CA2213
    private AudioStreamPlayer3D? player3D;
    private AudioStreamPlayer? playerNonPositional;
#pragma warning restore CA2213

    private bool positional;
    private AudioStream? stream;
    private float volume = 1.0f;
    private string bus = "SFX";

    [Export]
    public bool Positional
    {
        get => positional;
        set
        {
            if (value == positional)
                return;

            positional = value;
            ApplyPositionalType();
        }
    }

    [Export]
    public AudioStream? Stream
    {
        get => stream;
        set
        {
            stream = value;
            ApplyStream();
        }
    }

    public bool Playing => Positional ? player3D!.Playing : playerNonPositional!.Playing;

    /// <summary>
    ///   Volume in linear scale.
    /// </summary>
    [Export(PropertyHint.Range, "0,1")]
    public float Volume
    {
        get => volume;
        set
        {
            volume = Mathf.Clamp(value, 0, 1);
            ApplyVolume();
        }
    }

    [Export]
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
        ApplyPositionalType();
        ApplyStream();
        ApplyVolume();
        ApplyBus();
    }

    public void Play(float fromPosition = 0)
    {
        if (Positional)
        {
            player3D!.Play(fromPosition);
        }
        else
        {
            playerNonPositional!.Play(fromPosition);
        }
    }

    public void Stop()
    {
        if (Positional)
        {
            player3D!.Stop();
        }
        else
        {
            playerNonPositional!.Stop();
        }
    }

    private void ApplyPositionalType()
    {
        if (Positional)
        {
            if (playerNonPositional != null)
            {
                playerNonPositional.DetachAndQueueFree();
                playerNonPositional = null;
            }

            player3D = new AudioStreamPlayer3D();
            AddChild(player3D);
        }
        else
        {
            if (player3D != null)
            {
                player3D.DetachAndQueueFree();
                player3D = null;
            }

            playerNonPositional = new AudioStreamPlayer();
            AddChild(playerNonPositional);
        }
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
            player3D.VolumeDb = Mathf.LinearToDb(volume);
        }
        else if (!Positional && playerNonPositional != null)
        {
            playerNonPositional.VolumeDb = Mathf.LinearToDb(volume);
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
