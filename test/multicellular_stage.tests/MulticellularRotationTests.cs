using System.Collections.Generic;
using Arch.Core.Extensions;
using Components;
using GdUnit4;
using Godot;
using Test.Utils;
using static GdUnit4.Assertions;

[TestSuite]
[RequireGodotRuntime]
public class MulticellularRotationTests
{
    [TestCase]
    public void AddingActomyosinToColonyCellTypeImprovesRotationSpeed()
    {
        using var world = new TestWorldSimulation();

        var parameters = SimulationParameters.Instance;
        var cellType = new CellType(parameters.GetMembrane("single"))
        {
            CellTypeName = "TestCell",
        };

        cellType.ModifiableOrganelles.Add(new OrganelleTemplate(parameters.GetOrganelleType("cytoplasm"),
            new Hex(-1, 0), 0));
        cellType.ModifiableOrganelles.Add(new OrganelleTemplate(parameters.GetOrganelleType("nucleus"),
            new Hex(4, 0), 0));

        var workMemory1 = new List<Hex>();
        var workMemory2 = new List<Hex>();

        var leader = world.EntitySystem.Create(CreateOrganelleContainer(cellType, workMemory1, workMemory2),
            new SpecializationFactor { TotalSpecializationBonus = 1 });
        var member = world.EntitySystem.Create(CreateOrganelleContainer(cellType, workMemory1, workMemory2),
            new SpecializationFactor { TotalSpecializationBonus = 1 },
            new MicrobeColonyMember(leader),
            new AttachedToEntity(leader, new Vector3(10, 0, 0), Quaternion.Identity));

        leader.Add(new MicrobeColony(leader, MicrobeState.Normal, leader, member));
        ref var colony = ref leader.Get<MicrobeColony>();
        colony.CalculateRotationSpeed();
        var rotationSpeedWithoutActomyosin = colony.ColonyRotationSpeed;

        cellType.ModifiableOrganelles.Add(new OrganelleTemplate(parameters.GetOrganelleType("actomyosin"),
            new Hex(1, 0), 0));

        ref var leaderOrganelles = ref leader.Get<OrganelleContainer>();
        leaderOrganelles.CreateOrganelleLayout(cellType, workMemory1, workMemory2);
        leaderOrganelles.FetchLayoutOrganelleComponents();

        ref var memberOrganelles = ref member.Get<OrganelleContainer>();
        memberOrganelles.CreateOrganelleLayout(cellType, workMemory1, workMemory2);
        memberOrganelles.FetchLayoutOrganelleComponents();

        colony.CalculateRotationSpeed();

        // Lower rotation values mean faster rotation.
        AssertThat(colony.ColonyRotationSpeed).IsLess(rotationSpeedWithoutActomyosin);
    }

    private static OrganelleContainer CreateOrganelleContainer(CellType cellType, List<Hex> workMemory1,
        List<Hex> workMemory2)
    {
        var result = default(OrganelleContainer);
        result.CreateOrganelleLayout(cellType, workMemory1, workMemory2);
        result.FetchLayoutOrganelleComponents();
        return result;
    }
}
