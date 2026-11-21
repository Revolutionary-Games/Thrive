using AutoEvo;
using GdUnit4;
using Systems;
using static GdUnit4.Assertions;
using static SimulationCacheTestFixtures;

[TestSuite]
[RequireGodotRuntime]
public class SimulationCachePredationScoreTests
{
    [TestCase("cellulose", Constants.CELLULASE_ENZYME, false)]
    [TestCase("chitin", Constants.CHITINASE_ENZYME, false)]
    [TestCase("cellulose", Constants.CELLULASE_ENZYME, true)]
    [TestCase("chitin", Constants.CHITINASE_ENZYME, true)]
    public void SingleCellMulticellularPreyRequiresItsMembraneEnzyme(string membrane, string enzyme,
        bool addUnusedCellType)
    {
        var cellType = CreateCellType("Prey", membrane, CreateOrganelle("cytoplasm", new Hex(0, 0)));
        var prey = CreateMulticellular(30, "SingleCellPrey", (cellType, new Hex(0, 0)));
        if (addUnusedCellType)
        {
            // An unplaced type must not become the first selected real cell.
            prey.ModifiableCellTypes.Insert(0,
                CreateCellType("Unused", "single", CreateOrganelle("cytoplasm", new Hex(0, 0))));
            prey.OnEdited();
        }

        var cache = CreateCache();
        AssertThat(prey.EditorCells.Count).IsEqual(1);
        AssertThat(cache.GetBaseHexSizeForSpecies(prey)).IsEqual(cache.GetBaseHexSizeForCellType(cellType));
        AssertThat(cellType.MembraneType.DissolverEnzyme).IsEqual(enzyme);

        var predator = CreateMembraneTestPredator(31);
        AssertThat(AssertPredationLifecycle(predator, prey)).IsEqual(0.0f);

        // A matching enzyme makes this same prey digestible at the public score seam.
        var equippedPredator = CreateMembraneTestPredator(32, enzyme);
        var score = CalculatePredationScore(equippedPredator, prey);
        AssertThat(float.IsFinite(score)).IsTrue();
        AssertThat(score).IsGreater(0.0f);
    }

    [TestCase("cellulose", "single")]
    [TestCase("single", "cellulose")]
    public void MulticellularPreyUsesSmallerCellsMembraneEnzyme(string largerMembrane, string smallerMembrane)
    {
        var largerCell = CreateCellType("Larger", largerMembrane,
            CreateOrganelle("cytoplasm", new Hex(0, 0)), CreateOrganelle("cytoplasm", new Hex(1, 0)));
        var smallerCell = CreateCellType("Smaller", smallerMembrane,
            CreateOrganelle("cytoplasm", new Hex(0, 0)));
        var prey = CreateMulticellular(33, "DifferentSizes",
            (largerCell, new Hex(0, 0)), (smallerCell, new Hex(2, 0)));
        var cache = CreateCache();
        AssertThat(cache.GetBaseHexSizeForCellType(smallerCell))
            .IsLess(cache.GetBaseHexSizeForCellType(largerCell));

        var predator = CreateMembraneTestPredator(34);
        var score = CalculatePredationScore(predator, prey);
        AssertThat(float.IsFinite(score)).IsTrue();
        if (smallerMembrane == "single")
        {
            AssertThat(score).IsGreater(0.0f);
        }
        else
        {
            AssertThat(AssertPredationLifecycle(predator, prey)).IsEqual(0.0f);
        }
    }

