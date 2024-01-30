using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DefaultEcs.System;
using Godot;
using Environment = System.Environment;

/// <summary>
///   Tool that generates a multithreaded sequence of runs for systems to run in parallel. This is implemented in the
///   main module as this needs to be able to load for example <see cref="MicrobeWorldSimulation"/> that depends on
///   Godot types.
/// </summary>
public class GenerateThreadedSystems : Node
{
    private const int DefaultSeed = 334564234;

    private readonly Random random;

    private readonly IReadOnlyList<(Type Class, string File, string EndOfProcess)> simulationTypes =
        new List<(Type Class, string File, string EndOfProcess)>
        {
            (typeof(MicrobeWorldSimulation), "src/microbe_stage/MicrobeWorldSimulation.generated.cs",
                "cellCountingEntitySet.Complete();\nreportedPlayerPosition = null;"),
        };

    private readonly Type systemBaseType = typeof(ISystem<>);
    private readonly Type systemWithAttribute = typeof(WithAttribute);

    // private readonly Type systemWithoutAttribute = typeof(WithoutAttribute);

    private readonly Type writesToAttribute = typeof(WritesToComponentAttribute);
    private readonly Type readsFromAttribute = typeof(ReadsComponentAttribute);
    private readonly Type runsAfterAttribute = typeof(RunsAfterAttribute);
    private readonly Type runsBeforeAttribute = typeof(RunsBeforeAttribute);
    private readonly Type runsOnMainAttribute = typeof(RunsOnMainThreadAttribute);
    private readonly Type conditionalRunAttribute = typeof(RunsConditionallyAttribute);
    private readonly Type customRunAttribute = typeof(RunsWithCustomCodeAttribute);

    private readonly Type runsOnFrameAttribute = typeof(RunsOnFrameAttribute);

    private readonly Type compilerGeneratedAttribute = typeof(CompilerGeneratedAttribute);

    private bool done;

    private GenerateThreadedSystems()
    {
        random = new Random(DefaultSeed);
    }

    public override void _Ready()
    {
        TaskExecutor.Instance.ParallelTasks = Environment.ProcessorCount;
        TaskExecutor.Instance.AddTask(new Task(Run));
    }

    public override void _Process(float delta)
    {
        if (done)
            GetTree().Quit();
    }

    public void Run()
    {
        try
        {
            RunInternal();
        }
        catch (Exception e)
        {
            GD.PrintErr("Failed to run: ", e);
            throw;
        }
        finally
        {
            done = true;
        }
    }

    private static string GetIndentForText(int indent)
    {
        if (indent < 1)
            return string.Empty;

        return new string(' ', indent);
    }

    private static int DetectLineIndentationLevel(string line)
    {
        int spaceCount = 0;

        foreach (var character in line)
        {
            if (character <= ' ')
            {
                ++spaceCount;
            }
            else
            {
                break;
            }
        }

        return spaceCount;
    }

    private static void EnsureOneBlankLine(List<string> lines)
    {
        if (lines.Count < 1)
            return;

        if (!string.IsNullOrWhiteSpace(lines[lines.Count - 1]))
        {
            lines.Add(string.Empty);
        }
    }

    private void RunInternal()
    {
        GD.Print("Beginning generation of threaded system runs, it might take a while to find valid sequences of runs");

        foreach (var (simulationClass, file, processEnd) in simulationTypes)
        {
            GD.Print($"Beginning processing class {simulationClass}");

            GetSystemsInCategories(simulationClass, out var frameSystems, out var processSystems);

            // Frame systems are not currently multithreaded, so just sort those
            SortSingleGroupOfSystems(frameSystems);
            VerifyOrderOfSystems(frameSystems);

            var frameSystemTextLines = new List<string>
            {
                "ThrowIfNotInitialized();",
                string.Empty,
                "// NOTE: not currently ran in parallel due to low frame system count",
            };

            AddSystemSingleGroupRunningLines(frameSystems, frameSystemTextLines, 0);

            var processSystemTextLines = new List<string>();

            var (mainSystems, otherSystems) = SplitSystemsToMainThread(processSystems);

            GenerateThreadedSystemsRun(mainSystems, otherSystems, processSystemTextLines);

            if (!string.IsNullOrWhiteSpace(processEnd))
            {
                foreach (var line in processEnd.Split("\n"))
                {
                    EnsureOneBlankLine(processSystemTextLines);

                    processSystemTextLines.Add(line);
                }
            }

            InsertNewProcessMethods(file, simulationClass.Name, processSystemTextLines, frameSystemTextLines);

            GD.Print($"Successfully handled. {file} has been updated");
        }
    }

