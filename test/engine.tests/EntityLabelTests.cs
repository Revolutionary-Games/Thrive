using Arch.Core.Extensions;
using Components;
using GdUnit4;
using static GdUnit4.Assertions;

/// <summary>
///   Verifies that entity labels are formatted correctly based on entity data.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class EntityLabelTests
{
    [TestCase]
    public void EntityLabel_RawEntityFormat()
    {
        using var world = ThriveWorld.Create();
        var entity = world.Create();

        AssertThat(DebugOverlays.FormatEntityDebugLabel(entity)).IsEqual($"[{entity.Id}-{entity.Version}]");
    }

    [TestCase]
    public void EntityLabel_ReadableNameFormat()
    {
        using var world = ThriveWorld.Create();

        // We don't want translation extraction to find this, so we have a separate string
        const string testName = "TestEntity";

        var entity = world.Create(new ReadableName(new LocalizedString(testName)));

        AssertThat(DebugOverlays.FormatEntityDebugLabel(entity))
            .IsEqual($"[{entity.Id}-{entity.Version}:{testName}]");
    }

    [TestCase]
    public void EntityLabel_SpeciesMemberFormat()
    {
        using var world = ThriveWorld.Create();
        var species = new MicrobeSpecies(1, "Organism", "prima");
        var entity = world.Create(new SpeciesMember(species));

        AssertThat(DebugOverlays.FormatEntityDebugLabel(entity)).IsEqual($"[{entity.Id}-{entity.Version}:O.prim]");
    }

    [TestCase]
    public void EntityLabel_MicrobeSignalNoneNotAppended()
    {
        using var world = ThriveWorld.Create();
        var species = new MicrobeSpecies(1, "Organism", "prima");
        var entity = world.Create(new SpeciesMember(species),
            new CommandSignaler
            {
                Command = MicrobeSignalCommand.None,
            });

        AssertThat(DebugOverlays.FormatEntityDebugLabel(entity)).IsEqual($"[{entity.Id}-{entity.Version}:O.prim]");
    }

    [TestCase]
    public void EntityLabel_MicrobeSignalAppendedWhenNotNone()
    {
        using var world = ThriveWorld.Create();
        var species = new MicrobeSpecies(1, "Organism", "prima");
        var entity = world.Create(new SpeciesMember(species),
            new CommandSignaler
            {
                Command = MicrobeSignalCommand.CallMate,
            });

        AssertThat(DebugOverlays.FormatEntityDebugLabel(entity))
            .IsEqual($"[{entity.Id}-{entity.Version}:O.prim]\nCallMate");

        ref var signaler = ref entity.Get<CommandSignaler>();
        signaler.Command = MicrobeSignalCommand.MoveToMe;

        AssertThat(DebugOverlays.FormatEntityDebugLabel(entity))
            .IsEqual($"[{entity.Id}-{entity.Version}:O.prim]\nMoveToMe");

        signaler.Command = MicrobeSignalCommand.None;

        AssertThat(DebugOverlays.FormatEntityDebugLabel(entity))
            .IsEqual($"[{entity.Id}-{entity.Version}:O.prim]");
    }
}
