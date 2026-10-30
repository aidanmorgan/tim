using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using Godot;

namespace CuriousContraptions.Tests;

public sealed class WorkshopIntegrationHarnessTests
{

    [Fact]
    public async Task SuccessiveCadenceCommandsPublishNewAuthorityAndDriveTheRealWorld()
    {
        await using var harness = await WorkshopIntegrationHarness.Building(Bench(SimulationCadence.Hz120));
        var authority = harness.Latest.Cadence;
        foreach (var cadence in new[] { SimulationCadence.Hz60, SimulationCadence.Hz240 })
        {
            var settings = harness.Construction.Settings with { Simulation = cadence };
            var response = await harness.Execute(WorkshopCommandKind.ConfigureCadence, settings: settings);
            Assert.Equal(WorkshopCommandOutcome.Applied, response.Result.Outcome);
            Assert.Equal(settings, harness.Construction.Settings);
            Assert.True(response.Cadence.Value > authority.Value);
            Assert.Equal(response.Cadence, harness.Schedule.Revision);
            authority = response.Cadence;
            await harness.Run();
            var before = harness.Time.Nanoseconds;
            await harness.Advance(2 * (int)settings.SimulationRate.Numerator);
            Assert.Equal(2_000_000_000, harness.Time.Nanoseconds - before);
            Assert.True(harness.Committed.Presentation.Actuators[0].Coordinate > 1.3f);
            await harness.Reset();
        }
    }

