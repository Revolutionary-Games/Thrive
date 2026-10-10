using System;
using AutoEvo;
using GdUnit4;
using Systems;
using static GdUnit4.Assertions;
using static SimulationCacheTestFixtures;

/// <summary>
///   Public capability and defence contracts against a baseline computed in the same run.
///   Tool routing guards the mechanism; organelle changes can also affect geometry, mass and metabolism.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class SimulationCachePredationRoleTests
{
    [TestCase(false, false, false)]
    [TestCase(false, false, true)]
    [TestCase(false, true, false)]
    [TestCase(false, true, true)]
    [TestCase(true, false, false)]
    [TestCase(true, false, true)]
    [TestCase(true, true, false)]
    [TestCase(true, true, true)]
    public void OffensivePilusAcrossSpeciesPairings(bool multicellularPredator, bool multicellularPrey,
        bool injectisome)
    {
        var control = CreateRoleSpecies(201, multicellularPredator);
        var armed = CreateRoleSpecies(202, multicellularPredator,
            CreateOrganelle("pilus", new Hex(0, -4), injectisome ? Constants.PILUS_INJECTISOME_UPGRADE_NAME : null));
        var controlPrey = CreateRoleSpecies(203, multicellularPrey);
        var armedPrey = CreateRoleSpecies(204, multicellularPrey);
        AssertCannotEngulf(control);
        AssertCannotEngulf(armed);
        var initial = GetRaw(control);
        var changed = GetRaw(armed);
        AssertThat(initial.PilusScore + initial.InjectisomeScore + initial.OxytoxyScore + initial.CytotoxinScore +
                initial.MacrolideScore + initial.ChannelInhibitorScore + initial.OxygenMetabolismInhibitorScore)
            .IsEqual(0.0f);
        AssertRawBits(changed,
            injectisome ?
                initial with { InjectisomeScore = changed.InjectisomeScore } :
                initial with { PilusScore = changed.PilusScore });
        AssertThat(injectisome ? changed.InjectisomeScore : changed.PilusScore).IsGreater(0.0f);

        // Cell walls and absent attack tools make the baseline unable to predate.
        // The forward tool opens a physical attack path despite its additional mass and metabolic cost.
        var (baseline, armedScore) = AssertScorePair(control, controlPrey, armed, armedPrey);
        AssertThat(baseline).IsEqual(0.0f);
        AssertThat(armedScore).IsGreater(baseline);
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void RearPilusOnFleeingPrey(bool multicellular, bool injectisome)
    {
        var controlPredator = CreateRoleSpecies(211, false, CreateOrganelle("pilus", new Hex(0, -4)));
        var defendedPredator = CreateRoleSpecies(214, false, CreateOrganelle("pilus", new Hex(0, -4)));
        var control = CreateRoleSpecies(212, multicellular);
        var defended = CreateRoleSpecies(213, multicellular,
            CreateOrganelle("pilus", new Hex(0, 4), injectisome ? Constants.PILUS_INJECTISOME_UPGRADE_NAME : null));
        control.ModifiableBehaviour.Fear = defended.ModifiableBehaviour.Fear = Constants.MAX_SPECIES_FEAR;
        control.ModifiableBehaviour.Aggression = defended.ModifiableBehaviour.Aggression = 0;
        AssertCannotEngulf(controlPredator);
        AssertCannotEngulf(defendedPredator);
        AssertThat(control.Behaviour.Fear).IsEqual(Constants.MAX_SPECIES_FEAR);
        AssertThat(defended.Behaviour.Fear).IsEqual(Constants.MAX_SPECIES_FEAR);
        AssertThat(control.Behaviour.Aggression).IsEqual(0.0f);
        AssertThat(defended.Behaviour.Aggression).IsEqual(0.0f);
        var cache = CreateCache();
        AssertThat(cache.GetSpeedForSpecies(control)).IsGreater(0.0f);
        AssertThat(cache.GetSpeedForSpecies(defended)).IsGreater(0.0f);

        // The scoring rotation modifier must stay positive for rear-facing defence to work.
        AssertThat(1.5f - cache.GetRotationSpeedForSpecies(defended) * 1.45f).IsGreater(0.0f);
        var initial = GetRaw(control);
        var changed = GetRaw(defended);
        AssertRawBits(changed,
            injectisome ?
                initial with { DefensiveInjectisomeScore = changed.DefensiveInjectisomeScore } :
                initial with { DefensivePilusScore = changed.DefensivePilusScore });
        AssertThat(injectisome ? changed.DefensiveInjectisomeScore : changed.DefensivePilusScore).IsGreater(0.0f);
        AssertThat(changed.PilusScore + changed.InjectisomeScore).IsEqual(0.0f);

        // Fleeing prey can present the rear tool; adding it also changes mass and metabolism.
        var (baseline, defendedScore) = AssertScorePair(controlPredator, control, defendedPredator, defended);
        AssertThat(baseline).IsGreater(0.0f);
        AssertThat(defendedScore).IsGreater(0.0f).IsLess(baseline);
    }

    [TestCase(false, ToxinType.Oxytoxy)]
    [TestCase(false, ToxinType.Cytotoxin)]
    [TestCase(false, ToxinType.OxygenMetabolismInhibitor)]
    [TestCase(true, ToxinType.Oxytoxy)]
    [TestCase(true, ToxinType.Cytotoxin)]
    [TestCase(true, ToxinType.OxygenMetabolismInhibitor)]
    public void DirectToxinEnablesPredation(bool multicellular, ToxinType toxin)
    {
        // Replacing the same-position placeholder opens only the selected direct attack path.
        var control = CreateRoleSpecies(221, multicellular, CreateOrganelle("cytoplasm", new Hex(0, 1)));
        var armed = CreateRoleSpecies(222, multicellular, CreateToxin(new Hex(0, 1), toxin));
        var controlPrey = CreateRoleSpecies(223, multicellular);
        var armedPrey = CreateRoleSpecies(224, multicellular);
        SetHuntingBehaviour(control);
        SetHuntingBehaviour(armed);
        AssertCannotEngulf(control);
        AssertCannotEngulf(armed);
        var initial = GetRaw(control);
        var changed = GetRaw(armed);
        AssertNoTools(initial);
        var expectedRaw = toxin switch
        {
            ToxinType.Oxytoxy => initial with { OxytoxyScore = changed.OxytoxyScore },
            ToxinType.Cytotoxin => initial with { CytotoxinScore = changed.CytotoxinScore },
            ToxinType.OxygenMetabolismInhibitor =>
                initial with { OxygenMetabolismInhibitorScore = changed.OxygenMetabolismInhibitorScore },
            _ => throw new ArgumentOutOfRangeException(nameof(toxin)),
        };
        AssertRawBits(changed, expectedRaw);
        AssertThat(changed.OxytoxyScore + changed.CytotoxinScore + changed.OxygenMetabolismInhibitorScore)
            .IsGreater(0.0f);
        var biome = CreateBiome();
        var oxygen = biome.Compounds[Compound.Oxygen];
        oxygen.Ambient = 1;
        biome.ChangeableCompounds[Compound.Oxygen] = oxygen;
        AssertThat(biome.Compounds[Compound.Oxygen].Ambient).IsGreater(0.0f);
        AssertToxinEncounter(armed, armedPrey, CreateCache(), changed.AverageToxicity);
        AssertNoTools(GetRaw(controlPrey));

        var (baseline, armedScore) = AssertScorePair(control, controlPrey, armed, armedPrey, biome);
        AssertThat(baseline).IsEqual(0.0f);
        AssertThat(armedScore).IsGreater(baseline);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MacrolideImprovesPilusCaptureOfFleeingPrey(bool multicellular)
    {
        // Same carrier, geometry and toxicity: zero oxygen disables the control toxin's damage.
        var control = CreateRoleSpecies(225, multicellular, CreateOrganelle("pilus", new Hex(0, -1)),
            CreateToxin(new Hex(0, 1), ToxinType.Oxytoxy));
        var slowed = CreateRoleSpecies(226, multicellular, CreateOrganelle("pilus", new Hex(0, -1)),
            CreateToxin(new Hex(0, 1), ToxinType.Macrolide));
        var controlPrey = CreateRoleSpecies(227, multicellular);
        var slowedPrey = CreateRoleSpecies(228, multicellular);

        // Hold the same positive specialization input low enough to keep toxin exposure below saturation.
        SetSpecialization(control, 0.01f);
        SetSpecialization(slowed, 0.01f);
        SetHuntingBehaviour(control);
        SetHuntingBehaviour(slowed);
        controlPrey.ModifiableBehaviour.Fear = slowedPrey.ModifiableBehaviour.Fear =
            Constants.MAX_SPECIES_FEAR * 0.25f;
        controlPrey.ModifiableBehaviour.Aggression = slowedPrey.ModifiableBehaviour.Aggression = 0;
        AssertCannotEngulf(control);
        AssertCannotEngulf(slowed);
        var initial = GetRaw(control);
        var changed = GetRaw(slowed);
        AssertThat(initial.PilusScore).IsGreater(0.0f);
        AssertThat(initial.OxytoxyScore).IsGreater(0.0f);
        AssertThat(changed.MacrolideScore).IsGreater(0.0f);
        AssertRawBits(initial,
            EmptyTools() with { PilusScore = initial.PilusScore, OxytoxyScore = initial.OxytoxyScore });
        AssertRawBits(changed, initial with { OxytoxyScore = 0, MacrolideScore = changed.MacrolideScore });
        AssertNoTools(GetRaw(controlPrey));
        AssertNoTools(GetRaw(slowedPrey));
        var biome = CreateBiome();
        AssertThat(biome.Compounds[Compound.Oxygen].Ambient).IsEqual(0.0f);
        var cache = CreateCache();
        var predatorSpeed = cache.GetSpeedForSpecies(control);
        var preySpeed = cache.GetSpeedForSpecies(controlPrey);
        AssertBits(cache.GetSpeedForSpecies(slowed), predatorSpeed);
        AssertBits(cache.GetSpeedForSpecies(slowedPrey), preySpeed);
        AssertBits(cache.GetBaseHexSizeForSpecies(slowed), cache.GetBaseHexSizeForSpecies(control));
        AssertBits(cache.GetBaseHexSizeForSpecies(slowedPrey), cache.GetBaseHexSizeForSpecies(controlPrey));
        AssertBits(cache.GetRotationSpeedForSpecies(slowed), cache.GetRotationSpeedForSpecies(control));
        AssertBits(cache.GetRotationSpeedForSpecies(slowedPrey), cache.GetRotationSpeedForSpecies(controlPrey));
        AssertEnergyBits(cache.GetEnergyBalanceForSpecies(slowed, biome),
            cache.GetEnergyBalanceForSpecies(control, biome));
        AssertEnergyBits(cache.GetEnergyBalanceForSpecies(slowedPrey, biome),
            cache.GetEnergyBalanceForSpecies(controlPrey, biome));
        var hitProportion = AssertToxinEncounter(slowed, slowedPrey, cache, changed.AverageToxicity);
        AssertThat(preySpeed).IsGreater(0.0f);
        AssertThat(controlPrey.Behaviour.Fear).IsGreater(0.0f);
        AssertThat(slowedPrey.Behaviour.Fear).IsEqual(controlPrey.Behaviour.Fear);
        AssertThat(controlPrey.Behaviour.Aggression).IsEqual(0.0f);
        AssertThat(slowedPrey.Behaviour.Aggression).IsEqual(0.0f);
        var fleeingSpeed = preySpeed * controlPrey.Behaviour.Fear / Constants.MAX_SPECIES_FEAR;
        AssertThat(fleeingSpeed).IsGreater(0.0f).IsLess(predatorSpeed);
        var slowFactor = 1 - Constants.MACROLIDE_BASE_MOVEMENT_DEBUFF *
            MicrobeEmissionSystem.ToxinAmountMultiplierFromToxicity(changed.AverageToxicity, ToxinType.Macrolide);
        var slowedProportion = 1 - MathF.Exp(-Constants.AUTO_EVO_TOXIN_AFFECTED_PROPORTION_SCALING *
            changed.MacrolideScore * hitProportion);
        AssertThat(slowFactor).IsGreater(0.0f).IsLess(1.0f);
        AssertThat(slowedProportion).IsGreater(0.0f).IsLess(1.0f);
        AssertThat(fleeingSpeed * slowFactor).IsGreater(0.0f).IsLess(fleeingSpeed);

        // A strictly faster predator keeps both catch paths positive; the partial slowing is not saturated.
        // Fix sprint funding on every cold cache and Clear, while leaving a cache hit's inputs unchanged.
        var baseline = AssertPredationLifecycle(control, controlPrey, biome,
            (current, conditions) => DisableSprint(current, control, controlPrey, conditions));
        var slowedScore = AssertPredationLifecycle(slowed, slowedPrey, biome,
            (current, conditions) => DisableSprint(current, slowed, slowedPrey, conditions));
        AssertThat(baseline).IsGreater(0.0f);
        AssertThat(slowedScore).IsGreater(baseline);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MacrolideWithoutAKillingToolScoresZero(bool multicellular)
    {
        var predator = CreateRoleSpecies(229, multicellular, CreateToxin(new Hex(0, 1), ToxinType.Macrolide));
        var prey = CreateRoleSpecies(230, multicellular);
        AssertCannotEngulf(predator);
        var raw = GetRaw(predator);
        AssertThat(raw.MacrolideScore).IsGreater(0.0f);
        AssertRawBits(raw, EmptyTools() with { MacrolideScore = raw.MacrolideScore });
        AssertNoTools(GetRaw(prey));
        AssertThat(AssertPredationLifecycle(predator, prey)).IsEqual(0.0f);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ChannelInhibitorKillsOnlyBelowOsmoregulationFunding(bool multicellular)
    {
        var control = CreateRoleSpecies(291, multicellular, CreateToxin(new Hex(0, 1), ToxinType.ChannelInhibitor));
        var damaging = CreateRoleSpecies(292, multicellular, CreateToxin(new Hex(0, 1), ToxinType.ChannelInhibitor));
        var controlPrey = CreateRoleSpecies(293, multicellular);
        var damagedPrey = CreateRoleSpecies(294, multicellular);
        SetHuntingBehaviour(control);
        SetHuntingBehaviour(damaging);
        AssertCannotEngulf(control);
        AssertCannotEngulf(damaging);
        var raw = GetRaw(control);
        AssertThat(raw.ChannelInhibitorScore).IsGreater(0.0f);
        AssertRawBits(raw, EmptyTools() with { ChannelInhibitorScore = raw.ChannelInhibitorScore });
        AssertRawBits(GetRaw(damaging), raw);
        AssertNoTools(GetRaw(controlPrey));
        AssertNoTools(GetRaw(damagedPrey));
        AssertToxinEncounter(damaging, damagedPrey, CreateCache(), raw.AverageToxicity);
        var biome = CreateBiome();

        // Synthetic public DTO inputs isolate the damaging threshold, independently of natural metabolism.
        // Only ATP production differs; the callback also re-establishes the threshold after Clear.
        var baseline = AssertPredationLifecycle(control, controlPrey, biome,
            (current, conditions) => SetChannelTargetEnergy(current, controlPrey, conditions, 4.0f,
                raw.AverageToxicity, false));
        var damagingScore = AssertPredationLifecycle(damaging, damagedPrey, biome,
            (current, conditions) => SetChannelTargetEnergy(current, damagedPrey, conditions, 2.0f,
                raw.AverageToxicity, true));
        AssertThat(baseline).IsEqual(0.0f);
        AssertThat(damagingScore).IsGreater(baseline);
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void PreySlimeOrMucocystDefence(bool multicellular, bool mucocyst)
    {
        var controlPredator = CreateRoleSpecies(231, false, CreateOrganelle("pilus", new Hex(0, -4)));
        var defendedPredator = CreateRoleSpecies(234, false, CreateOrganelle("pilus", new Hex(0, -4)));
        var control = CreateRoleSpecies(232, multicellular);
        var defended = CreateRoleSpecies(233, multicellular,
            CreateOrganelle("slimeJet", new Hex(0, 4), mucocyst ? SlimeJetComponent.MUCOCYST_UPGRADE_NAME : null));
        AssertCannotEngulf(controlPredator);
        AssertCannotEngulf(defendedPredator);

        // Predators with slime jets ignore the prey's immobilising slime defence.
        AssertThat(GetRaw(controlPredator).SlimeJetScore).IsEqual(0.0f);
        AssertThat(GetRaw(defendedPredator).SlimeJetScore).IsEqual(0.0f);
        var initial = GetRaw(control);
        var changed = GetRaw(defended);
        AssertRawBits(changed,
            mucocyst ?
                initial with { MucocystsScore = changed.MucocystsScore } :
                initial with { SlimeJetScore = changed.SlimeJetScore });
        AssertThat(mucocyst ? changed.MucocystsScore : changed.SlimeJetScore).IsGreater(0.0f);
        AssertThat(mucocyst ? changed.SlimeJetScore : changed.MucocystsScore).IsEqual(0.0f);

        // The defence fixture includes the added organelle's mass and metabolic effects.
        var (baseline, defendedScore) = AssertScorePair(controlPredator, control, defendedPredator, defended);
        AssertThat(baseline).IsGreater(0.0f);
        AssertThat(defendedScore).IsGreater(0.0f).IsLess(baseline);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void PredatorPullingCiliaUpgrade(bool multicellular)
    {
        var control = CreateRoleSpecies(241, multicellular, CreateOrganelle("pilus", new Hex(0, -4)),
            CreateOrganelle("cilia", new Hex(4, 0)));
        var pulling = CreateRoleSpecies(242, multicellular, CreateOrganelle("pilus", new Hex(0, -4)),
            CreateOrganelle("cilia", new Hex(4, 0), CiliaComponent.CILIA_PULL_UPGRADE_NAME));
        var controlPrey = CreateRoleSpecies(243, false);
        var pullingPrey = CreateRoleSpecies(244, false);
        AssertCannotEngulf(control);
        AssertCannotEngulf(pulling);
        var initial = GetRaw(control);
        var changed = GetRaw(pulling);
        AssertRawBits(changed, initial with { PullingCiliaModifier = changed.PullingCiliaModifier });
        AssertThat(initial.PilusScore).IsGreater(0.0f);
        AssertBits(initial.PullingCiliaModifier, 1.0f);
        AssertThat(changed.PullingCiliaModifier).IsGreater(1.0f);

        // Matching organelles and positions keep a reachable pilus catch path; only the cilia upgrade changes.
        var (baseline, pullingScore) = AssertScorePair(control, controlPrey, pulling, pullingPrey);
        AssertThat(baseline).IsGreater(0.0f);
        AssertThat(pullingScore).IsGreater(baseline);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void FixedToxinTypePreservesToxicityInputAndCacheLifecycle(bool multicellular)
    {
        // Toxicity affects both damage and hit chance: no universal final-score direction is asserted.
        var controlToxin = CreateToxin(new Hex(0, 1), ToxinType.Cytotoxin);
        var changedToxin = CreateToxin(new Hex(0, 1), ToxinType.Cytotoxin, 0.25f);
        var control = CreateRoleSpecies(251, multicellular, controlToxin);
        var toxic = CreateRoleSpecies(252, multicellular, changedToxin);
        var controlPrey = CreateRoleSpecies(253, false);
        var toxicPrey = CreateRoleSpecies(254, false);
        AssertThat(controlToxin.GetActiveToxin()).IsEqual(ToxinType.Cytotoxin);
        AssertThat(changedToxin.GetActiveToxin()).IsEqual(ToxinType.Cytotoxin);
        AssertBits(controlToxin.GetActiveToxicity(), 0.0f);
        AssertBits(changedToxin.GetActiveToxicity(), 0.25f);
        var initial = GetRaw(control);
        var changed = GetRaw(toxic);
        AssertBits(initial.AverageToxicity, 0);
        AssertBits(changed.AverageToxicity, 0.25f);
        AssertThat(initial.CytotoxinScore).IsGreater(0.0f);
        AssertThat(changed.CytotoxinScore).IsGreater(0.0f);
        AssertRawBits(changed, initial with
        {
            AverageToxicity = changed.AverageToxicity,
            CytotoxinScore = changed.CytotoxinScore,
        });

        AssertScorePair(control, controlPrey, toxic, toxicPrey);
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void ClearRecomputesNestedToxinCacheForANewOpponent(bool multicellular, bool specialization)
    {
        var toxin = CreateToxin(new Hex(4, 0), ToxinType.Cytotoxin);
        var predator = CreateRoleSpecies(261, multicellular, toxin);
        var firstPrey = CreateRoleSpecies(262, false);
        var secondPrey = CreateRoleSpecies(263, false);
        var thirdPrey = CreateRoleSpecies(264, false);
        var biome = CreateBiome();
        var cache = CreateCache();
        var id = predator.ID;
        var epithet = predator.Epithet;
        var attempt = predator.AutoEvoAttemptCache;
        var initial = cache.GetPredationScore(predator, firstPrey, biome);

        // Mutate either toxicity or specialization, preserving the species' cache identity.
        if (specialization)
        {
            if (predator is MicrobeSpecies microbe)
            {
                microbe.CellTypeSpecializationBonus = 2.0f;
            }
            else
            {
                ((MulticellularSpecies)predator).ModifiableCellTypes[0].CellTypeSpecializationBonus = 2.0f;
            }
        }
        else
        {
            toxin.ModifiableUpgrades!.CustomUpgradeData = new ToxinUpgrades(ToxinType.Cytotoxin, 0.25f);
        }

        AssertThat(predator.ID).IsEqual(id);
        AssertThat(predator.Epithet).IsEqual(epithet);
        AssertThat(predator.AutoEvoAttemptCache).IsEqual(attempt);
        AssertBits(cache.GetPredationScore(predator, firstPrey, biome), initial);
        var staleForNewOpponent = cache.GetPredationScore(predator, secondPrey, biome);
        var fresh = CreateCache().GetPredationScore(predator, thirdPrey, biome);
        AssertDifferentBits(fresh, initial);
        AssertDifferentBits(staleForNewOpponent, fresh);

        // Specialization also changes uncached storage, so a new pair can mix stale and current data.
        if (!specialization)
            AssertBits(staleForNewOpponent, initial);

        cache.Clear();

        // This pair has never been queried: an outer-cache hit cannot conceal stale nested tool data.
        AssertBits(cache.GetPredationScore(predator, thirdPrey, biome), fresh);
        AssertBits(cache.GetPredationScore(predator, firstPrey, biome), fresh);
        AssertBits(cache.GetPredationScore(predator, thirdPrey, biome), fresh);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SelfPredationRemainsZero(bool multicellular)
    {
        var species = CreateRoleSpecies(271, multicellular, CreateOrganelle("pilus", new Hex(0, -4)));
        AssertThat(AssertPredationLifecycle(species, species)).IsEqual(0.0f);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void UnsupportedSpeciesIsRejectedInEitherRole(bool multicellular)
    {
        var supported = CreateRoleSpecies(281, multicellular);
        var unsupported = new MacroscopicSpecies(282, "Characterization", "Unsupported");
        var cache = CreateCache();
        var biome = CreateBiome();
        for (var i = 0; i < 2; ++i)
        {
            AssertThrown(() => cache.GetPredationScore(unsupported, supported, biome))
                .IsInstanceOf<ArgumentException>();
            AssertThrown(() => cache.GetPredationScore(supported, unsupported, biome))
                .IsInstanceOf<ArgumentException>();
            cache.Clear();
        }
    }

    private static SimulationCache.PredationToolsRawScores EmptyTools()
    {
        return default(SimulationCache.PredationToolsRawScores) with { PullingCiliaModifier = 1 };
    }

    private static void AssertNoTools(SimulationCache.PredationToolsRawScores raw)
    {
        AssertRawBits(raw, EmptyTools());
    }

    private static void SetSpecialization(Species species, float specialization)
    {
        if (species is MicrobeSpecies microbe)
        {
            microbe.CellTypeSpecializationBonus = specialization;
            AssertBits(microbe.CellTypeSpecializationBonus, specialization);
        }
        else
        {
            var cellType = ((MulticellularSpecies)species).ModifiableCellTypes[0];
            cellType.CellTypeSpecializationBonus = specialization;
            AssertBits(cellType.CellTypeSpecializationBonus, specialization);
        }
    }

    private static void SetHuntingBehaviour(Species species)
    {
        species.ModifiableBehaviour.Aggression = Constants.MAX_SPECIES_AGGRESSION;
        species.ModifiableBehaviour.Activity = Constants.MAX_SPECIES_ACTIVITY;
        species.ModifiableBehaviour.Opportunism = Constants.MAX_SPECIES_OPPORTUNISM;
    }

    private static float AssertToxinEncounter(Species predator, Species prey, SimulationCache cache, float toxicity)
    {
        AssertThat(predator.Behaviour.Aggression).IsGreater(0.0f);
        AssertThat(predator.Behaviour.Activity).IsGreater(0.0f);
        AssertThat(predator.Behaviour.Opportunism).IsGreater(0.0f);
        AssertThat(cache.GetSpeedForSpecies(predator)).IsGreater(0.0f);
        AssertThat(MathF.Min(1, 1.5f - cache.GetRotationSpeedForSpecies(predator) * 1.45f)).IsGreater(0.0f);
        var hitProportion = 1 - Constants.AUTO_EVO_SIZE_AFFECTED_PROJECTILE_MISS_FACTOR /
            MathF.Sqrt(cache.GetBaseHexSizeForSpecies(prey)) - toxicity / Constants.AUTO_EVO_TOXICITY_HIT_MODIFIER;
        AssertThat(hitProportion).IsGreater(0.0f);
        AssertNoTools(GetRaw(prey));
        return hitProportion;
    }

    private static void DisableSprint(SimulationCache cache, Species predator, Species prey, BiomeConditions biome)
    {
        cache.GetEnergyBalanceForSpecies(predator, biome).FinalBalance = 0;
        cache.GetEnergyBalanceForSpecies(prey, biome).FinalBalance = 0;
        AssertThat(cache.GetEnergyBalanceForSpecies(predator, biome).FinalBalance).IsEqual(0.0f);
        AssertThat(cache.GetEnergyBalanceForSpecies(prey, biome).FinalBalance).IsEqual(0.0f);
    }

    private static void SetChannelTargetEnergy(SimulationCache cache, Species prey, BiomeConditions biome,
        float production, float toxicity, bool damaging)
    {
        var energy = cache.GetEnergyBalanceForSpecies(prey, biome);
        energy.TotalProduction = production;
        energy.Osmoregulation = 2;
        energy.TotalConsumptionStationary = 2;
        energy.TotalMovement = 10;
        energy.TotalConsumption = 12;
        energy.FinalBalance = 0;
        energy.FinalBalanceStationary = 0;
        var inhibitedProduction = energy.TotalProduction * (1 - Constants.CHANNEL_INHIBITOR_ATP_DEBUFF *
            MicrobeEmissionSystem.ToxinAmountMultiplierFromToxicity(toxicity, ToxinType.ChannelInhibitor));
        if (damaging)
        {
            AssertThat(inhibitedProduction).IsLess(energy.Osmoregulation);
        }
        else
        {
            AssertThat(inhibitedProduction).IsGreaterEqual(energy.Osmoregulation);
        }
    }

    private static void AssertEnergyBits(EnergyBalanceInfoSimple actual, EnergyBalanceInfoSimple expected)
    {
        AssertBits(actual.BaseMovement, expected.BaseMovement);
        AssertBits(actual.Flagella, expected.Flagella);
        AssertBits(actual.Actomyosin, expected.Actomyosin);
        AssertBits(actual.Cilia, expected.Cilia);
        AssertBits(actual.TotalMovement, expected.TotalMovement);
        AssertBits(actual.Osmoregulation, expected.Osmoregulation);
        AssertBits(actual.TotalProduction, expected.TotalProduction);
        AssertBits(actual.TotalConsumption, expected.TotalConsumption);
        AssertBits(actual.TotalConsumptionStationary, expected.TotalConsumptionStationary);
        AssertBits(actual.FinalBalance, expected.FinalBalance);
        AssertBits(actual.FinalBalanceStationary, expected.FinalBalanceStationary);
    }

    private static Species CreateRoleSpecies(uint id, bool multicellular, params OrganelleTemplate[] tools)
    {
        // Identical cytoplasm core and membrane; callers provide only the organelle delta they need.
        if (multicellular)
        {
            var cellType = CreateCellType("Role", "cellulose", CreateOrganelle("cytoplasm", new Hex(0, 0)));
            foreach (var organelle in tools)
                cellType.ModifiableOrganelles.Add(organelle);

            return CreateMulticellular(id, "Role", (cellType, new Hex(0, 0)));
        }

        var species = CreateMicrobe(id, "Role", "cellulose", "cytoplasm");
        foreach (var organelle in tools)
            species.Organelles.Add(organelle);

        species.OnEdited();
        return species;
    }

    private static SimulationCache.PredationToolsRawScores GetRaw(Species species)
    {
        var cache = CreateCache();
        return species switch
        {
            MicrobeSpecies microbe => cache.GetPredationToolsRawScores(microbe),
            MulticellularSpecies multicellular => cache.GetPredationToolsRawScores(multicellular),
            _ => throw new ArgumentException("Unsupported test fixture", nameof(species)),
        };
    }

    private static void AssertCannotEngulf(Species species)
    {
        if (species is MicrobeSpecies microbe)
        {
            AssertThat(microbe.CanEngulf).IsFalse();
        }
        else
        {
            foreach (var cellType in ((MulticellularSpecies)species).CellTypes)
                AssertThat(cellType.MembraneType.CanEngulf).IsFalse();
        }
    }
}
