public class HexWithData<T> : IPositionedHex
{
    public T? Data { get; set; }
    public Hex Position { get; set; }
}
