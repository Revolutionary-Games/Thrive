using System.Collections.Generic;

/// <summary>
///   Conditions of a microbe biome environment
/// </summary>
public interface IBiomeConditions
{
    public Dictionary<string, ChunkConfiguration> Chunks { get; }

    public BiomeCompoundProperties GetCompound(Compound compound, CompoundAmountType amountType);
    public bool TryGetCompound(Compound compound, CompoundAmountType amountType, out BiomeCompoundProperties result);

    /// <summary>
    ///   Get compounds that vary during the day
    /// </summary>
    /// <returns>The compounds that vary</returns>
    public IEnumerable<Compound> GetAmbientCompoundsThatVary();

    /// <summary>
    ///   Checks if the method <see cref="GetAmbientCompoundsThatVary"/> would return true
    /// </summary>
    /// <returns>True if there are compounds that vary</returns>
    public bool HasCompoundsThatVary();

    /// <summary>
    ///   Returns true if the specified compound varies during the day / night cycle
    /// </summary>
    /// <param name="compound">Compound type to check</param>
    /// <returns>True if compound varies</returns>
    public bool IsVaryingCompound(Compound compound);
}
