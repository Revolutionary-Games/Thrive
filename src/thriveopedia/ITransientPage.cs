/// <summary>
///   Thriveopedia page that gets deleted if not pinned when moving away from it
/// </summary>
public interface ITransientPage
{
    public bool Pinned { get; }
}
