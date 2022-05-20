/// <summary>
///   Interface for types that support loading from a save and using as is after calling the finish loading
/// </summary>
public interface ISaveLoadable
{
    void FinishLoading(ISaveContext? context);
}
