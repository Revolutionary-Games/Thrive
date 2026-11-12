using System.Collections.Generic;
using AutoEvo;
using GdUnit4;
using static GdUnit4.Assertions;

/// <summary>
///   Checks the species relationships retained by generated miche branches.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class GenerateMicheTests
{
    /// <summary>
    ///   Verifies that an endosymbiosis branch captures its host and symbiont, and that cancellation removes it
    ///   from newly generated trees without invalidating an existing branch's scoring and energy calculation.
    /// </summary>
    [TestCase]
    public void CancelledEndosymbiosisOnlyAffectsNewMicheTrees()
    {
        const long hostPopulation = 37;
        var host = SimulationCacheTestFixtures.CreateMicrobe(1, "Host", "single", "cytoplasm");
        var symbiont = SimulationCacheTestFixtures.CreateMicrobe(2, "Symbiont", "single", "cytoplasm");
        host.BecomePlayerSpecies();
        host.Endosymbiosis.EngulfedSpecies.Add(symbiont, 1);
        AssertThat(host.Endosymbiosis.StartEndosymbiosis((int)symbiont.ID,
            SimulationParameters.Instance.GetOrganelleType("metabolosome"), 1)).IsTrue();

        var template = SimulationParameters.Instance.GetBiome("aavolcanic_vent");
        var patch = new Patch(new LocalizedString("EndosymbiosisTest"), 1, template, BiomeType.Vents,
            new PatchSnapshot((BiomeConditions)template.Conditions.Clone(), template.Background), 1);
        AssertThat(patch.AddSpecies(host, hostPopulation)).IsTrue();
        var settings = new WorldGenerationSettings { Seed = 1 };
        var cache = new SimulationCache(settings);
        var globalCache = new AutoEvoGlobalCache(settings);
        var generator = new GenerateMiche(patch, cache, globalCache);
        var branches = new List<Miche>();

        generator.GenerateMicheTree(globalCache).GetLeafNodes(branches, m => m.Pressure is EndosymbiosisPressure);
        AssertThat(branches.Count).IsEqual(1);
        var existingPressure = (EndosymbiosisPressure)branches[0].Pressure;
        AssertThat(existingPressure.Host).IsSame(host);
        AssertThat(existingPressure.Endosymbiont).IsSame(symbiont);

        AssertThat(host.Endosymbiosis.CancelAllEndosymbiosisTargets()).IsTrue();
        branches.Clear();
        generator.GenerateMicheTree(globalCache).GetLeafNodes(branches, m => m.Pressure is EndosymbiosisPressure);
        AssertThat(branches.Count).IsEqual(0);

        AssertThat(existingPressure.Score(symbiont, patch, cache)).IsEqual(1.0f);
        AssertThat(existingPressure.Score(host, patch, cache)).IsEqual(0.0f);

        // This one-organelle symbiont receives one energy unit for each member of the captured host species.
        AssertThat(symbiont.Organelles.Count).IsEqual(1);
        AssertThat(existingPressure.GetEnergy(patch)).IsEqual(hostPopulation);
    }
}
