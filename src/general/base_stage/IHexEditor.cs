/// <summary>
///   Access to overall editor state for hex based editor components
/// </summary>
public interface IHexEditor : IEditor
{
    public bool HexPlacedThisSession<THex, TContext>(THex hex)
        where THex : class, IActionHex;
}
