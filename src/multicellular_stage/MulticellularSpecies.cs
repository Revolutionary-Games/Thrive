using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Components;
using Godot;
using Newtonsoft.Json;
using Saving.Serializers;
using Systems;

/// <summary>
///   Represents a multicellular species that is composed of multiple cells
/// </summary>
[JsonObject(IsReference = true)]
[TypeConverter($"Saving.Serializers.{nameof(ThriveTypeConverter)}")]
[JSONDynamicTypeAllowed]
[UseThriveConverter]
[UseThriveSerializer]
public class MulticellularSpecies : Species, ISimulationPhotographable
{
    public MulticellularSpecies(uint id, string genus, string epithet) : base(id, genus, epithet)
    {
    }

    /// <summary>
    ///   The cells that make up this species' body plan. The first index is the cell of the bud type and the cells
    ///   grow in order.
    /// </summary>
    [JsonProperty]
    public CellLayout<CellTemplate> Cells { get; private set; } = new();

    [JsonProperty]
    public List<CellType> CellTypes { get; private set; } = new();

    /// <summary>
    ///   All organelles in all the species' placed cells (there can be a lot of duplicates in this list)
    /// </summary>
    [JsonIgnore]
    public IEnumerable<OrganelleTemplate> Organelles => Cells.SelectMany(c => c.Organelles);

    [JsonIgnore]
    public override string StringCode => ThriveJsonConverter.Instance.SerializeObject(this);

    [JsonIgnore]
    public ISimulationPhotographable.SimulationType SimulationToPhotograph =>
        ISimulationPhotographable.SimulationType.MicrobeGraphics;

    public override void OnEdited()
    {
        base.OnEdited();

        RepositionToOrigin();
        UpdateInitialCompounds();

        // Make certain these are all up to date
        foreach (var cellType in CellTypes)
        {
            // See the comment in CellBodyPlanEditorComponent.OnFinishEditing
            if (cellType.RepositionToOrigin())
            {
                GD.Print("Repositioned a multicellular species' cell type. This might break / crash the " +
                    "body plan layout.");
            }
        }
    }

    public override bool RepositionToOrigin()
    {
        // TODO: should this actually reposition things as the cell at index 0 is always the colony leader so if it
        // isn't centered, that'll cause issues?
        // var centerOfMass = Cells.CenterOfMass;

        var centerOfMass = Cells[0].Position;

        if (centerOfMass.Q == 0 && centerOfMass.R == 0)
            return false;

        foreach (var cell in Cells)
        {
            // This calculation aligns the center of mass with the origin by moving every organelle of the microbe.
            cell.Position -= centerOfMass;
        }

        return true;
    }

    public override void UpdateInitialCompounds()
    {
        var simulationParameters = SimulationParameters.Instance;

        // Since the initial compounds are only set once per species they can't be calculated for each Biome.
        // So, the compound balance calculation uses a special biome.
        // TODO: see the TODOS in MicrobeSpecies as well as: https://github.com/Revolutionary-Games/Thrive/issues/5446
        var biomeConditions = simulationParameters.GetBiome("speciesInitialCompoundsBiome").Conditions;

        var compoundBalances = new Dictionary<Compound, CompoundBalance>();

        // TODO: environmental tolerances for multicellular
        var environmentalTolerances = new ResolvedMicrobeTolerances
        {
            HealthModifier = 1,
            OsmoregulationModifier = 1,
            ProcessSpeedModifier = 1,
        };

        ProcessSystem.ComputeCompoundBalance(Cells[0].Organelles,
            biomeConditions, environmentalTolerances, CompoundAmountType.Biome, false, compoundBalances);
        var storageCapacity = MicrobeInternalCalculations.CalculateCapacity(Cells[0].Organelles);

        InitialCompounds.Clear();

        foreach (var compoundBalance in compoundBalances)
        {
            if (compoundBalance.Value.Balance >= 0)
                continue;

            // Skip compounds we don't want to give as initial compounds
            if (!simulationParameters.GetCompoundDefinition(compoundBalance.Key).CanBeInitialCompound)
                continue;

            // Initial compounds should suffice for a fixed amount of time.
            // Some extra is given to accommodate multicellular growth
            var compoundInitialAmount = Math.Abs(compoundBalance.Value.Balance) *
                Constants.INITIAL_COMPOUND_TIME * Constants.MULTICELLULAR_INITIAL_COMPOUND_MULTIPLIER;

            if (compoundInitialAmount > storageCapacity)
                compoundInitialAmount = storageCapacity;

            InitialCompounds.Add(compoundBalance.Key, compoundInitialAmount);
        }
    }

