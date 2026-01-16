public interface IPositionedHex : IReadOnlyPositionedHex
{
    public new Hex Position { get; set; }

    public new int Orientation { get; set; }
}

public interface IReadOnlyPositionedHex
{
    public Hex Position { get; }

    /// <summary>
    ///   Orientation / rotation of this hex, specified in number of rotations with 6 being full circle.
    /// </summary>
    public int Orientation { get; }
}
