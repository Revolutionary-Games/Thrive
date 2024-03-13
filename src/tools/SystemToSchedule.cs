namespace Tools;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DefaultEcs.System;

public class SystemToSchedule
{
    public static readonly Type CompilerGeneratedAttribute = typeof(CompilerGeneratedAttribute);

    public readonly Type Type;
    public readonly string FieldName;

    // public readonly int OriginalOrder;

    // TODO: add a relative cost field here and weight the amount of systems that can be given to a single thread
    // Also should remember to modify AheadPenaltyPerTask value with the system weight multiplier

    public string? RunCondition;
    public string? CustomRunCode;

    public bool RunsOnFrame;
    public bool RunsOnMainThread;

    public HashSet<SystemToSchedule> RunsBefore = new();
    public HashSet<SystemToSchedule> RunsAfter = new();

    public List<Type> ReadsComponents = new();
    public List<Type> WritesComponents = new();

    public float RuntimeCost = 1;

    // Count of how many barriers are needed
    public int RequiresBarrierBefore;
    public int RequiresBarrierAfter;

    /// <summary>
    ///   Thread that this was simulated to run on
    /// </summary>
    public int ThreadId;

    /// <summary>
    ///   Timeslot this was simulated to run in
    /// </summary>
    public int Timeslot;

    private static readonly Type SystemWithAttribute = typeof(WithAttribute);
    private static readonly Type WritesToAttribute = typeof(WritesToComponentAttribute);
    private static readonly Type ReadsFromAttribute = typeof(ReadsComponentAttribute);
    private static readonly Type ReadByDefaultAttribute = typeof(ComponentIsReadByDefaultAttribute);
    private static readonly Type RunsAfterAttribute = typeof(RunsAfterAttribute);
    private static readonly Type RunsBeforeAttribute = typeof(RunsBeforeAttribute);
    private static readonly Type RunsOnMainAttribute = typeof(RunsOnMainThreadAttribute);
    private static readonly Type ConditionalRunAttribute = typeof(RunsConditionallyAttribute);
    private static readonly Type CustomRunAttribute = typeof(RunsWithCustomCodeAttribute);
    private static readonly Type RunsOnFrameAttribute = typeof(RunsOnFrameAttribute);
    private static readonly Type RuntimeCostAttribute = typeof(RuntimeCostAttribute);

    public SystemToSchedule(Type type, string name)
    {
        Type = type;
        FieldName = name;
    }

    public static void ParseSystemAttributes(SystemToSchedule systemToSchedule)
    {
        systemToSchedule.RunsOnFrame = systemToSchedule.Type.GetCustomAttribute(RunsOnFrameAttribute) != null;
        systemToSchedule.RunsOnMainThread = systemToSchedule.Type.GetCustomAttribute(RunsOnMainAttribute) != null;

        var conditionalRun = systemToSchedule.Type.GetCustomAttribute(ConditionalRunAttribute);

        if (conditionalRun != null)
        {
            systemToSchedule.RunCondition = ((RunsConditionallyAttribute)conditionalRun).Condition;
        }

        var customRun = systemToSchedule.Type.GetCustomAttribute(CustomRunAttribute);

        if (customRun != null)
        {
            systemToSchedule.CustomRunCode = ((RunsWithCustomCodeAttribute)customRun).CustomCode;
        }

        var customCost = systemToSchedule.Type.GetCustomAttribute(RuntimeCostAttribute);

        if (customCost != null)
        {
            systemToSchedule.RuntimeCost = ((RuntimeCostAttribute)customCost).Cost;
        }

        var expectedWritesTo = new List<Type>();
        var expectedReadsFrom = new List<Type>();

        var explicitReads = new HashSet<Type>();

        var withRaw = systemToSchedule.Type.GetCustomAttributes(SystemWithAttribute);

        foreach (var attributeRaw in withRaw)
        {
            var attribute = (WithAttribute)attributeRaw;

            // TODO: handle without attributes?
            if (attribute.FilterType is not (ComponentFilterType.WithoutEither or ComponentFilterType.Without))
            {
                foreach (var componentType in attribute.ComponentTypes)
                {
                    // Handle components that are read by default
                    if (componentType.GetCustomAttribute(ReadByDefaultAttribute) != null)
                    {
                        expectedReadsFrom.Add(componentType);
                        continue;
                    }

                    expectedWritesTo.Add(componentType);
                }
            }
        }

        var readsRaw = systemToSchedule.Type.GetCustomAttributes(ReadsFromAttribute);

        foreach (var attributeRaw in readsRaw)
        {
            var attribute = (ReadsComponentAttribute)attributeRaw;

            // Convert writes to reads if exist
            expectedWritesTo.Remove(attribute.ReadsFrom);

            if (!expectedReadsFrom.Contains(attribute.ReadsFrom))
                expectedReadsFrom.Add(attribute.ReadsFrom);

            explicitReads.Add(attribute.ReadsFrom);
        }

        var writesRaw = systemToSchedule.Type.GetCustomAttributes(WritesToAttribute);

        foreach (var attributeRaw in writesRaw)
        {
            var attribute = (WritesToComponentAttribute)attributeRaw;

            if (explicitReads.Contains(attribute.WritesTo))
            {
                throw new Exception(
                    "Shouldn't specify a writes to component with already a read attribute for same type");
            }

            // Convert implicit reads to writes
            expectedReadsFrom.Remove(attribute.WritesTo);

            if (!expectedWritesTo.Contains(attribute.WritesTo))
                expectedWritesTo.Add(attribute.WritesTo);
        }

        systemToSchedule.WritesComponents = expectedWritesTo;
        systemToSchedule.ReadsComponents = expectedReadsFrom;
    }