    [TestCase]
    public void PreySlimeJetPropulsionReducesCatchability()
    {
        var controlPredator = CreateSlimeJetPredator(11);
        var changedPredator = CreateSlimeJetPredator(12);
        var preyWithoutSlimeJet = CreatePrey(13, false);
        var preyWithSlimeJet = CreatePrey(14, true);
        var cache = CreateCache();
        var preyWithoutSlimeJetRawScores = cache.GetPredationToolsRawScores(preyWithoutSlimeJet);
        var preyWithSlimeJetRawScores = cache.GetPredationToolsRawScores(preyWithSlimeJet);

        // Slime on both predators bypasses the prey's immobilising defence, leaving the catchability path.
        AssertThat(cache.GetPredationToolsRawScores(controlPredator).SlimeJetScore).IsGreater(0.0f);
        AssertThat(cache.GetPredationToolsRawScores(changedPredator).SlimeJetScore).IsGreater(0.0f);
        AssertThat(preyWithoutSlimeJetRawScores.SlimeJetScore).IsEqual(0.0f);
        AssertThat(preyWithSlimeJetRawScores.SlimeJetScore).IsGreater(0.0f);
        AssertThat(preyWithoutSlimeJetRawScores.MucocystsScore).IsEqual(0.0f);
        AssertThat(preyWithSlimeJetRawScores.MucocystsScore).IsEqual(0.0f);

        // Replacing the organelle also changes mass and metabolism; this fixture checks the resulting catchability.
        var (baseline, propelledScore) =
            AssertScorePair(controlPredator, preyWithoutSlimeJet, changedPredator, preyWithSlimeJet);
        AssertThat(baseline).IsGreater(0.0f);
        AssertThat(propelledScore).IsGreater(0.0f).IsLess(baseline);
    }

    /// <summary>
    ///   Channel inhibitor reduces an energy-limited attacker's score and helps the carrier hunt the same target.
    ///   When the prey has maximum fear, the inhibitor and the inactive toxin give equal predation scores.
    /// </summary>
    [TestCase]
    public void PreyChannelInhibitorDefendsAgainstEnergyLimitedPredator()
    {
        var predator = CreateEnergyLimitedMicrobe(20);
        var channelPrey = CreateInhibitorMicrobe(21, ToxinType.ChannelInhibitor);
        var controlPrey = CreateInhibitorMicrobe(22, ToxinType.OxygenMetabolismInhibitor);
        AssertChannelInhibitorScenario(predator, channelPrey, controlPrey);

        var controlScore = CalculatePredationScore(predator, controlPrey);
        var defendedScore = CalculatePredationScore(predator, channelPrey);

        AssertThat(float.IsFinite(controlScore) && float.IsFinite(defendedScore)).IsTrue();
        AssertThat(controlScore).IsGreater(0.0f);
        AssertThat(defendedScore).IsGreater(0.0f).IsLess(controlScore);

        var biome = CreateBiome();
        var attackCache = CreateCache();
        var controlAttack = attackCache.GetPredationScore(controlPrey, predator, biome);
        AssertThat(controlAttack).IsGreater(0.0f);
        AssertThat(attackCache.GetPredationScore(channelPrey, predator, biome)).IsGreater(controlAttack);

        channelPrey.ModifiableBehaviour.Fear = Constants.MAX_SPECIES_FEAR;
        controlPrey.ModifiableBehaviour.Fear = Constants.MAX_SPECIES_FEAR;
        var fearCache = CreateCache();
        var fleeingControl = fearCache.GetPredationScore(predator, controlPrey, biome);
        AssertThat(fleeingControl).IsGreater(0.0f);
        AssertThat(fearCache.GetPredationScore(predator, channelPrey, biome)).IsEqual(fleeingControl);
    }

    [TestCase]
    public void ChannelInhibitorPredationScoreIncreasesAsMovementFundingDecreases()
    {
        var fullyFundedMovementScore = CalculateChannelInhibitorPredationScore(24.0f);
        var halfFundedMovementScore = CalculateChannelInhibitorPredationScore(14.0f);
        var unfundedMovementScore = CalculateChannelInhibitorPredationScore(4.0f);

        AssertThat(float.IsFinite(fullyFundedMovementScore) && float.IsFinite(halfFundedMovementScore) &&
            float.IsFinite(unfundedMovementScore)).IsTrue();
        AssertThat(fullyFundedMovementScore < halfFundedMovementScore &&
            halfFundedMovementScore < unfundedMovementScore).IsTrue();
    }

