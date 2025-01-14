using System;
using Newtonsoft.Json;

public class CreatureAI
{
    [JsonProperty]
    private MacroscopicCreature creature;

    public CreatureAI(MacroscopicCreature creature)
    {
        this.creature = creature ?? throw new ArgumentException("no creature given", nameof(creature));
    }
}
