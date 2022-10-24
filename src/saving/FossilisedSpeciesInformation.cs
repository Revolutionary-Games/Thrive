using System;
using System.ComponentModel;

public class FossilisedSpeciesInformation
{
    public enum SpeciesType
    {
        [Description("MICROBE")]
        Microbe,

        [Description("EARLY_MULTICELLULAR")]
        EarlyMulticellular,

        [Description("LATE_MULTICELLULAR")]
        LateMulticellular,
    }

    public string ThriveVersion { get; set; } = Constants.Version;

    public string Creator { get; set; } = Settings.Instance.ActiveUsername;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Guid ID { get; set; } = Guid.NewGuid();

    public SpeciesType Type { get; set; }
}