    [Fact]
    public async Task RetransmissionCannotRewindObservationOrConsumeTheNextCommand()
    {
        await using var harness = await WorkshopIntegrationHarness.Building(Bench(SimulationCadence.Hz120));
        var run = harness.Command(WorkshopCommandKind.Run);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Dispatch(run)).Result.Outcome);
        await harness.Advance(30);
        var pause = harness.Command(WorkshopCommandKind.Pause);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Dispatch(pause)).Result.Outcome);
        var paused = harness.Latest;
        Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Dispatch(run)).Result.Outcome);
        Assert.Equal(paused, harness.Latest);
        var resume = harness.Command(WorkshopCommandKind.Resume);
        Assert.True(resume.Sequence.Value > pause.Sequence.Value);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Dispatch(resume)).Result.Outcome);
        await harness.Advance(1);
        Assert.Equal(paused.Read.Tick.Value + 1, harness.Committed.Tick.Value);
        Assert.Equal(WorkshopSimulationPhase.Running, harness.Latest.Phase);
    }

    [Fact]
    public async Task UndefinedWireKindAndRevisionEnumNeverReachTheAdmittedWorld()
    {
        await using var harness = await WorkshopIntegrationHarness.Building(Bench(SimulationCadence.Hz120));
        var before = harness.Committed;
        var command = harness.Command(WorkshopCommandKind.Run);
        byte[] Invalid(uint kind, uint revision)
        {
            var builder = new Google.FlatBuffers.FlatBufferBuilder(128);
            var session = new byte[RuntimeSessionId.ByteLength]; command.Session.WriteTo(session);
            var vector = CuriousContraptions.Wire.WorkshopCommand.CreateSessionVectorBlock(builder, session);
            CuriousContraptions.Wire.WorkshopCommand.StartWorkshopCommand(builder);
            CuriousContraptions.Wire.WorkshopCommand.AddSequence(builder, command.Sequence.Value);
            CuriousContraptions.Wire.WorkshopCommand.AddKind(builder, (CuriousContraptions.Wire.CommandKind)kind);
            CuriousContraptions.Wire.WorkshopCommand.AddEpoch(builder, command.Epoch.Value);
            CuriousContraptions.Wire.WorkshopCommand.AddRevisionKind(builder, revision);
            CuriousContraptions.Wire.WorkshopCommand.AddRevision(builder, command.Revision.Value);
            CuriousContraptions.Wire.WorkshopCommand.AddSession(builder, vector);
            CuriousContraptions.Wire.WorkshopCommand.AddCadence(builder, command.Cadence.Value);
            CuriousContraptions.Wire.WorkshopCommand.AddProjection(builder, command.Projection.Value);
            builder.FinishSizePrefixed(CuriousContraptions.Wire.WorkshopCommand.EndWorkshopCommand(builder).Value,
                WorkshopWireFlatBuffers.CommandIdentifier);
            return builder.SizedByteArray();
        }
        Assert.Equal(WireFaultReason.UnknownCommand, Assert.Throws<WireFaultException>(() =>
            harness.Router.Dispatch(Invalid(99, (uint)ExpectedRevisionKind.Exact))).Reason);
        Assert.Throws<ArgumentException>(() => harness.Router.Dispatch(Invalid((uint)WorkshopCommandKind.Run, 99)));
        Assert.Equal(before.Revision, harness.Committed.Revision);
        Assert.Equal(before.Epoch, harness.Committed.Epoch);
        Assert.Equal(WorkshopSimulationPhase.Building, harness.Simulation.Phase);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Dispatch(command)).Result.Outcome);
    }
    private static WorkshopConstruction Bench(SimulationCadence cadence, bool wired = true)
    {
        var settings = WorkshopCadenceSettings.Default() with { Simulation = cadence };
        var gate = WorkshopInput.Generic(new PartId("powered_gate"), new(1), 0, 3, 0, 0, 0, 0, 1);
        var battery = PlacedParts.Battery(new(2), 3, 3, 0);
        return new(new(2), settings, new(gate, battery), Connections: wired
            ? new(new WorkshopConnection(battery.Id, WorkshopSocket.Supply, gate.Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)) : new());
    }

    [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz120)]
    [InlineData(SimulationCadence.Hz240)]
    public async Task WiredGateOpensWhileControlStaysClosedAndLifecycleRestoresAuthoredState(SimulationCadence cadence)
    {
        var construction = Bench(cadence);
        var ticks = (int)construction.Settings.SimulationRate.Numerator;
        await using var harness = await WorkshopIntegrationHarness.Building(construction);
        var authored = harness.Committed;
        await harness.Run();
        await harness.Advance(ticks * 2);
        Assert.True(harness.Committed.Presentation.Actuators[0].Coordinate > 1.3f, "The supplied blade clears the opening.");
        Assert.NotEmpty(harness.Presentation!.Advance(harness.Schedule, new(960)));
        Assert.Equal(2_000_000_000, harness.Time.Nanoseconds);
        var retained = harness.Committed;
        var retainedHeight = retained.Bodies[0].Body.Local.Y;
        await harness.Require(WorkshopCommandKind.Pause);
        Assert.Equal(WorkshopRejection.WrongPhase, (await harness.AdvanceOne()).Reason);
        Assert.Equal(retained.Tick, harness.Committed.Tick);
        var masterBeforeStep = harness.Time.Nanoseconds;
        await harness.Require(WorkshopCommandKind.Step);
        Assert.Equal(retained.Tick.Value + 1, harness.Committed.Tick.Value);
        Assert.Equal(masterBeforeStep, harness.Time.Nanoseconds);
        await harness.Require(WorkshopCommandKind.Resume);
        await harness.Advance(2);
        Assert.Equal(retainedHeight, retained.Bodies[0].Body.Local.Y);
        Assert.Equal(WorkshopRejection.WrongPhase, (await harness.Execute(WorkshopCommandKind.Save)).Result.Reason);
        await harness.Reset();
        await harness.Require(WorkshopCommandKind.Save);
        var saved = WorkshopSaveCodec.Encode(new(harness.Construction, new(3)));
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, saved).Construction;
        Assert.Equal(construction.Instances, harness.Construction.Instances);
        Assert.Equal(construction.Connections, harness.Construction.Connections);
        Assert.Equal(0UL, harness.Committed.Tick.Value);
        Assert.Equal(authored.Bodies[0].Body.Local, harness.Committed.Bodies[0].Body.Local);
        await using var replay = await WorkshopIntegrationHarness.Running(loaded);
        await replay.Advance(2 * ticks);
        Assert.True(replay.Committed.Presentation.Actuators[0].Coordinate > 1.3f);
        await using var control = await WorkshopIntegrationHarness.Running(Bench(cadence, false));
        await control.Advance(2 * ticks);
        Assert.True(control.Committed.Presentation.Actuators[0].Coordinate < .2f, "No wire leaves the opening closed.");
    }

    [Fact]
    public async Task StaleAuthorityRefusesWithoutChangingCommittedStateAndMemoryRequiresQuietStoppedWorld()
    {
        await using var harness = await WorkshopIntegrationHarness.Building(Bench(SimulationCadence.Hz120));
        foreach (var changed in new[] { 0, 1, 2, 3 })
        {
            var command = harness.Command(WorkshopCommandKind.Run);
            command = changed switch
            {
                0 => command with { Epoch = new(command.Epoch.Value + 1) },
                1 => command with { Revision = new(command.Revision.Value + 1) },
                2 => command with { Cadence = new(command.Cadence.Value + 1) },
                _ => command with { Projection = new(command.Projection.Value + 1) }
            };
            var before = harness.Committed;
            Assert.Equal(WorkshopRejection.StaleGeneration, (await harness.Dispatch(command)).Result.Reason);
            Assert.Equal(before.Revision, harness.Committed.Revision);
            Assert.Equal(before.Tick, harness.Committed.Tick);
            Assert.Equal(WorkshopSimulationPhase.Building, harness.Latest.Phase);
        }
        var beforeInvalid = harness.Committed;
        var invalid = harness.Construction with
        {
            Revision = new(harness.Construction.Revision.Value + 1),
            Connections = new(new WorkshopConnection(new(2), WorkshopSocket.Supply, new(999), WorkshopSocket.PowerIn,
                WorkshopConnectionDomain.Electrical))
        };
        var refusal = await Assert.ThrowsAsync<WorkshopEditRejectedException>(() => harness.Execute(WorkshopCommandKind.Construct, invalid));
        Assert.Equal(WorkshopEditRejection.InvalidConnection, refusal.Reason);
        Assert.Equal(beforeInvalid.Revision, harness.Committed.Revision);
        Assert.Equal(beforeInvalid.Epoch, harness.Committed.Epoch);
        var measured = await harness.Execute(WorkshopCommandKind.ObserveMemory);
        Assert.Equal(WorkshopCommandOutcome.Applied, measured.Result.Outcome);
        Assert.NotNull(measured.Memory);
        measured.Memory.Value.Validate();
        harness.ReadsIdle = false;
        Assert.Equal(WorkshopRejection.Busy, (await harness.Execute(WorkshopCommandKind.ObserveMemory)).Result.Reason);
        harness.ReadsIdle = true;
        await harness.Run();
        Assert.Equal(WorkshopRejection.WrongPhase, (await harness.Execute(WorkshopCommandKind.ObserveMemory)).Result.Reason);
    }

    [Fact]
    public async Task HeldInstallationOwnsBusyCancelAndDisposeWithoutWallClockWaiting()
    {
        await using var harness = await WorkshopIntegrationHarness.Building(Bench(SimulationCadence.Hz120));
        var original = harness.Construction;
        harness.HoldInstallation();
        var candidate = harness.Construction with { Revision = new(harness.Construction.Revision.Value + 1) };
        var running = harness.Execute(WorkshopCommandKind.Construct, candidate);
        Assert.False(running.IsCompleted);
        var target = harness.Executor.Pending!.Value;
        Assert.Equal(WorkshopRejection.Busy, (await harness.Execute(WorkshopCommandKind.Save)).Result.Reason);
        Assert.Equal(WorkshopRejection.AlreadyCommitted, (await harness.Execute(WorkshopCommandKind.Cancel,
            target: target with { Sequence = new(target.Sequence.Value + 1) })).Result.Reason);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await harness.Execute(WorkshopCommandKind.Cancel, target: target)).Result.Outcome);
        Assert.Equal(new WorkshopCommandResult(WorkshopCommandOutcome.Rejected, WorkshopRejection.Transport), (await running).Result);
        Assert.Equal(WorkshopSimulationPhase.Building, harness.Simulation.Phase);
        Assert.Equal(original, harness.Construction);
        harness.HoldInstallation();
        var completed = harness.Execute(WorkshopCommandKind.Construct, candidate);
        Assert.False(completed.IsCompleted);
        harness.CompleteInstallation();
        Assert.Equal(WorkshopCommandOutcome.Applied, (await completed).Result.Outcome);
        candidate = candidate with { Revision = new(candidate.Revision.Value + 1) };
        harness.HoldInstallation();
        var second = harness.Execute(WorkshopCommandKind.Construct, candidate);
        await harness.DisposeAsync();
        Assert.Equal(new WorkshopCommandResult(WorkshopCommandOutcome.Rejected, WorkshopRejection.Transport), (await second).Result);
        Assert.Equal(WorkshopSimulationPhase.Disposed, harness.Simulation.Phase);
    }

    [Fact]
    public async Task RealImpactDeliverySurvivesPauseAndResetRetiresItsWorld()
    {
        var bench = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(
            WorkshopInput.Generic(new PartId("ball"), new(1), 0, 4.5f, 0, 0, 0, 0, 1),
            PlacedParts.Bumper(new(2), 0, 3, 0)));
        await using var harness = await WorkshopIntegrationHarness.Running(bench);
        for (var tick = 0; tick < 240 && harness.Records.Count == 0; tick++) await harness.Advance(1);
        var impact = Assert.Single(harness.Records, record => record.Kind == SimulationEventKind.Impact);
        Assert.Equal(2UL, impact.Source);
        Assert.Equal(1UL, impact.Other);
        float HeadScale() => Assert.Single(harness.Presentation!.Advance(harness.Schedule,
            PhysicalTime.FromNanoseconds(harness.Time.Nanoseconds)), sample => sample.Owner == new GpuBodyId(2) && sample.Binding == 0).Value.Scale.Value;
        // The declared 0.32-second pulse compresses from 1 to 0.88. At 0.1 seconds it must visibly differ from rest;
        // a 3% scale change is deliberately much broader than a numerical curve oracle.
        await harness.AdvanceUndrained(12);
        Assert.True(HeadScale() < .97f, "The real paid impact visibly compresses the bumper head.");
        var presentation = harness.Presentation;
        var epoch = harness.Committed.Epoch;
        await harness.Require(WorkshopCommandKind.Pause);
        Assert.Same(presentation, harness.Presentation);
        for (var tick = 0; tick < 120; tick++) harness.Time.Tick(bench.Settings);
        Assert.True(HeadScale() < .97f, "Paused master time must not retire the active world-time cue.");
        await harness.Require(WorkshopCommandKind.Step);
        Assert.Same(presentation, harness.Presentation);
        Assert.True(HeadScale() < .97f, "Step retains the active impact cue.");
        await harness.Require(WorkshopCommandKind.Resume);
        Assert.Same(presentation, harness.Presentation);
        Assert.True(HeadScale() < .97f, "Resume continues the cue instead of restarting it from rest.");
        await harness.Reset();
        Assert.NotSame(presentation, harness.Presentation);
        Assert.Equal(1f, HeadScale());
        Assert.True(harness.Committed.Epoch.Value > epoch.Value);
        Assert.Throws<ArgumentException>(() => harness.Presentation!.AcceptEvents(new(epoch.Value, 0, [impact]), harness.Schedule.World));
        Assert.Equal(0UL, harness.Shared!.LoadWriteSequence());
    }
}

