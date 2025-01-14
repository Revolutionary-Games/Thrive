using System;
using Newtonsoft.Json;

public class CreatureAI
{
    [JsonProperty]
    private MulticellularCreature creature;

    public CreatureAI(MulticellularCreature creature)
    {
        this.creature = creature ?? throw new ArgumentException("no creature given", nameof(creature));
    }
}
