namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;
using static CommonMutationFunctions;

public class RemoveOrganelle : IMutationStrategy<Species>
{
    private static readonly OrganelleDefinition Nucleus = SimulationParameters.Instance.GetOrganelleType("nucleus");
    private readonly Func<OrganelleDefinition, bool> criteria;

    public RemoveOrganelle(Func<OrganelleDefinition, bool> criteria)
    {
        this.criteria = criteria;
    }

    public bool Repeatable => true;

    // Formatter and inspect code disagree here.
    // ReSharper disable InvokeAsExtensionMethod
    public static RemoveOrganelle ThatUseCompound(CompoundDefinition compound)
    {
        return new RemoveOrganelle(organelle =>
            Enumerable.Any(organelle.RunnableProcesses, proc => proc.Process.Inputs.ContainsKey(compound)));
    }

    public static RemoveOrganelle ThatUseCompound(Compound compound)
    {
        var compoundResolved = SimulationParameters.GetCompound(compound);

        return ThatUseCompound(compoundResolved);
    }

    public static RemoveOrganelle ThatCreateCompound(CompoundDefinition compound)
    {
        return new RemoveOrganelle(organelle =>
            Enumerable.Any(organelle.RunnableProcesses, proc => proc.Process.Outputs.ContainsKey(compound)));
    }

    public static RemoveOrganelle ThatCreateCompound(Compound compound)
    {
        var compoundResolved = SimulationParameters.GetCompound(compound);

        return ThatCreateCompound(compoundResolved);
    }

    // ReSharper restore InvokeAsExtensionMethod

    public List<Mutant>? MutationsOf(Species baseSpecies, double mp, bool lawk,
        Random random, BiomeConditions biomeToConsider)
    {
        return baseSpecies switch
        {
            MicrobeSpecies microbeSpecies => MutationsOfMicrobe(microbeSpecies, mp, random),
            MulticellularSpecies multicellularSpecies => MutationsOfMulticellular(multicellularSpecies, mp, random),
            _ => null,
        };
    }

    private static bool HasLaterDuplicate(IReadOnlyList<OrganelleTemplate> organelles, int organelleIndex,
        int organelleCount)
    {
        var organelle = organelles[organelleIndex];

        // External organelles like pili and flagella are too dependent on exact locations to be considered equivalent
        if (organelle.Definition.PositionedExternally)
            return false;

        // We take the last possible duplicate part in the list, since that's less likely to create islands
        // So j starts from i + 1
        for (int j = organelleIndex + 1; j < organelleCount; ++j)
        {
            var potentialDuplicate = organelles[j];

            if (!ReferenceEquals(potentialDuplicate.Definition, organelle.Definition))
                continue;

            // If two organelles of the same type have different upgrades, they are not duplicates
            if (!Equals(organelle.Upgrades, potentialDuplicate.Upgrades))
                continue;

            return true;
        }

        return false;
    }

    private List<Mutant>? MutationsOfMicrobe(MicrobeSpecies baseSpecies, double mp, Random random)
    {
        if (mp < Constants.ORGANELLE_REMOVE_COST)
            return null;

        if (baseSpecies.Organelles.Count <= 1)
            return null;

        var baseOrganelles = baseSpecies.Organelles.Organelles;
        Span<int> candidateIndices = stackalloc int[Constants.AUTO_EVO_ORGANELLE_REMOVE_ATTEMPTS];
        int candidateCount = SelectOrganelleIndices(baseOrganelles, candidateIndices, random);

        List<Mutant>? mutated = null;

        MutationWorkMemory? workMemory = null;

        foreach (int candidateIndex in candidateIndices[..candidateCount])
        {
            var organelle = baseOrganelles[candidateIndex];

            // Don't clone organelles as we want to do those ourselves
            var newSpecies = baseSpecies.Clone(false);

            workMemory ??= new MutationWorkMemory();

            // Is this the best way to do this? Probably not, but this is how mutations.cs does it
            // and the other way outright did not work
            // This is now slightly improved - hhyyrylainen
            var count = baseSpecies.Organelles.Count;

            var occupied = workMemory.WorkingMemory3;
            occupied.Clear();

            for (var i = 0; i < count; ++i)
            {
                var parentOrganelle = baseOrganelles[i];

                if (ReferenceEquals(parentOrganelle, organelle))
                    continue;

                var definition = parentOrganelle.Definition;
                var position = parentOrganelle.Position;
                var orientation = parentOrganelle.Orientation;

                // Same decision as CanPlace: skipped only if it overlaps an organelle copied earlier, which means the
                // parent layout was already invalid
                if (!newSpecies.Organelles.IsOrganellePositionFree(definition, position.Q, position.R, orientation,
                        occupied, out _))
                {
                    continue;
                }

                var rotated = definition.GetRotatedHexes(orientation);
                int hexCount = rotated.Count;
                for (var rotatedIndex = 0; rotatedIndex < hexCount; ++rotatedIndex)
                    occupied.Add(rotated[rotatedIndex] + position);

                newSpecies.Organelles.AddAutoEvoAttemptOrganelle(parentOrganelle.Clone());
            }

            AttachIslandHexes(newSpecies.Organelles, workMemory);

            mutated ??= new List<Mutant>();
            mutated.Add(new Mutant(newSpecies, mp - Constants.ORGANELLE_REMOVE_COST));
        }

        return mutated;
    }

