using System;
using AutoEvo;
using GdUnit4;
using static GdUnit4.Assertions;

/// <summary>
///   Protects the division of labour scoring rules of the multicellular structural niche.
/// </summary>
/// <remarks>
///   <para>
///     Every cell type used here contains at least one non-cytoplasm organelle, because a cell type that only has
///     cytoplasm occupies no hexes in the gameplay layout and therefore cannot be connected to the body plan.
///   </para>
/// </remarks>
[TestSuite]
[RequireGodotRuntime]
public class CellDivisionOfLabourPressureTests
{
    private const float EPSILON = 0.0001f;

    /// <summary>
    ///   Single-celled species cannot divide labour between cells, so they keep the neutral baseline score and are
    ///   never pruned out of the miche tree by this pressure.
    /// </summary>
    [TestCase]
    public void SingleCelledSpeciesKeepsBaselineScore()
    {
        var species = SimulationCacheTestFixtures.CreateMicrobe(2100, "labour", "single", "cytoplasm");
        AssertThat(Score(species)).IsEqual(CellDivisionOfLabourPressure.BASELINE_SCORE);
    }

    /// <summary>
    ///   A colony of identical cells has no division of labour and must not gain any score, even though its cells
    ///   carry a functional organelle.
    /// </summary>
    [TestCase]
    public void ColonyOfIdenticalCellsKeepsBaselineScore()
    {
        var colonyCell = CreateCellType("ColonyCell", "flagellum");
        var species = SimulationCacheTestFixtures.CreateMulticellular(2101, "labour",
            (colonyCell, new Hex(0, 0)), (colonyCell, new Hex(1, 0)));
        AssertThat(species.EditorCells.Count).IsEqual(2);
        AssertThat(Score(species)).IsEqual(CellDivisionOfLabourPressure.BASELINE_SCORE);
    }

    /// <summary>
    ///   Adding a second cell type that covers an extra functional role must score above a colony of identical cells.
    /// </summary>
    [TestCase]
    public void SecondSpecialisedCellTypeAddsTypeAndRoleBonus()
    {
        var mover = CreateCellType("Mover", "flagellum");
        var defender = CreateCellType("Defender", "flagellum", "pilus");
        var species = SimulationCacheTestFixtures.CreateMulticellular(2102, "labour",
            (mover, new Hex(0, 0)), (defender, new Hex(1, 0)));

        var expected = CellDivisionOfLabourPressure.BASELINE_SCORE +
            CellDivisionOfLabourPressure.CELL_TYPE_BONUS + 2 * CellDivisionOfLabourPressure.ROLE_BONUS;
        AssertNear(Score(species), expected);
    }

    /// <summary>
    ///   A cell type that is defined but not used in the body plan must not count towards the score.
    /// </summary>
    [TestCase]
    public void UnusedCellTypeDoesNotContribute()
    {
        var mover = CreateCellType("Mover", "flagellum");
        var defender = CreateCellType("Defender", "flagellum", "pilus");
        var species = SimulationCacheTestFixtures.CreateMulticellular(2103, "labour",
            (mover, new Hex(0, 0)), (defender, new Hex(1, 0)));
        var usedScore = Score(species);

        species.ModifiableCellTypes.Add(CreateCellType("Unused", "flagellum", "chemoreceptor"));
        species.OnEdited();

        AssertThat(species.ModifiableCellTypes.Count).IsEqual(3);
        AssertThat(species.EditorCells.Count).IsEqual(2);
        AssertNear(Score(species), usedScore);
    }

    /// <summary>
    ///   Functional roles only count when they are split between cell types. A single do-everything cell type must not
    ///   score as if it were a differentiated body plan.
    /// </summary>
    [TestCase]
    public void RolesInOneCellTypeDoNotCountAsDivisionOfLabour()
    {
        var allInOne = CreateCellType("AllInOne", "flagellum", "pilus", "chemoreceptor");
        var species = SimulationCacheTestFixtures.CreateMulticellular(2104, "labour",
            (allInOne, new Hex(0, 0)), (allInOne, new Hex(GetFootprint(allInOne), 0)));
        AssertThat(Score(species)).IsEqual(CellDivisionOfLabourPressure.BASELINE_SCORE);
    }

    /// <summary>
    ///   The cell type bonus must be capped so that very large body plans cannot run away with the score.
    /// </summary>
    [TestCase]
    public void CellTypeBonusIsCapped()
    {
        var types = new[]
        {
            CreateCellType("Mover", "flagellum"),
            CreateCellType("Defender", "flagellum", "pilus"),
            CreateCellType("Sensor", "flagellum", "chemoreceptor"),
            CreateCellType("Store", "flagellum", "vacuole"),
            CreateCellType("Toxic", "flagellum", "oxytoxy"),
            CreateCellType("Photo", "flagellum", "chromatophore"),
        };

        var cells = new (CellType, Hex)[types.Length];
        var offset = 0;
        for (var i = 0; i < types.Length; ++i)
        {
            cells[i] = (types[i], new Hex(offset, 0));
            offset += GetFootprint(types[i]);
        }

        var species = SimulationCacheTestFixtures.CreateMulticellular(2105, "labour", cells);
        var score = Score(species);

        AssertThat(types.Length - 1)
            .OverrideFailureMessage("This fixture needs more cell types than the bonus cap")
            .IsGreater(CellDivisionOfLabourPressure.MAX_BONUSED_CELL_TYPES);

        var cappedTypeBonus = CellDivisionOfLabourPressure.MAX_BONUSED_CELL_TYPES *
            CellDivisionOfLabourPressure.CELL_TYPE_BONUS;

        AssertThat(score).IsGreater(CellDivisionOfLabourPressure.BASELINE_SCORE + cappedTypeBonus);
        AssertThat(score).IsLess(CellDivisionOfLabourPressure.BASELINE_SCORE + cappedTypeBonus +
            5 * CellDivisionOfLabourPressure.ROLE_BONUS + EPSILON);
    }