    public static void ResolveSystemDependencies(List<SystemToSchedule> systems)
    {
        SystemToSchedule GetSystemByType(Type type)
        {
            return systems.First(s => s.Type == type);
        }

        foreach (var system in systems)
        {
            var runsBeforeRaw = system.Type.GetCustomAttributes(RunsBeforeAttribute);

            foreach (var attributeRaw in runsBeforeRaw)
            {
                var attribute = (RunsBeforeAttribute)attributeRaw;

                var otherSystem = GetSystemByType(attribute.BeforeSystem);
                system.RunsBefore.Add(otherSystem);

                // TODO: should we instead require explicit attributes on both sides of the relationship?
                // That would require more lines of code but there would be less hidden things to know about when
                // reading a class.

                // Add the other side of the relationship automatically
                otherSystem.RunsAfter.Add(system);
            }

            var runsAfterRaw = system.Type.GetCustomAttributes(RunsAfterAttribute);

            foreach (var attributeRaw in runsAfterRaw)
            {
                var attribute = (RunsAfterAttribute)attributeRaw;

                var otherSystem = GetSystemByType(attribute.AfterSystem);
                system.RunsAfter.Add(otherSystem);
                otherSystem.RunsBefore.Add(system);
            }
        }

        // Detect cycles in runs before or runs after
        // TODO: this is likely not good enough currently
        var seenSystems = new HashSet<SystemToSchedule>();

        foreach (var system in systems)
        {
            if (system.RunsAfter.Any(s => system.RunsBefore.Contains(s)))
                throw new Exception("A system is set to run both after and before another");

            if (system.RunsBefore.Any(s => system.RunsAfter.Contains(s)))
                throw new Exception("A system is set to run both after and before another");

            // System dependencies are not allowed to run after itself
            DoNotAllowSeeingRunsAfter(system, system, seenSystems);
            seenSystems.Clear();

            // Or before itself
            DoNotAllowSeeingRunsBefore(system, system, seenSystems);
            seenSystems.Clear();
        }

        // Add recursive dependencies
        foreach (var system in systems)
        {
            foreach (var runsAfter in system.RunsAfter.ToList())
            {
                CollectRunsAfter(runsAfter, system.RunsAfter);
            }

            if (system.RunsAfter.Contains(system))
                throw new Exception("System ended up running after itself after recursive resolve");

            foreach (var runsBefore in system.RunsBefore.ToList())
            {
                CollectRunsBefore(runsBefore, system.RunsBefore);
            }

            if (system.RunsBefore.Contains(system))
                throw new Exception("System ended up running before itself after recursive resolve");
        }
    }

    public bool ShouldRunBefore(SystemToSchedule other)
    {
        // This depends on the data setup to already check for conflicting (circular) run relationships
        if (RunsBefore.Contains(other))
            return true;

        if (other.RunsAfter.Contains(this))
            return true;

        return false;
    }

    public bool WantsToWriteBefore(SystemToSchedule other)
    {
        return WritesComponents.Any(c => other.ReadsComponents.Contains(c) && !other.WritesComponents.Contains(c));
    }