    private void GenerateThreadedSystemsRun(List<SystemToSchedule> mainSystems, List<SystemToSchedule> otherSystems,
        List<string> processSystemTextLines)
    {
        // Make sure main systems are sorted according to when they should run
        SortSingleGroupOfSystems(mainSystems);
        VerifyOrderOfSystems(mainSystems);

        // Create rough ordering for the other systems to run (this is just an initial list and will be used just to
        // create thread groups)
        SortSingleGroupOfSystems(otherSystems);
        VerifyOrderOfSystems(otherSystems);

        // First go of execution groups based on each one of the main thread systems (but combine subsequent ones that
        // require the next)
        var groups = new List<ExecutionGroup>();

        // Add systems that are considered equal to the same execution group
        var comparer = new SystemRequirementsBasedComparer();

        foreach (var mainSystem in mainSystems)
        {
            if (groups.Count > 0 && comparer.CompareWeak(mainSystem, groups[groups.Count - 1].Systems.Last()) <= 0)
            {
                groups[groups.Count - 1].Systems.Add(mainSystem);
                continue;
            }

            groups.Add(new ExecutionGroup
            {
                Systems = { mainSystem },
            });
        }

        // TODO: temporary code
        // Add all other systems to a group it needs to go in
        foreach (var group in groups)
        {
            // Add stuff needing to run before the group systems

            for (int i = 0; i < otherSystems.Count; ++i)
            {
                var current = otherSystems[i];

                bool added = false;

                for (var j = 0; j < group.Systems.Count; j++)
                {
                    var groupSystem = group.Systems[j];
                    if (comparer.CompareWeak(current, groupSystem) < 0)
                    {
                        // Insert before the system this needs to be before
                        group.Systems.Insert(j, current);
                        added = true;
                        break;
                    }
                }

                if (added)
                {
                    otherSystems.RemoveAt(i);
                    --i;
                }
            }
        }

        // Add remaining systems to the last group
        while (otherSystems.Count > 0)
        {
            groups[groups.Count - 1].Systems.Add(otherSystems[0]);
            otherSystems.RemoveAt(0);
        }

        // Need to sort this as the above loop didn't take sorting into account
        SortSingleGroupOfSystems(groups[groups.Count - 1].Systems);

        // Verify all group orders are correct
        foreach (var group in groups)
        {
            VerifyOrderOfSystems(group.Systems);
        }

        // Generate the final results
        WriteResultOfThreadedRunning(groups, processSystemTextLines);
    }

    private void WriteResultOfThreadedRunning(List<ExecutionGroup> groups, List<string> lineReceiver)
    {
        int count = 1;

        foreach (var group in groups)
        {
            group.GenerateCode(lineReceiver, count++);
        }
    }

    private void AddSystemSingleGroupRunningLines(IEnumerable<SystemToSchedule> systems, List<string> textOutput,
        int indent)
    {
        foreach (var system in systems)
        {
            system.GetRunningText(textOutput, indent);
        }
    }

