using System;
using AutoEvo;
using GdUnit4;
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

    [TestCase(false, ToxinType.Oxytoxy, 9.939762f, 9.953802f)]
    [TestCase(false, ToxinType.Cytotoxin, 9.939762f, 9.303881f)]
    [TestCase(false, ToxinType.Macrolide, 9.939762f, 42.95154f)]
    [TestCase(false, ToxinType.ChannelInhibitor, 9.939762f, 8546.542f)]
    [TestCase(false, ToxinType.OxygenMetabolismInhibitor, 9.939762f, 9.93459f)]
    [TestCase(true, ToxinType.Oxytoxy, 0.31303066f, 0.33341095f)]
    [TestCase(true, ToxinType.Cytotoxin, 0.31303066f, 0.32250232f)]
    [TestCase(true, ToxinType.Macrolide, 0.31303066f, 14.746543f)]
    [TestCase(true, ToxinType.ChannelInhibitor, 0.31303066f, 892.47577f)]
    [TestCase(true, ToxinType.OxygenMetabolismInhibitor, 0.31303066f, 0.3331734f)]
    public void SinglePredatorToxin(bool multicellular, ToxinType toxin,
        float expectedControl, float expectedArmed)
    {
        // The pilus keeps the catch path reachable for macrolide, which cannot kill on its own.
        // Replace one cytoplasm with a toxin organelle at the same hex: adding it would recenter the pilus.
        var control = CreateRoleSpecies(221, multicellular, CreateOrganelle("pilus", new Hex(0, -4)),
            CreateOrganelle("cytoplasm", new Hex(4, 0)));
        var armed = CreateRoleSpecies(222, multicellular, CreateOrganelle("pilus", new Hex(0, -4)),
            CreateToxin(new Hex(4, 0), toxin));
        var prey = CreateRoleSpecies(223, multicellular);
        prey.ModifiableBehaviour.Fear = Constants.MAX_SPECIES_FEAR * 0.25f;
        prey.ModifiableBehaviour.Aggression = 0;
        var initial = GetRaw(control);
        var changed = GetRaw(armed);
        var expectedRaw = toxin switch
        {
            ToxinType.Oxytoxy => initial with { OxytoxyScore = changed.OxytoxyScore },
            ToxinType.Cytotoxin => initial with { CytotoxinScore = changed.CytotoxinScore },
            ToxinType.Macrolide => initial with { MacrolideScore = changed.MacrolideScore },
            ToxinType.ChannelInhibitor => initial with { ChannelInhibitorScore = changed.ChannelInhibitorScore },
            ToxinType.OxygenMetabolismInhibitor =>
                initial with { OxygenMetabolismInhibitorScore = changed.OxygenMetabolismInhibitorScore },
            _ => throw new ArgumentOutOfRangeException(nameof(toxin)),
        };
        AssertRawBits(changed, expectedRaw);
        AssertThat(changed.OxytoxyScore + changed.CytotoxinScore + changed.MacrolideScore +
            changed.ChannelInhibitorScore + changed.OxygenMetabolismInhibitorScore > 0).IsTrue();

        var biome = CreateBiome();
        var oxygen = biome.Compounds[Compound.Oxygen];
        oxygen.Ambient = 1;
        biome.ChangeableCompounds[Compound.Oxygen] = oxygen;
        AssertScorePair(control, prey, armed, prey,
            expectedControl, expectedArmed, biome);
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

    [TestCase(false, 0.17904808f, 0.16923125f)]
    [TestCase(true, 0.17904808f, 0.16923125f)]
    public void PredatorToxicityAtFixedToxinTypeAndAmount(bool multicellular,
        float expectedControl, float expectedToxic)
    {
        // Toxicity changes both damage and hit chance, so characterize the final result without choosing a direction.
        var control = CreateRoleSpecies(251, multicellular, CreateToxin(new Hex(4, 0), ToxinType.Cytotoxin));
        var toxic = CreateRoleSpecies(252, multicellular, CreateToxin(new Hex(4, 0), ToxinType.Cytotoxin, 0.25f));
        var prey = CreateRoleSpecies(253, false);
        var initial = GetRaw(control);
        var changed = GetRaw(toxic);
        AssertBits(initial.AverageToxicity, 0);
        AssertBits(changed.AverageToxicity, 0.25f);
        AssertRawBits(changed, initial with
        {
            AverageToxicity = changed.AverageToxicity,
            CytotoxinScore = changed.CytotoxinScore,
        });

        AssertScorePair(control, prey, toxic, prey, expectedControl, expectedToxic);
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
