using System;
using AutoEvo;
using GdUnit4;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class RandomMicrobeSpeciesTests
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(Constants.ORGANELLE_CHEAPEST_COST - 1)]
    public void GenerateRandomMicrobeSpecies_InsufficientBudgetReturnsInitialSpecies(int mp)
    {
        var world = CreateWorld();
        var original = new MicrobeSpecies(2, string.Empty, string.Empty);

        var result = CommonMutationFunctions.GenerateRandomMicrobeSpecies(original, world.Map.CurrentPatch!,
            new MutationWorkMemory(), new Random(42), mp);

        AssertThat(result).IsSame(original);
        AssertThat(result.Organelles.Count).IsEqual(1);
        AssertThat(result.Organelles[0].Definition.InternalName).IsEqual("cytoplasm");
        AssertThat(string.IsNullOrEmpty(result.Genus)).IsFalse();
        AssertThat(string.IsNullOrEmpty(result.Epithet)).IsFalse();
    }

    [TestCase(42)]
    [TestCase(123)]
    [TestCase(456)]
    public void GenerateRandomMicrobeSpecies_AppliesMutationsAndFinalisesSpecies(int seed)
    {
        var world = CreateWorld();
        var original = new MicrobeSpecies(2, string.Empty, string.Empty);

        var result = CommonMutationFunctions.GenerateRandomMicrobeSpecies(original, world.Map.CurrentPatch!,
            new MutationWorkMemory(), new Random(seed));

        AssertThat(result.ID).IsEqual(original.ID);
        AssertThat(result.Organelles.Count).IsGreater(1);
        AssertThat(original.Organelles.Count).IsEqual(1);
        AssertThat(string.IsNullOrEmpty(result.Genus)).IsFalse();
        AssertThat(string.IsNullOrEmpty(result.Epithet)).IsFalse();
    }

    [TestCase]
    public void GenerateRandomMicrobeSpecies_EmptyCandidatesReturnsInitialSpecies()
    {
        var world = CreateWorld();
        var patch = world.Map.CurrentPatch!;
        var memory = new MutationWorkMemory();
        var probe = new MicrobeSpecies(2, string.Empty, string.Empty);
        GameWorld.SetInitialSpeciesProperties(probe, memory.WorkingMemory1, memory.WorkingMemory2);

        // This seed samples organelles that cannot be added within the minimum budget.
        var candidates = new AddOrganelleAnywhere(_ => true).MutationsOf(probe,
            Constants.ORGANELLE_CHEAPEST_COST, true, new Random(0), patch.Biome);
        AssertThat(candidates).IsNotNull();
        AssertThat(candidates!.Count).IsEqual(0);

        var original = new MicrobeSpecies(2, string.Empty, string.Empty);
        var result = CommonMutationFunctions.GenerateRandomMicrobeSpecies(original, patch, memory,
            new Random(0), Constants.ORGANELLE_CHEAPEST_COST);

        AssertThat(result).IsSame(original);
        AssertThat(result.Organelles.Count).IsEqual(1);
        AssertThat(string.IsNullOrEmpty(result.Genus)).IsFalse();
        AssertThat(string.IsNullOrEmpty(result.Epithet)).IsFalse();
    }

    private static GameWorld CreateWorld()
    {
        return new GameWorld(new WorldGenerationSettings
        {
            Seed = 0x5EED,
            WorldSize = WorldGenerationSettings.WorldSizeEnum.Small,
        });
    }
}
