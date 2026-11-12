using System.Collections.Generic;
using Godot;
using Systems;

/// <summary>
///   Calculates the cell editor's statistics independently of the controls displaying them.
///   Each stage takes its current display options explicitly, so publishing one result need not
///   wait for later calculations or freeze the options used by those calculations.
/// </summary>
public class CellEditorStatisticsCalculator
{
    private readonly Dictionary<OrganelleDefinition, int> specializationWorkMemory = new();
    private readonly Dictionary<Compound, float> processSpeedWorkMemory = new();
    private readonly Dictionary<Compound, List<Compound>> compoundSources = new();
    private readonly HashSet<Compound> compoundsThatDependOnDay = new();

    public CellProperties PrepareCell(IReadOnlyList<OrganelleTemplate> organelles)
    {
        var maximumMovementDirection = MicrobeInternalCalculations.MaximumSpeedDirection(organelles);

        // Treats cellTypeSpecializationBonus as totalSpecializationBonus, because adjacency is ignored in this editor.
        var specialization = MicrobeInternalCalculations.CalculateSpecializationBonus(organelles,
            specializationWorkMemory);

        return new CellProperties(organelles, maximumMovementDirection, specialization);
    }

    public void CalculateEnergyBalance(in CellProperties cell, MembraneType membrane, IBiomeConditions biome,
        in ResolvedMicrobeTolerances tolerances, bool moving, WorldGenerationSettings worldSettings,
        CompoundAmountType amountType, EnergyBalanceInfoFull result)
    {
        ProcessSystem.ComputeEnergyBalanceFull(cell.Organelles, biome, tolerances, cell.Specialization,
            membrane, cell.MaximumMovementDirection, moving, true, worldSettings, amountType, null, result);
    }

    public Dictionary<Compound, CompoundBalance> CalculateCompoundBalance(in CellProperties cell,
        BalanceDisplayType calculationType, CompoundAmountType amountType, IBiomeConditions biome,
        EnergyBalanceInfoFull energyBalance, ref Dictionary<Compound, float>? specificStorages,
        ref float nominalStorage, in ResolvedMicrobeTolerances tolerances)
    {
        var compoundBalanceData = new Dictionary<Compound, CompoundBalance>();
        switch (calculationType)
        {
            case BalanceDisplayType.MaxSpeed:
                ProcessSystem.ComputeCompoundBalance(cell.Organelles, biome, tolerances, cell.Specialization,
                    amountType, true, compoundBalanceData);
                break;
            case BalanceDisplayType.EnergyEquilibrium:
                ProcessSystem.ComputeCompoundBalanceAtEquilibrium(cell.Organelles, biome, tolerances,
                    cell.Specialization, amountType, energyBalance, compoundBalanceData);
                break;
            default:
                GD.PrintErr("Unknown compound balance type: ", calculationType);
                goto case BalanceDisplayType.EnergyEquilibrium;
        }

        specificStorages ??= MicrobeInternalCalculations.GetTotalSpecificCapacity(cell.Organelles,
            cell.Specialization, out nominalStorage);

        return ProcessSystem.ComputeCompoundFillTimes(compoundBalanceData, nominalStorage, specificStorages);
    }

    /// <summary>
    ///   Returns working memory consumed immediately by the current refresh's two balance displays.
    ///   The caller must consume the set before the next call replaces its contents.
    /// </summary>
    public HashSet<Compound> CalculateDayDependentCompounds(in CellProperties cell, IBiomeConditions biome,
        in ResolvedMicrobeTolerances tolerances, CompoundAmountType amountType)
    {
        compoundSources.Clear();
        ProcessSystem.CalculateInputCompoundsNeededForOutputs(cell.Organelles, biome, tolerances, cell.Specialization,
            amountType, true, compoundSources);

        biome.GetProducedCompoundsThatDependOnVarying(compoundSources, compoundsThatDependOnDay);
        return compoundsThatDependOnDay;
    }

    public List<ProcessSpeedInformation> CalculateProcessStatistics(IReadOnlyList<OrganelleTemplate> organelles,
        List<TweakedProcess> processes, in ResolvedMicrobeTolerances tolerances, EnergyBalanceInfoFull energyBalance,
        IBiomeConditions biome)
    {
        // Empty list to later fill
        var processStatistics = new List<ProcessSpeedInformation>();

        float consumptionProductionRatio = energyBalance.TotalConsumption / energyBalance.TotalProduction;

        var specialization =
            MicrobeInternalCalculations.CalculateSpecializationBonus(organelles, specializationWorkMemory);

        var speedModifier = tolerances.ProcessSpeedModifier * specialization;

        foreach (var process in processes)
        {
            // This requires the inputs to be in the biome to give a realistic prediction of how fast the processes
            // *might* run once swimming around in the stage.
            var singleProcess = ProcessSystem.CalculateProcessMaximumSpeed(process, speedModifier,
                biome, CompoundAmountType.Current, true);

            // If produces more ATP than consumes, lower down production for inputs and for outputs,
            // otherwise use maximum production values (this matches the equilibrium display mode and what happens
            // in the game once exiting the editor)
            if (consumptionProductionRatio < 1.0f)
            {
                singleProcess.ScaleSpeed(consumptionProductionRatio, processSpeedWorkMemory);
            }

            processStatistics.Add(singleProcess);
        }

        return processStatistics;
    }

    /// <summary>
    ///   The main cell's derived properties, computed before reading the editor's environmental tolerances.
    ///   This borrows the organelles for the current refresh; it is not an immutable cell snapshot.
    /// </summary>
    public readonly struct CellProperties
    {
        public readonly IReadOnlyList<OrganelleTemplate> Organelles;
        public readonly Vector3 MaximumMovementDirection;
        public readonly float Specialization;

        public CellProperties(IReadOnlyList<OrganelleTemplate> organelles, Vector3 maximumMovementDirection,
            float specialization)
        {
            Organelles = organelles;
            MaximumMovementDirection = maximumMovementDirection;
            Specialization = specialization;
        }
    }
}
