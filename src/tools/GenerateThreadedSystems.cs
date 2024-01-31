﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

    // private readonly Type systemWithoutAttribute = typeof(WithoutAttribute);

    private bool done;

    private GenerateThreadedSystems()
    {
        random = new Random(DefaultSeed);
    }

    public static void EnsureOneBlankLine(List<string> lines, bool acceptBlockStart = true)
    {
        if (lines.Count < 1)
            return;

        var line = lines[lines.Count - 1];
        if (!string.IsNullOrWhiteSpace(line) && (!acceptBlockStart || !line.EndsWith("{")))
        {
            lines.Add(string.Empty);
        }
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

            Dictionary<string, VariableInfo> variables = new();
            GenerateThreadedSystemsRun(mainSystems, otherSystems, processSystemTextLines, variables);

            if (!string.IsNullOrWhiteSpace(processEnd))
            {
                foreach (var line in processEnd.Split("\n"))
                {
                    EnsureOneBlankLine(processSystemTextLines);

                    processSystemTextLines.Add(line);
                }
            }

            InsertNewProcessMethods(file, simulationClass.Name, processSystemTextLines, frameSystemTextLines,
                variables);

            GD.Print($"Successfully handled. {file} has been updated");
        }
    }

    private void GenerateThreadedSystemsRun(List<SystemToSchedule> mainSystems, List<SystemToSchedule> otherSystems,
        List<string> processSystemTextLines, Dictionary<string, VariableInfo> variables)
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
        var comparer = new SystemToSchedule.SystemRequirementsBasedComparer();

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

        int count = 1;

        // Verify all group orders are correct
        foreach (var group in groups)
        {
            VerifyOrderOfSystems(group.Systems);
            group.GroupNumber = count++;
        }

        // Generate barrier points
        var mainReads = new HashSet<Type>();
        var mainWrites = new HashSet<Type>();
        var seenMainSystems = new HashSet<SystemToSchedule>();

        bool firstMainGroup = true;
        bool firstOtherGroup = true;

        bool nextMainShouldBlock = false;
        bool skipOtherBlock = false;

        bool hadOtherBlock = false;

        foreach (var group in groups)
        {
            // Probably fine to have inter-group dependencies when adding the blocks
            // mainReads.Clear();
            // mainWrites.Clear();
            // seenMainSystems

            using var mainEnumerator = group.Systems.Where(s => s.RunsOnMainThread).GetEnumerator();
            using var otherEnumerator = group.Systems.Where(s => !s.RunsOnMainThread).GetEnumerator();

            bool firstMain = true;
            bool firstOther = true;

            /*foreach (var mainSystem in group.Systems.Where(s => s.RunsOnMainThread))
            {
                // Require barrier on first group item (but not required on the first group)
                if (firstMain && !firstMainGroup)
                {
                    firstMain = false;

                    // Also don't require barrier if there hasn't been another thread in between the main threads
                    if (hadOtherBlock)
                    {
                        mainSystem.RequiresBarrierBefore = true;
                        hadOtherBlock = false;
                    }
                }

                foreach (var usedComponent in mainSystem.WritesComponents)
                {
                    mainWrites.Add(usedComponent);
                }

                foreach (var usedComponent in mainSystem.ReadsComponents)
                {
                    mainReads.Add(usedComponent);
                }

                seenMainSystems.Add(mainSystem);

                if (nextMainShouldBlock)
                {
                    nextMainShouldBlock = false;

                    // Ensure consistent number of thread waits
                    if (!mainSystem.RequiresBarrierBefore)
                    {
                        mainSystem.RequiresBarrierBefore = true;
                    }
                    else
                    {
                        skipOtherBlock = true;
                    }
                }

                firstMainGroup = false;
            }

            foreach (var otherSystem in group.Systems.Where(s => !s.RunsOnMainThread))
            {
                if (firstOther && !firstOtherGroup)
                {
                    firstOther = false;
                    if (!skipOtherBlock)
                    {
                        if (otherSystem.RequiresBarrierBefore)
                            throw new Exception("This shouldn't be able to be true here");

                        otherSystem.RequiresBarrierBefore = true;
                    }
                    else
                    {
                        skipOtherBlock = false;
                    }
                }

                // If conflicts with a main system needs to block, and main needs to wait after the current main
                // system has ran
                if (otherSystem.WritesComponents.Any(c =>
                        mainReads.Contains(c) || mainWrites.Contains(c)) ||
                    otherSystem.ReadsComponents.Any(c => mainWrites.Contains(c)) ||
                    seenMainSystems.Any(s => comparer.CompareWeak(otherSystem, s) < 0))
                {
                    if (!otherSystem.RequiresBarrierBefore && !nextMainShouldBlock)
                    {
                        otherSystem.RequiresBarrierBefore = true;
                        nextMainShouldBlock = true;

                        // Clear the interference info as we've decided on a blocking location
                        mainReads.Clear();
                        mainWrites.Clear();
                        seenMainSystems.Clear();
                    }
                }

                firstOtherGroup = false;
                hadOtherBlock = true;
            }*/

            while (true)
            {
                bool seenAnything = false;

                if (mainEnumerator.MoveNext() && mainEnumerator.Current != null)
                {
                    // Require barrier on first group item (but not required on the first group)
                    if (firstMain && !firstMainGroup)
                    {
                        firstMain = false;

                        // Also don't require barrier if there hasn't been another thread in between the main threads
                        if (hadOtherBlock)
                        {
                            mainEnumerator.Current.RequiresBarrierBefore = true;
                            hadOtherBlock = false;
                        }
                    }

                    foreach (var usedComponent in mainEnumerator.Current.WritesComponents)
                    {
                        mainWrites.Add(usedComponent);
                    }

                    foreach (var usedComponent in mainEnumerator.Current.ReadsComponents)
                    {
                        mainReads.Add(usedComponent);
                    }

                    seenMainSystems.Add(mainEnumerator.Current);

                    if (nextMainShouldBlock)
                    {
                        nextMainShouldBlock = false;

                        // Ensure consistent number of thread waits
                        if (!mainEnumerator.Current.RequiresBarrierBefore)
                        {
                            mainEnumerator.Current.RequiresBarrierBefore = true;
                        }
                        else
                        {
                            skipOtherBlock = true;
                        }
                    }

                    seenAnything = true;
                }

                if (otherEnumerator.MoveNext() && otherEnumerator.Current != null)
                {
                    if (firstOther && !firstOtherGroup)
                    {
                        firstOther = false;
                        if (!skipOtherBlock)
                        {
                            if (otherEnumerator.Current.RequiresBarrierBefore)
                                throw new Exception("This shouldn't be able to be true here");

                            otherEnumerator.Current.RequiresBarrierBefore = true;
                        }
                        else
                        {
                            skipOtherBlock = false;
                        }
                    }

                    // If conflicts with a main system needs to block, and main needs to wait after the current main
                    // system has ran
                    if (otherEnumerator.Current.WritesComponents.Any(c =>
                            mainReads.Contains(c) || mainWrites.Contains(c)) ||
                        otherEnumerator.Current.ReadsComponents.Any(c => mainWrites.Contains(c)) ||
                        seenMainSystems.Any(s => comparer.CompareWeak(otherEnumerator.Current, s) < 0))
                    {
                        if (!otherEnumerator.Current.RequiresBarrierBefore && !nextMainShouldBlock)
                        {
                            otherEnumerator.Current.RequiresBarrierBefore = true;
                            nextMainShouldBlock = true;

                            // Clear the interference info as we've decided on a blocking location
                            mainReads.Clear();
                            mainWrites.Clear();
                            seenMainSystems.Clear();
                        }
                    }

                    seenAnything = true;
                }

                if (!seenAnything)
                    break;
            }

            if (!firstMain)
                firstMainGroup = false;

            if (!firstOther)
                firstOtherGroup = false;
        }

        CheckBarrierCounts(groups);

        // Generate the final results
        WriteResultOfThreadedRunning(groups, processSystemTextLines, variables);
    }

    private void CheckBarrierCounts(List<ExecutionGroup> groups)
    {
        int mainBarriers = 0;
        int otherBarriers = 0;

        foreach (var group in groups)
        {
            foreach (var system in group.Systems)
            {
                if (system.RequiresBarrierBefore)
                {
                    if (system.RunsOnMainThread)
                    {
                        ++mainBarriers;
                    }
                    else
                    {
                        ++otherBarriers;
                    }
                }
            }
        }

        if (mainBarriers != otherBarriers)
            throw new Exception("Uneven barrier counts, run will get blocked");
    }

    private void WriteResultOfThreadedRunning(List<ExecutionGroup> groups, List<string> lineReceiver,
        Dictionary<string, VariableInfo> variables)
    {
        // TODO: make parallel task count configurable
        int barrierMemberCount = 2;

        // Task for background operations
        lineReceiver.Add("var background1 = new Task(() =>");
        lineReceiver.Add($"{StringUtils.GetIndent(4)}{{");

        foreach (var group in groups)
        {
            group.GenerateCodeThread(lineReceiver, 8);
        }

        lineReceiver.Add(string.Empty);
        lineReceiver.Add($"{StringUtils.GetIndent(8)}barrier1.SignalAndWait();");

        lineReceiver.Add($"{StringUtils.GetIndent(4)}}});");

        lineReceiver.Add(string.Empty);
        lineReceiver.Add("TaskExecutor.Instance.AddTask(background1);");

        // Main thread operations
        foreach (var group in groups)
        {
            group.GenerateCodeMain(lineReceiver, 0);
        }

        lineReceiver.Add(string.Empty);
        lineReceiver.Add("barrier1.SignalAndWait();");

        variables["barrier1"] = new VariableInfo("Barrier", true, $"new({barrierMemberCount})")
        {
            Dispose = true,
        };
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
        var comparer = new SystemToSchedule.SystemRequirementsBasedComparer();

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
        var comparer = new SystemToSchedule.SystemRequirementsBasedComparer();

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

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
        {
            // Skip automatic property backing fields
            if (field.CustomAttributes.Any(a => a.AttributeType == SystemToSchedule.CompilerGeneratedAttribute))
                continue;

            if (!field.FieldType.IsAssignableToGenericType(systemBaseType))
                continue;

            result.Add(new SystemToSchedule(field.FieldType, field.Name));
        }

        foreach (var property in type.GetProperties(
                     BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
        {
            if (!property.PropertyType.IsAssignableToGenericType(systemBaseType))
                continue;

            // Skip things that are already used through fields
            if (result.Any(r => r.FieldName.Equals(property.Name, StringComparison.OrdinalIgnoreCase)))
                continue;

            result.Add(new SystemToSchedule(property.PropertyType, property.Name));
        }

        foreach (var systemToSchedule in result)
        {
            SystemToSchedule.ParseSystemAttributes(systemToSchedule);
        }

        SystemToSchedule.ResolveSystemDependencies(result);

        // Sanity check no duplicate systems found
        if (result.GroupBy(s => s.Type).Any(g => g.Count() > 1))
            throw new Exception("Some type of system is included multiple times");

        // Make sure sorting works sensibly
        var comparer = new SystemToSchedule.SystemRequirementsBasedComparer();
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

    private void InsertNewProcessMethods(string file, string className, List<string> process, List<string> processFrame,
        Dictionary<string, VariableInfo> variables)
    {
        GD.Print($"Updating simulation class partial in {file}");

        using var writer = System.IO.File.CreateText(file);

        int indent = 0;

        writer.WriteLine("// Automatically generated file. DO NOT EDIT!");
        writer.WriteLine("// Run GenerateThreadedSystems to generate this file");

        writer.WriteLine("using System.Threading;");
        writer.WriteLine("using System.Threading.Tasks;");
        writer.WriteLine();

        writer.WriteLine($"public partial class {className}");
        writer.WriteLine('{');

        indent += 4;
        bool addedVariables = false;

        foreach (var pair in variables)
        {
            var info = pair.Value;

            var initializer = info.Initializer != null ? $" = {info.Initializer}" : string.Empty;

            writer.WriteLine(StringUtils.GetIndent(indent) +
                $"private {(info.IsReadonly ? "readonly " : string.Empty)}{info.Type} {pair.Key}{initializer};");

            addedVariables = true;
        }

        if (addedVariables)
            writer.WriteLine();

        writer.WriteLine(StringUtils.GetIndent(indent) + "protected override void OnProcessFixedLogic(float delta)");
        indent = WriteBlockContents(writer, process, indent);

        writer.WriteLine();
        writer.WriteLine(StringUtils.GetIndent(indent) + "private void OnProcessFrameLogic(float delta)");
        indent = WriteBlockContents(writer, processFrame, indent);

        writer.WriteLine();
        writer.WriteLine(StringUtils.GetIndent(indent) + "private void DisposeGenerated()");
        writer.WriteLine(StringUtils.GetIndent(indent) + "{");
        indent += 4;

        foreach (var pair in variables)
        {
            if (pair.Value.Dispose)
            {
                writer.WriteLine(StringUtils.GetIndent(indent) + $"{pair.Key}.Dispose();");
            }
        }

        indent -= 4;
        writer.WriteLine(StringUtils.GetIndent(indent) + "}");

        // End of class
        writer.WriteLine('}');
        indent -= 4;

        if (indent != 0)
            throw new Exception("Writer didn't end closing all indents");
    }

    private int WriteBlockContents(StreamWriter writer, List<string> lines, int indent)
    {
        writer.WriteLine(StringUtils.GetIndent(indent) + "{");
        indent += 4;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                writer.WriteLine();
                continue;
            }

            writer.WriteLine(StringUtils.GetIndent(indent) + line);
        }

        indent -= 4;
        writer.WriteLine(StringUtils.GetIndent(indent) + "}");
        return indent;
    }

    private class ExecutionGroup
    {
        public readonly List<SystemToSchedule> Systems = new();
        public int GroupNumber;

        public void GenerateCodeMain(List<string> lineReceiver, int indent)
        {
            Write(lineReceiver, $"// Execution group {GroupNumber} (on main)", indent,
                Systems.Where(s => s.RunsOnMainThread));
        }

        public void GenerateCodeThread(List<string> lineReceiver, int indent)
        {
            Write(lineReceiver, $"// Execution group {GroupNumber}", indent,
                Systems.Where(s => !s.RunsOnMainThread));
        }

        private void Write(List<string> lineReceiver, string comment, int indent,
            IEnumerable<SystemToSchedule> filteredSystems)
        {
            bool first = true;

            foreach (var system in filteredSystems)
            {
                if (first)
                {
                    first = false;
                    EnsureOneBlankLine(lineReceiver);
                    lineReceiver.Add(StringUtils.GetIndent(indent) + comment);
                }

                system.GetRunningText(lineReceiver, indent);
            }
        }
    }

    private class VariableInfo
    {
        public string Type;
        public string? Initializer;
        public bool IsReadonly;
        public bool Dispose;

        public VariableInfo(string type, bool isReadonly, string? initializer = "new()")
        {
            Type = type;
            Initializer = initializer;
            IsReadonly = isReadonly;
        }
    }
}
