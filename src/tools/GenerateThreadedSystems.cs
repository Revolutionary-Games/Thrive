﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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
            frameSystems = SortSingleGroupOfSystems(frameSystems).ToList();

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
        mainSystems = SortSingleGroupOfSystems(mainSystems).ToList();

        // Create rough ordering for the other systems to run (this is just an initial list and will be used just to
        // create thread groups)
        otherSystems = SortSingleGroupOfSystems(otherSystems).ToList();

        // First go of execution groups based on each one of the main thread systems (but combine subsequent ones that
        // require the next)
        var groups = new List<ExecutionGroup>();

        foreach (var mainSystem in mainSystems)
        {
            if (groups.Count > 0 && mainSystem.ShouldRunAfter(groups[groups.Count - 1].Systems.Last()))
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

                foreach (var groupSystem in group.Systems)
                {
                    if (current.ShouldRunBefore(groupSystem))
                    {
                        // Prepend to system
                        group.Systems.Insert(0, current);
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

    private IOrderedEnumerable<SystemToSchedule> SortSingleGroupOfSystems(IEnumerable<SystemToSchedule> systems)
    {
        return systems.OrderBy(s => s, new SystemRequirementsBasedComparer()).ThenBy(s => s.OriginalOrder);
    }

    private void AddSystemSingleGroupRunningLines(IEnumerable<SystemToSchedule> systems, List<string> textOutput,
        int indent)
    {
        foreach (var system in systems)
        {
            system.GetRunningText(textOutput, indent);
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

                system.RunsBefore.Add(GetSystemByType(attribute.BeforeSystem));
            }

            var runsAfterRaw = system.Type.GetCustomAttributes(runsAfterAttribute);

            foreach (var attributeRaw in runsAfterRaw)
            {
                var attribute = (RunsAfterAttribute)attributeRaw;

                system.RunsAfter.Add(GetSystemByType(attribute.AfterSystem));
            }
        }

        // Detect cycles in runs before or runs after
        // TODO: this is likely not good enough currently
        var seenSystems = new HashSet<SystemToSchedule>();

        foreach (var system in systems)
        {
            seenSystems.Clear();

            foreach (var runsAfter in system.RunsAfter)
            {
                seenSystems.Add(runsAfter);
                CollectRunsBefore(runsAfter, seenSystems);
            }

            if (seenSystems.Contains(system))
                throw new Exception("System has a circular running before / after relationship");

            seenSystems.Clear();

            foreach (var runsBefore in system.RunsBefore)
            {
                seenSystems.Add(runsBefore);
                CollectRunsAfter(runsBefore, seenSystems);
            }

            if (seenSystems.Contains(system))
                throw new Exception("System has a circular running before / after relationship");
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

        // Make relationships always two-way for easier checking
        foreach (var system in systems)
        {
            foreach (var runsAfter in system.RunsAfter)
            {
                runsAfter.RunsBefore.Add(system);
            }

            foreach (var runsBefore in system.RunsBefore.ToList())
            {
                runsBefore.RunsAfter.Add(system);
            }
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

            // TODO: component writes should happen before reads

            return false;
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

            // No requirement for either system to run before the other
            return 0;
        }
    }
}