    /// <summary>
    ///   Need to use an insertion sort (that doesn't exit early) to make sure that items are fully sorted as the
    ///   comparer can't pick a relative order for some items causing breaks in the sorting. Sorts in a stable way
    ///   (i.e. OriginalOrder is respected for equal items, assuming <see cref="systems"/> hasn't been modified in way
    ///   that doesn't preserve OriginalOrder).
    /// </summary>
    /// <param name="systems">Systems to sort in-place</param>
    /// <returns>The value in <see cref="systems"/></returns>
    private IList<SystemToSchedule> SortSingleGroupOfSystems(IList<SystemToSchedule> systems)
    {
        var comparer = new SystemRequirementsBasedComparer();

        // This is really not efficient but for now the sorting needs to continue until all the constraints are taken
        // into account and nothing is sorted anymore

        int maxSorts = systems.Count;
        int sortAttempt = 0;
        bool sortedSomething;
        do
        {
            sortedSomething = false;
            ++sortAttempt;

            // Used to prevent infinite loop when trying to sort something that cannot fulfil all the constraints
            int singleSpotRetries = systems.Count * 10;

            for (int i = 1; i < systems.Count; ++i)
            {
                var item = systems[i];
                int insertPoint = i;

                // Look for any items the current item should be before. This is required as the comparer can encounter
                // sequences of items it cannot order
                for (int j = i - 1; j >= 0; --j)
                {
                    var other = systems[j];

                    // Stop if encountering a system this should be after
                    if (comparer.CompareWeak(item, other) > 0)
                        break;

                    // Keep track of the earliest item this wants to be before
                    if (comparer.Compare(item, other) < 0)
                        insertPoint = j;
                }

                if (insertPoint >= i)
                {
                    // Item is already sorted
                    continue;
                }

                if (sortAttempt >= maxSorts)
                {
                    GD.PrintErr($"Sorting is stuck, still wanting to sort {item.Type.Name} to be before " +
                        $"{systems[insertPoint].Type.Name}");
                }

                // Insert to the right index
                systems.RemoveAt(i);
                systems.Insert(insertPoint, item);

                // New item at i so this needs to be repeated
                if (--singleSpotRetries >= 0)
                    --i;

                sortedSomething = true;
            }
        }
        while (sortedSomething && sortAttempt <= maxSorts);

        if (sortAttempt + 1 >= maxSorts)
        {
            VerifyOrderOfSystems(systems);
        }

        return systems;
    }

    private void VerifyOrderOfSystems(IList<SystemToSchedule> systems)
    {
        var comparer = new SystemRequirementsBasedComparer();

        for (int i = 0; i < systems.Count; ++i)
        {
            for (int j = i + 1; j < systems.Count; ++j)
            {
                if (comparer.CompareWeak(systems[i], systems[j]) > 0)
                {
                    throw new Exception($"Systems not fully sorted according to rules ({systems[i].Type.Name}" +
                        $"> {systems[j].Type.Name})");
                }
            }
        }
    }

    private void GetSystemsInCategories(Type type, out List<SystemToSchedule> frameSystems,
        out List<SystemToSchedule> processSystems)
    {
        var all = GetAllSystemsFromClass(type);

        frameSystems = all.Where(s => s.RunsOnFrame).ToList();
        processSystems = all.Where(s => !s.RunsOnFrame).ToList();
    }

