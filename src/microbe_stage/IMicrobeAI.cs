using System;

public interface IMicrobeAI
{
    float TimeUntilNextAIUpdate { get; set; }

    /// <summary>
    ///   Runs AI thinking on this microbe. Should only be called by the MicrobeAISystem.
    ///   This is ran in parallel so this shouldn't affect the states of other microbes or rely on their variables that
    ///   the AI updates. Otherwise the results are not deterministic.
    /// </summary>
    /// <param name="delta">Elapsed time in seconds.</param>
    void AIThink(float delta, Random random, MicrobeAICommonData data);
}
