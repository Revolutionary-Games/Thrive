namespace ThriveTest.MulticellularStage.Tests;

using System.Collections.Generic;
using Xunit;

/// <summary>
///   Tests geometry and persistence used by multicellular achievements.
/// </summary>
public class MulticellularAchievementTests
{
    [Fact]
    public void PackedLayoutDistinguishesEnclosedHoleFromOpenConcavity()
    {
        var organelle = new OrganelleDefinition { Name = "Dummy", Hexes = [new Hex(0, 0)] };
        var organelles = new OrganelleLayout<OrganelleTemplate>();
        var temporary1 = new List<Hex>();
        var temporary2 = new List<Hex>();
        organelles.AddFast(new OrganelleTemplate(organelle, new Hex(0, 0), 0), temporary1, temporary2);
        var type = new CellType(organelles, new MembraneType { Name = "Dummy" });
        var layout = new CellLayout<CellTemplate>();

        Assert.False(MulticellularAchievementConditions.HasNoEnclosedGaps(layout));
        foreach (var offset in Hex.HexNeighbourOffset.Values)
            layout.AddFast(new CellTemplate(type, offset, 0), temporary1, temporary2);

        Assert.False(MulticellularAchievementConditions.HasNoEnclosedGaps(layout));
        layout.AddFast(new CellTemplate(type, new Hex(0, 0), 0), temporary1, temporary2);
        Assert.True(MulticellularAchievementConditions.HasNoEnclosedGaps(layout));

        var openLayout = new CellLayout<CellTemplate>();
        foreach (var offset in Hex.HexNeighbourOffset.Values)
        {
            if (offset == new Hex(0, 1))
                continue;

            openLayout.AddFast(new CellTemplate(type, offset, 0), temporary1, temporary2);
        }

        Assert.True(MulticellularAchievementConditions.HasNoEnclosedGaps(openLayout));
    }

    [Fact]
    public void PackedLayoutRejectsDeepCreviceUntilCellTypeFillsIt()
    {
        var cytoplasm = new OrganelleDefinition { Name = "Dummy cytoplasm", Hexes = [new Hex(0, 0)] };
        var temporary1 = new List<Hex>();
        var temporary2 = new List<Hex>();

        var wallOrganelles = new OrganelleLayout<OrganelleTemplate>();
        wallOrganelles.AddFast(new OrganelleTemplate(cytoplasm, new Hex(0, 0), 0), temporary1, temporary2);
        var wallCellType = new CellType(wallOrganelles, new MembraneType { Name = "Dummy" });

        var bottomOrganelles = new OrganelleLayout<OrganelleTemplate>();
        bottomOrganelles.AddFast(new OrganelleTemplate(cytoplasm, new Hex(0, 0), 0), temporary1, temporary2);
        var bottomCellType = new CellType(bottomOrganelles, new MembraneType { Name = "Dummy" });

        var layout = new CellLayout<CellTemplate>();
        for (int r = 0; r <= 2; ++r)
        {
            layout.AddFast(new CellTemplate(wallCellType, new Hex(0, r), 0), temporary1, temporary2);
            layout.AddFast(new CellTemplate(wallCellType, new Hex(2, r), 0), temporary1, temporary2);
        }

        layout.AddFast(new CellTemplate(bottomCellType, new Hex(1, 0), 0), temporary1, temporary2);

        Assert.True(MulticellularAchievementConditions.HasNoEnclosedGaps(layout));
        Assert.False(MulticellularAchievementConditions.HasNoDeepCrevices(layout));

        bottomCellType.ModifiableOrganelles.AddFast(new OrganelleTemplate(cytoplasm, new Hex(0, 1), 0), temporary1,
            temporary2);

        Assert.True(MulticellularAchievementConditions.HasNoDeepCrevices(layout));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void PackedLayoutAllowsBroadConcavityBetweenProtrusions(int rotation)
    {
        var organelle = new OrganelleDefinition { Name = "Dummy", Hexes = [new Hex(0, 0)] };
        var organelles = new OrganelleLayout<OrganelleTemplate>();
        var temporary1 = new List<Hex>();
        var temporary2 = new List<Hex>();
        organelles.AddFast(new OrganelleTemplate(organelle, new Hex(0, 0), 0), temporary1, temporary2);
        var type = new CellType(organelles, new MembraneType { Name = "Dummy" });
        var layout = new CellLayout<CellTemplate>();
        var footprint = new HashSet<Hex>();

        // Two long protrusions with a broad opening between them, rather than a narrow crevice
        for (int r = 0; r <= 6; ++r)
        {
            footprint.Add(new Hex(0, r));
            footprint.Add(new Hex(6, r));
        }

        for (int q = 1; q < 6; ++q)
            footprint.Add(new Hex(q, 0));

        foreach (var hex in footprint)
        {
            var position = Hex.RotateAxialNTimes(hex, rotation) + new Hex(-12, 7);
            layout.AddFast(new CellTemplate(type, position, 0), temporary1, temporary2);
        }

        Assert.True(MulticellularAchievementConditions.HasNoEnclosedGaps(layout));
        Assert.True(MulticellularAchievementConditions.HasNoDeepCrevices(layout));

        // A narrow entrance must not hide a larger empty chamber from the packing check
        for (int q = 1; q < 6; ++q)
        {
            if (q == 3)
                continue;

            var position = Hex.RotateAxialNTimes(new Hex(q, 4), rotation) + new Hex(-12, 7);
            layout.AddFast(new CellTemplate(type, position, 0), temporary1, temporary2);
        }

        Assert.True(MulticellularAchievementConditions.HasNoEnclosedGaps(layout));
        Assert.False(MulticellularAchievementConditions.HasNoDeepCrevices(layout));
    }

    [Fact]
    public void MulticellularStatsPersistAndOlderStoresDefaultToZero()
    {
        var store = new AchievementStatStore();
        var saved = new Dictionary<int, int>();
        for (int stat = IAchievementStatStore.STAT_ULTIMATE_COLONY;
             stat <= IAchievementStatStore.STAT_PERFECTLY_PACKED; ++stat)
        {
            Assert.True(IAchievementStatStore.IsValidStatistic(stat));
            Assert.Equal(0, store.GetIntStat(stat));
            Assert.True(store.SetIntStat(stat, 1));
        }

        store.Save(saved);
        var restored = new AchievementStatStore();
        restored.Load(saved);
        for (int stat = IAchievementStatStore.STAT_ULTIMATE_COLONY;
             stat <= IAchievementStatStore.STAT_PERFECTLY_PACKED; ++stat)
        {
            Assert.Equal(1, restored.GetIntStat(stat));
        }

        var olderStore = new AchievementStatStore();
        olderStore.Load(new Dictionary<int, int>());
        Assert.Equal(0, olderStore.GetIntStat(IAchievementStatStore.STAT_ULTIMATE_COLONY));
        Assert.Equal(0, olderStore.GetIntStat(IAchievementStatStore.STAT_PERFECTLY_PACKED));
    }
}
