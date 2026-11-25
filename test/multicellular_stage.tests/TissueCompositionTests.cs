using System;
using System.Collections.Generic;
using GdUnit4;
using Godot;
using static GdUnit4.Assertions;

/// <summary>
///   Protects the shared tissue role classifier and the tissue composition view that multicellular and macroscopic
///   code are both meant to build on.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class TissueCompositionTests
{
    [TestCase]
    public void RoleClassifier_DerivesRolesFromSingleOrganelles()
    {
        AssertThat(RolesOf("cytoplasm")).IsEqual(CellFunctionalRole.None);
        AssertThat(RolesOf("flagellum")).IsEqual(CellFunctionalRole.Movement);
        AssertThat(RolesOf("cilia")).IsEqual(CellFunctionalRole.Movement);
        AssertThat(RolesOf("pilus")).IsEqual(CellFunctionalRole.Defense);

        // The toxin vacuole also holds compounds, so it covers defense and storage at the same time
        AssertThat(RolesOf("oxytoxy")).IsEqual(CellFunctionalRole.Defense | CellFunctionalRole.Storage);
        AssertThat(RolesOf("chemoreceptor")).IsEqual(CellFunctionalRole.Sensing);
        AssertThat(RolesOf("vacuole")).IsEqual(CellFunctionalRole.Storage);

        // The chromatophore also stores the glucose it makes, so it covers storage and photosynthesis
        AssertThat(RolesOf("chromatophore")).IsEqual(CellFunctionalRole.Photosynthesis |
            CellFunctionalRole.Storage);
    }

    [TestCase]
    public void RoleClassifier_CombinesRolesOfOneCell()
    {
        var cellType = CreateCellType("Combined", "cytoplasm", "flagellum", "pilus", "chemoreceptor");
        var roles = CellFunctionalRoleCalculator.CalculateFromCell(cellType);

        AssertThat(roles).IsEqual(CellFunctionalRole.Movement | CellFunctionalRole.Defense |
            CellFunctionalRole.Sensing);
        AssertThat(CellFunctionalRoleCalculator.CountRoles(roles)).IsEqual(3);
        AssertThat(new List<CellFunctionalRole>(CellFunctionalRoleCalculator.Split(roles)).Count).IsEqual(3);
    }

    [TestCase]
    public void TissueComposition_CountsTypesAndRolesOfMulticellularBody()
    {
        var mover = CreateCellType("Mover", "flagellum");
        var defender = CreateCellType("Defender", "pilus");

        var species = SimulationCacheTestFixtures.CreateMulticellular(4001, "tissue",
            (mover, new Hex(0, 0)), (defender, new Hex(1, 0)));

        var composition = TissueComposition.FromMulticellular(species);

        AssertThat(composition.TissueCount).IsEqual(2);
        AssertThat(composition.CellTypeCount).IsEqual(2);
        AssertThat(composition.CoveredRoles).IsEqual(CellFunctionalRole.Movement | CellFunctionalRole.Defense);
        AssertThat(composition.RoleCount).IsEqual(2);
        AssertThat(composition.HasDivisionOfLabour).IsTrue();
        AssertThat(composition.TissuesPerRole[CellFunctionalRole.Movement]).IsEqual(1);
        AssertThat(composition.TissuesPerRole[CellFunctionalRole.Defense]).IsEqual(1);
        AssertThat(composition.TissuesPerRole[CellFunctionalRole.Photosynthesis]).IsEqual(0);
    }

    [TestCase]
    public void TissueComposition_IdenticalCellsHaveNoDivisionOfLabour()
    {
        var cellType = CreateCellType("Only", "cytoplasm", "flagellum");
        var species = SimulationCacheTestFixtures.CreateMulticellular(4002, "tissue",
            (cellType, new Hex(0, 0)), (cellType, new Hex(Footprint(cellType), 0)));

        var composition = TissueComposition.FromMulticellular(species);

        AssertThat(composition.TissueCount).IsEqual(2);
        AssertThat(composition.CellTypeCount).IsEqual(1);
        AssertThat(composition.HasDivisionOfLabour).IsFalse();
    }

    [TestCase]
    public void TissueComposition_UsesMetaballsOfMacroscopicBody()
    {
        var mover = CreateCellType("Mover", "cytoplasm", "flagellum");
        var sensor = CreateCellType("Sensor", "cytoplasm", "chemoreceptor");
        var species = new MacroscopicSpecies(4003, "Tissue", "Macro");
        species.ModifiableCellTypes.Add(mover);
        species.ModifiableCellTypes.Add(sensor);

        var root = new MacroscopicMetaball(mover);
        species.ModifiableBodyLayout.Add(root);

        var child = new MacroscopicMetaball(sensor);
        child.ModifiableParent = root;
        species.ModifiableBodyLayout.Add(child);

        var composition = TissueComposition.FromMacroscopic(species);

        AssertThat(composition.TissueCount).IsEqual(2);
        AssertThat(composition.CellTypeCount).IsEqual(2);
        AssertThat(composition.CoveredRoles).IsEqual(CellFunctionalRole.Movement | CellFunctionalRole.Sensing);
        AssertThat(composition.HasDivisionOfLabour).IsTrue();
    }

    [TestCase]
    public void TissueBlocksJoinTouchingCellsOfSameType()
    {
        var mover = CreateCellType("Mover", "flagellum");
        var defender = CreateCellType("Defender", "pilus");

        var species = SimulationCacheTestFixtures.CreateMulticellular(4004, "tissue",
            (mover, new Hex(0, 0)), (mover, new Hex(1, 0)), (defender, new Hex(2, 0)));

        var composition = TissueComposition.FromMulticellular(species);

        AssertThat(composition.TissueCount).IsEqual(3);
        AssertThat(composition.TissueBlockCount).IsEqual(2);
    }

    [TestCase]
    public void TissueBlocksSplitWhenSameTypeIsSeparated()
    {
        var mover = CreateCellType("Mover", "flagellum");
        var defender = CreateCellType("Defender", "pilus");

        var species = SimulationCacheTestFixtures.CreateMulticellular(4005, "tissue",
            (mover, new Hex(0, 0)), (defender, new Hex(1, 0)), (mover, new Hex(2, 0)));

        var composition = TissueComposition.FromMulticellular(species);

        AssertThat(composition.TissueCount).IsEqual(3);
        AssertThat(composition.TissueBlockCount).IsEqual(3);
    }

    [TestCase]
    public void MacroscopicBlocksFollowMetaballParentLinks()
    {
        var mover = CreateCellType("Mover", "flagellum");
        var species = new MacroscopicSpecies(4102, "Tissue", "Blocky");
        species.ModifiableCellTypes.Add(mover);

        var root = new MacroscopicMetaball(mover);
        species.ModifiableBodyLayout.Add(root);

        var child = new MacroscopicMetaball(mover);
        child.ModifiableParent = root;
        species.ModifiableBodyLayout.Add(child);

        var connected = TissueComposition.FromMacroscopic(species);
        AssertThat(connected.TissueBlockCount).IsEqual(1);
        AssertThat(connected.CellTypeCount).IsEqual(1);

        // Insert a different type between two metaballs of the same type: the same type no longer forms one block
        var defender = CreateCellType("Defender", "pilus");
        species.ModifiableCellTypes.Add(defender);

        var middle = new MacroscopicMetaball(defender);
        middle.ModifiableParent = root;
        species.ModifiableBodyLayout.Add(middle);

        var far = new MacroscopicMetaball(mover);
        far.ModifiableParent = middle;
        species.ModifiableBodyLayout.Add(far);

        var split = TissueComposition.FromMacroscopic(species);
        AssertThat(split.TissueCount).IsEqual(4);
        AssertThat(split.CellTypeCount).IsEqual(2);
        AssertThat(split.TissueBlockCount).IsEqual(3);
    }

    private static int Footprint(CellType cellType)
    {
        var hexes = 0;
        foreach (var organelle in cellType.Organelles)
            hexes += organelle.Definition.HexCount;

        return Math.Max(hexes, 1);
    }

    private static CellFunctionalRole RolesOf(string organelle)
    {
        var organelleType = SimulationParameters.Instance.GetOrganelleType(organelle);
        return CellFunctionalRoleCalculator.CalculateFromOrganelle(organelleType);
    }

    private static CellType CreateCellType(string name, params string[] organelles)
    {
        var templates = new OrganelleTemplate[organelles.Length];
        var offset = 0;
        for (var i = 0; i < organelles.Length; ++i)
        {
            templates[i] = SimulationCacheTestFixtures.CreateOrganelle(organelles[i], new Hex(offset, 0));
            offset += templates[i].Definition.HexCount;
        }

        return SimulationCacheTestFixtures.CreateCellType(name, "single", templates);
    }
}