[Collection<NativeSceneCollectionDefinition>]
public sealed class WorkshopIntegrationStartupTests(NativeSceneFixture godot)
{
    private static IEnumerable<Node> Descendants(Node node)
    {
        foreach (var child in node.GetChildren()) { yield return child; foreach (var descendant in Descendants(child)) yield return descendant; }
    }
    private static bool HasLabel(Workshop scene, string text) => Descendants(scene).OfType<Label>().Any(label => label.Text == text);
    private static Workshop Scene() => GD.Load<PackedScene>(NativeTestBoundary.WorkshopResource).Instantiate<Workshop>();

    [Fact]
    public async Task DeclaredIncompleteRopeReadsProduceDashedArtworkAndCompleteReadsRemainSolid()
    {
        var scene = Scene();
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        scene.ClientFactory = () => Task.FromResult<IWorkshopClient>(new WorkshopIntegrationClient(harness));
        godot.Tree.Root.AddChild(scene);
        try
        {
            await scene.Startup;
            var a = WorkshopInput.Generic(new PartId("rope_anchor"), new(1), 0, 6, -.18f, 0, 0, 0, 1);
            var b = WorkshopInput.Generic(new PartId("rope_anchor"), new(2), 0, 3, -.18f, 0, 0, 0, 1);
            var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(a, b),
                Connections: new(WorkshopConnections.MeasureRope(new(a, b), a.Id, b.Id)));
            await scene.World.ReplaceConstruction(construction);
            var compiled = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
            var span = Assert.Single(compiled.TensileSpans.ToArray());
            foreach (var complete in new[] { true, false, true })
            {
                // The supported generic declaration boundary supplies open/complete rows without inventing a Guide package.
                var declaration = new PhysicsSceneDeclaration(compiled.Document, compiled.NextIdentity, compiled.Bodies,
                    compiled.Colliders, compiled.Materials, [], [], tensileSpans: [span with { Complete = complete }]);
                var source = PhysicsGpuAbi.Admission(declaration, new(1),
                    new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
                var candidate = new byte[source.Length];
                var inputs = new PhysicsTickInputs([], []);
                var engine = new CuriousContraptions.Simulation.WorkshopPhysicsEngine();
                var interactions = engine.Advance(source, candidate, inputs, PhysicsInteractionSnapshot.Empty);
                _ = PhysicsGpuAbi.ValidateCandidate(candidate, source, new(1), interactions, PhysicsInteractionSnapshot.Empty);
                var read = PhysicsGpuAbi.ReadPresentation(candidate, inputs, false);
                var rope = Assert.Single(read.Ropes.ToArray());
                Assert.Equal(complete ? RopeState.Taut : RopeState.Open, rope.State);
                scene.RefreshConnectionArtwork(read.Ropes);
                var artwork = scene.GetNode<Node3D>("WorkshopConnections");
                var meshes = artwork.GetChildren().OfType<MeshInstance3D>().ToArray();
                var strands = meshes.Where(node => node.Mesh is CylinderMesh).ToArray();
                var knots = meshes.Where(node => node.Mesh is SphereMesh).ToArray();
                Assert.NotEmpty(strands); Assert.Equal(2, knots.Length);
                Assert.All(knots, knot => Assert.True(knot.Visible));
                Assert.InRange(knots[0].Position.DistanceTo(new(rope.AttachmentA.X, rope.AttachmentA.Y, rope.AttachmentA.Z)), 0f, .01f);
                Assert.InRange(knots[1].Position.DistanceTo(new(rope.AttachmentB.X, rope.AttachmentB.Y, rope.AttachmentB.Z)), 0f, .01f);
                if (complete) Assert.All(strands, strand => Assert.True(strand.Visible));
                else
                {
                    Assert.Contains(strands, strand => strand.Visible);
                    Assert.Contains(strands, strand => !strand.Visible);
                    for (var i = 1; i < strands.Length; i++)
                        Assert.NotEqual(strands[i - 1].Visible, strands[i].Visible);
                }
            }
        }
        finally { scene.Free(); godot.Engine.Iteration(); }
        Assert.Empty(godot.Errors.Drain());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RefreshConnectionArtworkSuppressesKnotsOnPulleyGuideSocketsInBothDirections(bool reversed)
    {
        var scene = Scene();
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        scene.ClientFactory = () => Task.FromResult<IWorkshopClient>(new WorkshopIntegrationClient(harness));
        godot.Tree.Root.AddChild(scene);
        try
        {
            await scene.Startup;
            var a = WorkshopInput.Generic(new PartId("rope_anchor"), new(1), 0, 6, -.18f, 0, 0, 0, 1);
            var pulley = WorkshopInput.Generic(new PartId("pulley"), new(2), 2, 6, 0f, 0, 0, 0, 1);
            var b = WorkshopInput.Generic(new PartId("rope_anchor"), new(3), 4, 6, -.18f, 0, 0, 0, 1);
            var instances = new WorkshopInstances(a, pulley, b);
            var conn1 = reversed
                ? WorkshopConnections.MeasureRope(instances, pulley.Id, a.Id)
                : WorkshopConnections.MeasureRope(instances, a.Id, pulley.Id);
            var conn2 = reversed
                ? WorkshopConnections.MeasureRope(instances, b.Id, pulley.Id)
                : WorkshopConnections.MeasureRope(instances, pulley.Id, b.Id);
            var construction = new WorkshopConstruction(new(3), WorkshopCadenceSettings.Default(), instances,
                Connections: new([conn1, conn2]));
            await scene.World.ReplaceConstruction(construction);
            var compiled = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
            var declaration = new PhysicsSceneDeclaration(compiled.Document, compiled.NextIdentity, compiled.Bodies,
                compiled.Colliders, compiled.Materials, [], [], tensileSpans: compiled.TensileSpans.ToArray());
            var source = PhysicsGpuAbi.Admission(declaration, new(1),
                new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
            var candidate = new byte[source.Length];
            var inputs = new PhysicsTickInputs([], []);
            var engine = new CuriousContraptions.Simulation.WorkshopPhysicsEngine();
            var interactions = engine.Advance(source, candidate, inputs, PhysicsInteractionSnapshot.Empty);
            _ = PhysicsGpuAbi.ValidateCandidate(candidate, source, new(1), interactions, PhysicsInteractionSnapshot.Empty);
            var read = PhysicsGpuAbi.ReadPresentation(candidate, inputs, false);
            Assert.Equal(2, read.Ropes.Length);
            scene.RefreshConnectionArtwork(read.Ropes);
            var artwork = scene.GetNode<Node3D>("WorkshopConnections");
            var knots = artwork.GetChildren().OfType<MeshInstance3D>().Where(node => node.Mesh is SphereMesh).ToArray();
            var visibleKnots = knots.Where(k => k.Visible).ToArray();
            Assert.Equal(2, visibleKnots.Length);
            var pulleySocket = new Godot.Vector3(2f, 6.4f, 0.12f);
            Assert.DoesNotContain(visibleKnots, knot => knot.Position.DistanceTo(pulleySocket) < 0.1f);
        }
        finally { scene.Free(); godot.Engine.Iteration(); }
        Assert.Empty(godot.Errors.Drain());
    }

    [Fact]
    public async Task RopePresenterUsesCommittedKnotsAndCutStateAndRetainsNodesUntilTeardown()
    {
        var scene = Scene();
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        scene.ClientFactory = () => Task.FromResult<IWorkshopClient>(new WorkshopIntegrationClient(harness));
        godot.Tree.Root.AddChild(scene);
        MeshInstance3D[] retained = [];
        try
        {
            await scene.Startup;
            var anchor = WorkshopInput.Generic(new PartId("rope_anchor"), new(1), 0, 6, -.18f, 0, 0, 0, 1);
            var weight = WeightTests.Placed(2, 1, 2, 0, 4);
            await scene.World.ReplaceConstruction(new(new(2), WorkshopCadenceSettings.Default(), new(anchor, weight)));
            await scene.ChangeConnectionAsync(new(anchor.Id, WorkshopSocket.Tie, weight.Id, WorkshopSocket.Tie, WorkshopConnectionDomain.Rope), true);
            var link = Assert.Single(scene.World.Construction.Connections);
            void Frame() { scene.World._Process(0); scene._Process(0); }
            Frame();
            var artwork = scene.GetNode<Node3D>("WorkshopConnections");
            retained = artwork.GetChildren().OfType<MeshInstance3D>().ToArray();
            var knots = retained.Where(node => node.Mesh is SphereMesh).ToArray();
            var segments = retained.Where(node => node.Mesh is CylinderMesh).ToArray();
            Assert.Equal(2, knots.Length); Assert.NotEmpty(segments);
            // Rendered cylinders must form one connected path between the actual knots, within a 1cm artwork allowance.
            var previous = knots[0].Position;
            var pathLength = 0f;
            foreach (var node in segments)
            {
                var cylinder = (CylinderMesh)node.Mesh;
                var half = node.Quaternion * (Vector3.Up * (cylinder.Height * .5f));
                var start = node.Position - half; var end = node.Position + half;
                Assert.InRange(start.DistanceTo(previous), 0f, .01f);
                pathLength += start.DistanceTo(end); previous = end;
            }
            Assert.InRange(previous.DistanceTo(knots[1].Position), 0f, .01f);
            Assert.InRange(MathF.Abs(pathLength - link.RopeLength!.Value), 0f, .01f);
            Assert.All(knots, node => Assert.Equal(new Color("#f7cb52"), ((StandardMaterial3D)node.MaterialOverride).AlbedoColor));
            Assert.All(retained, node => Assert.True(node.Visible));
            Assert.InRange(knots[0].Position.DistanceTo(new(0, 6, 0)), 0f, .001f);
            Assert.InRange(knots[1].Position.DistanceTo(new(1, 2.57710344f, 0)), 0f, .001f);
            var authoredKnots = knots.Select(node => node.Position).ToArray();
            using (var run = new InputEventKey { Keycode = Key.Space, Pressed = true }) scene._UnhandledInput(run);
            Assert.Equal(WorkshopSimulationPhase.Running, scene.World.WorkshopPhase);
            await harness.Advance(60); Frame();
            var committed = Assert.Single(scene.World.WorkshopRead.Presentation.Ropes.ToArray());
            var expected = new[] { committed.AttachmentA, committed.AttachmentB };
            for (var i = 0; i < knots.Length; i++)
                Assert.InRange(knots[i].Position.DistanceTo(new(expected[i].X, expected[i].Y, expected[i].Z)), 0f, .001f);
            Assert.True(knots[1].Position.DistanceTo(authoredKnots[1]) > .05f);
            await scene.World.PlaybackWorkshop(WorkshopCommandKind.Pause);
            var part = scene.World.Parts.Single(item => item.AuthoredId == anchor.Id);
            var camera = Assert.Single(Descendants(scene).OfType<Camera3D>());
            using (var click = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true,
                Position = camera.UnprojectPosition(part.GlobalPosition) }) scene._UnhandledInput(click);
            Assert.Single(Descendants(scene).OfType<Button>(), button => button.Text == "Cut rope");
            await scene.ChangeConnectionAsync(link, false); Frame();
            Assert.Single(Descendants(scene).OfType<Button>(), button => button.Text == "Cut queued" && button.Disabled);
            Assert.Equal(RopeIntegrity.Intact, Assert.Single(scene.World.WorkshopRead.Presentation.Ropes.ToArray()).Integrity);
            Assert.All(retained, node => Assert.True(node.Visible));
            await scene.World.PlaybackWorkshop(WorkshopCommandKind.Step); Frame();
            Assert.Equal(RopeIntegrity.Cut, Assert.Single(scene.World.WorkshopRead.Presentation.Ropes.ToArray()).Integrity);
            Assert.Single(Descendants(scene).OfType<Button>(), button => button.Text == "Rope cut" && button.Disabled);
            Assert.All(retained, node => Assert.False(node.Visible));
            await scene.World.ResetWorkshop(); Frame();
            Assert.All(retained, node => Assert.True(node.Visible));
            for (var i = 0; i < knots.Length; i++) Assert.Equal(authoredKnots[i], knots[i].Position);
            for (var i = 0; i < 5; i++) Frame();
            Assert.Equal(retained.Select(node => node.GetInstanceId()), artwork.GetChildren().OfType<MeshInstance3D>().Select(node => node.GetInstanceId()));

            var installed = scene.World.WorkshopRead;
            scene.RefreshConnectionArtwork([]);
            Assert.All(retained, node => Assert.False(node.Visible));
            var fault = Assert.Single(scene.Diagnostics.Snapshot().OfType<WorkshopFaultDiagnostic>());
            Assert.Equal(WorkshopFaultKind.Fault, fault.Kind);
            Assert.Contains("WireFaultException", fault.ExceptionType, StringComparison.Ordinal);
            Assert.Contains(Descendants(scene).OfType<Label>(), label => label.Text.Contains("Missing committed rope observation", StringComparison.Ordinal));
            Assert.Equal(installed, scene.World.WorkshopRead);
            scene.RefreshConnectionArtwork(installed.Presentation.Ropes);
            Assert.All(retained, node => Assert.True(node.Visible));
        }
        finally { scene.Free(); godot.Engine.Iteration(); }
        Assert.All(retained, node => Assert.False(GodotObject.IsInstanceValid(node)));
        Assert.Empty(godot.Errors.Drain());
    }

    [Fact]
    public async Task RopeAuthoringMeasuresSocketsAndRefusesBeforeUndoThenReconnectsAtChangedMass()
    {
        var scene = Scene();
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        scene.ClientFactory = () => Task.FromResult<IWorkshopClient>(new WorkshopIntegrationClient(harness));
        godot.Tree.Root.AddChild(scene);
        try
        {
            await scene.Startup;
            var anchor = WorkshopInput.Generic(new PartId("rope_anchor"), new(1), 0, 6, -.18f, 0, 0, 0, 1);
            var weight = WeightTests.Placed(2, 0, 2, 0, 4);
            await scene.World.ReplaceConstruction(new(new(2), WorkshopCadenceSettings.Default(), new(anchor, weight)));
            var request = new WorkshopConnection(anchor.Id, WorkshopSocket.Tie, weight.Id, WorkshopSocket.Tie, WorkshopConnectionDomain.Rope);
            await scene.ChangeConnectionAsync(request, true);
            var connected = Assert.Single(scene.World.Construction.Connections);
            // Anchor eye is y=6; the four-kilogram load eye is y=2+0.57710344.
            Assert.InRange(connected.RopeLength!.Value, 3.422f, 3.424f);
            var revision = scene.World.Construction.Revision;
            await scene.ChangeConnectionAsync(request with { Target = anchor.Id }, true);
            Assert.Equal(revision, scene.World.Construction.Revision);
            Assert.Equal(connected, Assert.Single(scene.World.Construction.Connections));
            await scene.UndoAsync();
            Assert.Empty(scene.World.Construction.Connections); // Refusal must not insert an Undo entry.
            await scene.ChangeConnectionAsync(request, true);
            await scene.ApplyDeclaredParametersAsync(weight.Id, new([new(new("mass"), 8f)]));
            Assert.Equal(connected.RopeLength, Assert.Single(scene.World.Construction.Connections).RopeLength);
            await scene.ChangeConnectionAsync(connected, false);
            await scene.ChangeConnectionAsync(request, true);
            var reconnected = Assert.Single(scene.World.Construction.Connections);
            Assert.True(reconnected.RopeLength < connected.RopeLength - .3f);
            var saved = WorkshopSaveCodec.Encode(new(scene.World.Construction, new(3)));
            var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, saved);
            Assert.Equal(reconnected, Assert.Single(loaded.Construction.Connections));
            Assert.Equal(WorkshopCommandOutcome.Applied, (await scene.World.RunWorkshop()).Result.Outcome);
            await harness.Advance(120);
            Assert.Single(harness.Committed.Presentation.Ropes.ToArray());
            Assert.Equal(WorkshopCommandOutcome.Applied, (await scene.World.ResetWorkshop()).Result.Outcome);
            Assert.Equal(reconnected, Assert.Single(scene.World.Construction.Connections));
        }
        finally { scene.Free(); godot.Engine.Iteration(); }
    }


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DelayedObservationStartupKeepsFramesSafeAndStopsAfterFailureOrRemoval(bool remove)
    {
        var scene = Scene();
        var pending = new TaskCompletionSource();
        var created = false;
        scene.StartObservation = _ => pending.Task;
        scene.ClientFactory = () => { created = true; throw new InvalidOperationException("Client must not start."); };
        godot.Tree.Root.AddChild(scene);
        var observation = scene.Observation!;
        try
        {
            Assert.False(scene.Startup.IsCompleted);
            scene._Process(0); scene._PhysicsProcess(0);
            Assert.Empty(scene.World.Registry.Definitions);
            if (remove) { godot.Tree.Root.RemoveChild(scene); pending.SetResult(); }
            else pending.SetException(new InvalidOperationException("external module import failed"));
            await scene.Startup;
            Assert.False(created);
            Assert.NotNull(observation.Terminal);
            Assert.Contains(remove ? "scene removed" : "external module import failed", observation.Terminal.Value.Detail, StringComparison.Ordinal);
            if (!remove) Assert.True(HasLabel(scene, "OBSERVATION UNAVAILABLE"));
        }
        finally { scene.Free(); godot.Engine.Iteration(); }
    }

