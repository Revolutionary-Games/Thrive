public static class GameteHelpers
{
    /// <summary>
    ///   Basic gamete compatibility check that doesn't check for species compatibility
    /// </summary>
    /// <returns>True on being compatible</returns>
    public static bool IsCompatible(GameteType a, GameteType b)
    {
        if (a == GameteType.All || b == GameteType.All)
            return true;
        if (a == GameteType.A && b == GameteType.B)
            return true;
        if (a == GameteType.B && b == GameteType.A)
            return true;

        return false;
    }
}
