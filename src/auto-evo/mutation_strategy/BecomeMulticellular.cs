namespace AutoEvo;

using System;
using System.Collections.Generic;
using static CommonMutationFunctions;

public class BecomeMulticellular : IMutationStrategy<Species>
{
    public bool Repeatable => false;

    public List<Mutant>? MutationsOf(Species baseSpecies, double mp, bool lawk,
        Random random, BiomeConditions biomeToConsider)
    {
        // We probably want to avoid auto-evo creating multicellular species from the player, or it might even happen
        // twice at once
        if (baseSpecies.PlayerSpecies)
            return null;

        if (baseSpecies is not MicrobeSpecies baseMicrobeSpecies)
            return null;

        var organelles = baseMicrobeSpecies.Organelles;

        // Right now the only requirement for becoming multicellular is that the species has a Binding Agent.
        // (the in-gameplay requirement of having a colony of size 5 can be supposed to happen anytime)
        // If more requirements are added for the player, that should extend to auto-evo as well.
        /*var hasBindingFeature = false;
        var count = organelles.Count;
        for (int i = 0; i < count; ++i)
        {
            if (organelles[i].Definition.HasBindingFeature)
            {
                hasBindingFeature = true;
                break;
            }
        }

        if (!hasBindingFeature)
            return null;*/

        var newSpecies = GameWorld.GenerateMulticellularVersion(baseMicrobeSpecies, true, true);

        // Like with the player, becoming Multicellular does not cost any MP.
        return [new Mutant(newSpecies, mp)];
    }
}
