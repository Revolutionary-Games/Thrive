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
}