    [TestCase]
    public void ChannelInhibitorMovementFundingUsesFullStationaryConsumption()
    {
        var allStationaryConsumptionIsOsmoregulation =
            CalculateChannelInhibitorPredationScore(14.0f, 5.0f, 5.0f);
        var stationaryConsumptionIncludesOtherProcesses =
            CalculateChannelInhibitorPredationScore(14.0f, 0.0f, 5.0f);

        AssertThat(float.IsFinite(allStationaryConsumptionIsOsmoregulation) &&
            float.IsFinite(stationaryConsumptionIncludesOtherProcesses)).IsTrue();
        AssertThat(stationaryConsumptionIncludesOtherProcesses)
            .IsEqual(allStationaryConsumptionIsOsmoregulation);
    }

    [TestCase]
    public void ChannelInhibitorDoesNotSlowSpeciesWithoutMovementCost()
    {
        var inhibitedBelowStationaryConsumption =
            CalculateChannelInhibitorPredationScore(4.0f, 0.0f, 5.0f, 0.0f);
        var productionAboveStationaryConsumption =
            CalculateChannelInhibitorPredationScore(12.0f, 0.0f, 5.0f, 0.0f);

        AssertThat(float.IsFinite(inhibitedBelowStationaryConsumption) &&
            float.IsFinite(productionAboveStationaryConsumption)).IsTrue();
        AssertThat(inhibitedBelowStationaryConsumption)
            .IsEqual(productionAboveStationaryConsumption);
    }

    [TestCase]
    public void ChannelInhibitorSlowsSprintEscapeSpeed()
    {
        // Normal movement stays catchable, while sprinting lets the prey escape.
        const float preyFear = Constants.MAX_SPECIES_FEAR * 0.25f;
        var fullyFundedMovementWithoutSprint = CalculateChannelInhibitorPredationScore(24.0f, preyFear: preyFear);
        var unfundedMovementWithoutSprint = CalculateChannelInhibitorPredationScore(4.0f, preyFear: preyFear);
        var fullyFundedMovementWithSprint =
            CalculateChannelInhibitorPredationScore(24.0f, finalBalance: 1.0f, preyFear: preyFear);
        var unfundedMovementWithSprint =
            CalculateChannelInhibitorPredationScore(4.0f, finalBalance: 1.0f, preyFear: preyFear);

        var inhibitionBenefitWithoutSprint = unfundedMovementWithoutSprint - fullyFundedMovementWithoutSprint;
        var inhibitionBenefitWithSprint = unfundedMovementWithSprint - fullyFundedMovementWithSprint;

        AssertThat(inhibitionBenefitWithSprint > inhibitionBenefitWithoutSprint).IsTrue();
    }

    private static MicrobeSpecies CreateMembraneTestPredator(uint id, string? enzyme = null)
    {
        var predator = CreateMicrobe(id, "MembranePredator", "single",
            "cytoplasm", "cytoplasm", "cytoplasm", "cytoplasm");
        predator.IsBacteria = false;
        foreach (var organelle in predator.Organelles)
            organelle.Position = new Hex(organelle.Position.Q / 4, organelle.Position.R / 4);

        if (enzyme != null)
        {
            var lysosome = CreateOrganelle("lysosome", new Hex(0, 1));
            lysosome.ModifiableUpgrades = new OrganelleUpgrades
            {
                CustomUpgradeData = new LysosomeUpgrades(SimulationParameters.Instance.GetEnzyme(enzyme)),
            };
            predator.Organelles.Add(lysosome);
        }

        predator.OnEdited();
        return predator;
    }

    private static float CalculatePredationScore(Species predator, Species prey)
    {
        return AssertPredationLifecycle(predator, prey);
    }

