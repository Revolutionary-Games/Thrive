using System.Collections.Generic;
using System.Linq;
using GdUnit4;
using Godot;
using Systems;
using static GdUnit4.Assertions;

/// <summary>
///   Tests the ATP budget and its breakdown as calculated by the process system.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class EnergyBalanceTests
{
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public void FullBalance_AccountsForProcessAndMovementCosts(bool includeMovement, bool isPlayerSpecies)
    {
        const float specialization = 1.5f;
        const float playerEnergyCostMultiplier = 0.8f;

        var parameters = SimulationParameters.Instance;
        var cytoplasm = parameters.GetOrganelleType("cytoplasm");
        var toxinVacuole = parameters.GetOrganelleType("oxytoxy");
        var flagellum = parameters.GetOrganelleType("flagellum");
        var cilia = parameters.GetOrganelleType("cilia");
        var organelles = new OrganelleTemplate[]
        {
            new(cytoplasm, new Hex(0, 0), 0),
            new(cytoplasm, new Hex(0, 1), 0),
            new(toxinVacuole, new Hex(0, -1), 0),
            new(flagellum, new Hex(1, 0), 0),
            new(flagellum, new Hex(-1, 0), 0),
            new(cilia, new Hex(-1, 1), 0),
        };

        var biome = CreateBiome(1.0f);
        var tolerances = new ResolvedMicrobeTolerances
        {
            ProcessSpeedModifier = 0.5f,
            OsmoregulationModifier = 1.25f,
            SliderOsmoregulationAdditionalModifier = 0.25f,
        };

        var settings = new WorldGenerationSettings
        {
            Difficulty = new CustomDifficulty { EnergyCostMultiplier = playerEnergyCostMultiplier },
        };

        var membrane = parameters.GetMembrane("cellulose");

        var result = new EnergyBalanceInfoFull();
        result.SetupTrackingForRequiredCompounds();

        // The flagella on opposite sides of the cell can only contribute one at a time to this direction.
        var direction = -Hex.AxialToCartesian(new Hex(1, 0)).Normalized();
        ProcessSystem.ComputeEnergyBalanceFull(organelles, biome, tolerances, specialization, membrane, direction,
            includeMovement, isPlayerSpecies, settings, CompoundAmountType.Current, null, result);

        var glycolysis = parameters.GetBioProcess("glycolysis_cytoplasm");
        var toxinSynthesis = parameters.GetBioProcess("cytotoxinSynthesis");
        var atp = SimulationParameters.GetCompound(Compound.ATP);
        var glucose = SimulationParameters.GetCompound(Compound.Glucose);
        var processSpeed = tolerances.ProcessSpeedModifier * specialization;
        var glycolysisRate = 2 * cytoplasm.Processes![glycolysis.InternalName] * processSpeed;
        var toxinSynthesisRate = toxinVacuole.Processes![toxinSynthesis.InternalName] * processSpeed;
        var production = glycolysis.Outputs[atp] * glycolysisRate;
        var toxinCost = toxinSynthesis.Inputs[atp] * toxinSynthesisRate;
        var energyCostMultiplier = isPlayerSpecies ? playerEnergyCostMultiplier : 1.0f;
        var hexCount = organelles.Sum(o => o.Definition.HexCount);
        var osmoregulation = Constants.ATP_COST_FOR_OSMOREGULATION * hexCount * membrane.OsmoregulationFactor *
            (tolerances.OsmoregulationModifier + tolerances.SliderOsmoregulationAdditionalModifier) *
            energyCostMultiplier;
        var baseMovement = Constants.BASE_MOVEMENT_ATP_COST * hexCount * energyCostMultiplier;
        var flagellumCost = Constants.FLAGELLA_ENERGY_COST * energyCostMultiplier;
        var ciliaCost = Constants.CILIA_ENERGY_COST * energyCostMultiplier;
        var movement = baseMovement + flagellumCost + ciliaCost;
        var stationaryConsumption = toxinCost + osmoregulation;
        var totalConsumption = stationaryConsumption + (includeMovement ? movement : 0);

        AssertThat(result.SpecializationFactor).IsEqual(specialization);
        AssertThat(result.TotalProduction).IsEqualApprox(production, 0.0001f);
        AssertThat(result.BaseMovement).IsEqualApprox(baseMovement, 0.0001f);
        AssertThat(result.Flagella).IsEqualApprox(includeMovement ? flagellumCost : 0, 0.0001f);
        AssertThat(result.Cilia).IsEqualApprox(includeMovement ? ciliaCost : 0, 0.0001f);
        AssertThat(result.Osmoregulation).IsEqualApprox(osmoregulation, 0.0001f);
        AssertThat(result.TotalConsumptionStationary).IsEqualApprox(stationaryConsumption, 0.0001f);
        AssertThat(result.TotalConsumption).IsEqualApprox(totalConsumption, 0.0001f);
        AssertThat(result.FinalBalanceStationary).IsEqualApprox(production - stationaryConsumption, 0.0001f);
        AssertThat(result.FinalBalance).IsEqualApprox(production - totalConsumption, 0.0001f);

        AssertThat(result.Production["cytoplasm"]).IsEqualApprox(production, 0.0001f);
        AssertThat(result.ProductionRequiresCompounds!["cytoplasm"][Compound.Glucose])
            .IsEqualApprox(glycolysis.Inputs[glucose] * glycolysisRate, 0.0001f);
        AssertThat(result.Consumption["oxytoxy"]).IsEqualApprox(toxinCost, 0.0001f);
        AssertThat(result.Consumption["osmoregulation"]).IsEqualApprox(osmoregulation, 0.0001f);
        AssertThat(result.Consumption.ContainsKey("baseMovement")).IsEqual(includeMovement);

        if (includeMovement)
        {
            AssertThat(result.TotalMovement).IsEqualApprox(movement, 0.0001f);
            AssertThat(result.Consumption["baseMovement"]).IsEqualApprox(baseMovement, 0.0001f);
            AssertThat(result.Consumption["flagellum"]).IsEqualApprox(flagellumCost, 0.0001f);
            AssertThat(result.Consumption["cilia"]).IsEqualApprox(ciliaCost, 0.0001f);
        }

        AssertThat(result.Consumption.Values.Sum()).IsEqualApprox(totalConsumption, 0.0001f);
    }

    [TestCase(CompoundAmountType.Current, 1.0f, 0.5f)]
    [TestCase(CompoundAmountType.Average, 1.0f, 0.25f)]
    [TestCase(CompoundAmountType.Current, 0.0f, 0.0f)]
    public void SimpleBalance_UsesRequestedOxygenLevelAndRequiresGlucose(CompoundAmountType amountType,
        float glucoseAmount, float expectedRespirationFraction)
    {
        var parameters = SimulationParameters.Instance;
        var mitochondrion = parameters.GetOrganelleType("mitochondrion");
        var organelles = new OrganelleTemplate[] { new(mitochondrion, new Hex(0, 0), 0) };
        var biome = CreateBiome(glucoseAmount);
        var respiration = parameters.GetBioProcess("respiration");
        var oxygen = SimulationParameters.GetCompound(Compound.Oxygen);
        var fullSpeedOxygen = respiration.Inputs[oxygen];

        biome.CurrentCompoundAmounts[Compound.Oxygen] = new BiomeCompoundProperties { Ambient = fullSpeedOxygen / 2 };
        biome.AverageCompounds[Compound.Oxygen] = new BiomeCompoundProperties { Ambient = fullSpeedOxygen / 4 };

        var tolerances = new ResolvedMicrobeTolerances { ProcessSpeedModifier = 1, OsmoregulationModifier = 1 };
        var membrane = parameters.GetMembrane("single");
        var settings = new WorldGenerationSettings();
        var microbeBalance = new EnergyBalanceInfoSimple();
        var cellTypeBalance = new EnergyBalanceInfoSimple();

        ProcessSystem.ComputeEnergyBalanceSimple(organelles, biome, tolerances, 1.0f, membrane, Vector3.Zero,
            false, false, settings, amountType, null, microbeBalance);
        ProcessSystem.ComputeEnergyBalanceSimple((IReadOnlyCollection<IReadOnlyOrganelleTemplate>)organelles, biome,
            tolerances, 1.0f, membrane, Vector3.Zero, false, false, settings, amountType, null, cellTypeBalance);

        var atp = SimulationParameters.GetCompound(Compound.ATP);
        var production = respiration.Outputs[atp] * mitochondrion.Processes![respiration.InternalName] *
            expectedRespirationFraction;
        var osmoregulation = Constants.ATP_COST_FOR_OSMOREGULATION * mitochondrion.HexCount *
            membrane.OsmoregulationFactor;
        var baseMovement = Constants.BASE_MOVEMENT_ATP_COST * mitochondrion.HexCount;

        foreach (var balance in new[] { microbeBalance, cellTypeBalance })
        {
            AssertThat(balance.TotalProduction).IsEqualApprox(production, 0.0001f);
            AssertThat(balance.BaseMovement).IsEqualApprox(baseMovement, 0.0001f);
            AssertThat(balance.TotalConsumption).IsEqualApprox(osmoregulation, 0.0001f);
            AssertThat(balance.FinalBalance).IsEqualApprox(production - osmoregulation, 0.0001f);
        }
    }

    private static BiomeConditions CreateBiome(float glucoseAmount)
    {
        return new BiomeConditions(new Dictionary<Compound, BiomeCompoundProperties>
        {
            [Compound.Glucose] = new() { Amount = glucoseAmount },
        }, null, null, null, null)
        {
            Chunks = new Dictionary<string, ChunkConfiguration>(),
        };
    }
}
