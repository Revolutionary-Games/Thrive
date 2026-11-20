namespace ThriveTest.MicrobeStage;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Xunit;

public class MembraneGenerationCoordinatorTests
{
    private static MembraneType CreateTestMembraneType()
    {
        return new MembraneType
        {
            Name = "TestMembrane",
            BaseWigglyness = 1.0f,
            MovementWigglyness = 1.0f,
            CellWall = false,
        };
    }

    private static MembranePointData CreatePreStretchMembraneData(MembraneShapeGenerator generator,
        MembraneType membraneType)
    {
        var hexes = ArrayPool<Vector2>.Shared.Rent(1);
        hexes[0] = Vector2.Zero;

        var parameters = new MembraneGenerationParameters(hexes, 1, membraneType)
        {
            IsPreMulticellularStretch = true,
        };

        return generator.GenerateMicrobeShape(ref parameters, true);
    }

    [Fact]
    public void MembraneGenerationCoordinator_IncreasedExpectedCountWaitsForNewCell()
    {
        var generator = new MembraneShapeGenerator();
        var membraneType = CreateTestMembraneType();

        const long leaderKey = 2000;
        const long cell1Key = 2001;
        const long cell2Key = 2002;
        const long cell3Key = 2003;

        try
        {
            var data1 = CreatePreStretchMembraneData(generator, membraneType);
            var data2 = CreatePreStretchMembraneData(generator, membraneType);
            var data3 = CreatePreStretchMembraneData(generator, membraneType);

            var pos1 = new MulticellularMembraneGenerationCellData(new Vector2(0, 0), 0);
            var pos2 = new MulticellularMembraneGenerationCellData(new Vector2(2, 0), 0);
            var pos3 = new MulticellularMembraneGenerationCellData(new Vector2(4, 0), 0);

            // Cell 1 arrives expecting 2 cells
            var result1 = MembraneGenerationCoordinator.CoordinateMulticellularGeneration(
                generator, data1, leaderKey, cell1Key, pos1, 2);
            Assert.Empty(result1);

            // Cell 3 is added to colony and arrives expecting 3 cells before cell 2 arrives
            var result3 = MembraneGenerationCoordinator.CoordinateMulticellularGeneration(
                generator, data3, leaderKey, cell3Key, pos3, 3);
            Assert.Empty(result3);

            // Cell 2 arrives (even with the older expected count of 2) — all 3 cells are now present
            var result2 = MembraneGenerationCoordinator.CoordinateMulticellularGeneration(
                generator, data2, leaderKey, cell2Key, pos2, 2);

            Assert.Equal(3, result2.Count);
            Assert.Contains(cell1Key, result2);
            Assert.Contains(cell2Key, result2);
            Assert.Contains(cell3Key, result2);

            Assert.NotNull(MembraneGenerationCoordinator.TryTakeFinishedMulticellularMembrane(cell1Key));
            Assert.NotNull(MembraneGenerationCoordinator.TryTakeFinishedMulticellularMembrane(cell2Key));
            Assert.NotNull(MembraneGenerationCoordinator.TryTakeFinishedMulticellularMembrane(cell3Key));
        }
        finally
        {
            MembraneGenerationCoordinator.ClearCoordinator();
        }
    }

    [Fact]
    public async Task MembraneGenerationCoordinator_ConcurrentCellAddedDuringSecondPassIsNotStranded()
    {
        var membraneType = CreateTestMembraneType();

        // Run multiple iterations to exercise concurrent interleaving between second-pass completion and late arrivals
        for (int iteration = 0; iteration < 20; ++iteration)
        {
            long leaderKey = 3000 + iteration * 10L;
            long cell1Key = leaderKey + 1;
            long cell2Key = leaderKey + 2;
            long cell3Key = leaderKey + 3;

            try
            {
                var pos1 = new MulticellularMembraneGenerationCellData(new Vector2(0, 0), 0);
                var pos2 = new MulticellularMembraneGenerationCellData(new Vector2(2, 0), 0);
                var pos3 = new MulticellularMembraneGenerationCellData(new Vector2(4, 0), 0);

                var initGenerator = new MembraneShapeGenerator();
                var data1 = CreatePreStretchMembraneData(initGenerator, membraneType);
                Assert.Empty(MembraneGenerationCoordinator.CoordinateMulticellularGeneration(
                    initGenerator, data1, leaderKey, cell1Key, pos1, 2));

                // Concurrently complete the 2-cell batch with cell 2 while cell 3 arrives for a 3-cell colony
                var task2 = Task.Run(() =>
                {
                    var gen = new MembraneShapeGenerator();
                    var d2 = CreatePreStretchMembraneData(gen, membraneType);
                    return MembraneGenerationCoordinator.CoordinateMulticellularGeneration(
                        gen, d2, leaderKey, cell2Key, pos2, 2);
                });

                var task3 = Task.Run(() =>
                {
                    var gen = new MembraneShapeGenerator();
                    var d3 = CreatePreStretchMembraneData(gen, membraneType);
                    return MembraneGenerationCoordinator.CoordinateMulticellularGeneration(
                        gen, d3, leaderKey, cell3Key, pos3, 3);
                });

                var results = await Task.WhenAll(task2, task3);
                var result2 = results[0];
                var result3 = results[1];

                // Consume any membranes produced so far
                var completedKeys = new HashSet<long>(result2);
                completedKeys.UnionWith(result3);

                foreach (var key in completedKeys)
                {
                    Assert.NotNull(MembraneGenerationCoordinator.TryTakeFinishedMulticellularMembrane(key));
                }

                // If cell 3 arrived after the 2-cell batch started its second pass, result3 is empty and cell 3
                // is waiting in a fresh tracker for cells 1 and 2 to re-request with expectedCellCount = 3.
                if (!completedKeys.Contains(cell3Key))
                {
                    Assert.Contains(cell1Key, completedKeys);
                    Assert.Contains(cell2Key, completedKeys);

                    var gen = new MembraneShapeGenerator();
                    var reData1 = CreatePreStretchMembraneData(gen, membraneType);
                    var reData2 = CreatePreStretchMembraneData(gen, membraneType);

                    var reResult1 = MembraneGenerationCoordinator.CoordinateMulticellularGeneration(
                        gen, reData1, leaderKey, cell1Key, pos1, 3);
                    Assert.Empty(reResult1);

                    var reResult2 = MembraneGenerationCoordinator.CoordinateMulticellularGeneration(
                        gen, reData2, leaderKey, cell2Key, pos2, 3);

                    Assert.Equal(3, reResult2.Count);
                    Assert.Contains(cell1Key, reResult2);
                    Assert.Contains(cell2Key, reResult2);
                    Assert.Contains(cell3Key, reResult2);

                    Assert.NotNull(MembraneGenerationCoordinator.TryTakeFinishedMulticellularMembrane(cell1Key));
                    Assert.NotNull(MembraneGenerationCoordinator.TryTakeFinishedMulticellularMembrane(cell2Key));
                    Assert.NotNull(MembraneGenerationCoordinator.TryTakeFinishedMulticellularMembrane(cell3Key));
                }
                else
                {
                    // Cell 3 joined before the second pass started; all 3 should have completed together
                    Assert.Equal(3, completedKeys.Count);
                }
            }
            finally
            {
                MembraneGenerationCoordinator.ClearCoordinator();
            }
        }
    }
}
