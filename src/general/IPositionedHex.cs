public interface IPositionedHex : IReadOnlyPositionedHex
{
    public new Hex Position { get; set; }

    // Note that as we use 6-step rotations, any setters for this should normalize the value
    public new int Orientation { get; set; }
}

public interface IReadOnlyPositionedHex
{
    public Hex Position { get; }

    /// <summary>
    ///   Orientation / rotation of this hex, specified in number of rotations with 6 being full circle. Meaning any
    ///   setters must normalize the value to ensure consistency.
    /// </summary>
    public int Orientation { get; }
}
