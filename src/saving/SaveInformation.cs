using System;
using System.ComponentModel;
using System.Globalization;
using Newtonsoft.Json;
using SharedBase.Archive;

/// <summary>
///   Info embedded in a save file
/// </summary>
public class SaveInformation : IArchivable
{
    /// <summary>
    ///   Overall save format version
    /// </summary>
    public const int CURRENT_SAVE_VERSION = 2;

    /// <summary>
    ///   Version of this object when saved in a save archive.
    /// </summary>
    public const int SERIALIZATION_VERSION = 1;

    /// <summary>
    ///   Type of save. Do not reorder these, otherwise everything will break.
    /// </summary>
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

    /// <summary>
    ///   Unique ID of this *playthrough* (all saves of a playthrough have the same ID)
    /// </summary>
    public Guid PlaythroughID { get; set; }

    public SaveType Type { get; set; } = SaveType.Manual;

    /// <summary>
    ///   The main game state this save was made in
    /// </summary>
    public MainGameState GameState { get; set; } = MainGameState.Invalid;

    /// <summary>
    ///   Version of the save format used in this save file. This is not default initialized to detect really old
    ///   saves with no version number.
    /// </summary>
    public int SaveVersion { get; set; }

    /// <summary>
    ///   True if this save was made in one of the prototypes allowing saving. Disallows save upgrade and loading
    ///   from every different game version than exactly the one that made the save. This is so that prototype
    ///   developers can freely rework the prototypes as much as they want without having to worry about saves.
    /// </summary>
    public bool IsPrototype { get; set; }

    /// <summary>
    ///   Set to true if the player has cheated in this game
    /// </summary>
    public bool CheatsUsed { get; set; }

    /// <summary>
    ///   Hash of the archive content which can be used to check if a save is corrupted.
    /// </summary>
    public string HashOfArchiveContents { get; set; } = string.Empty;

    [JsonIgnore]
    public string TranslatedSaveTypeString =>
        Localization.Translate(Type.GetAttribute<DescriptionAttribute>().Description);

    [JsonIgnore]
    public ushort CurrentArchiveVersion => SERIALIZATION_VERSION;

    [JsonIgnore]
    public ArchiveObjectType ArchiveObjectType => (ArchiveObjectType)ThriveArchiveObjectType.SaveInformation;

    [JsonIgnore]
    public bool CanBeReferencedInArchive => false;

    /// <summary>
    ///   Creates save information for an invalid save
    /// </summary>
    /// <returns>Save information for an invalid save</returns>
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

    public static SaveInformation ReadFromArchive(ISArchiveReader reader, ushort version)
    {
        if (version is > SERIALIZATION_VERSION or <= 0)
            throw new InvalidArchiveVersionException(version, SERIALIZATION_VERSION);

        return new SaveInformation
        {
            ThriveVersion = reader.ReadString() ?? throw new NullArchiveObjectException(),
            Platform = reader.ReadString() ?? throw new NullArchiveObjectException(),
            Creator = reader.ReadString() ?? throw new NullArchiveObjectException(),
            CreatedAt = DateTime.Parse(reader.ReadString() ?? throw new NullArchiveObjectException(),
                CultureInfo.InvariantCulture),
            Description = reader.ReadString() ?? throw new NullArchiveObjectException(),
            ID = Guid.Parse(reader.ReadString() ?? throw new NullArchiveObjectException()),
            PlaythroughID = Guid.Parse(reader.ReadString() ?? throw new NullArchiveObjectException()),
            Type = (SaveType)reader.ReadInt32(),
            GameState = (MainGameState)reader.ReadInt32(),
            SaveVersion = reader.ReadInt32(),
            IsPrototype = reader.ReadBool(),
            CheatsUsed = reader.ReadBool(),
        };
    }

    public void WriteToArchive(ISArchiveWriter writer)
    {
        writer.Write(ThriveVersion);

        writer.Write(Platform);
        writer.Write(Creator);
        writer.Write(CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        writer.Write(Description);
        writer.Write(ID.ToString());
        writer.Write(PlaythroughID.ToString());
        writer.Write((int)Type);
        writer.Write((int)GameState);
        writer.Write(SaveVersion);
        writer.Write(IsPrototype);
        writer.Write(CheatsUsed);

        // This doesn't make sense to write to archives as this cannot be known before the archive is written
        // writer.Write(HashOfArchiveContents);
    }
}
