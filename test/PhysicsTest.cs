using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Tests / stress tests the physics system
/// </summary>
public class PhysicsTest : Node
{
    [Export]
    public TestType Type = TestType.MicrobePlaceholders;

    /// <summary>
    ///   Sets MultiMesh position data with a single array assignment. Faster when all of the data has changed, but
    ///   slower when a lot of the data has not changed.
    /// </summary>
    [Export]
    public bool UseSingleVectorMultiMeshUpdate;

    [Export]
    public NodePath? WorldVisualsPath;

    [Export]
    public NodePath CameraPath = null!;

    [Export]
    public NodePath GUIWindowRootPath = null!;

    [Export]
    public NodePath DeltaLabelPath = null!;

    [Export]
    public NodePath PhysicsTimingLabelPath = null!;

    private readonly List<PhysicsBody> allCreatedBodies = new();
    private readonly List<PhysicsBody> sphereBodies = new();

    private readonly List<Spatial> testVisuals = new();

    private readonly List<PhysicsBody> microbeAnalogueBodies = new();
    private readonly List<TestMicrobeAnalogue> testMicrobesToProcess = new();

#pragma warning disable CA2213
    private Node worldVisuals = null!;

    private Camera camera = null!;

    private CustomWindow guiWindowRoot = null!;
    private Label deltaLabel = null!;
    private Label physicsTimingLabel = null!;

    private MultiMesh? sphereMultiMesh;
    private PhysicalWorld physicalWorld = null!;
#pragma warning restore CA2213

    private JVecF3[]? testMicrobeOrganellePositions;

    private int followedTestVisualIndex;

    private float timeSincePhysicsReport;

    public enum TestType
    {
        Spheres,
        SpheresIndividualNodes,
        SpheresGodotPhysics,
        MicrobePlaceholders,
        MicrobePlaceholdersGodotPhysics,
    }

    public override void _Ready()
    {
        worldVisuals = GetNode(WorldVisualsPath);
        camera = GetNode<Camera>(CameraPath);

        guiWindowRoot = GetNode<CustomWindow>(GUIWindowRootPath);
        deltaLabel = GetNode<Label>(DeltaLabelPath);
        physicsTimingLabel = GetNode<Label>(PhysicsTimingLabelPath);

        physicalWorld = PhysicalWorld.Create();
        SetupPhysicsBodies();
        SetupCamera();

        guiWindowRoot.Open(false);
    }

    public override void _PhysicsProcess(float delta)
    {
        base._PhysicsProcess(delta);

        if (Type == TestType.SpheresGodotPhysics)
        {
            ProcessTestMicrobes(delta);
        }
    }