    public override void HandleNightSpawnCompounds(CompoundBag targetStorage, ISpawnEnvironmentInfo spawnEnvironment)
    {
        if (spawnEnvironment is not IMicrobeSpawnEnvironment microbeSpawnEnvironment)
            throw new ArgumentException("Multicellular species must have microbe spawn environment info");

        // TODO: this would be excellent to match the actual cell type being used for spawning
        var cellType = Cells[0].CellType;

        // TODO: environmental tolerances for multicellular
        var environmentalTolerances = new ResolvedMicrobeTolerances
        {
            HealthModifier = 1,
            OsmoregulationModifier = 1,
            ProcessSpeedModifier = 1,
        };

        // TODO: CACHING IS MISSING from here (but microbe has it)
        // TODO: should moving be false in some cases?
        var compoundTimes = MicrobeInternalCalculations.CalculateDayVaryingCompoundsFillTimes(cellType.Organelles,
            cellType.MembraneType, true, PlayerSpecies, microbeSpawnEnvironment.CurrentBiome, environmentalTolerances,
            microbeSpawnEnvironment.WorldSettings);

        MicrobeInternalCalculations.GiveNearNightInitialCompoundBuff(targetStorage, compoundTimes,
            spawnEnvironment.DaylightInfo);
    }

    public override void ApplyMutation(Species mutation)
    {
        base.ApplyMutation(mutation);

        var casted = (MulticellularSpecies)mutation;

        Cells.Clear();

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();

        foreach (var cellTemplate in casted.Cells)
        {
            Cells.AddFast((CellTemplate)cellTemplate.Clone(), workMemory1, workMemory2);
        }

        CellTypes.Clear();

        foreach (var cellType in casted.CellTypes)
        {
            CellTypes.Add((CellType)cellType.Clone());
        }
    }

    public override float GetPredationTargetSizeFactor()
    {
        var totalOrganelles = 0;

        int count = Cells.Count;
        for (int i = 0; i < count; ++i)
        {
            totalOrganelles += Cells[i].Organelles.Count;
        }

        return totalOrganelles;
    }

    public override object Clone()
    {
        var result = new MulticellularSpecies(ID, Genus, Epithet);

        ClonePropertiesTo(result);

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();

        foreach (var cellTemplate in Cells)
        {
            result.Cells.AddFast((CellTemplate)cellTemplate.Clone(), workMemory1, workMemory2);
        }

        foreach (var cellType in CellTypes)
        {
            result.CellTypes.Add((CellType)cellType.Clone());
        }

        return result;
    }

    public void SetupWorldEntities(IWorldSimulation worldSimulation)
    {
        ((MicrobeVisualOnlySimulation)worldSimulation).CreateVisualisationColony(this);
    }

    public bool StateHasStabilized(IWorldSimulation worldSimulation)
    {
        return true;
    }

    public Vector3 CalculatePhotographDistance(IWorldSimulation worldSimulation)
    {
        float radius = 0.0f;

        Vector3 center = Vector3.Zero;

        int count = Cells.Count;
        for (int i = 0; i < count; ++i)
        {
            center += Hex.AxialToCartesian(Cells[i].Position);
        }

        center /= count;

        foreach (var entity in worldSimulation.EntitySystem)
        {
            if (!entity.Has<CellProperties>())
                continue;

            ref var cellProperties = ref entity.Get<CellProperties>();

            // This uses the membrane as radius is not set as the physics system doesn't run
            if (!cellProperties.IsMembraneReady())
                throw new InvalidOperationException("Microbe doesn't have a ready membrane");

            var cellRadius = cellProperties.CreatedMembrane!.EncompassingCircleRadius;

            var farthestPoint = entity.Get<WorldPosition>().Position.DistanceTo(center) + cellRadius;

            if (farthestPoint > radius)
            {
                radius = farthestPoint;
            }
        }

        return new Vector3(center.X, PhotoStudio.CameraDistanceFromRadiusOfObject(radius), center.Z);
    }

    public override ulong GetVisualHashCode()
    {
        ulong hash = 1099511628211;

        foreach (var cell in Cells)
        {
            hash ^= cell.GetVisualHashCode() ^ (ulong)cell.Position.GetHashCode();

            hash = (hash << 7) | (hash >> 57);
        }

        return hash;
    }

    protected override Dictionary<Compound, float> CalculateBaseReproductionCost()
    {
        var baseReproductionCost = base.CalculateBaseReproductionCost();

        // Apply the multiplier to the costs for being multicellular
        var result = new Dictionary<Compound, float>();

        foreach (var entry in baseReproductionCost)
        {
            result[entry.Key] = entry.Value * Constants.MULTICELLULAR_BASE_REPRODUCTION_COST_MULTIPLIER;
        }

        return result;
    }
}
