/// <summary>
///   Holds some constants that must be kept constant after first setting
/// </summary>
public class Constants
{
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

    public int CLOUD_SIMULATION_WIDTH
    {
        get
        {
            return 50;
        }
    }

    public int MEMBRANE_RESOLUTION
    {
        get
        {
            return 10;
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
