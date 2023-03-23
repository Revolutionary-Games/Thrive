using System.ComponentModel;

public enum InteractionType
{
    [Description("INTERACTION_PICK_UP")]
    Pickup,

    [Description("INTERACTION_CRAFT")]
    Craft,

    [Description("INTERACTION_HARVEST")]
    Harvest,

    [Description("INTERACTION_DEPOSIT_RESOURCES")]
    DepositResources,

    [Description("INTERACTION_CONSTRUCT")]
    Construct,
}
