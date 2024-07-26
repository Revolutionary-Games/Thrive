/// <summary>
///   A simple wrapper to an object reference allowing it to be marked and unmarked
/// </summary>
public class MarkableWrapper<T> : IMarkable
{
    public T Item;

    public MarkableWrapper(T item)
    {
        Item = item;
    }

    public bool Marked { get; set; }
}
