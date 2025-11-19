public interface IReadOnlyCellTemplate : IReadOnlyPositionedHex, IReadOnlyCellDefinition
{
    IReadOnlyCellDefinition CellType { get; }
}
