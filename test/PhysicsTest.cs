﻿using System;
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

    [Export]
    public int SpawnPattern = 2;

    [Export]
    public float CameraZoomSpeed = 11.4f;

    /// <summary>
    ///   Sets MultiMesh position data with a single array assignment. Faster when all of the data has changed, but
    ///   slower when a lot of the data has not changed.
    /// </summary>
    [Export]
    public bool UseSingleVectorMultiMeshUpdate;

    [Export]
    public bool CreateMicrobeAsSpheres;

    [Export]
    public float MicrobeDamping = 0.3f;

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

    [Export]
    public NodePath TestNameLabelPath = null!;

    [Export]
    public NodePath TestExtraInfoLabelPath = null!;

    [Export]
    public NodePath PhysicsBodiesCountLabelPath = null!;

    [Export]
    public NodePath SpawnPatternInfoLabelPath = null!;

    /// <summary>
    ///   When using external physics it is possible to not display any visuals when far away
    /// </summary>
    private const float MicrobeVisibilityDistance = 150;

    /// <summary>
    ///   Initially used bigger visuals range to ensure first frame loads most of the displayers to make sure lag spike
    ///   happens only on the first frame
    /// </summary>
    private const float InitialVisibilityRangeIncrease = 100;

    private const float MicrobeCameraDefaultHeight = 50;

    private const float YDriftThreshold = 0.05f;

    private readonly List<NativePhysicsBody> allCreatedBodies = new();
    private readonly List<NativePhysicsBody> sphereBodies = new();

    private readonly List<Spatial> testVisuals = new();
    private readonly List<Node> otherCreatedNodes = new();

    private readonly List<NativePhysicsBody> microbeAnalogueBodies = new();
    private readonly List<TestMicrobeAnalogue> testMicrobesToProcess = new();

#pragma warning disable CA2213
    private Node worldVisuals = null!;

    private Camera camera = null!;

    private CustomWindow guiWindowRoot = null!;
    private Label deltaLabel = null!;
    private Label physicsTimingLabel = null!;
    private Label testNameLabel = null!;
    private Label testExtraInfoLabel = null!;
    private Label physicsBodiesCountLabel = null!;
    private Label spawnPatternInfoLabel = null!;

    private MultiMesh? sphereMultiMesh;
    private PhysicalWorld physicalWorld = null!;
