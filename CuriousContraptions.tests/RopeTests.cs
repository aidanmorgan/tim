using System.Buffers.Binary;
using System.Numerics;
using Google.FlatBuffers;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class RopeTests
{
    private static WorkshopConstruction Hanging(bool connected = true, float length = 3f)
    {
        var anchor = WorkshopInput.Generic(new("rope_anchor"), new(1), 0f, 6f, -.18f, 0f, 0f, 0f, 1f);
        var weight = WeightTests.Placed(2, 0, 3 - .57710344f, 0, 4f);
        return new(new(2), WorkshopCadenceSettings.Default(), new(anchor, weight), Connections: connected
            ? new(new WorkshopConnection(new(1), WorkshopSocket.Tie, new(2), WorkshopSocket.Tie, WorkshopConnectionDomain.Rope, length))
            : WorkshopConnections.Empty);
    }

    [Fact]
    public async Task ACompleteRopeSupportsItsLoadWhileAnUnconnectedLoadFalls()
    {
        await using var supported = await WorkshopIntegrationHarness.Running(Hanging());
        await using var free = await WorkshopIntegrationHarness.Running(Hanging(false));
        // A 0.1m support allowance is smaller than the load's height and far smaller than its 2m floor clearance.
        // Across three game seconds a disconnected or cumulatively falling load must fail.
        foreach (var read in await supported.Advance(360))
            Assert.InRange(Height(read), 2.32f, 2.53f);
        await free.Advance(120);
        Assert.True(Height(free.Committed) < 1f);
    }

    [Fact]
    public async Task SlackRopeDoesNotAccelerateTheFallingLoad()
    {
        await using var slack = await WorkshopIntegrationHarness.Running(Hanging(length: 8f));
        await using var free = await WorkshopIntegrationHarness.Running(Hanging(false));
        await slack.Advance(30); await free.Advance(30);
        Assert.InRange(MathF.Abs(Height(slack.Committed) - Height(free.Committed)), 0f, .005f);
        Assert.True(Height(slack.Committed) < 2.3f);
    }

    [Theory]
    [InlineData(.05f)]
    [InlineData(200f)]
    public void AuthoredLengthBoundariesRoundTrip(float length)
    {
        var construction = Hanging(length: length);
        construction.Validate();
        var saved = new WorkshopSavedConstruction(construction, new(3));
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(saved));
        Assert.Equal(length, Assert.Single(loaded.Construction.Connections).RopeLength);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(.049f)]
    [InlineData(200.01f)]
    [InlineData(float.NaN)]
    public void InvalidLengthsRefuseBeforeInstallation(float length) =>
        Assert.Equal(WorkshopEditRejection.InvalidConnection,
            Assert.Throws<WorkshopEditRejectedException>(() => Hanging(length: length).Validate()).Reason);


    [Fact]
    public async Task CutWaitsForCommittedStepAndSurvivesDiscardThenResetRestoresTheRope()
    {
        var construction = Hanging();
        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        await harness.Advance(60);
        await harness.Require(WorkshopCommandKind.Pause);
        var before = harness.Committed;
        var span = Assert.Single(before.Presentation.Ropes.ToArray()).Id;
        await harness.Require(WorkshopCommandKind.CutRope, cut: span);
        Assert.Equal(before.Tick, harness.Committed.Tick);
        Assert.Equal(RopeIntegrity.Intact, Assert.Single(harness.Committed.Presentation.Ropes.ToArray()).Integrity);
        var duplicate = await harness.Execute(WorkshopCommandKind.CutRope, cut: span);
        Assert.Equal(WorkshopRejection.RopeAlreadyCut, duplicate.Result.Reason);
        var unknown = await harness.Execute(WorkshopCommandKind.CutRope, cut: new(999));
        Assert.Equal(WorkshopRejection.UnknownRopeSpan, unknown.Result.Reason);
        harness.FailNextStep = true;
        Assert.Equal(WorkshopCommandOutcome.Rejected, (await harness.Execute(WorkshopCommandKind.Step)).Result.Outcome);
        Assert.Equal(before.Tick, harness.Committed.Tick);
        Assert.Equal(RopeIntegrity.Intact, Assert.Single(harness.Committed.Presentation.Ropes.ToArray()).Integrity);
        harness.FailNextStep = false;
        await harness.Require(WorkshopCommandKind.Step);
        var cut = Assert.Single(harness.Committed.Presentation.Ropes.ToArray());
        Assert.Equal(RopeIntegrity.Cut, cut.Integrity); Assert.Equal(RopeState.Open, cut.State); Assert.Equal(span, cut.CutSpan);
        var committedCut = harness.Committed;
        var cutAgain = await harness.Execute(WorkshopCommandKind.CutRope, cut: span);
        Assert.Equal(WorkshopCommandOutcome.Rejected, cutAgain.Result.Outcome);
        Assert.Equal(WorkshopRejection.RopeAlreadyCut, cutAgain.Result.Reason);
        Assert.Equal(committedCut, harness.Committed);
        await harness.Require(WorkshopCommandKind.Resume);
        await harness.Advance(90);
        Assert.True(Height(harness.Committed) < 1f);
        await harness.Require(WorkshopCommandKind.Reset);
        Assert.Equal(construction.Connections, harness.Construction.Connections);
        Assert.Equal(RopeIntegrity.Intact, Assert.Single(harness.Committed.Presentation.Ropes.ToArray()).Integrity);
        var authored = (WorkshopGenericInstance)construction.Instances.Single(p => p.Id == new GpuBodyId(2));
        Assert.True(harness.Committed.Bodies.TryGet(authored.Id, out var restored));
        Assert.Equal(authored.Cell, restored.Body.Cell); Assert.Equal(authored.Local, restored.Body.Local);
        Assert.Equal(authored.Rotation, restored.Rotation);
        await harness.Run(); await harness.Advance(120);
        Assert.InRange(Height(harness.Committed), 2.32f, 2.53f);
    }

    [Fact]
    public async Task ThreeDimensionalReleaseSwingsWithoutLosingItsTether()
    {
        var construction = Hanging();
        var moved = WeightTests.Placed(2, 1.5, 4, 1.5, 4f);
        var anchor = new System.Numerics.Vector3(0, 6, 0);
        var tie = new System.Numerics.Vector3(1.5f, 4f + .57710344f, 1.5f);
        var length = System.Numerics.Vector3.Distance(anchor, tie);
        construction = construction with { Instances = construction.Instances.With(moved),
            Connections = new(construction.Connections[0] with { RopeLength = length }) };
        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        var swungThrough = false;
        foreach (var read in await harness.Advance(480))
        {
            var rope = Assert.Single(read.Presentation.Ropes.ToArray());
            // Ten centimetres is small relative to the 2.55m free span and the load's clearance.
            Assert.InRange(rope.CurrentLength, 0f, length + .1f);
            if (rope.AttachmentB.Z < -.5f) swungThrough = true;
            Assert.True(rope.AttachmentB.Y < 4.7f, "The freely swinging load gained a visibly higher turning point.");
        }
        Assert.True(swungThrough, "The 3D load must swing through its release plane.");
    }

    [Fact]
    public void SlackArtworkMatchesLengthAndRemainsFiniteForVerticalAndCoincidentEndpoints()
    {
        foreach (var end in new[] { System.Numerics.Vector3.Zero, System.Numerics.Vector3.UnitY, System.Numerics.Vector3.UnitX })
        {
            var points = RopeGeometry.Evaluate(System.Numerics.Vector3.Zero, end, 3f, RopeState.Slack);
            Assert.Equal(System.Numerics.Vector3.Zero, points[0]); Assert.Equal(end, points[^1]);
            var length = 0f;
            for (var i = 1; i < points.Length; i++) length += System.Numerics.Vector3.Distance(points[i - 1], points[i]);
            Assert.InRange(length, 2.998f, 3.002f); // 2mm visual allowance, below the 25mm strand radius.
        }
    }

    [Fact]
    public void OpenGuideTopologyIsIncompleteAndCyclesOrBranchesRefuse()
    {
        WorkshopConnection Edge(ulong a, ulong b) => new(new(a), WorkshopSocket.Tie, new(b), WorkshopSocket.Tie, WorkshopConnectionDomain.Rope, 1f);
        RopeAttachmentRole Role(RopeTopology.Endpoint endpoint) => endpoint.Body.Value == 1 ? RopeAttachmentRole.Load : RopeAttachmentRole.Guide;
        var route = Assert.Single(RopeTopology.Compile([Edge(2, 1), Edge(3, 2)], Role));
        Assert.False(route.Complete); Assert.Equal(new GpuBodyId(1), route.Attachments[0].Body);
        Assert.Throws<WorkshopEditRejectedException>(() => RopeTopology.Compile([Edge(1, 2), Edge(2, 3)],
            endpoint => endpoint.Body.Value == 2 ? RopeAttachmentRole.Guide : RopeAttachmentRole.Load));
        Assert.Throws<WorkshopEditRejectedException>(() => RopeTopology.Compile([Edge(2, 3), Edge(3, 4), Edge(4, 2)], Role));
        Assert.Throws<WorkshopEditRejectedException>(() => RopeTopology.Compile([Edge(1, 2), Edge(1, 3)], Role));
        Assert.Throws<WorkshopEditRejectedException>(() => RopeTopology.Compile([Edge(1, 2), Edge(2, 1)], Role));
    }

    [Theory]
    [InlineData(.25f)]
    [InlineData(4f)]
    [InlineData(8f)]
    public void TieTracksTheActualMassScaledEye(float mass)
    {
        var instance = WeightTests.Placed(2, 0, 3, 0, mass);
        var tie = DeclaredPorts.Current.LocalPosition(instance, WorkshopSocket.Tie);
        var expected = .5771f * (.136734f * mass + .453070f);
        Assert.InRange(MathF.Abs(tie.Y - expected), 0f, 1e-6f); // bounded linear evaluation roundoff, metres
        Assert.Equal(0f, tie.X); Assert.Equal(0f, tie.Z);
    }


    [Fact]
    public async Task DemandBeyondInitialReservesPreservesIndependentRopesAndSaveLoad()
    {
        var parts = new List<IWorkshopInstance>();
        var links = new List<WorkshopConnection>();
        for (var i = 0; i < 24; i++)
        {
            var x = (i % 6 - 3) * 3f; var z = (i / 6 - 2) * 3f;
            var a = new GpuBodyId((ulong)i * 2 + 1); var b = new GpuBodyId(a.Value + 1);
            parts.Add(WorkshopInput.Generic(new("rope_anchor"), a, x, 6f, z - .18f, 0, 0, 0, 1));
            parts.Add(WeightTests.Placed(b.Value, x, 3f - .57710344f, z, 4));
            links.Add(new(a, WorkshopSocket.Tie, b, WorkshopSocket.Tie, WorkshopConnectionDomain.Rope, 3f));
        }
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(parts.ToArray()), Connections: new(links.ToArray()));
        await using var world = await WorkshopIntegrationHarness.Running(construction);
        await world.Advance(30);
        Assert.Equal(24, world.Committed.Presentation.Ropes.Length);
        var selected = world.Committed.Presentation.Ropes[12].Id;
        await world.Require(WorkshopCommandKind.CutRope, cut: selected);
        await world.Advance(90);
        foreach (var rope in world.Committed.Presentation.Ropes.ToArray())
        {
            Assert.Equal(rope.Id == selected ? RopeIntegrity.Cut : RopeIntegrity.Intact, rope.Integrity);
            if (rope.Id != selected) Assert.InRange(rope.AttachmentB.Y, 2.9f, 3.1f);
            else Assert.True(rope.AttachmentB.Y < 1.5f);
        }
        await world.Reset();
        var saved = WorkshopSaveCodec.Decode(SaveFixture.Elements,
            WorkshopSaveCodec.Encode(new(construction, new(49))));
        Assert.Equal(construction.Connections, saved.Construction.Connections);
        await using var loaded = await WorkshopIntegrationHarness.Running(saved.Construction);
        await loaded.Advance(60);
        Assert.All(loaded.Committed.Presentation.Ropes.ToArray(), rope =>
        {
            Assert.Equal(RopeIntegrity.Intact, rope.Integrity); Assert.InRange(rope.AttachmentB.Y, 2.9f, 3.1f);
        });
    }

    [Fact]
    public void MeasurementPersistsAcrossMovesAndMassEditsButReconnectMeasuresAgain()
    {
        var original = Hanging(false);
        var measured = WorkshopConnections.MeasureRope(original.Instances, new(1), new(2));
        Assert.InRange(measured.RopeLength!.Value, 2.99999f, 3.00001f);
        var withRope = original with { Connections = new(measured) };
        var moved = WeightTests.Placed(2, 0, 4, 0, 8);
        var changed = withRope with { Instances = withRope.Instances.With(moved) };
        changed.Validate();
        Assert.Equal(measured.RopeLength, changed.Connections[0].RopeLength);
        var reconnected = WorkshopConnections.MeasureRope(changed.Instances, new(1), new(2));
        Assert.True(reconnected.RopeLength < measured.RopeLength - 1f);
        var saved = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(new(changed, new(3))));
        Assert.Equal(changed, saved.Construction);
        var reversed = WorkshopConnections.MeasureRope(original.Instances, new(2), new(1));
        Assert.Equal(measured.RopeLength, reversed.RopeLength);
    }

    [Fact]
    public async Task UnequalDynamicLoadsReactToASeparatingFallAndReversingTheEdgePreservesIt()
    {
        var a = WeightTests.Placed(1, -1, 2.5f, 0, .25f);
        var b = WeightTests.Placed(2, 1, 2.5f, 0, 8f);
        var shelf = WorkshopInput.Generic(new("wall"), new(3), -1f, 2f, 0f, 0, 0, 0, 1,
            new([new(new("width"), 1f), new(new("height"), 1f), new(new("thickness"), 2f)]));
        var instances = new WorkshopInstances(a, b, shelf);
        var rope = WorkshopConnections.MeasureRope(instances, a.Id, b.Id);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), instances, Connections: new(rope));
        await using var forward = await WorkshopIntegrationHarness.Running(construction);
        await using var reverse = await WorkshopIntegrationHarness.Running(construction with
        { Connections = new(rope with { Source = b.Id, Target = a.Id }) });
        await using var control = await WorkshopIntegrationHarness.Running(construction with { Connections = WorkshopConnections.Empty });
        await forward.Advance(120); await reverse.Advance(120); await control.Advance(120);
        Assert.True(forward.Committed.Bodies.TryGet(a.Id, out var pulled));
        Assert.True(control.Committed.Bodies.TryGet(a.Id, out var resting));
        Assert.True(reverse.Committed.Bodies.TryGet(a.Id, out var reversed));
        float X(PhysicsBodyRead body) => (body.Body.Cell.X + body.Body.Local.X) / 16f;
        Assert.True(X(pulled) > X(resting) + .2f, "The falling heavy load must pull the light load off its shelf.");
        Assert.InRange(MathF.Abs(X(pulled) - X(reversed)), 0f, .02f);
    }

    [Fact]
    public async Task SlackAndCutConnectionsKeepOrdinaryLoadContact()
    {
        var lower = WeightTests.Placed(1, 0, -.46f, 0, 4f);
        var upper = WeightTests.Placed(2, 0, 2, 0, 4f);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(lower, upper),
            Connections: new(new WorkshopConnection(lower.Id, WorkshopSocket.Tie, upper.Id, WorkshopSocket.Tie,
                WorkshopConnectionDomain.Rope, 10f)));
        foreach (var cut in new[] { false, true })
        {
            await using var world = await WorkshopIntegrationHarness.Running(construction);
            if (cut) await world.Require(WorkshopCommandKind.CutRope, cut: Assert.Single(world.Committed.Presentation.Ropes.ToArray()).Id);
            await world.Advance(240);
            Assert.True(world.Committed.Bodies.TryGet(lower.Id, out var bottom));
            Assert.True(world.Committed.Bodies.TryGet(upper.Id, out var top));
            var difference = ((top.Body.Cell.Y + top.Body.Local.Y) - (bottom.Body.Cell.Y + bottom.Body.Local.Y)) / 16f;
            Assert.True(difference > .45f, "Connected loads must contact rather than overlap.");
        }
    }


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheLengthRowHasMassWeightedReactionAndNeverCreatesKineticEnergy(bool reversed)
    {
        var (world, a, b) = TensileWorld(complete: true, outward: true, coincident: false, reversed: reversed);
        var end = world.Step(30, (_, state) =>
        {
            var first = state[a]; var second = state[b];
            Assert.True(first.Finite && second.Finite);
            Assert.InRange(first.Angular.Length() + second.Angular.Length(), 0f, .005f);
            // Initial momentum is -1 + 4 = 3kg m/s; 0.005 is below a visible 0.01m/s drift of either load.
            Assert.InRange(MathF.Abs(first.Velocity.X + 4f * second.Velocity.X - 3f), 0f, .005f);
            var energy = .5f * first.Velocity.LengthSquared() + 2f * second.Velocity.LengthSquared();
            Assert.True(energy <= 2.51f, "A central tension row must not materially add to the initial 2.5J kinetic energy.");
            Assert.InRange(Vector3.Distance(first.Position, second.Position), 0f, 3.05f);
        });
        Assert.True(end[a].Velocity.X > -.5f); Assert.True(end[b].Velocity.X < .9f);
        var (free, freeA, freeB) = TensileWorld(complete: false, outward: true);
        var control = free.Step(30);
        Assert.True(Vector3.Distance(control[freeA].Position, control[freeB].Position) > 3.4f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InwardMotionAndCoincidentSlackDoNotReceiveConstraintImpulse(bool coincident)
    {
        var (world, a, b) = TensileWorld(complete: true, outward: false, coincident: coincident);
        var (control, controlA, controlB) = TensileWorld(complete: false, outward: false, coincident: coincident);
        var actual = world.Step(30); var free = control.Step(30);
        foreach (var pair in new[] { (a, controlA), (b, controlB) })
        {
            Assert.True(actual[pair.Item1].Finite);
            Assert.InRange(Vector3.Distance(actual[pair.Item1].Position, free[pair.Item2].Position), 0f, .005f);
            Assert.InRange(Vector3.Distance(actual[pair.Item1].Velocity, free[pair.Item2].Velocity), 0f, .005f);
        }
        Assert.True(actual[a].Position.X > .2f && actual[b].Position.X < 2.8f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IncompleteRowsDiscardStaleTensionWhileCompleteRowsReact(bool complete)
    {
        var (world, a, b) = TensileWorld(complete, outward: true);
        // Public ABI boundary input: stale cached tension must not survive an open declaration's warm-start.
        BinaryPrimitives.WriteSingleLittleEndian(world.Admitted.AsSpan(
            world.Scene.World.TensileSpanOffset(0) + PhysicsGpuAbi.TensileImpulseOffset), .25f);
        var actual = world.Step(12);
        if (complete)
        {
            Assert.True(actual[a].Velocity.X > -.5f); Assert.True(actual[b].Velocity.X < .9f);
        }
        else
        {
            Assert.InRange(MathF.Abs(actual[a].Velocity.X + 1f), 0f, .005f);
            Assert.InRange(MathF.Abs(actual[b].Velocity.X - 1f), 0f, .005f);
            Assert.InRange(actual[a].Position.X, -.105f, -.095f);
            Assert.InRange(actual[b].Position.X, 3.095f, 3.105f);
        }
        Assert.True(actual[a].Finite && actual[b].Finite);
        Assert.InRange(actual[a].Angular.Length() + actual[b].Angular.Length(), 0f, .005f);
    }

    private static (LawWorld World, GpuBodyId A, GpuBodyId B) TensileWorld(bool complete, bool outward,
        bool coincident = false, bool reversed = false)
    {
        var declared = new LawScene(bench: false);
        var direction = outward ? -1f : 1f;
        var a = declared.Cylinder(new(0, 5, 0), .1f, .1f, 1f, new(0, 0, 0, 0), velocity: new(direction, 0, 0), gravity: 0f);
        var b = declared.Cylinder(new(3, 5, 0), .1f, .1f, 4f, new(0, 0, 0, 0), velocity: new(-direction, 0, 0), gravity: 0f);
        var compiled = declared.Admit().Scene;
        var localB = coincident ? new Vector3(-3, 0, 0) : Vector3.Zero;
        var row = reversed
            ? new TensileSpanDeclaration(new(1), new(1), 0, b, localB, a, Vector3.Zero, 3f, complete)
            : new TensileSpanDeclaration(new(1), new(1), 0, a, Vector3.Zero, b, localB, 3f, complete);
        var scene = new PhysicsSceneDeclaration(compiled.Document, compiled.NextIdentity, compiled.Bodies,
            compiled.Colliders, compiled.Materials, [], [], tensileSpans: [row]);
        return (new(scene, PhysicsGpuAbi.Admission(scene, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1))), SimulationCadence.Hz120), a, b);
    }


    [Fact]
    public async Task StaticFeasibleRoutesDisplayAndImmovableOverlengthRoutesRefuseAtomically()
    {
        var a = WorkshopInput.Generic(new("rope_anchor"), new(1), 0, 6, 0, 0, 0, 0, 1);
        var b = WorkshopInput.Generic(new("rope_anchor"), new(2), 0, 3, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(a, b),
            Connections: new(WorkshopConnections.MeasureRope(new(a, b), a.Id, b.Id)));
        await using var world = await WorkshopIntegrationHarness.Running(construction);
        await world.Advance(2);
        Assert.Equal(3f, Assert.Single(world.Committed.Presentation.Ropes.ToArray()).CurrentLength);
        var impossible = construction with { Connections = new(construction.Connections[0] with { RopeLength = 2f }) };
        Assert.Throws<ArgumentException>(() => WorkshopSceneCompiler.Compile(impossible, new(1, 2), new(1)));
        Assert.Equal(RopeIntegrity.Intact, Assert.Single(world.Committed.Presentation.Ropes.ToArray()).Integrity);
    }

    [Fact]
    public async Task MalformedCutCommandsAndCommittedGeometryRefuseWithoutChangingTheInstalledWorld()
    {
        await using var world = await WorkshopIntegrationHarness.Running(Hanging());
        var before = world.Committed;
        var cut = world.Command(WorkshopCommandKind.CutRope);
        Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.EncodeCommand(cut));
        Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.EncodeCommand(cut with { Cut = new(0) }));
        Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.EncodeCommand(cut with
        { Kind = WorkshopCommandKind.Pause, Cut = new(1) }));
        var rope = Assert.Single(before.Presentation.Ropes.ToArray());
        Assert.Throws<ArgumentException>(() => new PhysicsPresentationRead([], [], ropes: [rope with { CurrentLength = float.NaN }]));
        Assert.Throws<ArgumentException>(() => new PhysicsPresentationRead([], [], ropes: [rope with { State = (RopeState)99 }]));
        Assert.Throws<ArgumentException>(() => new PhysicsPresentationRead([], [], ropes: [rope, rope]));
        var foreign = before with { Presentation = new([], [], ropes: [rope with { Route = new(999) }]) };
        Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.EncodeResponse(world.Latest with { Read = foreign, Scene = world.Simulation.Scene }));
        Assert.Equal(before, world.Committed);
        await world.Advance(2);
        Assert.InRange(Height(world.Committed), 2.32f, 2.53f);
    }

    [Fact]
    public void SocketBindingRefusesUnknownParametersNonfiniteCoefficientsAndDefaultDisagreement()
    {
        var entry = TestBundle.Entries.Single(e => e.Metadata.Id == "weight");
        var port = Assert.Single(entry.Ports);
        var binding = port.PositionBinding!.Value;
        var invalid = new[]
        {
            port with { PositionBinding = binding with { Parameter = new("unknown") } },
            port with { PositionBinding = binding with { Multiplier = new(float.NaN, 0, 0) } },
            port with { LocalPosition = System.Numerics.Vector3.Zero },
            port with { Direction = PortDirection.Input },
            port with { AttachmentRole = RopeAttachmentRole.None }
        };
        foreach (var changed in invalid)
            Assert.NotEmpty(CuriousContraptions.Bundle.BundleValidator.Validate([new("weight", entry with { Ports = [changed] })]));
    }


    [Fact]
    public void TensileAdmissionRejectsMissingParticipantsAndUnsupportedMotion()
    {
        var compiled = WorkshopPhysicsCompiler.Compile(Hanging(), new(1, 2));
        var bodies = compiled.Bodies.ToArray();
        var span = Assert.Single(compiled.TensileSpans.ToArray());
        PhysicsSceneDeclaration Declare(RigidBodyDeclaration[] participants, TensileSpanDeclaration row) =>
            new(compiled.Document, compiled.NextIdentity, participants, compiled.Colliders, compiled.Materials, [], [], tensileSpans: [row]);
        var valid = Declare(bodies, span);
        Assert.Equal(span, Assert.Single(valid.TensileSpans.ToArray()));
        Assert.Throws<ArgumentException>(() => Declare(bodies, span with { BodyB = new(999) }));
        // Only Static and Dynamic motions are declared. A prescribed/kinematic tag is unsupported, never silently treated as either.
        var unsupported = bodies.Select(body => body.Id == span.BodyB ? body with { Motion = (RigidMotionKind)2 } : body).ToArray();
        Assert.Throws<ArgumentException>(() => Declare(unsupported, span));
        Assert.Equal(span, Assert.Single(valid.TensileSpans.ToArray()));
    }


    public enum CutWireDefect { Missing, Zero, UnexpectedPayload }

    [Theory]
    [InlineData(CutWireDefect.Missing)]
    [InlineData(CutWireDefect.Zero)]
    [InlineData(CutWireDefect.UnexpectedPayload)]
    public async Task MalformedCutPayloadsRefuseAtDecodeAndRouterWithoutChangingInstalledState(CutWireDefect defect)
    {
        await using var world = await WorkshopIntegrationHarness.Running(Hanging());
        var before = world.Committed;
        var span = Assert.Single(before.Presentation.Ropes.ToArray()).Id;
        var command = world.Command(WorkshopCommandKind.CutRope, cut: span);
        var valid = WorkshopWireFlatBuffers.EncodeCommand(command);
        Assert.Equal(command, WorkshopWireFlatBuffers.DecodeCommand(valid));
        var bytes = (byte[])valid.Clone();
        var root = WireRoot(bytes);
        switch (defect)
        {
            case CutWireDefect.Missing:
                BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(root.bb_pos - root.bb.GetInt(root.bb_pos) + 4 + 2 * 9), 0);
                break;
            case CutWireDefect.Zero:
                BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(WireSlot(root, 9)), 0);
                break;
            case CutWireDefect.UnexpectedPayload:
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(WireSlot(root, 1)), (uint)WorkshopCommandKind.Pause);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(defect));
        }
        Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.DecodeCommand(bytes));
        Assert.Throws<ArgumentException>(() => world.Router.Dispatch(bytes));
        Assert.Equal(before, world.Committed);
        Assert.Equal(WorkshopSimulationPhase.Running, world.Simulation.Phase);
        // The same command sequence is still usable: malformed input never entered the executor.
        Assert.Equal(WorkshopCommandOutcome.Applied, (await world.Dispatch(command)).Result.Outcome);
        await world.Advance(1);
        Assert.Equal(RopeIntegrity.Cut, Assert.Single(world.Committed.Presentation.Ropes.ToArray()).Integrity);
        var pause = world.Command(WorkshopCommandKind.Pause);
        Assert.Equal(pause, WorkshopWireFlatBuffers.DecodeCommand(WorkshopWireFlatBuffers.EncodeCommand(pause)));
        Assert.Equal(WorkshopCommandOutcome.Applied, (await world.Dispatch(pause)).Result.Outcome);
    }

    public enum RopeReadWireDefect { TranslatedAttachments, NonfiniteLength, UnknownState, ForeignRoute }

    [Theory]
    [InlineData(RopeReadWireDefect.TranslatedAttachments)]
    [InlineData(RopeReadWireDefect.NonfiniteLength)]
    [InlineData(RopeReadWireDefect.UnknownState)]
    [InlineData(RopeReadWireDefect.ForeignRoute)]
    public async Task MalformedRopeReadBytesRefuseBeforePublishingDetachedOrInvalidKnots(RopeReadWireDefect defect)
    {
        await using var world = await WorkshopIntegrationHarness.Running(Hanging());
        var before = world.Committed;
        var response = world.Latest with { Scene = world.Simulation.Scene };
        var valid = WorkshopWireFlatBuffers.EncodeResponse(response);
        Assert.Equal(before.Presentation.Ropes.ToArray(), WorkshopWireFlatBuffers.DecodeResponse(valid).Read.Presentation.Ropes.ToArray());
        var bytes = (byte[])valid.Clone();
        var root = WireRoot(bytes);
        var vector = root.__vector(root.__offset(4 + 26 * 2));
        var rope = new Table(root.__indirect(vector), root.bb);
        switch (defect)
        {
            case RopeReadWireDefect.TranslatedAttachments:
                // Both x coordinates are zero: translation preserves length/state and all record-local validation.
                foreach (var field in new[] { 5, 6 }) BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(WireSlot(rope, field)), .5f);
                var original = Assert.Single(before.Presentation.Ropes.ToArray());
                (original with { AttachmentA = original.AttachmentA + Vector3.UnitX * .5f,
                    AttachmentB = original.AttachmentB + Vector3.UnitX * .5f }).Validate();
                break;
            case RopeReadWireDefect.NonfiniteLength:
                BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(WireSlot(rope, 8)), float.NaN); break;
            case RopeReadWireDefect.UnknownState:
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(WireSlot(rope, 10)), 99); break;
            case RopeReadWireDefect.ForeignRoute:
                BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(WireSlot(rope, 1)), 999); break;
            default: throw new ArgumentOutOfRangeException(nameof(defect));
        }
        Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.DecodeResponse(bytes));
        Assert.Equal(before, world.Committed);
        Assert.Equal(before.Presentation.Ropes.ToArray(), WorkshopWireFlatBuffers.DecodeResponse(valid).Read.Presentation.Ropes.ToArray());
        await world.Advance(2);
        Assert.Equal(before.Tick.Value + 2, world.Committed.Tick.Value);
    }

    [Fact]
    public async Task RopeReadCodecAcceptsRotatedSocketsAndMovingCommittedBodies()
    {
        var original = Hanging();
        var instances = original.Instances.Select(instance => (WorkshopGenericInstance)instance)
            .Select(instance => instance with { Rotation = instance.Id.Value == 1
                ? CanonicalRotation.Normalize(0, .70710677f, 0, .70710677f)
                : CanonicalRotation.Normalize(0, 0, .70710677f, .70710677f) }).ToArray();
        var construction = original with { Instances = new(instances.Cast<IWorkshopInstance>().ToArray()),
            Connections = new(WorkshopConnections.MeasureRope(new(instances.Cast<IWorkshopInstance>().ToArray()), new(1), new(2))) };
        await using var world = await WorkshopIntegrationHarness.Running(construction);
        var initial = Assert.Single(world.Committed.Presentation.Ropes.ToArray());
        for (var i = 0; i < 2; i++)
        {
            var response = world.Latest with { Scene = world.Simulation.Scene };
            var decoded = WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(response));
            Assert.Equal(world.Committed.Presentation.Ropes.ToArray(), decoded.Read.Presentation.Ropes.ToArray());
            if (i == 0) await world.Advance(30);
        }
        Assert.True(Vector3.Distance(initial.AttachmentB, world.Committed.Presentation.Ropes[0].AttachmentB) > .01f);
    }

    private static Table WireRoot(byte[] bytes)
    {
        var buffer = new ByteBuffer(bytes);
        return new(4 + buffer.GetInt(4), buffer);
    }
    private static int WireSlot(Table table, int field)
    {
        var offset = table.__offset(4 + 2 * field);
        Assert.NotEqual(0, offset);
        return table.bb_pos + offset;
    }
    private static float Height(WorkshopRead read)
    {
        Assert.True(read.Bodies.TryGet(new(2), out var value));
        return (value.Body.Cell.Y + value.Body.Local.Y) / 16f;
    }
}
