using System;
using System.Reflection;

/// <summary>
///   Holds some constants that must be kept constant after first setting
/// </summary>
public class Constants
{
    /// <summary>
    ///   The (default) size of the hexagons, used in
    ///   calculations. Don't change this.
    /// </summary>
    public const float DEFAULT_HEX_SIZE = 2.0f;

    /// <summary>
    ///   Don't change this, so much stuff will break
    /// </summary>
    public const int CLOUDS_IN_ONE = 4;

    // NOTE: these 4 constants need to match what is setup in CompoundCloudPlane.tscn
    public const int CLOUD_WIDTH = 100;
    public const int CLOUD_X_EXTENT = CLOUD_WIDTH * 2;
    public const int CLOUD_HEIGHT = 100;

    // This is cloud local Y not world Y
    public const int CLOUD_Y_EXTENT = CLOUD_HEIGHT * 2;

    public const float CLOUD_Y_COORDINATE = 0;

    public const int MEMBRANE_RESOLUTION = 10;

    public const float MEMBRANE_BORDER = DEFAULT_HEX_SIZE;

    /// <summary>
    ///   BASE MOVEMENT ATP cost. Cancels out a little bit more then one cytoplasm's glycolysis
    /// </summary>
    /// <remarks>
    ///   this is applied *per* hex
    /// </remarks>
    public const float BASE_MOVEMENT_ATP_COST = 1.0f;

    public const float FLAGELLA_ENERGY_COST = 7.1f;

    public const float FLAGELLA_BASE_FORCE = 40.7f;

    public const float CELL_BASE_THRUST = 50.6f;

    /// <summary>
    ///   All Nodes tagged with this are handled by the spawn system for despawning
    /// </summary>
    public const string SPAWNED_GROUP = "spawned";

    /// <summary>
    ///   All Nodes tagged with this are handled by the timed life system for despawning
    /// </summary>
    public const string TIMED_GROUP = "timed";

    public const string CONFIGURATION_FILE = "user://thrive_settings.json";

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

    public static string Version
    {
        get
        {
            var assembly = Assembly.GetExecutingAssembly();
            Version version = assembly.GetName().Version;
            var versionSuffix = (AssemblyInformationalVersionAttribute[])assembly.
                GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
            return version.ToString() + versionSuffix[0].InformationalVersion;
        }
    }
}
