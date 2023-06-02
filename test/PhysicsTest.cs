using System.Collections.Generic;
using Godot;

/// <summary>
///   Tests / stress tests the physics system
/// </summary>
public class PhysicsTest : Node
{
    [Export]
    public NodePath? WorldVisualsPath;

    private readonly List<PhysicsBody> allCreatedBodies = new();
    private readonly List<PhysicsBody> sphereBodies = new();

    private readonly List<Spatial> sphereVisuals = new();

#pragma warning disable CA2213
    private Node worldVisuals = null!;
#pragma warning restore CA2213

    private PhysicalWorld physicalWorld = null!;

    public override void _Ready()
    {
        worldVisuals = GetNode(WorldVisualsPath);

        physicalWorld = PhysicalWorld.Create();
        SetupPhysicsBodies();
    }

    public override void _Process(float delta)
    {
        if (!physicalWorld.ProcessPhysics(delta))
            return;

        // Display the spheres
        int index = 0;

        foreach (var body in sphereBodies)
        {
            if (index >= sphereVisuals.Count)
            {
                sphereVisuals.Add(CreateSphereVisual());
            }

            sphereVisuals[index].Transform = physicalWorld.ReadBodyTransform(body);

            ++index;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var body in allCreatedBodies)
            {
                physicalWorld.DestroyBody(body);
            }

            allCreatedBodies.Clear();

            if (WorldVisualsPath != null)
            {
                WorldVisualsPath.Dispose();
                physicalWorld.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void SetupPhysicsBodies()
    {
        var sphere = PhysicsShape.CreateSphere(0.5f);

        sphereBodies.Add(physicalWorld.CreateMovingBody(sphere, new Vector3(0, 5, 0), Quat.Identity));

        var groundShape = PhysicsShape.CreateBox(new Vector3(100, 0.05f, 100));

        allCreatedBodies.Add(physicalWorld.CreateStaticBody(groundShape, new Vector3(0, -0.025f, 0), Quat.Identity));

        allCreatedBodies.AddRange(sphereBodies);
    }

    private Spatial CreateSphereVisual()
    {
        var sphere = new CSGMesh
        {
            Mesh = new SphereMesh
            {
                Radius = 0.5f,
                Height = 1.0f,
            },
        };

        worldVisuals.AddChild(sphere);

        return sphere;
    }
}
