using System.ComponentModel;

public enum MulticellularReproductionMethod
{
    [Description("REPRODUCTION_BUDDING")]
    Budding,

    [Description("REPRODUCTION_SPORE")]
    Sporulation,

    [Description("REPRODUCTION_MASS_BUDDING")]
    MassBudding,

    [Description("REPRODUCTION_SEXUAL_ISOGAMY")]
    SexualIsogamy,

    [Description("REPRODUCTION_SEXUAL_ANISOGAMY")]
    SexualAnisogamy,
}
