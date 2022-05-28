/// <summary>
///   Interface needed for the generic base hex actions to work with specific types
/// </summary>
public interface IActionHex
{
    /// <summary>
    ///   Checks if the definition of this hex matches another. Should not check position or other such fields for
    ///   equality
    /// </summary>
    /// <param name="other">The object to compare against</param>
    /// <returns>True if the same</returns>
    public bool MatchesDefinition(IActionHex other);
}