    private static float CalculateChannelInhibitorPredationScore(float totalProduction, float osmoregulation = 2.0f,
        float stationaryConsumption = 2.0f, float movementConsumption = 10.0f, float finalBalance = 0.0f,
        float preyFear = Constants.MAX_SPECIES_FEAR)
    {
        var predator = CreateChannelInhibitorPredator(9);
        var prey = CreateMicrobe(10, "ChannelInhibitorPrey", "single", "cytoplasm");
        prey.ModifiableBehaviour.Fear = preyFear;
        prey.ModifiableBehaviour.Aggression = 0;
        var biome = CreateBiome();
        return AssertPredationLifecycle(predator, prey, biome, (cache, conditions) =>
        {
            var rawScores = cache.GetPredationToolsRawScores(predator);
            AssertThat(rawScores.ChannelInhibitorScore).IsGreater(0.0f);
            AssertThat(rawScores.MacrolideScore).IsEqual(0.0f);

            // Reapply the public energy inputs to each cold cache and Clear; hits preserve them.
            var preyEnergyBalance = cache.GetEnergyBalanceForSpecies(prey, conditions);
            preyEnergyBalance.TotalProduction = totalProduction;
            preyEnergyBalance.Osmoregulation = osmoregulation;
            preyEnergyBalance.TotalConsumptionStationary = stationaryConsumption;
            preyEnergyBalance.TotalMovement = movementConsumption;
            preyEnergyBalance.TotalConsumption = stationaryConsumption + movementConsumption;
            preyEnergyBalance.FinalBalance = finalBalance;
            preyEnergyBalance.FinalBalanceStationary = 0.0f;
        });
    }

    private static MicrobeSpecies CreateChannelInhibitorPredator(uint id)
    {
        var simulationParameters = SimulationParameters.Instance;
        var species = CreateMicrobe(id, "ChannelInhibitorPredator", "single", "cytoplasm", "pilus");
        species.Organelles.Add(new OrganelleTemplate(simulationParameters.GetOrganelleType("oxytoxy"),
            new Hex(8, 0), 0)
        {
            ModifiableUpgrades = new OrganelleUpgrades
            {
                ModifiableUnlockedFeatures = [ToxinUpgradeNames.CHANNEL_INHIBITOR_UPGRADE_NAME],
                CustomUpgradeData = new ToxinUpgrades(ToxinType.ChannelInhibitor, 0.0f),
            },
        });
        species.OnEdited();

        return species;
    }

    private static MicrobeSpecies CreateSlimeJetPredator(uint id)
    {
        var simulationParameters = SimulationParameters.Instance;
        var species = CreateMicrobe(id, "SlimeJetPredator", "cellulose", "cytoplasm");
        species.Organelles.Add(new OrganelleTemplate(simulationParameters.GetOrganelleType("pilus"),
            new Hex(0, -4), 0));
        species.Organelles.Add(new OrganelleTemplate(simulationParameters.GetOrganelleType("slimeJet"),
            new Hex(0, 4), 0));
        species.OnEdited();

        return species;
    }

    private static MicrobeSpecies CreatePrey(uint id, bool hasSlimeJet)
    {
        var simulationParameters = SimulationParameters.Instance;
        var species = CreateMicrobe(id, hasSlimeJet ? "SlimeJetPrey" : "NoSlimeJetPrey", "cellulose",
            "cytoplasm");
        species.Organelles.Add(new OrganelleTemplate(
            simulationParameters.GetOrganelleType(hasSlimeJet ? "slimeJet" : "chemoreceptor"), new Hex(0, 4), 0));
        species.ModifiableBehaviour.Fear = 0;
        species.OnEdited();

        return species;
    }

    private static MicrobeSpecies CreateEnergyLimitedMicrobe(uint id)
    {
        // Non-producing organelles make inhibition cross the osmoregulation threshold.
        var species = CreateMicrobe(id, "EnergyLimited", "cellulose", "cytoplasm",
            "chemoreceptor", "chemoreceptor", "chemoreceptor", "chemoreceptor");
        species.Organelles.Add(new OrganelleTemplate(SimulationParameters.Instance.GetOrganelleType("pilus"),
            new Hex(0, -4), 0));
        species.ModifiableBehaviour.Fear = 0;
        species.ModifiableBehaviour.Aggression = Constants.MAX_SPECIES_AGGRESSION;
        species.ModifiableBehaviour.Activity = Constants.MAX_SPECIES_ACTIVITY;
        species.ModifiableBehaviour.Opportunism = Constants.MAX_SPECIES_OPPORTUNISM;

        // Keep the connected layout compact enough for both species to turn towards their target.
        foreach (var organelle in species.Organelles)
            organelle.Position = new Hex(organelle.Position.Q / 4, organelle.Position.R / 4);

        species.OnEdited();
        return species;
    }