    [Fact]
    public async Task NativePaletteGroupsDeclaredCategoriesAndPreservesTheirDisplayOrder()
    {
        var scene = Scene();
        var discovery = new TaskCompletionSource();
        scene.StartObservation = _ => discovery.Task;
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        scene.ClientFactory = () => Task.FromResult<IWorkshopClient>(new WorkshopIntegrationClient(harness));
        godot.Tree.Root.AddChild(scene);
        try
        {
            Assert.False(scene.Startup.IsCompleted);
            scene._Process(0); scene._PhysicsProcess(0);
            discovery.SetResult();
            await scene.Startup;
            Assert.Null(scene.Observation!.Terminal);
            var buttons = Descendants(scene).OfType<Button>().Where(button => button.HasMeta("part_kind")).ToArray();
            var first = Assert.Single(buttons, button => button.GetMeta("part_kind").AsString() == "ball");
            var children = first.GetParent().GetChildren();
            var rows = children.Select(node => node is Label label ? label.Text :
                node is Button button ? button.GetMeta("part_kind").AsString() : throw new InvalidOperationException("Unexpected palette row.")).ToArray();
            string[] expected =
            [
                "Motion", "ball", "bumper", "domino", "bowling", "spring", "pipe", "pipe_bend_45",
                "pipe_bend_90", "funnel", "powered_gate", "weight", "rope_anchor", "pulley",
                "Goals", "basket", "lamp", "Power", "switch", "battery",
                "Control", "delay", "ball_detector", "pressure_plate", "counter", "clock", "latch",
                "hold_timer", "both_gate", "electrical_nand", "electrical_nor", "electrical_or", "electrical_xor",
                "Structure", "wall", "ramp"
            ];
            Assert.Equal(expected, rows);
            var impact = scene.World.Registry.Definitions[new PartId("switch")];
            Assert.Equal("Impact switch", impact.Title);
            Assert.Equal("A qualifying impact sends one activation and closes the independently supplied electrical contact.", impact.Description);
            Assert.Equal(new Color(.94f, .43f, .33f, 1), impact.Color);
            Assert.Equal("res://elements/ImpactSwitch/icon.svg", impact.IconPath);
            var control = Assert.Single(buttons, button => button.GetMeta("part_kind").AsString() == "switch");
            Assert.Contains(Descendants(control).OfType<Label>(), label => label.Text == "Impact switch");
            Assert.Same(WorkshopIcons.Element("res://elements/ImpactSwitch/icon.svg"), control.Icon);
        }
        finally { scene.Free(); godot.Engine.Iteration(); }
    }

