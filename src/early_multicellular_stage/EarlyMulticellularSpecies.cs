using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using Saving.Serializers;
using Systems;

/// <summary>
///   Represents an early multicellular species that is composed of multiple cells
/// </summary>
[JsonObject(IsReference = true)]
[TypeConverter($"Saving.Serializers.{nameof(ThriveTypeConverter)}")]
[JSONDynamicTypeAllowed]
[UseThriveConverter]
[UseThriveSerializer]
public class EarlyMulticellularSpecies : Species
{
    public EarlyMulticellularSpecies(uint id, string genus, string epithet) : base(id, genus, epithet)
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
                GD.Print("Repositioned an early multicellular species' cell type. This might break / crash the " +
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
        // Since the initial compounds are only set once per species they can't be calculated for each Biome.
        // So, the compound balance calculation uses the default biome.
        var biomeConditions = SimulationParameters.Instance.GetBiome("default").Conditions;
        var compoundBalances = ProcessSystem.ComputeCompoundBalance(Cells[0].Organelles,
            biomeConditions, CompoundAmountType.Biome);
        var storageCapacity = MicrobeInternalCalculations.CalculateCapacity(Cells[0].Organelles);

        InitialCompounds.Clear();

        foreach (var compoundBalance in compoundBalances)
        {
            if (compoundBalance.Value.Balance >= 0)
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

    public override void ApplyMutation(Species mutation)
    {
        base.ApplyMutation(mutation);

        var casted = (EarlyMulticellularSpecies)mutation;

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

    public override object Clone()
    {
        var result = new EarlyMulticellularSpecies(ID, Genus, Epithet);

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

    protected override Dictionary<Compound, float> CalculateBaseReproductionCost()
    {
        var baseReproductionCost = base.CalculateBaseReproductionCost();

        // Apply the multiplier to the costs for being multicellular
        var result = new Dictionary<Compound, float>();

        foreach (var entry in baseReproductionCost)
        {
            result[entry.Key] = entry.Value * Constants.EARLY_MULTICELLULAR_BASE_REPRODUCTION_COST_MULTIPLIER;
        }

        return result;
    }
}
