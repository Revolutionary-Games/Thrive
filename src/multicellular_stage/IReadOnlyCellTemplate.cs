public interface IReadOnlyCellTemplate : IReadOnlyPositionedHex, IReadOnlyCellDefinition, IActionHex
{
    public IReadOnlyCellTypeDefinition CellType { get; }
}
