public interface IPositionedHex : IReadOnlyPositionedHex
{
    public new Hex Position { get; set; }
}

public interface IReadOnlyPositionedHex
{
    public Hex Position { get; }
}
