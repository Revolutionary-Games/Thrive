namespace ThriveTest.Engine.Console.Tests;

using Godot;
using Xunit;

public class DebugEntryFactoryTests
{
    [Fact]
    public static void DebugEntryFactory_FlushAllExceptPreventsStackingAcrossCommandOutput()
    {
        var factory = new DebugEntryFactory();
        var normalMessage = new DebugConsoleManager.RawDebugEntry("Jukebox: starting track", Colors.White, 1, 1);
        var commandMessage = new DebugConsoleManager.RawDebugEntry("Skipped to next track", Colors.White, 2, 1 << 16);

        Assert.True(factory.TryAddMessage(normalMessage.Id, normalMessage));
        var firstNormalEntry = factory.GetDebugEntry(normalMessage.Id);

        factory.FlushAllExcept(commandMessage.Id);

        Assert.True(firstNormalEntry.Frozen);

        Assert.True(factory.TryAddMessage(commandMessage.Id, commandMessage));
        Assert.True(factory.TryAddMessage(normalMessage.Id, normalMessage));
        var secondNormalEntry = factory.GetDebugEntry(normalMessage.Id);

        Assert.NotSame(firstNormalEntry, secondNormalEntry);
        Assert.Equal(1, secondNormalEntry.Amount);
    }
}
