public interface IPositionedCell : IPositionedHex, ICellDefinition
{
    /// <summary>
    ///   How many times this cell is rotated
    /// </summary>
    public int Orientation { get; set; }
}
