using System;
using System.ComponentModel;

/// <summary>
///   Details for a fossilised species saved on disk.
/// </summary>
public class FossilisedSpeciesInformation
{
    /// <summary>
    ///   Details for a fossilised species saved on disk.
    /// </summary>
    /// <param name="type">The type of this species (e.g. microbe)</param>
    public FossilisedSpeciesInformation(SpeciesType type)
    {
        Type = type;
    }

    /// <summary>
    ///   Type of this species. This enum should not be reordered as that will break existing fossilized files which
    ///   can never be done (as there's no process for version upgrading them).
    /// </summary>
    public enum SpeciesType
    {
        [Description("MICROBE")]
        Microbe,

        [Description("MULTICELLULAR")]
        Multicellular,

        [Description("MACROSCOPIC")]
        Macroscopic,
    }

    /// <summary>
    ///   The version of Thrive the species was saved in.
    /// </summary>
    public string ThriveVersion { get; set; } = Constants.Version;

    /// <summary>
    ///   The name of the user who saved this species.
    /// </summary>
    public string Creator { get; set; } = Settings.Instance.ActiveUsername;

    /// <summary>
    ///   The time at which this species was saved.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    ///   ID for this fossilised species.
    /// </summary>
    public Guid ID { get; set; } = Guid.NewGuid();

    /// <summary>
    ///   The type of this species, e.g. microbe.
    /// </summary>
    public SpeciesType Type { get; set; }
}