    public override void _Process(float delta)
    {
        UpdateGUI(delta);
        HandleInput();

        if (Type == TestType.SpheresGodotPhysics)
            return;

        if (!physicalWorld.ProcessPhysics(delta))
            return;

        if (Type == TestType.Spheres)
        {
            // Display the spheres
            if (sphereMultiMesh == null)
            {
                sphereMultiMesh = new MultiMesh
                {
                    Mesh = CreateSphereMesh().Mesh,
                    TransformFormat = MultiMesh.TransformFormatEnum.Transform3d,
                };

                worldVisuals.AddChild(new MultiMeshInstance
                {
                    Multimesh = sphereMultiMesh,
                });
            }

            if (sphereMultiMesh.InstanceCount != sphereBodies.Count)
                sphereMultiMesh.InstanceCount = sphereBodies.Count;

            var count = sphereBodies.Count;

            if (!UseSingleVectorMultiMeshUpdate)
            {
                for (int i = 0; i < count; ++i)
                {
                    sphereMultiMesh.SetInstanceTransform(i, physicalWorld.ReadBodyTransform(sphereBodies[i]));
                }
            }
            else
            {
                var transformData = new Vector3[count * 4];

                for (int i = 0; i < count; ++i)
                {
                    var transform = physicalWorld.ReadBodyTransform(sphereBodies[i]);

                    transformData[i * 4] = transform[0];
                    transformData[i * 4 + 1] = transform[1];
                    transformData[i * 4 + 2] = transform[2];
                    transformData[i * 4 + 3] = transform[3];
                }

                sphereMultiMesh.TransformArray = transformData;
            }
        }
        else if (Type == TestType.SpheresIndividualNodes)
        {
            // To not completely destroy things we need to generate the shape once
            var sphereVisual = new Lazy<Mesh>(() => CreateSphereMesh().Mesh);

            var count = sphereBodies.Count;
            for (int i = 0; i < count; ++i)
            {
                if (i >= testVisuals.Count)
                {
                    var sphere = new MeshInstance
                    {
                        Mesh = sphereVisual.Value,
                    };

                    sphere.Transform = physicalWorld.ReadBodyTransform(sphereBodies[i]);
                    worldVisuals.AddChild(sphere);
                    testVisuals.Add(sphere);
                }
                else
                {
                    testVisuals[i].Transform = physicalWorld.ReadBodyTransform(sphereBodies[i]);
                }
            }
        }
        else if (Type == TestType.MicrobePlaceholders)
        {
            // The delta here is based on the physics framerate
            ProcessTestMicrobes(1 / 60.0f);

            var count = microbeAnalogueBodies.Count;
            for (int i = 0; i < count; ++i)
            {
                if (i >= testVisuals.Count)
                {
                    var visuals = CreateTestMicrobeVisuals(testMicrobeOrganellePositions!);

                    visuals.Transform = physicalWorld.ReadBodyTransform(microbeAnalogueBodies[i]);
                    worldVisuals.AddChild(visuals);
                    testVisuals.Add(visuals);
                }
                else
                {
                    testVisuals[i].Transform = physicalWorld.ReadBodyTransform(microbeAnalogueBodies[i]);
                }
            }

            UpdateCameraFollow(1 / 60.0f);
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
                CameraPath.Dispose();
                GUIWindowRootPath.Dispose();
                DeltaLabelPath.Dispose();
                PhysicsTimingLabelPath.Dispose();

                physicalWorld.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void ProcessTestMicrobes(float delta)
    {
        foreach (var testMicrobe in testMicrobesToProcess)
        {
            testMicrobe.Process(delta, physicalWorld);
        }
    }

    private void UpdateGUI(float delta)
    {
        // Console logging of performance
        timeSincePhysicsReport += delta;

        var physicsTime = GetPhysicsTime();
        var physicsFPSLimit = 1 / physicsTime;

        if (timeSincePhysicsReport > 0.5)
        {
            timeSincePhysicsReport = 0;
            GD.Print($"Physics time: {physicsTime} Physics FPS limit: " +
                $"{physicsFPSLimit}, FPS: {Engine.GetFramesPerSecond()}");
        }

        // The actual GUI update part
        deltaLabel.Text = new LocalizedString("FRAME_DURATION", delta).ToString();

        // This is not translated as the test folder is not extracted in terms of translations (and this is only used
        // in here)
        // physicsTimingLabel.Text = new LocalizedString("PHYSICS_TEST_TIMINGS", physicsTime, physicsFPSLimit);
        physicsTimingLabel.Text = $"Avg physics: {physicsTime} (physics FPS limit: {physicsFPSLimit})";
        guiWindowRoot.WindowTitle = new LocalizedString("FPS", Engine.GetFramesPerSecond()).ToString();
    }

    private void HandleInput()
    {
        if (Input.IsActionJustPressed("e_rotate_right"))
            ++followedTestVisualIndex;

        if (Input.IsActionJustPressed("e_rotate_left"))
            --followedTestVisualIndex;
    }

    private void SetupPhysicsBodies()
    {
        if (Type is TestType.MicrobePlaceholders or TestType.MicrobePlaceholdersGodotPhysics)
        {
            SetupMicrobeTest();
            return;
        }

        var random = new Random(234654642);

        if (Type == TestType.SpheresGodotPhysics)
        {
            var sphere = new SphereShape
            {
                Radius = 0.5f,
            };

            var visuals = CreateSphereMesh();
            int created = 0;

            for (int x = -100; x < 100; x += 2)
            {
                for (int z = -100; z < 100; z += 2)
                {
                    var body = new RigidBody();
                    body.AddChild(new MeshInstance
                    {
                        Mesh = visuals.Mesh,
                    });
                    var owner = body.CreateShapeOwner(body);
                    body.ShapeOwnerAddShape(owner, sphere);

                    body.Translation = new Vector3(x, 1 + (float)random.NextDouble() * 25, z);

                    worldVisuals.AddChild(body);
                    ++created;
                }
            }

            GD.Print("Created Godot rigid bodies: ", created);

            var groundShape = new BoxShape
            {
                Extents = new Vector3(100, 0.05f, 100),
            };

            var ground = new StaticBody();
            var groundShapeOwner = ground.CreateShapeOwner(ground);
            ground.ShapeOwnerAddShape(groundShapeOwner, groundShape);

            ground.Translation = new Vector3(0, -0.025f, 0);
            worldVisuals.AddChild(ground);

            return;
        }

        if (Type is TestType.Spheres or TestType.SpheresIndividualNodes)
        {
            var sphere = PhysicsShape.CreateSphere(0.5f);

            for (int x = -100; x < 100; x += 2)
            {
                for (int z = -100; z < 100; z += 2)
                {
                    sphereBodies.Add(physicalWorld.CreateMovingBody(sphere,
                        new Vector3(x, 1 + (float)random.NextDouble() * 25, z), Quat.Identity));
                }
            }

            GD.Print("Created physics spheres: ", sphereBodies.Count);

            var groundShape = PhysicsShape.CreateBox(new Vector3(100, 0.05f, 100));

            allCreatedBodies.Add(physicalWorld.CreateStaticBody(groundShape, new Vector3(0, -0.025f, 0),
                Quat.Identity));

            allCreatedBodies.AddRange(sphereBodies);
        }
    }

    private void SetupMicrobeTest()
    {
        var random = new Random(234546798);

        var mutator = new Mutations(random);

        // Generate a random, pretty big microbe species to use for testing
        var microbeSpecies =
            mutator.CreateRandomSpecies(new MicrobeSpecies(1, string.Empty, string.Empty), 1, false, 25);

        testMicrobeOrganellePositions =
            microbeSpecies.Organelles.Select(o => new JVecF3(Hex.AxialToCartesian(o.Position))).ToArray();

        if (Type == TestType.MicrobePlaceholdersGodotPhysics)
        {
            throw new NotImplementedException();
        }

        // for (int x = -200; x < 200; x += 10)
        // {
        //     for (int z = -200; z < 200; z += 10)
        //     {
        for (int x = -20; x < 20; x += 5)
        {
            for (int z = -20; z < 20; z += 5)
            {
                // Don't optimize shapes as microbes can almost all be different shapes
                // TODO: calculate actual density
                var shape = PhysicsShape.CreateMicrobeShape(testMicrobeOrganellePositions, 1000, false);

                var body = physicalWorld.CreateMovingBody(shape,
                    new Vector3(x, 0, z), Quat.Identity);

                physicalWorld.AddAxisLockConstraint(body, Vector3.Up, true);

                // Add an initial impulse
                physicalWorld.GiveImpulse(body,
                    new Vector3(random.NextFloat(), random.NextFloat(), random.NextFloat()));

                microbeAnalogueBodies.Add(body);
                testMicrobesToProcess.Add(new TestMicrobeAnalogue(body, random.Next()));
            }
        }

        // Follow the middle microbe
        followedTestVisualIndex = (int)Math.Floor(testMicrobesToProcess.Count * 0.5f);

        GD.Print("Created microbe physics test instances: ", microbeAnalogueBodies.Count);
        allCreatedBodies.AddRange(microbeAnalogueBodies);
    }

    private Spatial CreateTestMicrobeVisuals(IReadOnlyList<JVecF3> organellePositions)
    {
        var multiMesh = new MultiMesh
        {
            Mesh = CreateSphereMesh().Mesh,
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3d,
        };

        multiMesh.InstanceCount = organellePositions.Count;

        for (int i = 0; i < organellePositions.Count; ++i)
        {
            multiMesh.SetInstanceTransform(i, new Transform(Basis.Identity, new Vector3(organellePositions[i])));
        }

        return new MultiMeshInstance
        {
            Multimesh = multiMesh,
        };
    }

    private void SetupCamera()
    {
        if (Type == TestType.MicrobePlaceholders)
        {
            // Top down view
            camera.Translation = new Vector3(0, 50, 0);
            camera.LookAt(new Vector3(0, 0, 0), Vector3.Forward);
        }
    }

    private void UpdateCameraFollow(float delta)
    {
        var index = followedTestVisualIndex % testVisuals.Count;

        var target = testVisuals[index].Translation;

        var currentPos = camera.Translation;
        var targetPos = new Vector3(target.x, currentPos.y, target.z);

        camera.Translation = currentPos.LinearInterpolate(targetPos, 3 * delta);
    }

    private float GetPhysicsTime()
    {
        if (Type == TestType.SpheresGodotPhysics)
            return Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess);

        return physicalWorld.AveragePhysicsDuration;
    }

    private CSGMesh CreateSphereMesh()
    {
        var sphere = new CSGMesh
        {
            Mesh = new SphereMesh
            {
                Radius = 1.0f,
                Height = 2.0f,
            },
        };

        return sphere;
    }

    private class TestMicrobeAnalogue
    {
        private const float JoltImpulseStrength = 840;

        private readonly PhysicsBody body;
        private readonly Random random;

        private float timeUntilDirectionChange = 1;
        private float timeUntilMovementChange = 1;

        private int notMovedToOrigin = 15;

        private Quat lookDirection;
        private Vector3 movementDirection;

        public TestMicrobeAnalogue(PhysicsBody body, int randomSeed)
        {
            this.body = body;
            random = new Random(randomSeed);

            SetLookDirection();
        }

        public void Process(float delta, PhysicalWorld physicalWorld)
        {
            timeUntilDirectionChange -= delta;
            timeUntilMovementChange -= delta;

            if (timeUntilDirectionChange < 0)
            {
                timeUntilDirectionChange = 2.5f;
                SetLookDirection();
            }

            if (timeUntilMovementChange < 0)
            {
                --notMovedToOrigin;
                timeUntilMovementChange = 1.5f;

                if (notMovedToOrigin < 0)
                {
                    notMovedToOrigin = 30;

                    var currentPosition = physicalWorld.ReadBodyTransform(body).origin;

                    if (currentPosition.Length() < 1)
                    {
                        movementDirection = (-currentPosition).Normalized();
                    }
                }
                else
                {
                    movementDirection = new Vector3(random.NextFloat() + 0.001f, 0, random.NextFloat()).Normalized();
                }
            }

            // Impulse should not be scaled by delta as the physics update happens with consistent
            physicalWorld.ApplyBodyMicrobeControl(body, movementDirection * JoltImpulseStrength, lookDirection,
                0.8f);
        }

        private void SetLookDirection()
        {
            lookDirection = new Quat(Vector3.Up, random.NextFloat() * 2 * Mathf.Pi);
        }
    }
}
