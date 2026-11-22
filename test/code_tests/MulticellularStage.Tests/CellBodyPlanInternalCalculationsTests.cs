namespace ThriveTest.MulticellularStage.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public class CellBodyPlanInternalCalculationsTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void AdjacencySpecializationBonus_CavitiesDoubleOnlyTheBonusTheyGive(bool receiverIsCavity,
        bool neighbourIsCavity)
    {
        var type = CreateTestCellType();
        var layout = new IndividualHexLayout<CellTemplate>();
        var receiver = AddCell(layout, type, new Hex(0, 0), receiverIsCavity);
        var neighbour = AddCell(layout, type, new Hex(1, 0), neighbourIsCavity);
        AddCell(layout, type, new Hex(-1, 0));

        // Neither a different cell type nor a distant cell of the same type should contribute.
        AddCell(layout, CreateTestCellType(), new Hex(0, 1), true);
        AddCell(layout, type, new Hex(4, 0), true);

        var readonlyLayout = new ReadonlyIndividualLayoutAdapter<CellTemplate, IReadOnlyCellTemplate>(layout);
        var cells = layout.AsModifiable().ToList();
        var expectedReceiverBonus = 1 + Constants.CELL_ADJACENCY_SPECIALIZATION_BONUS *
            (1 + (neighbourIsCavity ? 2 : 1));
        var expectedNeighbourBonus = 1 + Constants.CELL_ADJACENCY_SPECIALIZATION_BONUS *
            (receiverIsCavity ? 2 : 1);

        Assert.Equal(expectedReceiverBonus,
            CellBodyPlanInternalCalculations.GetAdjacencySpecializationBonusFromBodyPlan(receiver, readonlyLayout), 6);
        Assert.Equal(expectedReceiverBonus,
            CellBodyPlanInternalCalculations.GetAdjacencySpecializationBonusFromBodyPlan(receiver, cells), 6);
        Assert.Equal(expectedNeighbourBonus,
            CellBodyPlanInternalCalculations.GetAdjacencySpecializationBonusFromBodyPlan(neighbour, readonlyLayout), 6);
        Assert.Equal(expectedNeighbourBonus,
            CellBodyPlanInternalCalculations.GetAdjacencySpecializationBonusFromBodyPlan(neighbour, cells), 6);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CavitySupport_CountsOnlyOccupiedNeighbourPositions(bool neighboursAreCavities)
    {
        var type = CreateTestCellType();
        var layout = new IndividualHexLayout<CellTemplate>();
        var centre = new Hex(7, -9);
        AddCell(layout, type, centre, true);
        var offsets = Hex.HexNeighbourOffset.Values.ToList();

        Assert.InRange(Constants.CAVITY_CELL_MINIMUM_NEIGHBOURS, 1, offsets.Count);

        // Cells farther away must not provide support, even when there are enough of them.
        foreach (var offset in offsets)
            AddCell(layout, type, centre + offset + offset);

        for (int i = 0; i <= offsets.Count; ++i)
        {
            Assert.Equal(i >= Constants.CAVITY_CELL_MINIMUM_NEIGHBOURS,
                CellBodyPlanInternalCalculations.HasEnoughCavityNeighbours(centre, layout));

            if (i < offsets.Count)
                AddCell(layout, type, centre + offsets[i], neighboursAreCavities);
        }
    }

    [Fact]
    public void CavitySupport_RequiresEnoughNeighboursForEveryCavity()
    {
        var type = CreateTestCellType();
        var layout = new IndividualHexLayout<CellTemplate>();
        var offsets = Hex.HexNeighbourOffset.Values.ToList();
        Assert.InRange(Constants.CAVITY_CELL_MINIMUM_NEIGHBOURS, 1, offsets.Count);
        Assert.False(CellBodyPlanInternalCalculations.HasUnsupportedCavities(layout));

        // Ordinary cells can have fewer neighbours than the cavity support requirement.
        var first = AddCell(layout, type, new Hex(0, 0));
        Assert.False(CellBodyPlanInternalCalculations.HasUnsupportedCavities(layout));
        first.IsCavity = true;
        Assert.True(CellBodyPlanInternalCalculations.HasUnsupportedCavities(layout));

        for (int i = 0; i < Constants.CAVITY_CELL_MINIMUM_NEIGHBOURS; ++i)
            AddCell(layout, type, first.Position + offsets[i]);

        Assert.False(CellBodyPlanInternalCalculations.HasUnsupportedCavities(layout));

        // A supported cavity must not hide another cavity that is just below the requirement.
        var second = AddCell(layout, type, new Hex(10, -4), true);
        for (int i = 0; i < Constants.CAVITY_CELL_MINIMUM_NEIGHBOURS - 1; ++i)
            AddCell(layout, type, second.Position + offsets[i]);

        Assert.True(CellBodyPlanInternalCalculations.HasUnsupportedCavities(layout));
        AddCell(layout, type, second.Position + offsets[Constants.CAVITY_CELL_MINIMUM_NEIGHBOURS - 1]);
        Assert.False(CellBodyPlanInternalCalculations.HasUnsupportedCavities(layout));
    }

    [Fact]
    public void CalculateFinalColonyRotation_AppliesCellCountAsPenalty()
    {
        const float averageCellRotationSpeed = 2.0f;

        var singleCellRotationSpeed =
            CellBodyPlanInternalCalculations.CalculateFinalColonyRotation(averageCellRotationSpeed, 1);
        var colonyRotationSpeed =
            CellBodyPlanInternalCalculations.CalculateFinalColonyRotation(averageCellRotationSpeed, 2);

        // Higher values are slower, so this means that colony rotation is slower than single cell rotation.
        Assert.True(colonyRotationSpeed > singleCellRotationSpeed);
    }

    [Fact]
    public void CalculateActomyosinRotationMultiplier_AppliesActomyosinCountAsBonus()
    {
        var zeroActomyosinBonus = CellBodyPlanInternalCalculations.CalculateActomyosinRotationMultiplier(0);
        var oneActomyosinBonus = CellBodyPlanInternalCalculations.CalculateActomyosinRotationMultiplier(1);
        var twoActomyosinBonus = CellBodyPlanInternalCalculations.CalculateActomyosinRotationMultiplier(2);

        // The higher the rotation value, the slower the rotation is,
        // but this bonus is used to multiply the denominator in the rotation calculation.

        // Having one Actomyosin provides a bonus, while 0 Actomyosin does not.
        Assert.True(Math.Abs(zeroActomyosinBonus - 1.0f) < float.Epsilon);
        Assert.True(1 / zeroActomyosinBonus > 1 / oneActomyosinBonus);

        // Having two Actomyosins provides more of a bonus than having one.
        Assert.True(1 / oneActomyosinBonus > 1 / twoActomyosinBonus);
    }

    private static CellType CreateTestCellType()
    {
        return new CellType(new OrganelleLayout<OrganelleTemplate>(), new MembraneType { Name = "Dummy" });
    }

    private static CellTemplate AddCell(IndividualHexLayout<CellTemplate> layout, CellType type, Hex position,
        bool isCavity = false)
    {
        var cell = new CellTemplate(type, position, 0, isCavity);
        layout.AddFast(new HexWithData<CellTemplate>(cell, position, 0), new List<Hex>(), new List<Hex>());
        return cell;
    }
}
