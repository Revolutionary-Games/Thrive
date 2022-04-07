public interface IPositionedCell : IPositionedHex, ICellProperties
{
    /// <summary>
    ///   How many times this cell is rotated
    /// </summary>
    int Orientation { get; set; }
}