    public bool ShouldRunAfter(SystemToSchedule other)
    {
        if (RunsAfter.Contains(other))
            return true;

        if (other.RunsBefore.Contains(this))
            return true;

        return false;
    }

    public bool CanRunConcurrently(SystemToSchedule otherSystem)
    {
        // System ordering constraints
        if (ShouldRunAfter(otherSystem) || ShouldRunBefore(otherSystem))
            return false;

        if (otherSystem.ShouldRunAfter(this) || otherSystem.ShouldRunBefore(this))
            return false;

        // Write / read conflicts
        if (ReadsComponents.Any(c =>
                otherSystem.ReadsComponents.Contains(c) || otherSystem.WritesComponents.Contains(c)))
        {
            return false;
        }

        if (otherSystem.WritesComponents.Any(c => ReadsComponents.Contains(c) || WritesComponents.Contains(c)))
        {
            return false;
        }

        return true;
    }

    public void GetRunningText(List<string> lineReceiver, int indent, int thread)
    {
        for (int i = 0; i < RequiresBarrierBefore; ++i)
        {
            GenerateThreadedSystems.AddBarrierWait(lineReceiver, 1, thread, indent);
        }

        bool closeBrace = false;

        if (RunCondition != null)
        {
            lineReceiver.Add(StringUtils.GetIndent(indent) + $"if ({RunCondition})");
            lineReceiver.Add(StringUtils.GetIndent(indent) + '{');

            // Indent inside the condition
            indent += 4;
            closeBrace = true;
        }

        if (GenerateThreadedSystems.DebugGuardComponentWrites)
        {
            GenerateThreadedSystems.EnsureOneBlankLine(lineReceiver);

            if (thread == -1)
            {
                lineReceiver.Add(StringUtils.GetIndent(indent) +
                    "// Omitted component check as not running multithreaded");
            }
            else
            {
                foreach (var component in WritesComponents)
                {
                    lineReceiver.Add(StringUtils.GetIndent(indent) +
                        $"OnThreadAccessComponent(true, \"{component.Name}\", \"{Type.Name}\",{thread});");
                }

                foreach (var component in ReadsComponents)
                {
                    lineReceiver.Add(StringUtils.GetIndent(indent) +
                        $"OnThreadAccessComponent(false, \"{component.Name}\", \"{Type.Name}\",{thread});");
                }
            }
        }

        if (GenerateThreadedSystems.UseCheckedComponentAccess)
        {
            foreach (var component in WritesComponents)
            {
                lineReceiver.Add(StringUtils.GetIndent(indent) +
                    $"ComponentAccessChecks.ReportAllowedAccess(\"{component.Name}\");");
            }

            foreach (var component in ReadsComponents)
            {
                lineReceiver.Add(StringUtils.GetIndent(indent) +
                    $"ComponentAccessChecks.ReportAllowedAccess(\"{component.Name}\");");
            }
        }

        if (CustomRunCode != null)
        {
            foreach (var customLine in CustomRunCode.Split("\n"))
            {
                lineReceiver.Add(StringUtils.GetIndent(indent) + string.Format(customLine, FieldName));
            }
        }
        else
        {
            lineReceiver.Add(StringUtils.GetIndent(indent) + $"{FieldName}.Update(delta);");
        }

        if (GenerateThreadedSystems.UseCheckedComponentAccess)
        {
            lineReceiver.Add(StringUtils.GetIndent(indent) +
                "ComponentAccessChecks.ClearAccessForCurrentThread();");
        }

        if (closeBrace)
        {
            indent -= 4;
            lineReceiver.Add(StringUtils.GetIndent(indent) + '}');
            GenerateThreadedSystems.EnsureOneBlankLine(lineReceiver);
        }

        // Barriers after condition so that barriers aren't conditionally skipped, that would be really hard to
        // balance across threads
        for (int i = 0; i < RequiresBarrierAfter; ++i)
        {
            GenerateThreadedSystems.AddBarrierWait(lineReceiver, 1, thread, indent);
        }
    }

    public override string ToString()
    {
        return Type.Name;
    }

