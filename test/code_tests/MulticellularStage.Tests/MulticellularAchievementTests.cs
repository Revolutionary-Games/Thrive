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
