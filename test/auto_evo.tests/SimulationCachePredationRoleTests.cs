using System;
using AutoEvo;
using GdUnit4;
using static GdUnit4.Assertions;
using static SimulationCacheTestFixtures;

/// <summary>
///   Public final-score characterizations, with one organelle or upgrade delta per matched pair.
///   Raw scores only guard which tool is activated; final scores are the lasting regression seam.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class SimulationCachePredationRoleTests
{
    [TestCase(false, false, false, 0.0f, 15.367006f)]
    [TestCase(false, false, true, 0.0f, 46.101017f)]
    [TestCase(false, true, false, 0.0f, 28.612566f)]
    [TestCase(false, true, true, 0.0f, 85.8377f)]
    [TestCase(true, false, false, 0.0f, 0.6317793f)]
    [TestCase(true, false, true, 0.0f, 1.895338f)]
    [TestCase(true, true, false, 0.0f, 8.119033f)]
    [TestCase(true, true, true, 0.0f, 24.357098f)]
    public void OffensivePilusAcrossSpeciesPairings(bool multicellularPredator, bool multicellularPrey,
        bool injectisome, float expectedControl, float expectedArmed)
    {
        var control = CreateRoleSpecies(201, multicellularPredator);
        var armed = CreateRoleSpecies(202, multicellularPredator,
            CreateOrganelle("pilus", new Hex(0, -4), injectisome ? Constants.PILUS_INJECTISOME_UPGRADE_NAME : null));
        var prey = CreateRoleSpecies(203, multicellularPrey);
        var initial = GetRaw(control);
        var changed = GetRaw(armed);
        AssertRawBits(changed,
            injectisome ?
                initial with { InjectisomeScore = changed.InjectisomeScore } :
                initial with { PilusScore = changed.PilusScore });
        AssertThat(injectisome ? changed.InjectisomeScore > 0 : changed.PilusScore > 0).IsTrue();

        AssertScorePair(control, prey, armed, prey, expectedControl, expectedArmed);
    }

    [TestCase(false, false, 0.6317793f, 0.044224553f)]
    [TestCase(false, true, 0.6317793f, 0.022112276f)]
    [TestCase(true, false, 5.6066546f, 1.0482321f)]
    [TestCase(true, true, 5.6066546f, 0.52411604f)]
    public void RearPilusOnFleeingPrey(bool multicellular, bool injectisome,
        float expectedControl, float expectedDefended)
    {
        var predator = CreateRoleSpecies(211, false, CreateOrganelle("pilus", new Hex(0, -4)));
        var control = CreateRoleSpecies(212, multicellular);
        var defended = CreateRoleSpecies(213, multicellular,
            CreateOrganelle("pilus", new Hex(0, 4), injectisome ? Constants.PILUS_INJECTISOME_UPGRADE_NAME : null));
        control.ModifiableBehaviour.Fear = defended.ModifiableBehaviour.Fear = Constants.MAX_SPECIES_FEAR;
        control.ModifiableBehaviour.Aggression = defended.ModifiableBehaviour.Aggression = 0;
        var initial = GetRaw(control);
        var changed = GetRaw(defended);
        AssertRawBits(changed,
            injectisome ?
                initial with { DefensiveInjectisomeScore = changed.DefensiveInjectisomeScore } :
                initial with { DefensivePilusScore = changed.DefensivePilusScore });
        AssertThat(injectisome ? changed.DefensiveInjectisomeScore > 0 : changed.DefensivePilusScore > 0).IsTrue();

        AssertScorePair(predator, control, predator, defended,
            expectedControl, expectedDefended);
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

    [TestCase(false, false, 15.367006f, 0.11534659f)]
    [TestCase(false, true, 15.367006f, 0.88910663f)]
    [TestCase(true, false, 28.612566f, 10.973157f)]
    [TestCase(true, true, 28.612566f, 1.986625f)]
    public void PreySlimeOrMucocystDefence(bool multicellular, bool mucocyst,
        float expectedControl, float expectedDefended)
    {
        var predator = CreateRoleSpecies(231, false, CreateOrganelle("pilus", new Hex(0, -4)));
        var control = CreateRoleSpecies(232, multicellular);
        var defended = CreateRoleSpecies(233, multicellular,
            CreateOrganelle("slimeJet", new Hex(0, 4), mucocyst ? SlimeJetComponent.MUCOCYST_UPGRADE_NAME : null));
        var initial = GetRaw(control);
        var changed = GetRaw(defended);
        AssertRawBits(changed,
            mucocyst ?
                initial with { MucocystsScore = changed.MucocystsScore } :
                initial with { SlimeJetScore = changed.SlimeJetScore });
        AssertThat(mucocyst ? changed.MucocystsScore > 0 : changed.SlimeJetScore > 0).IsTrue();

        AssertScorePair(predator, control, predator, defended,
            expectedControl, expectedDefended);
    }

    [TestCase(false, 11.885521f, 22.295465f)]
    [TestCase(true, 0.6260613f, 2.0284386f)]
    public void PredatorPullingCiliaUpgrade(bool multicellular, float expectedControl, float expectedPulling)
    {
        var control = CreateRoleSpecies(241, multicellular, CreateOrganelle("pilus", new Hex(0, -4)),
            CreateOrganelle("cilia", new Hex(4, 0)));
        var pulling = CreateRoleSpecies(242, multicellular, CreateOrganelle("pilus", new Hex(0, -4)),
            CreateOrganelle("cilia", new Hex(4, 0), CiliaComponent.CILIA_PULL_UPGRADE_NAME));
        var prey = CreateRoleSpecies(243, false);
        var initial = GetRaw(control);
        var changed = GetRaw(pulling);
        AssertRawBits(changed, initial with { PullingCiliaModifier = changed.PullingCiliaModifier });
        AssertBits(initial.PullingCiliaModifier, 1.0f);
        AssertThat(changed.PullingCiliaModifier > 1).IsTrue();

        AssertScorePair(control, prey, pulling, prey, expectedControl, expectedPulling);
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
        AssertPredationLifecycle(species, species, 0);
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

    private static void AssertScorePair(Species controlPredator, Species controlPrey,
        Species changedPredator, Species changedPrey, float expectedControl, float expectedChanged,
        BiomeConditions? biome = null)
    {
        biome ??= CreateBiome();
        var control = CreateCache().GetPredationScore(controlPredator, controlPrey, biome);
        var changed = CreateCache().GetPredationScore(changedPredator, changedPrey, biome);
        AssertBits(control, expectedControl);
        AssertBits(changed, expectedChanged);
        AssertThat(float.IsFinite(control)).IsTrue();
        AssertThat(float.IsFinite(changed)).IsTrue();
        AssertDifferentBits(changed, control);
        AssertPredationLifecycle(controlPredator, controlPrey, expectedControl, biome);
        AssertPredationLifecycle(changedPredator, changedPrey, expectedChanged, biome);
    }
}