    private static void DoNotAllowSeeingRunsAfter(SystemToSchedule systemToNotRunAfter,
        SystemToSchedule checkFrom, HashSet<SystemToSchedule> alreadyVisited)
    {
        foreach (var systemToSchedule in checkFrom.RunsAfter)
        {
            if (!alreadyVisited.Add(systemToSchedule))
                continue;

            // Check if we found a dependency on a system that is not allowed (but only if processing a list of the
            // wanted type, otherwise this is just meant to recursively traverse to find more lists of the
            // right type)
            if (systemToSchedule == systemToNotRunAfter && !systemToNotRunAfter.RunsBefore.Contains(checkFrom))
            {
                throw new Exception("Seen a system reference a system it shouldn't run after " +
                    $"({systemToNotRunAfter.Type.Name} depends on {checkFrom.Type.Name})");
            }

            DoNotAllowSeeingRunsAfter(systemToNotRunAfter, systemToSchedule, alreadyVisited);
        }

        foreach (var systemToSchedule in checkFrom.RunsBefore)
        {
            if (!alreadyVisited.Add(systemToSchedule))
                continue;

            DoNotAllowSeeingRunsAfter(systemToNotRunAfter, systemToSchedule, alreadyVisited);
        }
    }

    private static void DoNotAllowSeeingRunsBefore(SystemToSchedule systemToNotRunBefore,
        SystemToSchedule checkFrom, HashSet<SystemToSchedule> alreadyVisited)
    {
        foreach (var systemToSchedule in checkFrom.RunsAfter)
        {
            if (!alreadyVisited.Add(systemToSchedule))
                continue;

            DoNotAllowSeeingRunsBefore(systemToNotRunBefore, systemToSchedule, alreadyVisited);
        }

        foreach (var systemToSchedule in checkFrom.RunsBefore)
        {
            if (!alreadyVisited.Add(systemToSchedule))
                continue;

            if (systemToSchedule == systemToNotRunBefore && !systemToNotRunBefore.RunsAfter.Contains(checkFrom))
            {
                throw new Exception("Seen a system reference a system it shouldn't run after " +
                    $"({systemToNotRunBefore.Type.Name} depends on {checkFrom.Type.Name})");
            }

            DoNotAllowSeeingRunsBefore(systemToNotRunBefore, systemToSchedule, alreadyVisited);
        }
    }

    private static void CollectRunsAfter(SystemToSchedule systemToStart, HashSet<SystemToSchedule> result)
    {
        foreach (var runsAfter in systemToStart.RunsAfter)
        {
            if (result.Add(runsAfter))
                CollectRunsAfter(runsAfter, result);
        }
    }

    private static void CollectRunsBefore(SystemToSchedule systemToStart, HashSet<SystemToSchedule> result)
    {
        foreach (var runsBefore in systemToStart.RunsBefore)
        {
            if (result.Add(runsBefore))
                CollectRunsBefore(runsBefore, result);
        }
    }

    // NOTE: this doesn't work for a single pass sort as this can consider next to each other items equal, but then
    // items on either side of that block of non-sortables need to be sorted around the block
    public class SystemRequirementsBasedComparer : IComparer<SystemToSchedule>
    {
        public int Compare(SystemToSchedule x, SystemToSchedule y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (ReferenceEquals(null, y))
                return 1;
            if (ReferenceEquals(null, x))
                return -1;

            if (x.ShouldRunBefore(y))
                return -1;

            if (y.ShouldRunBefore(x))
                return 1;

            if (x.ShouldRunAfter(y))
                return 1;

            if (y.ShouldRunAfter(x))
                return -1;

            // Writes before reads ordering, but only if clean order can be found. Both ways need to be checked as
            // there are systems that both write to a component type that the other just reads.
            // Might be fun to debug print info when systems cannot do this cleanly to give potential places to
            // improve the systems decoupling
            bool xWantsWrite = x.WantsToWriteBefore(y);
            bool yWantsWrite = y.WantsToWriteBefore(x);
            if (xWantsWrite && !yWantsWrite)
                return -1;

            if (yWantsWrite && !xWantsWrite)
                return 1;

            // No requirement for either system to run before the other
            return 0;
        }

        public int CompareWeak(SystemToSchedule x, SystemToSchedule y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (ReferenceEquals(null, y))
                return 1;
            if (ReferenceEquals(null, x))
                return -1;

            if (x.ShouldRunBefore(y))
                return -1;

            if (y.ShouldRunBefore(x))
                return 1;

            if (x.ShouldRunAfter(y))
                return 1;

            if (y.ShouldRunAfter(x))
                return -1;

            // Weak variant that doesn't enforce write / read relationships

            return 0;
        }
    }
}