    /// <summary>
    ///   Scattering the same cell type into separate blocks must score lower than keeping it consolidated, and the
    ///   result must never fall below the baseline.
    /// </summary>
    [TestCase]
    public void ScatteredTissuesScoreLowerThanConsolidatedOnes()
    {
        var mover = CreateCellType("Mover", "flagellum");
        var defender = CreateCellType("Defender", "pilus");

        var consolidated = SimulationCacheTestFixtures.CreateMulticellular(2108, "labour",
            (mover, new Hex(0, 0)), (defender, new Hex(1, 0)));

        var scattered = SimulationCacheTestFixtures.CreateMulticellular(2109, "labour",
            (mover, new Hex(0, 0)), (defender, new Hex(1, 0)), (mover, new Hex(2, 0)));

        var consolidatedScore = Score(consolidated);
        var scatteredScore = Score(scattered);

        AssertThat(scatteredScore).IsLess(consolidatedScore);
        AssertNear(consolidatedScore - scatteredScore, CellDivisionOfLabourPressure.FRAGMENTATION_PENALTY);
        AssertThat(scatteredScore).IsGreaterEqual(CellDivisionOfLabourPressure.BASELINE_SCORE);
    }

    /// <summary>
    ///   Species that are not microbes or multicellular organisms belong to a different miche tree and must be
    ///   rejected loudly instead of being silently scored.
    /// </summary>
    [TestCase]
    public void UnsupportedSpeciesIsRejected()
    {
        var unsupported = new MacroscopicSpecies(2106, "labour", "unsupported");
        AssertThrown(() => Score(unsupported)).IsInstanceOf<ArgumentException>();
    }

    /// <summary>
    ///   Locks the score band and the weight so that this pressure cannot drown out the other structural pressures
    ///   before it has been balanced against real play testing data.
    /// </summary>
    [TestCase]
    public void ScoreBandAndCachedWeightStayWithinTheIntendedInfluence()
    {
        var maximum = CellDivisionOfLabourPressure.BASELINE_SCORE +
            CellDivisionOfLabourPressure.MAX_BONUSED_CELL_TYPES * CellDivisionOfLabourPressure.CELL_TYPE_BONUS +
            5 * CellDivisionOfLabourPressure.ROLE_BONUS;

        var cache = SimulationCacheTestFixtures.CreateCache();
        var cached = CreateGlobalCache().CellDivisionOfLabourPressure;

        AssertNear(cached.Weight, 1.0f);
        AssertNear(maximum, 2.75f);

        // Weighted influence of a fully differentiated colony compared to an undifferentiated one must stay below
        // three times the baseline, which keeps it in the same band as the existing structural pressures
        AssertThat(maximum * cached.Weight / CellDivisionOfLabourPressure.BASELINE_SCORE).IsLess(3.0f);

        // And a plain microbe must be completely unaffected by the weight
        var microbe = SimulationCacheTestFixtures.CreateMicrobe(2107, "labour", "single", "cytoplasm");
        AssertNear(cached.Score(microbe, CreatePatch(), cache), CellDivisionOfLabourPressure.BASELINE_SCORE);
        AssertNear(cached.WeightedComparedScores(CellDivisionOfLabourPressure.BASELINE_SCORE,
            CellDivisionOfLabourPressure.BASELINE_SCORE), 0.0f);
    }

    private static AutoEvoGlobalCache CreateGlobalCache()
    {
        return new AutoEvoGlobalCache(new WorldGenerationSettings { Seed = 3 });
    }

    private static float Score(Species species)
    {
        var pressure = new CellDivisionOfLabourPressure(1);
        var score = pressure.Score(species, CreatePatch(), SimulationCacheTestFixtures.CreateCache());
        AssertThat(float.IsFinite(score)).IsTrue();
        AssertThat(pressure.GetEnergy(CreatePatch())).IsEqual(0.0f);
        AssertThat(pressure.Name.ToString()).IsNotEmpty();
        return score;
    }

    private static void AssertNear(float actual, float expected)
    {
        AssertThat(Math.Abs(actual - expected) < EPSILON)
            .OverrideFailureMessage($"Expected about {expected} but got {actual}")
            .IsTrue();
    }

    private static CellType CreateCellType(string name, params string[] organelles)
    {
        var templates = new OrganelleTemplate[organelles.Length];
        for (var i = 0; i < organelles.Length; ++i)
            templates[i] = SimulationCacheTestFixtures.CreateOrganelle(organelles[i], new Hex(i, 0));

        return SimulationCacheTestFixtures.CreateCellType(name, "single", templates);
    }

    /// <summary>
    ///   Number of hexes a cell type occupies, used to lay cells out so that they touch but never overlap.
    /// </summary>
    private static int GetFootprint(CellType cellType)
    {
        var hexes = 0;
        foreach (var organelle in cellType.Organelles)
            hexes += organelle.Definition.HexCount;

        return Math.Max(hexes, 1);
    }

    private static Patch CreatePatch()
    {
        var template = SimulationParameters.Instance.GetBiome("aavolcanic_vent");
        var biome = (BiomeConditions)template.Conditions.Clone();
        return new Patch(new LocalizedString("LabourTest"), 1, template, BiomeType.Vents,
            new PatchSnapshot(biome, template.Background), 1);
    }
}