    private List<SystemToSchedule> GetAllSystemsFromClass(Type type)
    {
        var result = new List<SystemToSchedule>();

        int order = 0;

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
        {
            // Skip automatic property backing fields
            if (field.CustomAttributes.Any(a => a.AttributeType == compilerGeneratedAttribute))
                continue;

            if (!field.FieldType.IsAssignableToGenericType(systemBaseType))
                continue;

            result.Add(new SystemToSchedule(field.FieldType, field.Name, order++));
        }

        foreach (var property in type.GetProperties(
                     BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
        {
            if (!property.PropertyType.IsAssignableToGenericType(systemBaseType))
                continue;

            // Skip things that are already used through fields
            if (result.Any(r => r.FieldName.Equals(property.Name, StringComparison.OrdinalIgnoreCase)))
                continue;

            result.Add(new SystemToSchedule(property.PropertyType, property.Name, order++));
        }

        foreach (var systemToSchedule in result)
        {
            ParseSystemAttributes(systemToSchedule);
        }

        ResolveSystemDependencies(result);

        // Sanity check no duplicate systems found
        if (result.GroupBy(s => s.Type).Any(g => g.Count() > 1))
            throw new Exception("Some type of system is included multiple times");

        // Make sure sorting works sensibly
        var comparer = new SystemRequirementsBasedComparer();
        foreach (var item1 in result)
        {
            foreach (var item2 in result)
            {
                if (ReferenceEquals(item1, item2))
                    continue;

                int sort1 = comparer.Compare(item1, item2);
                int sort2 = comparer.Compare(item2, item1);

                if (sort1 == 0 && sort2 == 0)
                    continue;

                if (Math.Sign(sort1) == Math.Sign(sort2))
                {
                    throw new Exception($"Sorter cannot handle systems {item1.Type.Name} and {item2.Type.Name}");
                }
            }
        }

        return result;
    }

    private void ParseSystemAttributes(SystemToSchedule systemToSchedule)
    {
        systemToSchedule.RunsOnFrame = systemToSchedule.Type.GetCustomAttribute(runsOnFrameAttribute) != null;
        systemToSchedule.RunsOnMainThread = systemToSchedule.Type.GetCustomAttribute(runsOnMainAttribute) != null;

        var conditionalRun = systemToSchedule.Type.GetCustomAttribute(conditionalRunAttribute);

        if (conditionalRun != null)
        {
            systemToSchedule.RunCondition = ((RunsConditionallyAttribute)conditionalRun).Condition;
        }

        var customRun = systemToSchedule.Type.GetCustomAttribute(customRunAttribute);

        if (customRun != null)
        {
            systemToSchedule.CustomRunCode = ((RunsWithCustomCodeAttribute)customRun).CustomCode;
        }

        var expectedWritesTo = new List<Type>();

        var withRaw = systemToSchedule.Type.GetCustomAttributes(systemWithAttribute);

        foreach (var attributeRaw in withRaw)
        {
            var attribute = (WithAttribute)attributeRaw;

            // TODO: handle without attributes?
            if (attribute.FilterType is not (ComponentFilterType.WithoutEither or ComponentFilterType.Without))
            {
                foreach (var componentType in attribute.ComponentTypes)
                {
                    expectedWritesTo.Add(componentType);
                }
            }
        }

        var expectedReadsFrom = new List<Type>();

        var readsRaw = systemToSchedule.Type.GetCustomAttributes(readsFromAttribute);

        foreach (var attributeRaw in readsRaw)
        {
            var attribute = (ReadsComponentAttribute)attributeRaw;

            // Convert writes to reads if exist
            expectedWritesTo.Remove(attribute.ReadsFrom);

            if (!expectedReadsFrom.Contains(attribute.ReadsFrom))
                expectedReadsFrom.Add(attribute.ReadsFrom);
        }

        var writesRaw = systemToSchedule.Type.GetCustomAttributes(writesToAttribute);

        foreach (var attributeRaw in writesRaw)
        {
            var attribute = (WritesToComponentAttribute)attributeRaw;

            if (expectedReadsFrom.Contains(attribute.WritesTo))
            {
                throw new Exception(
                    "Shouldn't specify a writes to component with already a read attribute for same type");
            }

            if (!expectedWritesTo.Contains(attribute.WritesTo))
                expectedWritesTo.Add(attribute.WritesTo);
        }

        // TODO: should the following be done?:
        // All writes are also reads for simplicity in checking thread access

        systemToSchedule.WritesComponents = expectedWritesTo;
        systemToSchedule.ReadsComponents = expectedReadsFrom;
    }

    private void ResolveSystemDependencies(List<SystemToSchedule> systems)
    {
        SystemToSchedule GetSystemByType(Type type)
        {
            return systems.First(s => s.Type == type);
        }

        foreach (var system in systems)
        {
            var runsBeforeRaw = system.Type.GetCustomAttributes(runsBeforeAttribute);

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

            var runsAfterRaw = system.Type.GetCustomAttributes(runsAfterAttribute);

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

    private void DoNotAllowSeeingRunsAfter(SystemToSchedule systemToNotRunAfter,
        SystemToSchedule checkFrom, HashSet<SystemToSchedule> alreadyVisited)
    {
        foreach (var systemToSchedule in checkFrom.RunsAfter)
        {
            if (!alreadyVisited.Add(systemToSchedule))
                continue;

            // Check if we found a dependency on a system that is not allowed (but only if processing a list of the
            // wanted type, otherwise this is just meant to recursively traverse to find more lists of the right type)
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

    private void DoNotAllowSeeingRunsBefore(SystemToSchedule systemToNotRunBefore,
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

    private void CollectRunsAfter(SystemToSchedule systemToStart, HashSet<SystemToSchedule> result)
    {
        foreach (var runsAfter in systemToStart.RunsAfter)
        {
            if (result.Add(runsAfter))
                CollectRunsAfter(runsAfter, result);
        }
    }

    private void CollectRunsBefore(SystemToSchedule systemToStart, HashSet<SystemToSchedule> result)
    {
        foreach (var runsBefore in systemToStart.RunsBefore)
        {
            if (result.Add(runsBefore))
                CollectRunsBefore(runsBefore, result);
        }
    }

    private (List<SystemToSchedule> MainSystems, List<SystemToSchedule> OtherSystems) SplitSystemsToMainThread(
        List<SystemToSchedule> systems)
    {
        var main = new List<SystemToSchedule>();

        var other = new List<SystemToSchedule>();

        foreach (var system in systems)
        {
            if (system.RunsOnMainThread)
            {
                main.Add(system);
            }
            else
            {
                other.Add(system);
            }
        }

        return (main, other);
    }

    private void InsertNewProcessMethods(string file, string className, List<string> process, List<string> processFrame)
    {
        GD.Print($"Updating simulation class partial in {file}");

        using var writer = System.IO.File.CreateText(file);

        int indent = 0;

        writer.WriteLine("// Automatically generated file. DO NOT EDIT!");
        writer.WriteLine("// Run GenerateThreadedSystems to generate this file");

        writer.WriteLine($"public partial class {className}");
        writer.WriteLine('{');

        indent += 4;

        // TODO: variables

        writer.WriteLine(GetIndentForText(indent) + "protected override void OnProcessFixedLogic(float delta)");
        indent = WriteBlockContents(writer, process, indent);

        writer.WriteLine();
        writer.WriteLine(GetIndentForText(indent) + "private void OnProcessFrameLogic(float delta)");
        indent = WriteBlockContents(writer, processFrame, indent);

        // End of class
        writer.WriteLine('}');
        indent -= 4;

        if (indent != 0)
            throw new Exception("Writer didn't end closing all indents");
    }

    private int WriteBlockContents(StreamWriter writer, List<string> lines, int indent)
    {
        writer.WriteLine(GetIndentForText(indent) + "{");
        indent += 4;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                writer.WriteLine();
                continue;
            }

            writer.WriteLine(GetIndentForText(indent) + line);
        }

        indent -= 4;
        writer.WriteLine(GetIndentForText(indent) + "}");
        return indent;
    }

    private class SystemToSchedule
    {
        public readonly Type Type;
        public readonly string FieldName;
        public readonly int OriginalOrder;

        public string? RunCondition;
        public string? CustomRunCode;

        public bool RunsOnFrame;
        public bool RunsOnMainThread;

        public HashSet<SystemToSchedule> RunsBefore = new();
        public HashSet<SystemToSchedule> RunsAfter = new();

        public List<Type> ReadsComponents = new();
        public List<Type> WritesComponents = new();

        public SystemToSchedule(Type type, string name, int order)
        {
            Type = type;
            FieldName = name;
            OriginalOrder = order;
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

        public void GetRunningText(List<string> lineReceiver, int indent)
        {
            bool closeBrace = false;

            if (RunCondition != null)
            {
                lineReceiver.Add(GetIndentForText(indent) + $"if ({RunCondition})");
                lineReceiver.Add(GetIndentForText(indent) + '{');

                // Indent inside the condition
                indent += 4;
                closeBrace = true;
            }

            if (CustomRunCode != null)
            {
                foreach (var customLine in CustomRunCode.Split("\n"))
                {
                    lineReceiver.Add(GetIndentForText(indent) + string.Format(customLine, FieldName));
                }
            }
            else
            {
                lineReceiver.Add(GetIndentForText(indent) + $"{FieldName}.Update(delta);");
            }

            if (closeBrace)
            {
                indent -= 4;
                lineReceiver.Add(GetIndentForText(indent) + '}');
                EnsureOneBlankLine(lineReceiver);
            }
        }

        public override string ToString()
        {
            return Type.Name;
        }
    }

    private class ExecutionGroup
    {
        public readonly List<SystemToSchedule> Systems = new();

        public void GenerateCode(List<string> lineReceiver, int groupNumber)
        {
            EnsureOneBlankLine(lineReceiver);
            lineReceiver.Add($"// Execution group {groupNumber}");

            foreach (var system in Systems)
            {
                system.GetRunningText(lineReceiver, 0);
            }
        }
    }

    // NOTE: this doesn't work for a single pass sort as this can consider next to each other items equal, but then
    // items on either side of that block of non-sortables need to be sorted around the block
    private class SystemRequirementsBasedComparer : IComparer<SystemToSchedule>
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
            // Might be fun to debug print info when systems cannot do this cleanly to give potential places to improve
            // the systems decoupling
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
