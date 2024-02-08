using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DefaultEcs.System;
using Godot;
using Tools;
using Environment = System.Environment;
using File = System.IO.File;

/// <summary>
///   Tool that generates a multithreaded sequence of runs for systems to run in parallel. This is implemented in the
///   main module as this needs to be able to load for example <see cref="MicrobeWorldSimulation"/> that depends on
///   Godot types.
/// </summary>
public class GenerateThreadedSystems : Node
{
    /// <summary>
    ///   How many threads to use when generating threaded system run. Needs to be at least 2. Too high number splits
    ///   task so granularly that it just lowers performance
    /// </summary>
    public static int TargetThreadCount = 2;

    /// <summary>
    ///   When true inserts timing code around barriers to measure how long the wait times are
    /// </summary>
    public static bool MeasureThreadWaits = false;

    /// <summary>
    ///   When true and <see cref="MeasureThreadWaits"/> prints the measured wait times while running
    /// </summary>
    public static bool PrintThreadWaits = true;

    /// <summary>
    ///   When true inserts a lot of debug code to check that no conflicting systems are executed in the same timeslot
    ///   during runtime. Used to verify that this tool works correctly.
    /// </summary>
    public static bool DebugGuardComponentWrites = false;

    private const string ThreadComponentCheckCode = @"
        lock (debugWriteLock)
        {
            // Check conflict
            bool conflict = false;

            if (write)
            {
                // Writes conflict with reads
                foreach (var pair in readsFromComponents)
                {
                    if (pair.Key == thread)
                        continue;

                    if (pair.Value.Contains(component))
                    {
                        GD.PrintErr(
                            $""Conflicting component write (new is write: {write}) for {component} on "" + 
                            $""threads {pair.Key} to {thread} when processing {system}"");
                        conflict = true;
                        break;
                    }
                }
            }

            // Potential conflicts with writes
            foreach (var pair in writesToComponents)
            {
                if (pair.Key == thread)
                    continue;

                if (pair.Value.Contains(component))
                {
                    GD.PrintErr(
                        $""Conflicting component read (new is write: {write}) for {component} on threads "" +
                        $""{pair.Key} to {thread} when processing {system}"");
                    conflict = true;
                    break;
                }
            }

            if (conflict)
            {
                Debugger.Break();
                throw new Exception(""Conflicting component use in simulation"");
            }

            // Add this action
            if (write)
            {
                if (!writesToComponents.TryGetValue(thread, out var usedComponents))
                {
                    usedComponents = new HashSet<string>();
                    writesToComponents[thread] = usedComponents;
                }

                usedComponents.Add(component);
            }
            else
            {
                if (!readsFromComponents.TryGetValue(thread, out var usedComponents))
                {
                    usedComponents = new HashSet<string>();
                    readsFromComponents[thread] = usedComponents;
                }

                usedComponents.Add(component);
            }
        }";

    private readonly IReadOnlyList<(Type Class, string File, string EndOfProcess)> simulationTypes =
        new List<(Type Class, string File, string EndOfProcess)>
        {
            (typeof(MicrobeWorldSimulation), "src/microbe_stage/MicrobeWorldSimulation.generated.cs",
                "cellCountingEntitySet.Complete();\nreportedPlayerPosition = null;"),
        };

    private readonly Type systemBaseType = typeof(ISystem<>);

    private bool done;

    private GenerateThreadedSystems()
    {
    }

    private string BarrierType => DebugGuardComponentWrites ? "Barrier" : "SimpleBarrier";

    public static void EnsureOneBlankLine(List<string> lines, bool acceptBlockStart = true, bool acceptComments = true)
    {
        if (lines.Count < 1)
            return;

        var line = lines[lines.Count - 1];

        if (acceptComments)
        {
            for (int i = 0; i < line.Length; ++i)
            {
                if (line[i] <= ' ')
                    continue;

                if (line[i] == '/' && i + 1 < line.Length && line[i + 1] == '/')
                    return;

                break;
            }
        }

        if (!string.IsNullOrWhiteSpace(line) && (!acceptBlockStart || !line.EndsWith("{")))
        {
            lines.Add(string.Empty);
        }
    }

    public static void AddBarrierWait(List<string> lineReceiver, int barrier, int currentThread, int indent)
    {
        if (MeasureThreadWaits)
            lineReceiver.Add(StringUtils.GetIndent(indent) + GetBeforeBarrierMeasure(currentThread));

        lineReceiver.Add($"{StringUtils.GetIndent(indent)}barrier{barrier}.SignalAndWait();");

        if (MeasureThreadWaits)
            lineReceiver.Add(StringUtils.GetIndent(indent) + GetAfterBarrierMeasure(currentThread));
    }

    public static string GetBeforeBarrierMeasure(int thread)
    {
        if (!MeasureThreadWaits)
            throw new InvalidOperationException("Should only be called if measuring times");

        return $"timer{thread}.Restart();";
    }