#pragma warning restore CA2213

    private JVecF3[]? testMicrobeOrganellePositions;

    private int followedTestVisualIndex;

    /// <summary>
    ///   Player controller camera zoom level
    /// </summary>
    private float cameraHeightOffset;

    private float timeSincePhysicsReport;

    private bool testVisualsStarted;
    private bool resetTest;

    private float driftingCheckTimer = 30;

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
        testNameLabel = GetNode<Label>(TestNameLabelPath);
        testExtraInfoLabel = GetNode<Label>(TestExtraInfoLabelPath);
        physicsBodiesCountLabel = GetNode<Label>(PhysicsBodiesCountLabelPath);
        spawnPatternInfoLabel = GetNode<Label>(SpawnPatternInfoLabelPath);

        physicalWorld = PhysicalWorld.Create();

        StartTest();

        guiWindowRoot.Open(false);
    }

    public override void _PhysicsProcess(float delta)
    {
        base._PhysicsProcess(delta);

        if (Type == TestType.MicrobePlaceholdersGodotPhysics)
        {
            ProcessTestMicrobes(delta);
            UpdateCameraFollow(delta);

            var count = testMicrobesToProcess.Count;
            for (int i = 0; i < count; ++i)
            {
                if (Math.Abs(testMicrobesToProcess[i].GodotPhysicsPosition.y) > YDriftThreshold)
                {
                    if (driftingCheckTimer < 0)
                        GD.Print($"Drifting body Y in Godot physics (body index: {i})");
                }
            }

            if (driftingCheckTimer < 0)
                driftingCheckTimer = 10;
        }
    }

    public override void _Process(float delta)
    {
        if (resetTest)
        {
            RestartTest();
            return;
        }

        UpdateGUI(delta);
        HandleInput(delta);

        driftingCheckTimer -= delta;

        if (Type is TestType.SpheresGodotPhysics or TestType.MicrobePlaceholdersGodotPhysics)
        {
            return;
        }

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

                var instance = new MultiMeshInstance
                {
                    Multimesh = sphereMultiMesh,
                };

                worldVisuals.AddChild(instance);
                otherCreatedNodes.Add(instance);
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

            UpdateBodyCountGUI(count);
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

            UpdateBodyCountGUI(count);
        }
        else if (Type is TestType.MicrobePlaceholders)
        {
            // The delta here is based on the physics framerate
            ProcessTestMicrobes(1 / 60.0f);

            var distanceCutoff = MicrobeVisibilityDistance * MicrobeVisibilityDistance;

            if (!testVisualsStarted)
            {
                distanceCutoff += InitialVisibilityRangeIncrease * InitialVisibilityRangeIncrease;
            }

            var cameraPos = camera.Translation;

            var count = microbeAnalogueBodies.Count;
            int usedVisualIndex = 0;
            for (int i = 0; i < count; ++i)
            {
                var transform = physicalWorld.ReadBodyTransform(microbeAnalogueBodies[i]);

                if (transform.origin.DistanceSquaredTo(cameraPos) > distanceCutoff)
                {
                    if (usedVisualIndex < testVisuals.Count && testVisuals[usedVisualIndex].Visible)
                        testVisuals[usedVisualIndex].Visible = false;

                    ++usedVisualIndex;
                    continue;
                }

                if (usedVisualIndex >= testVisuals.Count)
                {
                    var visuals = CreateTestMicrobeVisuals(testMicrobeOrganellePositions!);

                    visuals.Transform = transform;
                    worldVisuals.AddChild(visuals);
                    testVisuals.Add(visuals);
                }
                else
                {
                    testVisuals[usedVisualIndex].Transform = transform;

                    if (!testVisuals[usedVisualIndex].Visible)
                        testVisuals[usedVisualIndex].Visible = true;

                    if (Math.Abs(transform.origin.y) > YDriftThreshold)
                    {
                        if (driftingCheckTimer < 0)
                            GD.Print($"Still drifting (body index: {i})");
                    }
                }

                ++usedVisualIndex;
            }

            while (usedVisualIndex < count)
            {
                if (testVisuals[usedVisualIndex].Visible)
                    testVisuals[usedVisualIndex].Visible = false;

                ++usedVisualIndex;
            }

            UpdateCameraFollow(1 / 60.0f);
            UpdateBodyCountGUI(count);
        }

        if (driftingCheckTimer < 0)
            driftingCheckTimer = 10;

        testVisualsStarted = true;
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
                TestNameLabelPath.Dispose();
                TestExtraInfoLabelPath.Dispose();
                PhysicsBodiesCountLabelPath.Dispose();
                SpawnPatternInfoLabelPath.Dispose();

                physicalWorld.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    // These used to be general helpers, but now are only needed to allow running this legacy benchmark
    private static uint CreateShapeOwnerWithTransform(CollisionObject entity, Transform transform, Shape shape)
    {
        var newShapeOwnerId = entity.CreateShapeOwner(shape);
        entity.ShapeOwnerAddShape(newShapeOwnerId, shape);
        entity.ShapeOwnerSetTransform(newShapeOwnerId, transform);
        return newShapeOwnerId;
    }

    private static uint CreateNewOwnerId(CollisionObject oldParent, CollisionObject newParent, Transform transform,
        uint oldShapeOwnerId)
    {
        var shape = oldParent.ShapeOwnerGetShape(oldShapeOwnerId, 0);
        var newShapeOwnerId = CreateShapeOwnerWithTransform(newParent, transform, shape);
        return newShapeOwnerId;
    }

    private void StartTest()
    {
        SetupPhysicsBodies();
        SetupCamera();
        UpdateTestNameLabel();

        testVisualsStarted = false;
        GD.Print("Test setup");
    }

    private void RestartTest()
    {
        GD.Print("Restarting test...");
        resetTest = false;

        // Destroy everything that currently exists
        foreach (var node in testVisuals)
        {
            node.DetachAndQueueFree();
        }

        testVisuals.Clear();

        foreach (var node in otherCreatedNodes)
        {
            node.DetachAndQueueFree();
        }

        otherCreatedNodes.Clear();

        foreach (var body in allCreatedBodies)
        {
            physicalWorld.DestroyBody(body);
        }

        sphereMultiMesh?.Free();
        sphereMultiMesh = null;

        allCreatedBodies.Clear();
        microbeAnalogueBodies.Clear();
        sphereBodies.Clear();
        testMicrobesToProcess.Clear();

        driftingCheckTimer = 30;
        timeSincePhysicsReport = 0;

        StartTest();
    }

    private void ProcessTestMicrobes(float delta)
    {
        if (Type == TestType.MicrobePlaceholders)
        {
            foreach (var testMicrobe in testMicrobesToProcess)
            {
                testMicrobe.Process(delta, physicalWorld);
            }
        }
        else if (Type == TestType.MicrobePlaceholdersGodotPhysics)
        {
            foreach (var testMicrobe in testMicrobesToProcess)
            {
                testMicrobe.ProcessGodot(delta);
            }
        }
    }

    private void UpdateGUI(float delta)
    {
        // Console logging of performance
        timeSincePhysicsReport += delta;

        var physicsTime = GetPhysicsTime();
        var physicsFPSLimit = 1 / physicsTime;

        if (timeSincePhysicsReport > 0.51)
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

    private void UpdateBodyCountGUI(int count)
    {
        physicsBodiesCountLabel.Text = count.ToString();
    }

    private void HandleInput(float delta)
    {
        if (Input.IsActionJustPressed("e_rotate_right"))
            ++followedTestVisualIndex;

        if (Input.IsActionJustPressed("e_rotate_left"))
            --followedTestVisualIndex;

        if (Input.IsActionJustPressed("e_reset_camera"))
            resetTest = true;

        // The zoom here doesn't work with mouse wheel, but as this had a bunch of other stuff already not using the
        // custom input system, this doesn't either
        if (Input.IsActionPressed("g_zoom_in"))
            cameraHeightOffset -= CameraZoomSpeed * delta;

        if (Input.IsActionPressed("g_zoom_out"))
            cameraHeightOffset += CameraZoomSpeed * delta;
    }

    private void SetupPhysicsBodies()
    {
        physicalWorld.SetGravity();

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

                    // This is added to the test visuals to allow the camera cycle algorithm to find these
                    worldVisuals.AddChild(body);
                    testVisuals.Add(body);
                    ++created;
                }
            }

            GD.Print("Created Godot rigid bodies: ", created);
            UpdateBodyCountGUI(created);

            var groundShape = new BoxShape
            {
                Extents = new Vector3(100, 0.05f, 100),
            };

            var ground = new StaticBody();
            var groundShapeOwner = ground.CreateShapeOwner(ground);
            ground.ShapeOwnerAddShape(groundShapeOwner, groundShape);

            ground.Translation = new Vector3(0, -0.025f, 0);
            worldVisuals.AddChild(ground);
            otherCreatedNodes.Add(ground);

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

        physicalWorld.RemoveGravity();

        var mutator = new Mutations(random);

        // Generate a random, pretty big microbe species to use for testing
        var microbeSpecies =
            mutator.CreateRandomSpecies(new MicrobeSpecies(1, string.Empty, string.Empty), 1, false, 25);

        testMicrobeOrganellePositions =
            microbeSpecies.Organelles.Select(o => new JVecF3(Hex.AxialToCartesian(o.Position))).ToArray();

        int created = 0;

        if (SpawnPattern < 2)
        {
            // Pattern 1: cells clumped together

            for (int x = -20; x < 20; x += 5)
            {
                for (int z = -20; z < 20; z += 5)
                {
                    ++created;

                    SpawnMicrobe(new Vector3(x, 0, z), random);
                }
            }
        }
        else if (SpawnPattern == 2)
        {
            // Pattern 2: a lot of spread out microbes

            for (int x = -200; x <= 200; x += 20)
            {
                for (int z = -200; z <= 200; z += 20)
                {
                    ++created;

                    SpawnMicrobe(new Vector3(x, 0, z), random);
                }
            }
        }
        else if (SpawnPattern == 3)
        {
            // Pattern 3: a full on stress test of the system

            for (int x = -300; x <= 300; x += 20)
            {
                for (int z = -300; z <= 300; z += 20)
                {
                    ++created;

                    SpawnMicrobe(new Vector3(x, 0, z), random);
                }
            }
        }
        else if (SpawnPattern == 4)
        {
            // Pattern 4: just a single cell for debug purposes

            ++created;

            SpawnMicrobe(new Vector3(0, 0, 0), random);
        }
        else
        {
            GD.PrintErr("Unknown microbe spawn pattern: ", SpawnPattern);
        }

        UpdateBodyCountGUI(created);

        if (Type == TestType.MicrobePlaceholdersGodotPhysics)
        {
            GD.Print("Created microbe physics test Godot rigid bodies: ", created);
        }
        else
        {
            GD.Print("Created microbe physics test instances: ", microbeAnalogueBodies.Count);
            allCreatedBodies.AddRange(microbeAnalogueBodies);
        }

        // Follow the middle microbe
        followedTestVisualIndex = (int)Math.Floor(testMicrobesToProcess.Count * 0.5f);
    }

    private void SpawnMicrobe(Vector3 location, Random random)
    {
        if (Type == TestType.MicrobePlaceholdersGodotPhysics)
        {
            var body = new RigidBody();
            body.Mass = 10;
            body.AxisLockAngularX = true;
            body.AxisLockAngularZ = true;
            body.AxisLockLinearY = true;
            body.LinearDamp = MicrobeDamping;

            if (CreateMicrobeAsSpheres)
            {
                CreateGodotMicrobePhysicsSpheres(body, testMicrobeOrganellePositions!);
            }
            else
            {
                CreateGodotMicrobePhysics(body, testMicrobeOrganellePositions!);
            }

            body.AddChild(CreateTestMicrobeVisuals(testMicrobeOrganellePositions!));
            body.Translation = location;

            worldVisuals.AddChild(body);
            testVisuals.Add(body);

            testMicrobesToProcess.Add(new TestMicrobeAnalogue(body, random.Next()));
        }
        else
        {
            // Don't optimize shape reuse as microbes can almost all be different shapes
            // TODO: calculate actual density
            var shape = PhysicsShape.CreateMicrobeShape(testMicrobeOrganellePositions!, 1000, false,
                CreateMicrobeAsSpheres);

            var body = physicalWorld.CreateMovingBodyWithAxisLock(shape, location, Quat.Identity, Vector3.Up, true);

            physicalWorld.SetDamping(body, MicrobeDamping);

            // Add an initial impulse
            physicalWorld.GiveImpulse(body, new Vector3(random.NextFloat(), random.NextFloat(), random.NextFloat()));

            microbeAnalogueBodies.Add(body);
            testMicrobesToProcess.Add(new TestMicrobeAnalogue(body, random.Next()));
        }
    }

    private void CreateGodotMicrobePhysics(RigidBody body, JVecF3[] points)
    {
        var shape = new ConvexPolygonShape();
        float thickness = 0.2f;

        shape.Points = points.Select(p => new Vector3(p))
            .SelectMany(p => new[] { p, new Vector3(p.x, p.y + thickness, p.z) }).ToArray();

        var owner = body.CreateShapeOwner(body);
        body.ShapeOwnerAddShape(owner, shape);
    }

    private void CreateGodotMicrobePhysicsSpheres(RigidBody body, JVecF3[] organellePositions)
    {
        var sphere = new SphereShape
        {
            Radius = 1,
        };

        foreach (var position in organellePositions)
        {
            CreateShapeOwnerWithTransform(body, new Transform(Basis.Identity, position), sphere);
        }
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
        if (Type is TestType.MicrobePlaceholders or TestType.MicrobePlaceholdersGodotPhysics)
        {
            // Top down view
            camera.Translation = new Vector3(0, MicrobeCameraDefaultHeight, 0);
            camera.LookAt(new Vector3(0, 0, 0), Vector3.Forward);
        }

        // TODO: reset camera for other tests
    }

    private void UpdateCameraFollow(float delta)
    {
        // Even though some visuals may be hidden, using the visible count here makes this very unstable
        var index = followedTestVisualIndex % testVisuals.Count;

        var target = testVisuals[index].Translation;

        var currentPos = camera.Translation;

        var targetPos = new Vector3(target.x, MicrobeCameraDefaultHeight + cameraHeightOffset, target.z);

        camera.Translation = currentPos.LinearInterpolate(targetPos, 3 * delta);
    }

    private float GetPhysicsTime()
    {
        if (Type is TestType.SpheresGodotPhysics or TestType.MicrobePlaceholdersGodotPhysics)
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

    private void UpdateTestNameLabel()
    {
        testNameLabel.Text = Type.ToString();

        if (Type is TestType.MicrobePlaceholders or TestType.MicrobePlaceholdersGodotPhysics)
        {
            testExtraInfoLabel.Text = "Microbes are convex: " + !CreateMicrobeAsSpheres;
        }
        else
        {
            testExtraInfoLabel.Visible = false;
        }

        spawnPatternInfoLabel.Text = SpawnPattern.ToString();
    }

    private class TestMicrobeAnalogue
    {
        private const float JoltImpulseStrength = 2820;
        private const float GodotImpulseStrength = 0.9f;

        private const float ReachTargetRotationSpeed = 1.2f;

        private readonly NativePhysicsBody? body;
        private readonly RigidBody? godotBody;
        private readonly Random random;

        private float timeUntilDirectionChange = 1;
        private float timeUntilMovementChange = 1;

        private int notMovedToOrigin = 6;

        private Quat lookDirection;
        private Vector3 movementDirection;

        public TestMicrobeAnalogue(NativePhysicsBody body, int randomSeed)
        {
            this.body = body;
            random = new Random(randomSeed);

            SetLookDirection();
        }

        public TestMicrobeAnalogue(RigidBody godotBody, int randomSeed)
        {
            this.godotBody = godotBody;
            random = new Random(randomSeed);

            SetLookDirection();
        }

        public Vector3 GodotPhysicsPosition { get; private set; }

        public void Process(float delta, PhysicalWorld physicalWorld)
        {
            HandleMovementDirectionAndRotation(delta,
                new Lazy<Vector3>(() => physicalWorld.ReadBodyTransform(body!).origin));

            // Impulse should not be scaled by delta here as the physics system applies the control for each physics
            // step
            physicalWorld.ApplyBodyMicrobeControl(body!, movementDirection * JoltImpulseStrength, lookDirection,
                ReachTargetRotationSpeed);
        }

        public void ProcessGodot(float delta)
        {
            HandleMovementDirectionAndRotation(delta,
                new Lazy<Vector3>(() => godotBody!.Translation));

            godotBody!.ApplyCentralImpulse(movementDirection * GodotImpulseStrength);

            var currentTransform = godotBody!.Transform;
            GodotPhysicsPosition = currentTransform.origin;

            var currentRotation = currentTransform.basis.Quat();

            var difference = currentRotation * lookDirection.Inverse();

            // This needs a really high tolerance to fix the jitter. Seems like if we don't want to use Jolt, a smarter
            // approach may be needed for physics rotation
            // if ((Quat.Identity - difference).LengthSquared < 0.000000001f)
            if ((Quat.Identity - difference).LengthSquared < 2.0f)
            {
                godotBody.AngularVelocity = new Vector3(0, 0, 0);
            }
            else
            {
                godotBody.AngularVelocity = new Vector3(0, 0, 0);

                godotBody.AngularVelocity = difference.GetEuler() / ReachTargetRotationSpeed;
            }
        }

        private void HandleMovementDirectionAndRotation(float delta, Lazy<Vector3> currentPosition)
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
                timeUntilMovementChange = 1.6f;

                if (notMovedToOrigin < 0)
                {
                    notMovedToOrigin = 15;
                    timeUntilMovementChange = 3;

                    if (currentPosition.Value.Length() > 1)
                    {
                        movementDirection = (-currentPosition.Value).Normalized();
                        movementDirection.y = 0;
                    }
                }
                else
                {
                    movementDirection = new Vector3(random.NextFloat() * 2 - 1 + 0.001f, 0,
                        random.NextFloat() * 2 - 1 - 0.001f).Normalized();
                }
            }
        }

        private void SetLookDirection()
        {
            lookDirection = new Quat(Vector3.Up, random.NextFloat() * 2 * Mathf.Pi);
        }
    }
}
