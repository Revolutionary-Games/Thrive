using System;
using System.ComponentModel;
using Newtonsoft.Json;

/// <summary>
///   Info embedded in a save file
/// </summary>
public class SaveInformation
{
    public enum SaveType
    {
        /// <summary>
        ///   Player initiated save
        /// </summary>
        [Description("SAVE_MANUAL")]
        Manual,

        /// <summary>
        ///   Automatic save
        /// </summary>
        [Description("SAVE_AUTOSAVE")]
        AutoSave,

        /// <summary>
        ///   Quick save, separate from manual to make it easier to keep a fixed number of quick saves
        /// </summary>
        [Description("SAVE_QUICKSAVE")]
        QuickSave,

        /// <summary>
        ///   A broken save that (probably) cannot be loaded
        /// </summary>
        [Description("SAVE_INVALID")]
        Invalid,
    }

    /// <summary>
    ///   Version of the game the save was made with, used to detect incompatible versions
    /// </summary>
    public string ThriveVersion { get; set; } = Constants.Version;

    public string Platform { get; set; } = FeatureInformation.GetOS();

    public string Creator { get; set; } = Settings.Instance.ActiveUsername;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    ///   An extended description for this save
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///   Unique ID of this save
    /// </summary>
    public Guid ID { get; set; } = Guid.NewGuid();

    public SaveType Type { get; set; } = SaveType.Manual;

    /// <summary>
    ///   True if this save was made in one of the prototypes allowing saving. Disallows save upgrade and loading
    ///   from every different game version than exactly the one that made the save. This is so that prototype
    ///   developers can freely rework the prototypes as much as they want without having to worry about saves.
    /// </summary>
    public bool IsPrototype { get; set; }

    [JsonIgnore]
    public string TranslatedSaveTypeString =>
        Localization.Translate(Type.GetAttribute<DescriptionAttribute>().Description);

    /// <summary>
    ///   Creates save information for an invalid save
    /// </summary>
    /// <returns>A save information for an invalid save</returns>
    public static SaveInformation CreateInvalid()
    {
        return new SaveInformation
        {
            Type = SaveType.Invalid,
            ThriveVersion = "Invalid",
            Platform = "Invalid",
            Creator = "Invalid",
        };
    }
}
