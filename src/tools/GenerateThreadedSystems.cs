using System;
using System.Collections.Generic;
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

    private readonly IReadOnlyList<(Type Class, string File)> simulationTypes =
        new List<(Type Class, string File)>
            { (typeof(MicrobeWorldSimulation), "src/microbe_stage/MicrobeWorldSimulation.cs") };

    private readonly Type systemBaseType = typeof(ISystem<>);
    private readonly Type systemWithAttribute = typeof(WithAttribute);

    // private readonly Type systemWithoutAttribute = typeof(WithoutAttribute);

    private readonly Type writesToAttribute = typeof(WritesToComponentAttribute);
    private readonly Type readsFromAttribute = typeof(ReadsComponentAttribute);
    private readonly Type runsAfterAttribute = typeof(RunsAfterAttribute);
    private readonly Type runsBeforeAttribute = typeof(RunsBeforeAttribute);
    private readonly Type runsOnMainAttribute = typeof(RunsOnMainThreadAttribute);

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

    private void RunInternal()
    {
        GD.Print("Beginning generation of threaded system runs, it might take a while to find valid sequences of runs");

        foreach (var (simulationClass, file) in simulationTypes)
        {
            GD.Print($"Beginning processing class {simulationClass}");

            GetSystemsInCategories(simulationClass, out var frameSystems, out var processSystems);

            throw new NotImplementedException();

            InsertNewProcessMethods(file, "", "");

            GD.Print($"Successfully handled. {file} has been updated");
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

        // Sanity check no duplicate systems found
        if (result.GroupBy(s => s.Type).Any(g => g.Count() > 1))
            throw new Exception("Some type of system is included multiple times");

        return result;
    }

    private void ParseSystemAttributes(SystemToSchedule systemToSchedule)
    {
        systemToSchedule.RunsOnFrame = systemToSchedule.Type.GetCustomAttribute(runsOnFrameAttribute) != null;
    }

    private void InsertNewProcessMethods(string file, string process, string processFrame)
    {
        GD.Print($"Updating simulation class in {file}");
        GD.Print("Note that this relies on basic parsing of the file and won't work unless " +
            "the file is correctly intended and still has expected format of the given methods to rewrite");

        var tempFile = file + ".tmp";
        var writer = System.IO.File.CreateText(tempFile);

        bool suppress = false;

        foreach (var line in System.IO.File.ReadLines(file))
        {
            if (!suppress)
                writer.WriteLine(line);

            // TODO: detect when we need to start suppressing output and instead put the process or process frame info
        }

        System.IO.File.Delete(file);
        System.IO.File.Move(tempFile, file);
    }

    private class SystemToSchedule
    {
        public readonly Type Type;
        public readonly string FieldName;
        public readonly int OriginalOrder;

        public bool RunsOnFrame;

        public SystemToSchedule(Type type, string name, int order)
        {
            Type = type;
            FieldName = name;
            OriginalOrder = order;
        }
    }
}