    private List<Mutant>? MutationsOfMulticellular(MulticellularSpecies baseSpecies, double mp, Random random)
    {
        var mpCost = Constants.ORGANELLE_REMOVE_COST * Constants.MULTICELLULAR_EDITOR_COST_FACTOR;
        if (mp < mpCost)
            return null;

        List<Mutant>? mutated = null;

        var cellTypeCount = baseSpecies.CellTypes.Count;
        Span<int> candidateIndices = stackalloc int[Constants.AUTO_EVO_ORGANELLE_REMOVE_ATTEMPTS];

        MutationWorkMemory? workMemory = null;

        for (var i = 0; i < cellTypeCount; ++i)
        {
            var baseCellType = baseSpecies.ModifiableCellTypes[i];
            if (baseCellType.Organelles.Count <= 1)
                continue;

            var baseOrganelles = baseCellType.ModifiableOrganelles.Organelles;
            int candidateCount = SelectOrganelleIndices(baseOrganelles, candidateIndices, random);

            workMemory ??= new MutationWorkMemory();

            var occupied = workMemory.WorkingMemory3;
            occupied.Clear();

            foreach (int candidateIndex in candidateIndices[..candidateCount])
            {
                var organelle = baseOrganelles[candidateIndex];

                // The Binding Agent cannot be removed in the Multicellular Stage
                if (organelle.Definition.HasBindingFeature)
                    continue;

                // Don't clone organelles as we want to do those ourselves
                var newSpecies = baseSpecies.Clone(false, false);
                var newCellType = newSpecies.ModifiableCellTypes[i];
                var newCellTypeOrganelles = newCellType.ModifiableOrganelles;

                // Clone organelles for the cell types not currently targeted
                for (var j = 0; j < cellTypeCount; ++j)
                {
                    var clonedCellType = newSpecies.ModifiableCellTypes[j];

                    if (ReferenceEquals(clonedCellType, newCellType))
                        continue;

                    occupied.Clear();

                    var parentCellTypeOrganelles =
                        baseSpecies.ModifiableCellTypes[j].ModifiableOrganelles;
                    var copyOrganelleCount = parentCellTypeOrganelles.Count;

                    for (var k = 0; k < copyOrganelleCount; ++k)
                    {
                        var parentOrganelle = parentCellTypeOrganelles[k];

                        if (ReferenceEquals(parentOrganelle, organelle))
                            continue;

                        var definition = parentOrganelle.Definition;
                        var position = parentOrganelle.Position;
                        var orientation = parentOrganelle.Orientation;

                        if (!clonedCellType.ModifiableOrganelles.IsOrganellePositionFree(definition, position.Q,
                                position.R, orientation, occupied, out _))
                        {
                            continue;
                        }

                        var rotated = definition.GetRotatedHexes(orientation);
                        int hexCount = rotated.Count;
                        for (var rotatedIndex = 0; rotatedIndex < hexCount; ++rotatedIndex)
                            occupied.Add(rotated[rotatedIndex] + position);

                        clonedCellType.ModifiableOrganelles.AddAutoEvoAttemptOrganelle(parentOrganelle.Clone());
                    }
                }

                // Clone the organelles for the targeted cell type, excluding the targeted organelle
                // Is this the best way to do this?
                var organelleCount = baseCellType.Organelles.Count;

                occupied.Clear();

                for (var j = 0; j < organelleCount; ++j)
                {
                    var parentOrganelle = baseOrganelles[j];

                    if (ReferenceEquals(parentOrganelle, organelle))
                        continue;

                    var definition = parentOrganelle.Definition;
                    var position = parentOrganelle.Position;
                    var orientation = parentOrganelle.Orientation;

                    if (!newCellTypeOrganelles.IsOrganellePositionFree(definition, position.Q, position.R,
                            orientation, occupied, out _))
                    {
                        continue;
                    }

                    var rotated = definition.GetRotatedHexes(orientation);
                    int hexCount = rotated.Count;
                    for (var rotatedIndex = 0; rotatedIndex < hexCount; ++rotatedIndex)
                        occupied.Add(rotated[rotatedIndex] + position);

                    newCellTypeOrganelles.AddAutoEvoAttemptOrganelle(parentOrganelle.Clone());
                }

                AttachIslandHexes(newCellTypeOrganelles, workMemory);

                mutated ??= new List<Mutant>();
                mutated.Add(new Mutant(newSpecies, mp - mpCost));
            }
        }

        return mutated;
    }

