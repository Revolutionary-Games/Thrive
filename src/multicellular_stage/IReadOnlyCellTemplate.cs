public interface IReadOnlyCellTemplate : IReadOnlyPositionedHex, IReadOnlyCellDefinition, IActionHex
{
    public IReadOnlyCellTypeDefinition CellType { get; }

    public bool IsCavity { get; }
}
