using Newtonsoft.Json;

/// <summary>
///   Helper for types that don't derive from anything else to be save loadable
/// </summary>
/// <typeparam name="T">The type of temporary data that is used</typeparam>
public abstract class SaveLoadable<T> : ISaveLoadable
    where T : class, new()
{
    /// <summary>
    ///   The data that a converter has loaded but hasn't been applied yet due to requiring certain ISaveContext items
    /// </summary>
    [JsonIgnore]
    public T? UnAppliedSaveData;

    /// <summary>
    ///   Creates the unapplied data if missing and returns
    /// </summary>
    public T GetUnAppliedData()
    {
        return UnAppliedSaveData ??= new T();
    }

    public void FinishLoading(ISaveContext? context)
    {
        if (UnAppliedSaveData == null)
            return;

        ApplyUnAppliedSaveData(UnAppliedSaveData, context);

        UnAppliedSaveData = null;
    }

    protected abstract void ApplyUnAppliedSaveData(T data, ISaveContext? context);
}
