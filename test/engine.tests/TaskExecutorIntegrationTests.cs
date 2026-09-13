using System.Collections.Generic;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using Systems;
using Tools;
using static GdUnit4.Assertions;

/// <summary>
///   Exercises existing consumers of the shared task executor through their normal public entry points.
/// </summary>
[TestSuite]
[RequireGodotRuntime]
public class TaskExecutorIntegrationTests
{
    [TestCase]
    public void CloudBatches_MatchSerialTaskExecution()
    {
        using var world = ThriveWorld.Create();
        using var currents = new FluidCurrentsSystem(world, 0);
        var scene = GD.Load<PackedScene>("res://src/microbe_stage/CompoundCloudPlane.tscn");
        var serial = scene.Instantiate<CompoundCloudPlane>();
        CompoundCloudPlane? parallel = null;
        try
        {
            parallel = scene.Instantiate<CompoundCloudPlane>();
            foreach (var cloud in new[] { serial, parallel })
            {
                cloud._Ready();
                cloud.Init(currents, 0, Compound.Glucose, Compound.Invalid, Compound.Invalid, Compound.Invalid);
                cloud.UpdatePosition(Vector2I.Zero);
                cloud.AddCloudInterlocked(Compound.Glucose, cloud.PlaneSize / 2, cloud.PlaneSize / 2, 100);
                cloud.DiffuseEdges(0.01f);
            }

            var tasks = new List<Task>();
            serial.QueueDiffuseCloud(0.01f, tasks);
            foreach (var task in tasks)
                task.RunSynchronously();
            tasks.Clear();
            parallel.QueueDiffuseCloud(0.01f, tasks);
            TaskExecutor.Instance.RunTasks(tasks, false, true);
            AssertThat(parallel.OldDensity).IsEqual(serial.OldDensity);

            serial.ClearDensity();
            parallel.ClearDensity();
            tasks.Clear();
            serial.QueueAdvectCloud(0.01f, tasks);
            foreach (var task in tasks)
                task.RunSynchronously();
            tasks.Clear();
            parallel.QueueAdvectCloud(0.01f, tasks);
            TaskExecutor.Instance.RunTasks(tasks, false, true);
            AssertThat(parallel.Density).IsEqual(serial.Density);

            float remaining = 0;
            foreach (var density in parallel.Density)
                remaining += density.X;
            AssertThat(remaining > 0).IsTrue();
        }
        finally
        {
            serial.Free();
            parallel?.Free();
        }
    }

    [TestCase]
    public void DualContourer_RepeatedSmoothedMeshesHaveTheSameGeometry()
    {
        var contourer = new DualContourer(new SphereFunction())
        {
            PointsPerUnit = 4,
            UnitsFrom = new Vector3(-1, -1, -1),
            UnitsTo = new Vector3(1, 1, 1),
            Smoothen = true,
        };
        using var first = contourer.DualContour();
        using var second = contourer.DualContour();
        AssertThat(first.GetSurfaceCount()).IsEqual(1);
        AssertThat(second.GetSurfaceCount()).IsEqual(1);
        var firstArrays = first.SurfaceGetArrays(0);
        var secondArrays = second.SurfaceGetArrays(0);
        var vertices = firstArrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        AssertThat(vertices.Length > 0).IsTrue();
        AssertThat(secondArrays[(int)Mesh.ArrayType.Vertex].AsVector3Array()).IsEqual(vertices);
        AssertThat(secondArrays[(int)Mesh.ArrayType.Index].AsInt32Array())
            .IsEqual(firstArrays[(int)Mesh.ArrayType.Index].AsInt32Array());
    }

    [TestCase]
    public void ThreadedRunSimulator_PreservesAllScheduledSystems()
    {
        var mainSystem = new SystemToSchedule(typeof(FluidCurrentsSystem), "currents") { RunsOnMainThread = true };
        var workerSystem = new SystemToSchedule(typeof(CompoundCloudSystem), "clouds");
        var simulator = new ThreadedRunSimulator(new[] { mainSystem }, new[] { workerSystem }, 2);
        var result = simulator.Simulate(12345, 2);
        AssertThat(result.Count).IsEqual(2);
        AssertThat(result[0].Contains(mainSystem)).IsTrue();
        int workerOccurrences = 0;
        foreach (var thread in result)
        {
            foreach (var system in thread)
            {
                if (ReferenceEquals(system, workerSystem))
                    ++workerOccurrences;
            }
        }

        AssertThat(workerOccurrences).IsEqual(1);
    }

    private sealed class SphereFunction : IMeshGeneratingFunction
    {
        public float SurfaceValue { get; set; } = 0.25f;

        public float GetValue(Vector3 pos)
        {
            return 1 - pos.LengthSquared();
        }

        public Color GetColour(Vector3 pos)
        {
            return Colors.White;
        }
    }
}