    [Theory]
    [InlineData("res://elements/missing-integration-bundle.json")]
    [InlineData("res://project.godot")]
    public async Task RealStartupRefusesMissingOrInvalidResourceBeforeClientCreation(string path)
    {
        var scene = Scene();
        scene.BundleResourcePath = path;
        var created = false;
        scene.ClientFactory = () => { created = true; throw new InvalidOperationException("Failure must precede client startup."); };
        godot.Tree.Root.AddChild(scene);
        try
        {
            await scene.Startup;
            Assert.False(created);
            Assert.Equal(WorkshopSimulationPhase.Uninitialized, scene.World.WorkshopPhase);
            Assert.Empty(scene.World.Registry.Definitions);
            Assert.True(HasLabel(scene, "PARTS UNAVAILABLE"));
            var observation = Assert.IsAssignableFrom<IWorkshopObservation>(scene.Observation);
            Assert.NotNull(observation.Terminal);
            Assert.Contains("System.IO.InvalidDataException", observation.Terminal.Value.Detail, StringComparison.Ordinal);
            Assert.Equal(WorkshopObservationRefusal.Retired, observation.Query(observation.Session, WorkshopObservationQuery.Committed).Refusal);
        }
        finally { scene.Free(); godot.Engine.Iteration(); }
    }