    /// <summary>
    ///   Uses reservoir sampling to select matching organelles, then shuffles the selected indices into attempt order.
    /// </summary>
    /// <param name="organelles">The original organelle list, filtered using this strategy's criteria.</param>
    /// <param name="candidates">The output buffer, whose length limits the number of selected indices.</param>
    /// <param name="random">The random source used for reservoir replacement and shuffling.</param>
    /// <returns>
    ///   The number of valid entries at the start of <paramref name="candidates"/>. Each entry indexes the original
    ///   organelle list; entries beyond the returned count may be uninitialized or left over from an earlier call.
    /// </returns>
    /// <remarks>
    ///   <para>
    ///     This mainly filters by criteria, but also excludes the Nucleus and any fully duplicate organelles.
    ///     Callers may still skip protected organelles in the sample without replacing them, so the sample size limits
    ///     attempts rather than successful removals.
    ///   </para>
    /// </remarks>
    private int SelectOrganelleIndices(IReadOnlyList<OrganelleTemplate> organelles, Span<int> candidates, Random random)
    {
        var matchingCount = 0;
        var selectedCount = 0;
        var organelleCount = organelles.Count;
        for (int i = 0; i < organelleCount; ++i)
        {
            if (!criteria(organelles[i].Definition))
                continue;

            // The player cannot remove the nucleus, so Auto-Evo should not be able to either
            if (ReferenceEquals(organelles[i].Definition, Nucleus))
                continue;

            // If there are duplicate instances of organelles, we only attempt to delete one of them.
            if (HasLaterDuplicate(organelles, i, organelleCount))
                continue;

            // Count only matching organelles for sampling, but store their indices in the original list.
            ++matchingCount;
            if (selectedCount < candidates.Length)
            {
                candidates[selectedCount++] = i;
                continue;
            }

            // With uniform draws, the m-th match enters a full k-slot reservoir with probability k/m.
            // Each earlier match had selection probability k/(m - 1) and survives with probability (m - 1)/m,
            // so its probability of remaining in the sample is also k/m.
            // For example, with k = 2, the fifth match replaces a slot only on draws 0 or 1 out of [0, 5).
            var replacement = random.Next(matchingCount);
            if (replacement < candidates.Length)
            {
                candidates[replacement] = i;
            }
        }

        // Reservoir sampling chooses a uniform subset, not a uniform order. Use a Fisher-Yates shuffle for attempts.
        // This is also needed when all matches fit in the buffer and would otherwise remain in source order.
        for (int i = 0; i < selectedCount - 1; ++i)
        {
            var swapIndex = i + random.Next(selectedCount - i);
            (candidates[i], candidates[swapIndex]) = (candidates[swapIndex], candidates[i]);
        }

        return selectedCount;
    }
}
