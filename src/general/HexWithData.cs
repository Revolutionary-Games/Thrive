public class HexWithData<T> : IPositionedHex
{
    public HexWithData(T? data)
    {
        Data = data;
    }

    public T? Data { get; set; }
    public Hex Position { get; set; }
}