    private static MicrobeSpecies CreateInhibitorMicrobe(uint id, ToxinType toxinType)
    {
        // Both variants have identical geometry, production and behaviour; only toxin type differs.
        var species = CreateMicrobe(id, "Inhibitor", "cellulose",
            "cytoplasm", "cytoplasm", "cytoplasm", "cytoplasm");
        species.Organelles.Add(new OrganelleTemplate(SimulationParameters.Instance.GetOrganelleType("pilus"),
            new Hex(0, -4), 0));
        species.Organelles.Add(new OrganelleTemplate(SimulationParameters.Instance.GetOrganelleType("oxytoxy"),
            new Hex(0, 4), 0)
        {
            ModifiableUpgrades = new OrganelleUpgrades
            {
                ModifiableUnlockedFeatures = [ToxinUpgradeNames.ToxinNameFromType(toxinType)],
                CustomUpgradeData = new ToxinUpgrades(toxinType, 1.0f),
            },
        });
        species.ModifiableBehaviour.Fear = 0;
        species.ModifiableBehaviour.Aggression = Constants.MAX_SPECIES_AGGRESSION;
        species.ModifiableBehaviour.Activity = Constants.MAX_SPECIES_ACTIVITY;
        species.ModifiableBehaviour.Opportunism = Constants.MAX_SPECIES_OPPORTUNISM;

        // Keep the connected layout compact enough for both species to turn towards their target.
        foreach (var organelle in species.Organelles)
            organelle.Position = new Hex(organelle.Position.Q / 4, organelle.Position.R / 4);

        species.OnEdited();
        return species;
    }

    private static void AssertChannelInhibitorScenario(MicrobeSpecies target, MicrobeSpecies channelSpecies,
        MicrobeSpecies controlSpecies)
    {
        var cache = new SimulationCache(new WorldGenerationSettings { Seed = 1 });
        var biome = SimulationParameters.Instance.GetBiome("aavolcanic_vent").Conditions;
        biome.Compounds.TryGetValue(Compound.Oxygen, out var oxygen);

        // Zero oxygen disables the control toxin; cell walls exclude anti-engulfment toxin defence.
        AssertThat(oxygen.Ambient).IsEqual(0.0f);
        AssertThat(target.MembraneType.CanEngulf || channelSpecies.MembraneType.CanEngulf ||
            controlSpecies.MembraneType.CanEngulf).IsFalse();
        AssertThat(cache.GetPredationToolsRawScores(target).ChannelInhibitorScore).IsEqual(0.0f);
        var channelScores = cache.GetPredationToolsRawScores(channelSpecies);
        var controlScores = cache.GetPredationToolsRawScores(controlSpecies);
        AssertThat(channelScores.ChannelInhibitorScore).IsGreater(0.0f);
        AssertThat(channelScores.OxygenMetabolismInhibitorScore).IsEqual(0.0f);
        AssertThat(controlScores.ChannelInhibitorScore).IsEqual(0.0f);
        AssertThat(controlScores.OxygenMetabolismInhibitorScore).IsGreater(0.0f);
        AssertThat(channelScores.AverageToxicity).IsEqual(controlScores.AverageToxicity);

        var balance = cache.GetEnergyBalanceForSpecies(target, biome);
        var inhibitedProduction = balance.TotalProduction * (1 - Constants.CHANNEL_INHIBITOR_ATP_DEBUFF *
            MicrobeEmissionSystem.ToxinAmountMultiplierFromToxicity(channelScores.AverageToxicity,
                ToxinType.ChannelInhibitor));
        AssertThat(inhibitedProduction).IsLess(balance.Osmoregulation);

        // The toxin carrier must not gain another offensive contribution from its own energy deficit.
        var channelBalance = cache.GetEnergyBalanceForSpecies(channelSpecies, biome);
        AssertThat(channelBalance.TotalProduction).IsGreaterEqual(channelBalance.Osmoregulation);
    }
}
