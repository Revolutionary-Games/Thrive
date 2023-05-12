using Newtonsoft.Json;

/// <summary>
///   Objects that know they have been loaded from a save
/// </summary>
public interface ISaveLoadedTracked
{
    /// <summary>
    ///   Set to true when loaded from a save
    /// </summary>
    [JsonIgnore]
    public bool IsLoadedFromSave { get; set; }
}
