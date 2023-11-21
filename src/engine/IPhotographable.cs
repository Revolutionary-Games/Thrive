/// <summary>
///   Base photographable type for things that can be photographed. See <see cref="IScenePhotographable"/> for example.
/// </summary>
public interface IPhotographable<T>
{
    public float CalculatePhotographDistance(T photographableObjectState);
}
