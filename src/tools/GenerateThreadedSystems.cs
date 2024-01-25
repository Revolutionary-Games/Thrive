using System;
using System.Collections.Generic;
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

    private Type writesToAttribute = typeof(WritesToComponentAttribute);
    private Type readsFromAttribute = typeof(ReadsComponentAttribute);
    private Type runsAfterAttribute = typeof(RunsAfterAttribute);
    private Type runsBeforeAttribute = typeof(RunsBeforeAttribute);
    private Type runsOnMainAttribute = typeof(RunsOnMainThreadAttribute);

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

            throw new NotImplementedException();

            InsertNewProcessMethods(file, "", "");

            GD.Print($"Successfully handled. {file} has been updated");
        }
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
}
