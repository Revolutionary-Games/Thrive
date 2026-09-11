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

    /// <summary>
    ///   A more strict <see cref="IsCompatible"/> check that takes into account the species' changing of reproduction
    ///   modes.
    /// </summary>
    /// <returns>True if compatible</returns>
    public static bool IsCompatibleAfterSpeciesUpdate(GameteType a, GameteType b, MulticellularSpecies species)
    {
        // If the species is updated from isogamy to anisogamy, then that makes "All" act like gamete type A when
        // firing, so that can cause incompatibility.
        if (species.ReproductionMethod is MulticellularReproductionMethod.SexualAnisogamy)
        {
            if (a == GameteType.All)
                a = GameteType.A;

            if (b == GameteType.All)
                b = GameteType.A;
        }

        return IsCompatible(a, b);
    }
}
