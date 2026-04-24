namespace AutoEvo;

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

/// <summary>
///   Base mutation strategy for mutations that modify organelles (with upgrades)
/// </summary>
public abstract class ModifyOrganelleBase : IMutationStrategy<MicrobeSpecies>
{
    protected readonly FrozenSet<OrganelleDefinition> allOrganelles;

    private readonly bool shouldRepeat;

    public ModifyOrganelleBase(Func<OrganelleDefinition, bool> criteria, bool shouldRepeat)
    {
        allOrganelles = SimulationParameters.Instance.GetAllOrganelles().Where(criteria).ToFrozenSet();
        this.shouldRepeat = shouldRepeat;
    }

    public bool Repeatable => shouldRepeat;

    /// <summary>
    ///   Used to filter out totally pointless mutations when this costs some MP but none is remaining
    /// </summary>
    public abstract bool ExpectedToCostMP { get; }

    public List<CommonMutationFunctions.Mutant>? MutationsOf(MicrobeSpecies baseSpecies, double mp, bool lawk,
        Random random, BiomeConditions biomeToConsider)
    {
        // While some upgrades are free, it still might be good to stop looking for mutations once something has
        // consumed all the remaining MP
        if (allOrganelles.Count == 0 || (mp <= 0 && ExpectedToCostMP))
        {
            return null;
        }

        // TODO: maybe there is a way to avoid this memory allocation?
        List<int>? organelleIndexesToMutate = null;

        // Manual looping to avoid one enumerator allocation per call
        var organelleList = baseSpecies.Organelles.Organelles;
        var organelleCount = organelleList.Count;
        for (var i = 0; i < organelleCount; ++i)
        {
            var organelle = organelleList[i];
            if (!allOrganelles.Contains(organelle.Definition))
                continue;

            // Filter out upgrades early that cost too much / organelles that cannot be upgraded
            if (!CanModifyOrganelle(organelle, mp))
                continue;

            organelleIndexesToMutate ??= new List<int>();
            organelleIndexesToMutate.Add(i);
        }

        // Skip if nothing can be upgraded
        if (organelleIndexesToMutate == null)
        {
            return null;
        }

        var mutated = new List<CommonMutationFunctions.Mutant>();

        // Pick a random organelle that can be mutated each time
        organelleIndexesToMutate.Shuffle(random);

        for (int i = 0; i < Constants.AUTO_EVO_ORGANELLE_UPGRADE_ATTEMPTS; ++i)
        {
            if (i >= organelleIndexesToMutate.Count)
                break;

            int organelleToMutate = organelleIndexesToMutate[i];

            // We manually clone organelles to save on memory allocation
            var newSpecies = baseSpecies.Clone(false);

            bool mutatedOrganelle = false;
            double mpCost = 0;

            // TODO: this whole block would need to run twice if we wanted to try the feature flag and custom data
            // separately like in the first draft version of this mutation code
            for (var j = 0; j < organelleCount; ++j)
            {
                if (j == organelleToMutate)
                {
                    var originalOrganelle = organelleList[j];

                    if (!ApplyOrganelleUpgrade(mp, originalOrganelle, ref mpCost, out var upgradedOrganelle, random))
                        break;

                    if (ReferenceEquals(originalOrganelle, upgradedOrganelle))
                        throw new Exception("The overridden method should create new organelle instances");

                    // We did not change the position at all, so we can safely put down the organelle as upgrades
                    // cannot affect the shape
                    newSpecies.Organelles.AddAutoEvoAttemptOrganelle(upgradedOrganelle);
                    mutatedOrganelle = true;
                }
                else
                {
                    // TODO: switch away from cloning again once ensured that auto-evo does not modify original
                    // organelles
                    newSpecies.Organelles.AddAutoEvoAttemptOrganelle(organelleList[j].Clone());

                    // newSpecies.Organelles.AddAutoEvoAttemptOrganelle(organelleList[j]);
                }
            }

            if (mutatedOrganelle)
            {
                mutated.Add(new CommonMutationFunctions.Mutant(newSpecies, mp - mpCost));
            }
        }

        return mutated;
    }

    /// <summary>
    ///   An initial check whether an organelle that matched the type definition can be upgraded.
    /// </summary>
    /// <param name="organelle">Organelle to check</param>
    /// <param name="mpRemaining">How much MP there is available</param>
    /// <returns>True if this organelle is a candidate for upgrading</returns>
    protected abstract bool CanModifyOrganelle(OrganelleTemplate organelle, double mpRemaining);

    /// <summary>
    ///   Tries to apply the mutation this strategy is for. If at all possible, this should ensure the maximal chance
    ///   for success, because work has already been done to duplicate an attempt species so all that work is wasted
    ///   whenever this doesn't succeed.
    /// </summary>
    /// <param name="mpRemaining">Total MP remaining for this mutation strategy to use</param>
    /// <param name="originalOrganelle">
    ///   The original organelle that should be attempted to be upgraded. Do not modify this instance directly!
    /// </param>
    /// <param name="mpCost">The total cost of all upgrades applied. This needs to be updated.</param>
    /// <param name="upgradedOrganelle">Out parameter for the cloned organelle that has upgrades applied</param>
    /// <param name="random">Access to a random number generator</param>
    /// <returns>True if applied, false if failed, and the whole attempt should be abandoned</returns>
    protected abstract bool ApplyOrganelleUpgrade(double mpRemaining, OrganelleTemplate originalOrganelle,
        ref double mpCost, [NotNullWhen(true)] out OrganelleTemplate? upgradedOrganelle, Random random);
}