    [Theory]
    [InlineData(SimulationCadence.Hz60)]
    [InlineData(SimulationCadence.Hz120)]
    [InlineData(SimulationCadence.Hz240)]
    public async Task RealStartupAndEditorPlaceConfigureWireUndoAndCancelThroughProductionCommands(SimulationCadence cadence)
    {
        var scene = Scene();
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default() with { Simulation = cadence });
        scene.ClientFactory = () => Task.FromResult<IWorkshopClient>(new WorkshopIntegrationClient(harness));
        godot.Tree.Root.AddChild(scene);
        try
        {
            await scene.Startup;
            Assert.NotNull(harness);
            Assert.Equal(WorkshopSimulationPhase.Building, scene.World.WorkshopPhase);
            Assert.NotEmpty(scene.World.Registry.Definitions);
            async Task Place(PartId part, Vector3 at)
            {
                var composed = PlacementPose.Compose(scene.World.Registry.Definitions, scene.World.Construction.Instances,
                    new(part, default, false), at, Quaternion.Identity);
                await scene.PlaceAsync(part, composed);
            }
            await Place(new PartId("powered_gate"), new(0, 3, 0));
            await Place(new PartId("battery"), new(3, 3, 0));
            var instances = scene.World.Construction.Instances;
            Assert.Equal(2, instances.Count);
            var source = Assert.IsType<WorkshopGenericInstance>(instances[1]);
            var changed = source.Parameters.ToDictionary(); changed[new("enabled")] = 0;
            await scene.ApplyDeclaredParametersAsync(source.Id, new(changed));
            Assert.Equal(0, Assert.IsType<WorkshopGenericInstance>(scene.World.Construction.Instances[1]).Parameters[new("enabled")]);
            await scene.UndoAsync();
            Assert.Equal(1, Assert.IsType<WorkshopGenericInstance>(scene.World.Construction.Instances[1]).Parameters[new("enabled")]);
            var link = new WorkshopConnection(source.Id, WorkshopSocket.Supply, instances[0].Id, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
            await scene.ChangeConnectionAsync(link, true);
            Assert.Single(scene.World.Construction.Connections);
            await scene.ChangeConnectionAsync(link, false);
            Assert.Empty(scene.World.Construction.Connections);
            await scene.UndoAsync();
            Assert.Single(scene.World.Construction.Connections);
            var before = scene.World.Construction;
            await scene.PlaceAsync(new PartId("ball"), new(new(8, 2, 0), Quaternion.Identity, null, PlacementRefusal.OutsideBench));
            Assert.Equal(before.Revision, scene.World.Construction.Revision);
            Assert.Equal(before.Instances.Count, scene.World.Construction.Instances.Count);
            scene.CancelTool();
            Assert.True(HasLabel(scene, "Choose a part, or click a placed part to adjust it."));
            await scene.UndoAsync();
            Assert.Empty(scene.World.Construction.Connections);
            await Place(new PartId("clock"), new(-3, 3, 0));
            var clock = Assert.IsType<WorkshopGenericInstance>(scene.World.Construction.Instances[2]);
            Assert.Equal(new GpuBodyId(3), clock.Id);
            await scene.ApplyDeclaredParametersAsync(clock.Id, new([new(new("interval_seconds"), -1)]));
            Assert.Equal(.1f, Assert.IsType<WorkshopGenericInstance>(scene.World.Construction.Instances[2]).Parameters[new("interval_seconds")]);
            await scene.ApplyDeclaredParametersAsync(clock.Id, new([new(new("interval_seconds"), 13)]));
            Assert.Equal(12f, Assert.IsType<WorkshopGenericInstance>(scene.World.Construction.Instances[2]).Parameters[new("interval_seconds")]);
            var valid = scene.World.Construction;
            await scene.ApplyDeclaredParametersAsync(clock.Id, new([new(new("interval_seconds"), float.NaN)]));
            await scene.ChangeConnectionAsync(link with { Target = new(999) }, true);
            Assert.True(HasLabel(scene, "Connection does not own matching declared ports."));
            Assert.Equal(valid.Revision, scene.World.Construction.Revision);
            Assert.Equal(valid.Instances, scene.World.Construction.Instances);
            Assert.Empty(scene.World.Construction.Connections);
            await scene.UndoAsync();
            Assert.Equal(.1f, Assert.IsType<WorkshopGenericInstance>(scene.World.Construction.Instances[2]).Parameters[new("interval_seconds")]);
            foreach (var wired in new[] { false, true })
            {
                if (wired) await scene.ChangeConnectionAsync(link, true);
                Assert.Equal(wired ? 1 : 0, scene.World.Construction.Connections.Count);
                var run = await scene.World.RunWorkshop();
                Assert.True(run.Applicable);
                Assert.Equal(WorkshopCommandOutcome.Applied, run.Result.Outcome);
                await harness.Advance(2 * (int)harness.Construction.Settings.SimulationRate.Numerator);
                var blade = Assert.Single(harness.Committed.Presentation.Actuators.ToArray(), actuator => actuator.Owner == instances[0].Id);
                if (wired) Assert.True(blade.Coordinate > 1.3f, "The authored supplied gate clears the opening.");
                else Assert.True(blade.Coordinate < .2f, "Removing only the supply wire leaves the authored gate closed.");
                Assert.Equal(WorkshopCommandOutcome.Applied, (await scene.World.ResetWorkshop()).Result.Outcome);
            }
        }
        finally { scene.Free(); godot.Engine.Iteration(); }
    }
}
