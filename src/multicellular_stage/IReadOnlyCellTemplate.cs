public interface IReadOnlyCellTemplate : IReadOnlyPositionedHex, IReadOnlyCellDefinition
{
    public IReadOnlyCellTypeDefinition CellType { get; }
}
