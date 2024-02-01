using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DefaultEcs.System;
using Godot;
using Tools;
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

    private static void AddProcessEndIfConfigured(string? processEnd, List<string> processLines)
    {
        if (string.IsNullOrWhiteSpace(processEnd))
            return;

        foreach (var line in processEnd.Split("\n"))
        {
            EnsureOneBlankLine(processLines);

            processLines.Add(line);
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

            // This destroys the other systems data so a copy is made
            GenerateThreadedSystemsRun(mainSystems, otherSystems.ToList(), processSystemTextLines, variables);

            AddProcessEndIfConfigured(processEnd, processSystemTextLines);

            var nonThreadedLines = new List<string>();
            GenerateNonThreadedSystems(mainSystems, otherSystems, nonThreadedLines);
            AddProcessEndIfConfigured(processEnd, nonThreadedLines);

            InsertNewProcessMethods(file, simulationClass.Name, processSystemTextLines, nonThreadedLines,
                frameSystemTextLines, variables);

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

        // TODO: allow configuring more than just one background thread (and then generating plausible execution plans
        // for different threads)
        var mainTasks = groups.SelectMany(g => g.Systems.Where(s => s.RunsOnMainThread));
        var otherTasks = groups.SelectMany(g => g.Systems.Where(s => !s.RunsOnMainThread));

        // Generate barrier points by simulating the executions of the separate threads
        var simulator = new ThreadedRunSimulator(mainTasks.ToList(), otherTasks.ToList());

        simulator.Simulate();

        // TODO: remove double barriers that can be removed

        CheckBarrierCounts(groups);

        // Generate the final results
        WriteResultOfThreadedRunning(groups, processSystemTextLines, variables);
    }

    private void GenerateNonThreadedSystems(List<SystemToSchedule> mainSystems, List<SystemToSchedule> otherSystems,
        List<string> processSystemTextLines)
    {
        var allSystems = mainSystems.Concat(otherSystems).ToList();
        SortSingleGroupOfSystems(allSystems);
        VerifyOrderOfSystems(allSystems);

        // Clear barriers from a threaded generation
        foreach (var systemToSchedule in allSystems)
        {
            systemToSchedule.RequiresBarrierAfter = 0;
            systemToSchedule.RequiresBarrierBefore = 0;
        }

        // Generate simple result text
        processSystemTextLines.Add("// This variant doesn't use threading, use when not enough threads are " +
            "available");
        processSystemTextLines.Add("// or threaded run would be slower (or just for debugging)");

        foreach (var system in allSystems)
        {
            system.GetRunningText(processSystemTextLines, 0);
        }
    }

    private void CheckBarrierCounts(List<ExecutionGroup> groups)
    {
        int mainBarriers = 0;
        int otherBarriers = 0;

        foreach (var group in groups)
        {
            foreach (var system in group.Systems)
            {
                if (system.RequiresBarrierAfter < 0 || system.RequiresBarrierBefore < 0)
                    throw new Exception("Negative barrier amount detected");

                if (system.RunsOnMainThread)
                {
                    mainBarriers += system.RequiresBarrierAfter + system.RequiresBarrierBefore;
                }
                else
                {
                    otherBarriers += system.RequiresBarrierAfter + system.RequiresBarrierBefore;
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

    private void InsertNewProcessMethods(string file, string className, List<string> process,
        List<string> processNonThreaded, List<string> processFrame, Dictionary<string, VariableInfo> variables)
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

        writer.WriteLine(StringUtils.GetIndent(indent) + "private void OnProcessFixedWith3Threads(float delta)");
        indent = WriteBlockContents(writer, process, indent);

        writer.WriteLine();
        writer.WriteLine(StringUtils.GetIndent(indent) + "private void OnProcessFixedWithoutThreads(float delta)");
        indent = WriteBlockContents(writer, processNonThreaded, indent);

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
