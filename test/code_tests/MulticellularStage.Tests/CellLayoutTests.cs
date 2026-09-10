namespace ThriveTest.MulticellularStage.Tests;

using System.Collections.Generic;
using System.Linq;
using Xunit;

public class CellLayoutTests
{
    private readonly CellType testType1;

    private readonly OrganelleDefinition singleHexOrganelle = new()
    {
        Name = "DummyCytoplasm",
        Hexes = [new Hex(0, 0)],
    };

    private readonly OrganelleDefinition doubleHexOrganelle = new()
    {
        Name = "DummyDoubleHex",
        Hexes = [new Hex(0, 0), new Hex(0, -1)],
    };

    public CellLayoutTests()
    {
        var temp1 = new List<Hex>();
        var temp2 = new List<Hex>();

        var dummyMembrane = new MembraneType
        {
            Name = "Dummy",
        };

        // Build test cell type 1
        var layout = new OrganelleLayout<OrganelleTemplate>();

        layout.AddFast(new OrganelleTemplate(singleHexOrganelle, new Hex(0, 0), 0), temp1, temp2);

        layout.AddFast(new OrganelleTemplate(doubleHexOrganelle, new Hex(0, -1), 0), temp1, temp2);

        layout.AddFast(new OrganelleTemplate(doubleHexOrganelle, new Hex(1, -1), 1), temp1, temp2);

        // Verify the layout is as expected
        Assert.Equal(3, layout.Count);
        Assert.NotNull(layout.GetElementAt(new Hex(0, 0), temp1));
        Assert.NotNull(layout.GetElementAt(new Hex(0, -1), temp1));
        Assert.NotNull(layout.GetElementAt(new Hex(0, -2), temp1));
        Assert.Null(layout.GetElementAt(new Hex(1, -3), temp1));

        Assert.NotNull(layout.GetElementAt(new Hex(1, -1), temp1));
        Assert.NotNull(layout.GetElementAt(new Hex(2, -2), temp1));
        Assert.Null(layout.GetElementAt(new Hex(1, -2), temp1));

        testType1 = new CellType(layout, dummyMembrane);
    }

    [Fact]
    public void CellLayout_CellHasExpectedPositions()
    {
        var layout = new CellLayout<CellTemplate>();

        var temp1 = new List<Hex>();
        var temp2 = new List<Hex>();

        var placed1 = new CellTemplate(testType1, new Hex(0, 0), 0);

        layout.AddFast(placed1, temp1, temp2);

        // Test that expected positions are filled
        Assert.Equal(placed1, layout.GetElementAt(new Hex(0, 0), temp1));
        Assert.Equal(placed1, layout.GetElementAt(new Hex(0, -1), temp1));
        Assert.Equal(placed1, layout.GetElementAt(new Hex(0, -2), temp1));
        Assert.Equal(placed1, layout.GetElementAt(new Hex(1, -1), temp1));
        Assert.Equal(placed1, layout.GetElementAt(new Hex(2, -2), temp1));
        Assert.Null(layout.GetElementAt(new Hex(2, -1), temp1));

        TestThatHexCacheMatches(layout);
        layout.ThrowIfCellsOverlap();
    }

    [Fact]
    public void CellLayout_RotatingCellHasExpectedPositions()
    {
        var layout = new CellLayout<CellTemplate>();

        var temp1 = new List<Hex>();
        var temp2 = new List<Hex>();

        var placed1 = new CellTemplate(testType1, new Hex(0, 0), 1);

        layout.AddFast(placed1, temp1, temp2);

        // Test that expected positions are filled
        Assert.Equal(placed1, layout.GetElementAt(new Hex(0, 0), temp1));
        Assert.Equal(placed1, layout.GetElementAt(new Hex(1, -1), temp1));
        Assert.Equal(placed1, layout.GetElementAt(new Hex(2, -2), temp1));
        Assert.Equal(placed1, layout.GetElementAt(new Hex(1, 0), temp1));
        Assert.Equal(placed1, layout.GetElementAt(new Hex(2, 0), temp1));
        Assert.Null(layout.GetElementAt(new Hex(2, -1), temp1));

        TestThatHexCacheMatches(layout);
        layout.ThrowIfCellsOverlap();
    }

    [Fact]
    public void CellLayout_TwoRotatedCellsDoNotOverlap()
    {
        var layout = new CellLayout<CellTemplate>();

        var temp1 = new List<Hex>();
        var temp2 = new List<Hex>();

        var placed1 = new CellTemplate(testType1, new Hex(0, 0), 1);

        layout.AddFast(placed1, temp1, temp2);

        TestThatHexCacheMatches(layout);
        layout.ThrowIfCellsOverlap();

        var placed2 = new CellTemplate(testType1, new Hex(2, -1), 1);

        layout.AddFast(placed2, temp1, temp2);

        TestThatHexCacheMatches(layout);
        layout.ThrowIfCellsOverlap();
    }

