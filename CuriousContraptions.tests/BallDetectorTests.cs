using System;
using System.Buffers.Binary;
using System.Linq;
using CuriousContraptions.Gpu;
using Godot;
using NumQuaternion = System.Numerics.Quaternion;
using NumVector3 = System.Numerics.Vector3;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class BallDetectorTests(NativeSceneFixture godot)
{
    [Fact]
    public void PartRegistry_DiscoversBallDetectorDefinitionAndConfiguresInstance()
    {
        var registry = new PartRegistry();
        registry.Load();

        Assert.True(registry.Definitions.ContainsKey(PartId.BallDetector));
        var def = registry.Definitions[PartId.BallDetector];
        Assert.Equal(PartId.BallDetector, def.PartId);
        Assert.Equal("ball_detector", def.Id);
        Assert.Equal("Ball detector", def.Title);
        Assert.Equal(CuriousContraptions.Abstractions.ElementCategory.Control, def.Category);
        Assert.Equal("An open collar sends one trigger when a ball passes in the arrow direction. It must clear the upstream side before triggering again. Snaps to matching tube mouths; does not supply electricity.", def.Description);
        Assert.NotNull(def.Scene);

        var part = registry.Create(PartId.BallDetector);
        try
        {
            godot.Tree.Root.AddChild(part);
            Assert.NotNull(part);
            Assert.Same(def, part.Definition);
            Assert.Equal(PartId.BallDetector, part.PartId);
        }
        finally
        {
            part.GetParent()?.RemoveChild(part);
            part.Free();
        }
    }

    [Fact]
    public void BallDetectorScene_ExtractsExact17CollidersAndThreePortLocators()
    {
        var packedScene = ResourceLoader.Load<PackedScene>("res://elements/BallDetector/ball_detector.tscn");
        Assert.NotNull(packedScene);
        var instance = packedScene.Instantiate<Node3D>();
        try
        {
            var (colliders, ports) = TestBundle.SceneGeometry(instance);

            // Exactly 17 colliders: 16 circumference + 1 housing
            Assert.Equal(17, colliders.Count);

            // 16 circumference colliders
            var circumference = colliders.Take(16).ToArray();
            var expectedCircHalfExtents = new MetreVector((Half)0.28, (Half)0.025, (Half)0.14);
            foreach (var col in circumference)
            {
                Assert.Equal(ColliderShapeKind.Box, col.ShapeKind);
                Assert.Equal(expectedCircHalfExtents.X, col.HalfExtents.X);
                Assert.Equal(expectedCircHalfExtents.Y, col.HalfExtents.Y);
                Assert.Equal(expectedCircHalfExtents.Z, col.HalfExtents.Z);

                Assert.Equal((Half)0, col.LocalPose.Translation.X);
                var py = (float)col.LocalPose.Translation.Y;
                var pz = (float)col.LocalPose.Translation.Z;
                var dist = MathF.Sqrt(py * py + pz * pz);
                Assert.InRange(dist, 0.674f, 0.676f);
            }

            // 1 housing collider
            var housing = colliders[16];
            Assert.Equal(ColliderShapeKind.Box, housing.ShapeKind);
            Assert.Equal(new MetreVector((Half)0.25, (Half)0.12, (Half)0.25), housing.HalfExtents);
            Assert.Equal((Half)0, housing.LocalPose.Translation.X);
            Assert.Equal((Half)0.93, housing.LocalPose.Translation.Y);
            Assert.Equal((Half)0, housing.LocalPose.Translation.Z);

            // 3 port locators: ActivationOut, mouth_start, mouth_end
            Assert.Equal(3, ports.Count);
            Assert.True(ports.ContainsKey("ActivationOut"));
            Assert.True(ports.ContainsKey("mouth_start"));
            Assert.True(ports.ContainsKey("mouth_end"));

            var actOut = ports["ActivationOut"];
            Assert.Equal("ActivationOut", actOut.PortId);
            Assert.True(MathF.Abs(actOut.Position.X) < 0.001f);
            Assert.True(MathF.Abs(actOut.Position.Y - 1.12f) < 0.001f);
            Assert.True(MathF.Abs(actOut.Position.Z) < 0.001f);

            var start = ports["mouth_start"];
            Assert.Equal("mouth_start", start.PortId);
            Assert.True(MathF.Abs(start.Position.X - (-0.28f)) < 0.001f);
            Assert.True(MathF.Abs(start.Position.Y) < 0.001f);
            Assert.True(MathF.Abs(start.Position.Z) < 0.001f);

            var end = ports["mouth_end"];
            Assert.Equal("mouth_end", end.PortId);
            Assert.True(MathF.Abs(end.Position.X - 0.28f) < 0.001f);
            Assert.True(MathF.Abs(end.Position.Y) < 0.001f);
            Assert.True(MathF.Abs(end.Position.Z) < 0.001f);
        }
        finally
        {
            instance.Free();
        }
    }

    [Fact]
    public void WorkshopPhysicsCompiler_CompilesBallDetectorAndActivationNetwork()
    {
        var detector = WorkshopInput.BallDetector(new GpuBodyId(10), 0, 3, 0, 0, 0, 0, 1);
        var lamp = WorkshopInput.Lamp(new GpuBodyId(20), 5, 3, 0, 0, 0, 0, 1);
        var ball = WorkshopInput.Generic(PartId.Basketball,new GpuBodyId(2), 0, 10, 0, 0, 0, 0, 1);
        var connection = new WorkshopConnection(
            new GpuBodyId(10), WorkshopSocket.ActivationOut,
            new GpuBodyId(20), WorkshopSocket.ActivationIn,
            WorkshopConnectionDomain.Activation);

        var construction = new WorkshopConstruction(
            new ConstructionRevision(1),
            WorkshopCadenceSettings.Default(),
            new WorkshopInstances(ball, detector, lamp),
            Connections: new WorkshopConnections(connection));

        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var network = WorkshopActivationCompiler.Compile(construction);

        Assert.Equal(4, scene.Bodies.Length);
        var detectorBody = Assert.Single(scene.Bodies.ToArray(), b => b.Id == new GpuBodyId(10));
        Assert.Equal(RigidMotionKind.Static, detectorBody.Motion);

        var colliders = scene.Colliders.ToArray().Where(c => c.Body == detectorBody.Id).ToArray();
        Assert.Equal(17, colliders.Length);

        // Network checks
        Assert.Equal(2, network.Nodes.Length);
        var detectorNode = Assert.Single(network.Nodes.ToArray(), n => n.Owner == detector.Id);
        Assert.Equal(ActivationNodeKind.PassageSource, detectorNode.Kind);
        Assert.Equal(WorkshopPhysicsCompiler.PassageSensor(detector.Id), detectorNode.Passage);

        var lampNode = Assert.Single(network.Nodes.ToArray(), n => n.Owner == lamp.Id);
        Assert.Equal(ActivationNodeKind.StickyFirstInput, lampNode.Kind);

        Assert.Equal(1, network.Edges.Length);
        Assert.Equal(new ActivationEdge(detectorNode.Id, lampNode.Id, WorkshopSocket.ActivationIn), network.Edges[0]);

        Assert.Equal(1, network.PassageSensors.Length);
        var sensor = network.PassageSensors[0];
        Assert.Equal(detector.Id, sensor.Frame);
        Assert.Equal((Half)0.65, sensor.Radius.Value);
        Assert.Equal((Half)0.02, sensor.RearmClearance.Value);
    }

    [Theory]
    [InlineData(4f, 20f)]
    [InlineData(40f, 30f)]
    [InlineData(120f, 40f)]
    [InlineData(40f, -90f)]
    public void PassageSensorTracker_PositiveForwardPass_RotatedAndVariousSpeeds(float speed, float angleDegrees)
    {
        var rad = angleDegrees * MathF.PI / 180f;
        var rotation = CanonicalRotation.Normalize(0, 0, MathF.Sin(rad / 2f), MathF.Cos(rad / 2f));
        var q = new NumQuaternion((float)rotation.X, (float)rotation.Y, (float)rotation.Z, (float)rotation.W);

        var detector = new WorkshopBallDetector(
            new GpuBodyId(10),
            new CellOrigin(0, 48, 0),
            default,
            rotation);

        var lamp = new WorkshopLamp(
            new GpuBodyId(20),
            new CellOrigin(80, 48, 0),
            default,
            CanonicalRotation.Identity);

        var connection = new WorkshopConnection(
            detector.Id, WorkshopSocket.ActivationOut,
            lamp.Id, WorkshopSocket.ActivationIn,
            WorkshopConnectionDomain.Activation);

        var ballId = new GpuBodyId(2);
        var detectorPos = new NumVector3(0, 3f, 0);
        var forward = NumVector3.Transform(new NumVector3(1f, 0, 0), q);

        var startLocalX = -1.0f;
        var startPos = detectorPos + forward * startLocalX;

        var ballInitial = WorkshopInput.Generic(PartId.Basketball,
            ballId,
            startPos.X, startPos.Y, startPos.Z,
            0, 0, 0, 1);

        var construction = new WorkshopConstruction(
            new ConstructionRevision(1),
            WorkshopCadenceSettings.Default(),
            new WorkshopInstances(ballInitial, detector, lamp),
            Connections: new WorkshopConnections(connection));

        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var network = WorkshopActivationCompiler.Compile(construction);

        var initialBodies = new PhysicsBodyReadSet([
            new PhysicsBodyRead(new CanonicalBody(ballId, 0, 0, ballInitial.Cell, ballInitial.Local, default), CanonicalRotation.Identity, default, default),
            new PhysicsBodyRead(new CanonicalBody(detector.Id, 0, 0, detector.Cell, detector.Local, default), rotation, default, default)
        ]);

        var tracker = PassageSensorTracker.Create(network.PassageSensors, scene.Colliders.ToArray(), initialBodies);
        Assert.Single(tracker.States);
        Assert.Equal(PassageSensorPhase.Armed, tracker.States[0].Phase);

        var committedActivations = network.Clear();
        var committedTimers = network.ClearTimers();

        var dt = 1.0f / 480f;
        var currentPos = startPos;
        var endLocalX = 2.0f;
        var totalDist = endLocalX - startLocalX;
        var totalTime = totalDist / speed;
        var totalSteps = (int)MathF.Ceiling(totalTime / dt);

        Span<PassageSensorRead> passageOutputs = stackalloc PassageSensorRead[8];
        var eventCount = 0;
        var firedSpeed = 0f;
        var finalLocalX = 0f;

        for (var step = 1; step <= totalSteps; step++)
        {
            var t = step * dt;
            currentPos = startPos + forward * (speed * t);
            var localPos = NumVector3.Transform(currentPos - detectorPos, NumQuaternion.Conjugate(q));
            finalLocalX = localPos.X;

            var px = WorkshopInput.Position(currentPos.X);
            var py = WorkshopInput.Position(currentPos.Y);
            var pz = WorkshopInput.Position(currentPos.Z);

            var bodies = new PhysicsBodyReadSet([
                new PhysicsBodyRead(new CanonicalBody(ballId, 0, 0, new(px.Cell, py.Cell, pz.Cell), new((float)px.Local, (float)py.Local, (float)pz.Local), default), CanonicalRotation.Identity, default, default),
                new PhysicsBodyRead(new CanonicalBody(detector.Id, 0, 0, detector.Cell, detector.Local, default), rotation, default, default)
            ]);

            tracker = tracker.Advance(new SimulationTick((ulong)step), bodies, null, passageOutputs, out var count);
            if (count > 0)
            {
                eventCount += count;
                firedSpeed = (float)passageOutputs[0].Speed.Value;
                var checkpoint = network.Consume(committedActivations, committedTimers, default, new SimulationTick((ulong)step), default, passageOutputs[..count]);
                committedActivations = checkpoint.Activations;
                committedTimers = checkpoint.Timers;
            }
        }

        Assert.Equal(1, eventCount);
        Assert.True(MathF.Abs(firedSpeed - speed) <= 0.002f, $"Speed {firedSpeed} must match {speed} within 0.002 m/s");
        Assert.True(finalLocalX > 1.9f, $"Final local X {finalLocalX} must be > 1.9 m");

        var lampSlot = -1;
        for (var i = 0; i < network.Nodes.Length; i++) if (network.Nodes[i].Owner == lamp.Id) lampSlot = i;
        Assert.True(lampSlot >= 0);
        Assert.Equal(ActivationPhase.Latched, committedActivations[lampSlot].Phase);

        // Reset clears count and active state
        var resetTracker = PassageSensorTracker.Create(network.PassageSensors, scene.Colliders.ToArray(), initialBodies);
        var resetActivations = network.Clear();
        Assert.Equal(0UL, resetTracker.States[0].PassedCount);
        Assert.Equal(ActivationPhase.Clear, resetActivations[lampSlot].Phase);
    }

    [Fact]
    public void PassageSensorTracker_NegativeControls_ReverseOutsideStationary()
    {
        var detector = WorkshopInput.BallDetector(new GpuBodyId(10), 0, 3, 0, 0, 0, 0, 1);
        var ballId = new GpuBodyId(2);
        var ball = WorkshopInput.Generic(PartId.Basketball,ballId, 2, 3, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(
            new ConstructionRevision(1),
            WorkshopCadenceSettings.Default(),
            new WorkshopInstances(ball, detector));

        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var network = WorkshopActivationCompiler.Compile(construction);

        // Control 1: Reverse motion from X = 2.0 to X = -2.0 at 4 m/s
        var initialBodies = new PhysicsBodyReadSet([
            new PhysicsBodyRead(new CanonicalBody(ballId, 0, 0, ball.Cell, ball.Local, default), CanonicalRotation.Identity, default, default),
            new PhysicsBodyRead(new CanonicalBody(detector.Id, 0, 0, detector.Cell, detector.Local, default), CanonicalRotation.Identity, default, default)
        ]);
        var tracker = PassageSensorTracker.Create(network.PassageSensors, scene.Colliders.ToArray(), initialBodies);
        Span<PassageSensorRead> passageOutputs = stackalloc PassageSensorRead[8];

        var dt = 1.0f / 480f;
        var reverseSteps = (int)MathF.Ceiling(4.0f / (4f * dt));
        var reverseCount = 0;
        for (var step = 1; step <= reverseSteps; step++)
        {
            var posX = 2.0f - 4f * (step * dt);
            var px = WorkshopInput.Position(posX);
            var bodies = new PhysicsBodyReadSet([
                new PhysicsBodyRead(new CanonicalBody(ballId, 0, 0, new(px.Cell, 48, 0), new((float)px.Local, 0f, 0f), default), CanonicalRotation.Identity, default, default),
                new PhysicsBodyRead(new CanonicalBody(detector.Id, 0, 0, detector.Cell, detector.Local, default), CanonicalRotation.Identity, default, default)
            ]);
            tracker = tracker.Advance(new SimulationTick((ulong)step), bodies, null, passageOutputs, out var count);
            reverseCount += count;
        }
        Assert.Equal(0, reverseCount);

        // Control 2: Glancing outside pass at Y = +1.4m (outside bore radius 0.65m)
        var startOutsideX = WorkshopInput.Position(-1.0f);
        var outsideY = WorkshopInput.Position(3.0f + 1.4f);
        var outsideInitialBodies = new PhysicsBodyReadSet([
            new PhysicsBodyRead(new CanonicalBody(ballId, 0, 0, new(startOutsideX.Cell, outsideY.Cell, 0), new((float)startOutsideX.Local, (float)outsideY.Local, 0f), default), CanonicalRotation.Identity, default, default),
            new PhysicsBodyRead(new CanonicalBody(detector.Id, 0, 0, detector.Cell, detector.Local, default), CanonicalRotation.Identity, default, default)
        ]);
        var outsideTracker = PassageSensorTracker.Create(network.PassageSensors, scene.Colliders.ToArray(), outsideInitialBodies);
        var outsideSteps = (int)MathF.Ceiling(3.0f / (4f * dt));
        var outsideCount = 0;
        for (var step = 1; step <= outsideSteps; step++)
        {
            var posX = -1.0f + 4f * (step * dt);
            var px = WorkshopInput.Position(posX);
            var bodies = new PhysicsBodyReadSet([
                new PhysicsBodyRead(new CanonicalBody(ballId, 0, 0, new(px.Cell, outsideY.Cell, 0), new((float)px.Local, (float)outsideY.Local, 0f), default), CanonicalRotation.Identity, default, default),
                new PhysicsBodyRead(new CanonicalBody(detector.Id, 0, 0, detector.Cell, detector.Local, default), CanonicalRotation.Identity, default, default)
            ]);
            outsideTracker = outsideTracker.Advance(new SimulationTick((ulong)step), bodies, null, passageOutputs, out var count);
            outsideCount += count;
        }
        Assert.Equal(0, outsideCount);

        // Control 3: Stationary ball at X = 0 for 100 steps
        var stationaryX = WorkshopInput.Position(0f);
        var stationaryBodies = new PhysicsBodyReadSet([
            new PhysicsBodyRead(new CanonicalBody(ballId, 0, 0, new(stationaryX.Cell, 48, 0), new((float)stationaryX.Local, 0f, 0f), default), CanonicalRotation.Identity, default, default),
            new PhysicsBodyRead(new CanonicalBody(detector.Id, 0, 0, detector.Cell, detector.Local, default), CanonicalRotation.Identity, default, default)
        ]);
        var stationaryTracker = PassageSensorTracker.Create(network.PassageSensors, scene.Colliders.ToArray(), stationaryBodies);
        var stationaryCount = 0;
        for (var step = 1; step <= 100; step++)
        {
            stationaryTracker = stationaryTracker.Advance(new SimulationTick((ulong)step), stationaryBodies, null, passageOutputs, out var count);
            stationaryCount += count;
        }
        Assert.Equal(0, stationaryCount);
    }

    [Fact]
    public void PassageSensorTracker_JitterAcrossPlane_DoesNotRetriggerWithoutClearance()
    {
        var detector = WorkshopInput.BallDetector(new GpuBodyId(10), 0, 3, 0, 0, 0, 0, 1);
        var ballId = new GpuBodyId(2);
        var ball = WorkshopInput.Generic(PartId.Basketball,ballId, -1, 3, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(
            new ConstructionRevision(1),
            WorkshopCadenceSettings.Default(),
            new WorkshopInstances(ball, detector));

        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var network = WorkshopActivationCompiler.Compile(construction);

        var initialBodies = new PhysicsBodyReadSet([
            new PhysicsBodyRead(new CanonicalBody(ballId, 0, 0, ball.Cell, ball.Local, default), CanonicalRotation.Identity, default, default),
            new PhysicsBodyRead(new CanonicalBody(detector.Id, 0, 0, detector.Cell, detector.Local, default), CanonicalRotation.Identity, default, default)
        ]);

        var tracker = PassageSensorTracker.Create(network.PassageSensors, scene.Colliders.ToArray(), initialBodies);
        Span<PassageSensorRead> passageOutputs = stackalloc PassageSensorRead[8];

        // 1. Initial forward crossing from X = -0.5 to X = +0.5
        var pNeg = WorkshopInput.Position(-0.5f);
        var pPos = WorkshopInput.Position(0.5f);
        var bodiesNeg = new PhysicsBodyReadSet([
            new PhysicsBodyRead(new CanonicalBody(ballId, 0, 0, new(pNeg.Cell, 48, 0), new((float)pNeg.Local, 0f, 0f), default), CanonicalRotation.Identity, default, default),
            new PhysicsBodyRead(new CanonicalBody(detector.Id, 0, 0, detector.Cell, detector.Local, default), CanonicalRotation.Identity, default, default)
        ]);
        var bodiesPos = new PhysicsBodyReadSet([
            new PhysicsBodyRead(new CanonicalBody(ballId, 0, 0, new(pPos.Cell, 48, 0), new((float)pPos.Local, 0f, 0f), default), CanonicalRotation.Identity, default, default),
            new PhysicsBodyRead(new CanonicalBody(detector.Id, 0, 0, detector.Cell, detector.Local, default), CanonicalRotation.Identity, default, default)
        ]);

        tracker = tracker.Advance(new SimulationTick(1), bodiesNeg, null, passageOutputs, out var count1);
        Assert.Equal(0, count1);
        tracker = tracker.Advance(new SimulationTick(2), bodiesPos, null, passageOutputs, out var count2);
        Assert.Equal(1, count2);

        // 2. Twenty jitter oscillations across X = 0 (between -0.02 and +0.02)
        var pJitterNeg = WorkshopInput.Position(-0.02f);
        var pJitterPos = WorkshopInput.Position(0.02f);
        var bodiesJitterNeg = new PhysicsBodyReadSet([
            new PhysicsBodyRead(new CanonicalBody(ballId, 0, 0, new(pJitterNeg.Cell, 48, 0), new((float)pJitterNeg.Local, 0f, 0f), default), CanonicalRotation.Identity, default, default),
            new PhysicsBodyRead(new CanonicalBody(detector.Id, 0, 0, detector.Cell, detector.Local, default), CanonicalRotation.Identity, default, default)
        ]);
        var bodiesJitterPos = new PhysicsBodyReadSet([
            new PhysicsBodyRead(new CanonicalBody(ballId, 0, 0, new(pJitterPos.Cell, 48, 0), new((float)pJitterPos.Local, 0f, 0f), default), CanonicalRotation.Identity, default, default),
            new PhysicsBodyRead(new CanonicalBody(detector.Id, 0, 0, detector.Cell, detector.Local, default), CanonicalRotation.Identity, default, default)
        ]);

        var jitterFires = 0;
        ulong tick = 3;
        for (var i = 0; i < 20; i++)
        {
            tracker = tracker.Advance(new SimulationTick(tick++), bodiesJitterNeg, null, passageOutputs, out var cA);
            jitterFires += cA;
            tracker = tracker.Advance(new SimulationTick(tick++), bodiesJitterPos, null, passageOutputs, out var cB);
            jitterFires += cB;
        }
        Assert.Equal(0, jitterFires);

        // 3. Move fully upstream to X = -1.0m (clearing upstream rearm threshold -0.36m)
        var pUpstream = WorkshopInput.Position(-1.0f);
        var bodiesUpstream = new PhysicsBodyReadSet([
            new PhysicsBodyRead(new CanonicalBody(ballId, 0, 0, new(pUpstream.Cell, 48, 0), new((float)pUpstream.Local, 0f, 0f), default), CanonicalRotation.Identity, default, default),
            new PhysicsBodyRead(new CanonicalBody(detector.Id, 0, 0, detector.Cell, detector.Local, default), CanonicalRotation.Identity, default, default)
        ]);
        tracker = tracker.Advance(new SimulationTick(tick++), bodiesUpstream, null, passageOutputs, out var cUp);
        Assert.Equal(0, cUp);
        Assert.Equal(PassageSensorPhase.Armed, tracker.States[0].Phase);

        // 4. Move forward across X = 0 again -> triggers second crossing!
        tracker = tracker.Advance(new SimulationTick(tick++), bodiesPos, null, passageOutputs, out var countSecond);
        Assert.Equal(1, countSecond);
        Assert.Equal(2UL, tracker.States[0].PassedCount);
    }

    [Fact]
    public void PassageSensorTracker_MultipleBodies_TwoSeparatedBalls()
    {
        var detector = WorkshopInput.BallDetector(new GpuBodyId(10), 0, 3, 0, 0, 0, 0, 1);
        var ballA = WorkshopInput.Generic(PartId.Basketball,new GpuBodyId(2), -1, 3, 0, 0, 0, 0, 1);
        var ballB = WorkshopInput.Generic(PartId.BowlingBall, new GpuBodyId(3), -3, 3, 0, 0, 0, 0, 1);

        var construction = new WorkshopConstruction(
            new ConstructionRevision(1),
            WorkshopCadenceSettings.Default(),
            new WorkshopInstances(ballA, ballB, detector));

        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var network = WorkshopActivationCompiler.Compile(construction);

        var pAInit = WorkshopInput.Position(-1.0f);
        var pBInit = WorkshopInput.Position(-3.0f);
        var initialBodies = new PhysicsBodyReadSet([
            new PhysicsBodyRead(new CanonicalBody(new GpuBodyId(2), 0, 0, new(pAInit.Cell, 48, 0), new((float)pAInit.Local, 0f, 0f), default), CanonicalRotation.Identity, default, default),
            new PhysicsBodyRead(new CanonicalBody(new GpuBodyId(3), 0, 0, new(pBInit.Cell, 48, 0), new((float)pBInit.Local, 0f, 0f), default), CanonicalRotation.Identity, default, default),
            new PhysicsBodyRead(new CanonicalBody(detector.Id, 0, 0, detector.Cell, detector.Local, default), CanonicalRotation.Identity, default, default)
        ]);

        var tracker = PassageSensorTracker.Create(network.PassageSensors, scene.Colliders.ToArray(), initialBodies);
        Assert.Equal(2, tracker.States.Count);
        Span<PassageSensorRead> outputs = stackalloc PassageSensorRead[8];

        // Step 1: Ball A crosses forward to +1.0m, Ball B stays at -2.0m
        var pAPos = WorkshopInput.Position(1.0f);
        var pBMid = WorkshopInput.Position(-2.0f);
        var bodies1 = new PhysicsBodyReadSet([
            new PhysicsBodyRead(new CanonicalBody(new GpuBodyId(2), 0, 0, new(pAPos.Cell, 48, 0), new((float)pAPos.Local, 0f, 0f), default), CanonicalRotation.Identity, default, default),
            new PhysicsBodyRead(new CanonicalBody(new GpuBodyId(3), 0, 0, new(pBMid.Cell, 48, 0), new((float)pBMid.Local, 0f, 0f), default), CanonicalRotation.Identity, default, default),
            new PhysicsBodyRead(new CanonicalBody(detector.Id, 0, 0, detector.Cell, detector.Local, default), CanonicalRotation.Identity, default, default)
        ]);

        tracker = tracker.Advance(new SimulationTick(1), bodies1, null, outputs, out var count1);
        Assert.Equal(1, count1);
        Assert.Equal(new GpuBodyId(2), outputs[0].Body);

        // Step 2: Ball A moves further to +2.0m, Ball B crosses forward to +1.0m
        var pAFar = WorkshopInput.Position(2.0f);
        var pBPos = WorkshopInput.Position(1.0f);
        var bodies2 = new PhysicsBodyReadSet([
            new PhysicsBodyRead(new CanonicalBody(new GpuBodyId(2), 0, 0, new(pAFar.Cell, 48, 0), new((float)pAFar.Local, 0f, 0f), default), CanonicalRotation.Identity, default, default),
            new PhysicsBodyRead(new CanonicalBody(new GpuBodyId(3), 0, 0, new(pBPos.Cell, 48, 0), new((float)pBPos.Local, 0f, 0f), default), CanonicalRotation.Identity, default, default),
            new PhysicsBodyRead(new CanonicalBody(detector.Id, 0, 0, detector.Cell, detector.Local, default), CanonicalRotation.Identity, default, default)
        ]);

        tracker = tracker.Advance(new SimulationTick(2), bodies2, null, outputs, out var count2);
        Assert.Equal(1, count2);
        Assert.Equal(new GpuBodyId(3), outputs[0].Body);
    }

    [Fact]
    public void WorkbenchCapacity_AdmitsBallDetectorsWithoutColliderLimit()
    {
        // Story 9R.5b: the compiled scene is the footprint authority; the workbench plane adds one static body and collider.
        var world = WorkshopPhysicsCompiler.Compile(new WorkshopConstruction(new ConstructionRevision(1), WorkshopCadenceSettings.Default(),
            new WorkshopInstances(WorkshopInput.BallDetector(new GpuBodyId(1), 0, 3, 0, 0, 0, 0, 1))), new(1, 2)).World;
        Assert.Equal((1 + 1, 0, 17 + 1, 0, 0), (world.Bodies, world.Dynamic, world.Colliders, world.Triggers, world.ContactWorks));

        // 3 detectors = 51 colliders + 1 plane collider = 52 <= 64 (fits)
        var threeDetectors = new WorkshopInstances(
            WorkshopInput.BallDetector(new GpuBodyId(1), 0, 3, 0, 0, 0, 0, 1),
            WorkshopInput.BallDetector(new GpuBodyId(2), 5, 3, 0, 0, 0, 0, 1),
            WorkshopInput.BallDetector(new GpuBodyId(3), 10, 3, 0, 0, 0, 0, 1));
        WorkbenchCapacity.Validate(threeDetectors);

        // 4 detectors = 68 colliders: validates cleanly with unbounded colliders
        var fourDetectors = new WorkshopInstances(
            WorkshopInput.BallDetector(new GpuBodyId(1), 0, 3, 0, 0, 0, 0, 1),
            WorkshopInput.BallDetector(new GpuBodyId(2), 5, 3, 0, 0, 0, 0, 1),
            WorkshopInput.BallDetector(new GpuBodyId(3), 10, 3, 0, 0, 0, 0, 1),
            WorkshopInput.BallDetector(new GpuBodyId(4), 15, 3, 0, 0, 0, 0, 1));
        WorkbenchCapacity.Validate(fourDetectors);
    }

    [Fact]
    public void SaveCodec_RoundTripsBallDetectorAndPreservesDeclaredColliders()
    {
        var detector = WorkshopInput.BallDetector(new GpuBodyId(10), 0, 3, 0, 0, 0, 0, 1);
        var ball = WorkshopInput.Generic(PartId.Basketball,new GpuBodyId(2), 0, 4, 0, 0, 0, 0, 1);
        var lamp = WorkshopInput.Lamp(new GpuBodyId(20), 5, 3, 0, 0, 0, 0, 1);
        var connection = new WorkshopConnection(
            new GpuBodyId(10), WorkshopSocket.ActivationOut,
            new GpuBodyId(20), WorkshopSocket.ActivationIn,
            WorkshopConnectionDomain.Activation);

        var construction = new WorkshopConstruction(
            new ConstructionRevision(1),
            WorkshopCadenceSettings.Default(),
            new WorkshopInstances(ball, detector, lamp),
            Connections: new WorkshopConnections(connection));
        var save = new WorkshopSavedConstruction(construction, new(100));

        var bytes = WorkshopSaveCodec.Encode(save);
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes);

        Assert.Equal(save, decoded);
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(decoded));

        var decodedDetector = Assert.IsType<WorkshopBallDetector>(decoded.Construction.Instances[1]);
        Assert.Equal(PartId.BallDetector, decodedDetector.PartId);
        Assert.Equal(17, decodedDetector.DeclaredColliders.Count);

        // Verify FlatBuffers schema preserves PartId on saved instance record
        var fbSave = WorkshopSaveCodec.Open(bytes);
        Assert.True(fbSave.Construction.HasValue);
        var fbDetector = fbSave.Construction.Value.Instances(1);
        Assert.True(fbDetector.HasValue);
        Assert.Equal("ball_detector", fbDetector.Value.PartId);
    }
}
