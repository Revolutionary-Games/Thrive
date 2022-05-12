public class HexWithData<T> : IPositionedHex, IActionHex
    where T : IActionHex
{
    public HexWithData(T? data)
    {
        Data = data;
    }

    public T? Data { get; set; }
    public Hex Position { get; set; }

    public bool MatchesDefinition(IActionHex other)
    {
        if (other is HexWithData<T> casted)
        {
            if (ReferenceEquals(casted.Data, Data))
                return true;

            if (ReferenceEquals(null, casted.Data) || ReferenceEquals(null, Data))
                return false;

            return Data.MatchesDefinition(casted.Data);
        }

        return false;
    }
}
