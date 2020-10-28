using System;

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
        Manual,

        /// <summary>
        ///   Automatic save
        /// </summary>
        AutoSave,

        /// <summary>
        ///   Quick save, separate from manual to make it easier to keep a fixed number of quick saves
        /// </summary>
        QuickSave,

        /// <summary>
        ///   A broken save that (probably) cannot be loaded
        /// </summary>
        Invalid,
    }

    /// <summary>
    ///   Version of the game the save was made with, used to detect incompatible versions
    /// </summary>
    public string ThriveVersion { get; set; } = Constants.Version;

    public string Platform { get; set; } = FeatureInformation.GetOS();

    public string Creator { get; set; } =
        Settings.Instance.CustomUsernameEnabled ?
            Settings.Instance.CustomUsername.Value :
            Environment.UserName;

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
