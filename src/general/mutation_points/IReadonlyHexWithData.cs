public interface IReadOnlyHexWithData<T> : IReadOnlyPositionedHex, IActionHex
{
    // TODO: can we prevent the T type being modifiable?

    public T? Data { get; }
}