    public static string GetAfterBarrierMeasure(int thread)
    {
        if (!MeasureThreadWaits)
            throw new InvalidOperationException("Should only be called if measuring times");

        return $"waitTime{thread} += timer{thread}.Elapsed.TotalSeconds;";
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

        EnsureOneBlankLine(processLines);

        foreach (var line in processEnd.Split("\n"))
        {
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
                "// NOTE: not currently ran in parallel due to low frame system count",
            };

            AddSystemSingleGroupRunningLines(frameSystems, frameSystemTextLines, 0, -1);

            var processSystemTextLines = new List<string>();

            var (mainSystems, otherSystems) = SplitSystemsToMainThread(processSystems);

            Dictionary<string, VariableInfo> variables = new();

            // This destroys the other systems data so a copy is made
            GenerateThreadedSystemsRun(mainSystems, otherSystems.ToList(), TargetThreadCount - 1,
                processSystemTextLines, variables);

            AddProcessEndIfConfigured(processEnd, processSystemTextLines);

            var nonThreadedLines = new List<string>();
            GenerateNonThreadedSystems(mainSystems, otherSystems, nonThreadedLines);
            AddProcessEndIfConfigured(processEnd, nonThreadedLines);

            WriteGeneratedSimulationFile(file, simulationClass.Name, processSystemTextLines, nonThreadedLines,
                frameSystemTextLines, variables);

            GD.Print($"Successfully handled. {file} has been updated");
        }
    }

    private void GenerateThreadedSystemsRun(List<SystemToSchedule> mainSystems, List<SystemToSchedule> otherSystems,
        int backgroundThreads, List<string> processSystemTextLines, Dictionary<string, VariableInfo> variables)
    {
        // Make sure main systems are sorted according to when they should run
        SortSingleGroupOfSystems(mainSystems);
        VerifyOrderOfSystems(mainSystems);

        // Create rough ordering for the other systems to run (this is just an initial list and will be used just to
        // populate thread threads)
        SortSingleGroupOfSystems(otherSystems);
        VerifyOrderOfSystems(otherSystems);

        // Run a threaded run simulator to determine a good grouping of systems onto threads
        var simulator = new ThreadedRunSimulator(mainSystems, otherSystems, backgroundThreads + 1);

        var resultingThreads = simulator.Simulate();

        CheckBarrierCounts(resultingThreads);

        // Generate the final results
        WriteResultOfThreadedRunning(resultingThreads, processSystemTextLines, variables);
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
            system.GetRunningText(processSystemTextLines, 0, -1);
        }
    }

    private void CheckBarrierCounts(IEnumerable<List<SystemToSchedule>> threads)
    {
        var barrierCounts = new Dictionary<int, int>();

        foreach (var group in threads)
        {
            foreach (var system in group)
            {
                if (system.RequiresBarrierAfter < 0 || system.RequiresBarrierBefore < 0)
                    throw new Exception("Negative barrier amount detected");

                if (system.RequiresBarrierAfter == 0 && system.RequiresBarrierBefore == 0)
                    continue;

                barrierCounts.TryGetValue(system.ThreadId, out var earlier);

                barrierCounts[system.ThreadId] = earlier + system.RequiresBarrierAfter + system.RequiresBarrierBefore;
            }
        }

        var expectedCount = barrierCounts.Values.First();

        foreach (var pair in barrierCounts)
        {
            if (pair.Value != expectedCount)
                throw new Exception($"Uneven barrier counts ({pair.Value} != {expectedCount}), run will get blocked");
        }
    }

    private void WriteResultOfThreadedRunning(List<List<SystemToSchedule>> threads, List<string> lineReceiver,
        Dictionary<string, VariableInfo> variables)
    {
        int threadCount = threads.Count;

        // Tasks for background operations
        for (int i = 1; i < threadCount; ++i)
        {
            var thread = threads[i];
            int threadId = thread.First().ThreadId;
            lineReceiver.Add($"var background{i} = new Task(() =>");
            lineReceiver.Add($"{StringUtils.GetIndent(4)}{{");

            GenerateCodeForThread(thread, lineReceiver, 8);

            lineReceiver.Add(string.Empty);

            AddBarrierWait(lineReceiver, 1, threadId, 8);

            lineReceiver.Add($"{StringUtils.GetIndent(4)}}});");

            lineReceiver.Add(string.Empty);
            lineReceiver.Add($"TaskExecutor.Instance.AddTask(background{i});");
        }

        // Main thread operations
        var mainThread = threads[0];

        GenerateCodeForThread(mainThread, lineReceiver, 0);

        lineReceiver.Add(string.Empty);
        AddBarrierWait(lineReceiver, 1, mainThread.First().ThreadId, 0);

        variables["barrier1"] = new VariableInfo(BarrierType, !DebugGuardComponentWrites, $"new({threadCount})")
        {
            Dispose = DebugGuardComponentWrites,
            OriginalConstructorParameters = threadCount.ToString(),
        };

        if (DebugGuardComponentWrites)
        {
            variables["debugWriteLock"] = new VariableInfo("object", true);
            variables["readsFromComponents"] = new VariableInfo("Dictionary<int, HashSet<string>>", true);
            variables["writesToComponents"] = new VariableInfo("Dictionary<int, HashSet<string>>", true);
        }

        if (MeasureThreadWaits)
        {
            GenerateTimeMeasurementResultsCode(lineReceiver, variables, threadCount);
        }
    }

    private void GenerateCodeForThread(List<SystemToSchedule> systems, List<string> lineReceiver,
        int indent)
    {
        int timeslot = int.MaxValue;
        int threadId = int.MaxValue;

        foreach (var system in systems)
        {
            if (timeslot != system.Timeslot)
            {
                timeslot = system.Timeslot;
                threadId = system.ThreadId;

                EnsureOneBlankLine(lineReceiver);
                lineReceiver.Add(StringUtils.GetIndent(indent) + $"// Timeslot {timeslot} on thread {system.ThreadId}");
            }

            if (threadId != system.ThreadId)
                throw new Exception("Single system list has systems for multiple threads");

            system.GetRunningText(lineReceiver, indent, system.ThreadId);
        }
    }

    private void GenerateTimeMeasurementResultsCode(List<string> lineReceiver,
        Dictionary<string, VariableInfo> variables, int threadCount)
    {
        if (!MeasureThreadWaits)
            throw new Exception("Only call this when measurement is enabled");

        for (int i = 1; i <= threadCount; ++i)
        {
            variables["timer" + i] = new VariableInfo("Stopwatch", true);
            variables["waitTime" + i] = new VariableInfo("double", false, null);
        }

        variables["elapsedSinceTimePrint"] = new VariableInfo("float", false, null);

        // Outputting of the measurements
        EnsureOneBlankLine(lineReceiver);
        lineReceiver.Add("elapsedSinceTimePrint += delta;");
        lineReceiver.Add("if (elapsedSinceTimePrint >= 1)");
        lineReceiver.Add("{");
        lineReceiver.Add(StringUtils.GetIndent(4) + "elapsedSinceTimePrint = 0;");

        if (PrintThreadWaits)
            lineReceiver.Add(StringUtils.GetIndent(4) + @"GD.Print($""Simulation thread wait times: "");");

        for (int i = 1; i <= threadCount; ++i)
        {
            if (PrintThreadWaits)
                lineReceiver.Add(StringUtils.GetIndent(4) + $"GD.Print($\"\\t thread{i}:\\t{{waitTime{i}}}\");");
            lineReceiver.Add(StringUtils.GetIndent(4) + $"waitTime{i} = 0;");
        }

        lineReceiver.Add("}");
    }

    private void AddSystemSingleGroupRunningLines(IEnumerable<SystemToSchedule> systems, List<string> textOutput,
        int indent, int thread)
    {
        foreach (var system in systems)
        {
            system.GetRunningText(textOutput, indent, thread);
        }
    }

    /// <summary>
    ///   Need to use an insertion sort (that doesn't exit early) to make sure that items are fully sorted as the
    ///   comparer can't pick a relative order for some items causing breaks in the sorting. Sorts in a stable way
    ///   (i.e. OriginalOrder is respected for equal items, assuming <see cref="systems"/> hasn't been modified in way
    ///   that doesn't preserve OriginalOrder).
    /// </summary>
    /// <param name="systems">Systems to sort in-place</param>
    private void SortSingleGroupOfSystems(IList<SystemToSchedule> systems)
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

    private void WriteGeneratedSimulationFile(string file, string className, List<string> process,
        List<string> processNonThreaded, List<string> processFrame, Dictionary<string, VariableInfo> variables)
    {
        GD.Print($"Updating simulation class partial in {file}");

        using var fileStream = File.Create(file);

        using var writer = new StreamWriter(fileStream, new UTF8Encoding(true));

        int indent = 0;

        writer.WriteLine("// Automatically generated file. DO NOT EDIT!");
        writer.WriteLine("// Run GenerateThreadedSystems to generate this file");

        if (DebugGuardComponentWrites)
        {
            writer.WriteLine("using System;");
            writer.WriteLine("using System.Collections.Generic;");
        }

        if (MeasureThreadWaits || DebugGuardComponentWrites)
            writer.WriteLine("using System.Diagnostics;");

        writer.WriteLine("using System.Threading;");
        writer.WriteLine("using System.Threading.Tasks;");

        if (MeasureThreadWaits || DebugGuardComponentWrites)
            writer.WriteLine("using Godot;");

        writer.WriteLine();

        writer.WriteLine($"public partial class {className}");
        writer.WriteLine('{');

        indent += 4;
        bool addedVariables = false;

        foreach (var pair in variables.OrderByDescending(p => p.Value.IsReadonly))
        {
            var info = pair.Value;

            var initializer = info.Initializer != null ? $" = {info.Initializer}" : string.Empty;

            writer.WriteLine(StringUtils.GetIndent(indent) +
                $"private {(info.IsReadonly ? "readonly " : string.Empty)}{info.Type} {pair.Key}{initializer};");

            addedVariables = true;
        }

        if (addedVariables)
            writer.WriteLine();

        writer.WriteLine(StringUtils.GetIndent(indent) + "private void InitGenerated()");
        writer.WriteLine(StringUtils.GetIndent(indent) + "{");
        indent += 4;

        if (DebugGuardComponentWrites)
        {
            foreach (var variable in variables)
            {
                if (variable.Value.Type != BarrierType)
                    continue;

                writer.WriteLine(StringUtils.GetIndent(indent) +
                    $"{variable.Key} = new {BarrierType}({variable.Value.OriginalConstructorParameters}, " +
                    "OnBarrierPhaseCompleted);");
            }
        }

        indent -= 4;
        writer.WriteLine(StringUtils.GetIndent(indent) + "}");

        writer.WriteLine();
        writer.WriteLine(StringUtils.GetIndent(indent) + "private void OnProcessFixedWithThreads(float delta)");
        indent = WriteBlockContents(writer, process, indent);

        writer.WriteLine();
        writer.WriteLine(StringUtils.GetIndent(indent) + "private void OnProcessFixedWithoutThreads(float delta)");
        indent = WriteBlockContents(writer, processNonThreaded, indent);

        writer.WriteLine();
        writer.WriteLine(StringUtils.GetIndent(indent) + "private void OnProcessFrameLogic(float delta)");
        indent = WriteBlockContents(writer, processFrame, indent);

        if (DebugGuardComponentWrites)
        {
            indent = WriteWriteCheckHelperMethods(writer, indent);
        }

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

    private int WriteWriteCheckHelperMethods(StreamWriter writer, int indent)
    {
        // Clear method after complete phase
        writer.WriteLine();
        writer.WriteLine(StringUtils.GetIndent(indent) +
            $"private void OnBarrierPhaseCompleted({BarrierType} barrier)");
        writer.WriteLine(StringUtils.GetIndent(indent) + "{");
        indent += 4;

        writer.WriteLine(StringUtils.GetIndent(indent) + "lock (debugWriteLock)");
        writer.WriteLine(StringUtils.GetIndent(indent) + "{");
        indent += 4;

        writer.WriteLine(StringUtils.GetIndent(indent) + "foreach (var entry in readsFromComponents)");
        writer.WriteLine(StringUtils.GetIndent(indent) + "{");
        writer.WriteLine(StringUtils.GetIndent(indent + 4) + "entry.Value.Clear();");
        writer.WriteLine(StringUtils.GetIndent(indent) + "}");

        writer.WriteLine();
        writer.WriteLine(StringUtils.GetIndent(indent) + "foreach (var entry in writesToComponents)");
        writer.WriteLine(StringUtils.GetIndent(indent) + "{");
        writer.WriteLine(StringUtils.GetIndent(indent + 4) + "entry.Value.Clear();");
        writer.WriteLine(StringUtils.GetIndent(indent) + "}");

        indent -= 4;
        writer.WriteLine(StringUtils.GetIndent(indent) + "}");

        indent -= 4;
        writer.WriteLine(StringUtils.GetIndent(indent) + "}");

        // Check method
        writer.WriteLine();
        writer.WriteLine(StringUtils.GetIndent(indent) +
            "private void OnThreadAccessComponent(bool write, string component, string system, int thread)");
        writer.WriteLine(StringUtils.GetIndent(indent) + "{");
        indent += 4;

        bool firstBlank = true;

        foreach (var line in ThreadComponentCheckCode.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (!firstBlank)
                    writer.WriteLine();

                firstBlank = false;
                continue;
            }

            // The code is indented here in the source code so a substring is taken to replace the indent sizes
            writer.WriteLine(StringUtils.GetIndent(indent) + line.Substring(8));
        }

        indent -= 4;
        writer.WriteLine(StringUtils.GetIndent(indent) + "}");
        return indent;
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

    private class VariableInfo
    {
        public string Type;
        public string? Initializer;
        public bool IsReadonly;
        public bool Dispose;

        public string? OriginalConstructorParameters;

        public VariableInfo(string type, bool isReadonly, string? initializer = "new()")
        {
            Type = type;
            Initializer = initializer;
            IsReadonly = isReadonly;
        }
    }
}
