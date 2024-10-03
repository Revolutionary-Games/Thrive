/// <summary>
///   Item in a <see cref="IComputeCache{T}"/>
/// </summary>
public interface ICacheItem
{
    public ulong CalculateCacheHash();
}
