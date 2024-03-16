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

    public enum SpeciesType
    {
        [Description("MICROBE")]
        Microbe,

        [Description("EARLY_MULTICELLULAR")]
        EarlyMulticellular,

        [Description("LATE_MULTICELLULAR")]
        LateMulticellular,
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
