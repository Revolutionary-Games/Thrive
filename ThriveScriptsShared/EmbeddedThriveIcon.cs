namespace ThriveScriptsShared;

/// <summary>
///   Defines a set of icons that can be embedded in text
/// </summary>
public enum EmbeddedThriveIcon
{
    ConditionInsufficient,
    ConditionFulfilled,
    StorageIcon,
    OsmoIcon,
    MovementIcon,
    MP,
    Pressure,
}

public static class EmbeddedThriveIconExtensions
{
    public static bool TryGetIcon(string iconName, out EmbeddedThriveIcon icon)
    {
        return Enum.TryParse(iconName, out icon);
    }
}
