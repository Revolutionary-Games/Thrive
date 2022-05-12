/// <summary>
///   Access to overall editor state for hex based editor components
/// </summary>
public interface IHexEditor : IEditor
{
    public bool HexPlacedThisSession<THex>(THex hex)
        where THex : class, IActionHex;
}
