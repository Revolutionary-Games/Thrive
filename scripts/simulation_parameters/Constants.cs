/// <summary>
///   Holds some constants that must be kept constant after first setting
/// </summary>
public class Constants
{
    /// <summary>
    ///   Don't change this, so much stuff will break
    /// </summary>
    public static readonly int CLOUDS_IN_ONE = 4;

    // NOTE: these 4 constants need to match what is setup in CompoundCloudPlane.tscn
    public static readonly int CLOUD_WIDTH = 100;
    public static readonly int CLOUD_X_EXTENT = CLOUD_WIDTH * 2;
    public static readonly int CLOUD_HEIGHT = 100;

    // This is cloud local Y not world Y
    public static readonly int CLOUD_Y_EXTENT = CLOUD_HEIGHT * 2;

    public static readonly float CLOUD_Y_COORDINATE = 0;

    /// <summary>
    ///   All Nodes tagged with this are handled by the spawn system for despawning
    /// </summary>
    public static readonly string SPAWNED_GROUP = "spawned";

    private static readonly Constants INSTANCE = new Constants();

    static Constants()
    {
    }

    private Constants()
    {
    }

    public static Constants Instance
    {
        get
        {
            return INSTANCE;
        }
    }

    /// <summary>
    ///   This can be freely adjusted to adjust the performance The
    ///   higher this value is the smaller the size of the simulated
    ///   cloud is and the performance is better. Don't change this to
    ///   be higher than 1.
    /// </summary>
    public int CLOUD_RESOLUTION
    {
        get
        {
            return 2;
        }
    }

    /// <summary>
    ///   If this is over 0 then this limits how often compound clouds
    ///   are updated. The default value of 0.020 at 60 FPS makes
    ///   every other frame not update the clouds.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This should be made user configurable for different
    ///     computers. The choises should probably be:
    ///     0.0f, 0.020f, 0.040f, 0.1f, 0.25f
    ///   </para>
    /// </remarks>
    public float CLOUD_UPDATE_INTERVAL
    {
        get
        {
            return 0.040f;
        }
    }

    /// <summary>
    /// </summary>
    public int CLOUD_SIMULATION_WIDTH
    {
        get
        {
            return (int)(CLOUD_X_EXTENT / CLOUD_RESOLUTION);
        }
    }

    public int CLOUD_SIMULATION_HEIGHT
    {
        get
        {
            return (int)(CLOUD_Y_EXTENT / CLOUD_RESOLUTION);
        }
    }

    public int MEMBRANE_RESOLUTION
    {
        get
        {
            return 10;
        }
    }

    public float MEMBRANE_BORDER
    {
        get
        {
            return 1.0f;
        }
    }

    /// <summary>
    ///   BASE MOVEMENT ATP cost. Cancels out a little bit more then one cytoplasm's glycolysis
    /// </summary>
    /// <remarks>
    ///   this is applied *per* hex
    /// </remarks>
    public float BASE_MOVEMENT_ATP_COST
    {
        get
        {
            return 1.0f;
        }
    }

    public float FLAGELLA_ENERGY_COST
    {
        get
        {
            return 7.1f;
        }
    }

    public float FLAGELLA_BASE_FORCE
    {
        get
        {
            return 40.7f;
        }
    }

    public float CELL_BASE_THRUST
    {
        get
        {
            return 50.6f;
        }
    }
}
