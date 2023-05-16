/// <summary>
///   Interface for unit orders to implement, separate from the base class to avoid having to specify the generic
///   parameters everywhere
/// </summary>
public interface IUnitOrder
{
    public bool Completed { get; }

    // TODO: textual description of the order to be shown in various unit status screens
}