    [Fact]
    public void MulticellularLayoutHelpers_LowQualityLayoutKeepsCellsTouching()
    {
        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();
        var workMemory3 = new HashSet<Hex>();

        // This is real data, made by auto-evo, which triggers a bug
        var stemCellType = CreateCellType("Stem", [
            ([
                new Hex(0, 0), new Hex(1, 0), new Hex(0, 1), new Hex(0, -1), new Hex(1, -1),
                new Hex(-1, 1), new Hex(-1, 0), new Hex(1, 1), new Hex(0, 2), new Hex(-1, 2),
            ], new Hex(0, -3)),
            ([new Hex(0, 0)], new Hex(0, 2)),
            ([new Hex(0, 0), new Hex(0, -1)], new Hex(-1, 2)),
            ([new Hex(0, 0), new Hex(0, -1)], new Hex(1, 1)),
            ([new Hex(0, 0), new Hex(0, -1)], new Hex(0, 1)),
        ], workMemory1, workMemory2);

        var chemoreceptorCellType = CreateCellType("Chemoreceptor", [
            ([
                new Hex(0, 0), new Hex(1, 0), new Hex(0, 1), new Hex(0, -1), new Hex(1, -1),
                new Hex(-1, 1), new Hex(-1, 0), new Hex(1, 1), new Hex(0, 2), new Hex(-1, 2),
            ], new Hex(0, -4)),
            ([new Hex(0, 0)], new Hex(0, 1)),
            ([new Hex(0, 0), new Hex(0, -1)], new Hex(-1, 1)),
            ([new Hex(0, 0), new Hex(0, -1)], new Hex(1, 0)),
            ([new Hex(0, 0), new Hex(0, -1)], new Hex(0, 0)),
            ([new Hex(0, 0)], new Hex(0, 2)),
        ], workMemory1, workMemory2);

        var source = new IndividualHexLayout<CellTemplate>();
        AddCell(source, stemCellType, new Hex(0, 0), workMemory1, workMemory2);
        AddCell(source, stemCellType, new Hex(-3, 3), workMemory1, workMemory2);
        AddCell(source, stemCellType, new Hex(3, 0), workMemory1, workMemory2);
        AddCell(source, chemoreceptorCellType, new Hex(0, 8), workMemory1, workMemory2);
        AddCell(source, chemoreceptorCellType, new Hex(6, 6), workMemory1, workMemory2);
        AddCell(source, chemoreceptorCellType, new Hex(-5, 11), workMemory1, workMemory2);

        var gameplayLayout = new CellLayout<CellTemplate>();
        var editorLayout = new IndividualHexLayout<CellTemplate>();

        MulticellularLayoutHelpers.UpdateGameplayLayout(gameplayLayout, editorLayout, source, AlgorithmQuality.Low,
            workMemory1, workMemory2, workMemory3);

        gameplayLayout.ThrowIfCellsAreNotTouching();
    }

    private static CellType CreateCellType(string name,
        IEnumerable<(List<Hex> Hexes, Hex Position)> organelles, List<Hex> workMemory1, List<Hex> workMemory2)
    {
        var layout = new OrganelleLayout<OrganelleTemplate>();
        var index = 0;

        foreach (var (hexes, position) in organelles)
        {
            var definition = new OrganelleDefinition
            {
                Name = $"TestOrganelle{index++}",
                Hexes = hexes,
            };
            layout.AddFast(new OrganelleTemplate(definition, position, 0), workMemory1, workMemory2);
        }

        return new CellType(layout, new MembraneType { Name = "TestMembrane" }) { CellTypeName = name };
    }

    private static void AddCell(IndividualHexLayout<CellTemplate> layout, CellType cellType, Hex position,
        List<Hex> workMemory1, List<Hex> workMemory2)
    {
        layout.AddFast(new HexWithData<CellTemplate>(new CellTemplate(cellType, position, 0), position, 0),
            workMemory1, workMemory2);
    }

    private void TestThatHexCacheMatches(CellLayout<CellTemplate> layout)
    {
        var cache = layout.ComputeHexCache();

        var temp1 = new List<Hex>();

        foreach (var hex in cache)
        {
            Assert.NotNull(layout.GetElementAt(hex, temp1));

            // Test nearby positions don't match
            foreach (var offset in Hex.HexNeighbourOffset)
            {
                var neighbor = hex + offset.Value;

                // Only check free spots nearby
                if (!cache.Contains(neighbor))
                    Assert.Null(layout.GetElementAt(neighbor, temp1));
            }
        }

        // Make sure the cache size is correct (so that there are no duplicates that would reduce the generated hexes)
        var totalHexes = 0;

        foreach (var cellTemplate in layout)
        {
            totalHexes += cellTemplate.CellType.Organelles.Select(o => o.Definition.Hexes.Count).Sum();
        }

        Assert.Equal(totalHexes, cache.Count);
    }
}
