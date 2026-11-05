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
            ? new(new WorkshopConnection(new(1), PortId.Tie, new(2), PortId.Tie, WorkshopConnectionDomain.Rope, length))
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

    // ---- a taut rope at rest commits rest (engine-tensile-drive-coupling) ----------------------------------------------------------

    /// <summary>The observed window for a load held at rest: 3 s, the first second left to settle.</summary>
    private const int RestTicks = 360, SettleTicks = 120;
    /// <summary>The 0.5 mm resting envelope (m) of the game-grade envelope: how far a load held at rest may wander over the observed
    /// window.</summary>
    private const float RestingEnvelope = .0005f;
    /// <summary>The committed speed (m/s) a load held at rest may read. Committed body velocity is published state, read through
    /// <see cref="WorkshopRead"/> by every consumer of the committed world. A load held at rest by a rope must read as rest, as a
    /// bench-held load does. Integrated over the run, that reading must not imply motion beyond the resting envelope. The assertions
    /// cover the 2 s after the 1 s settle; the divisor is the full 3 s run, the more conservative choice, so |v| ≤ 0.5 mm / 3 s. A
    /// tensile row soft in relax committed g/(2ζω + hω²) = 1.25 mm/s on a static hang, whatever the mass, while
    /// the pose held still. This is a test bound, distinct from the engine's 0.01 m/s actuator rest speed.</summary>
    private const float HeldRestSpeed = RestingEnvelope / 3f;

    public enum Support { Rope, Bench }

    // A Weight hanging at rest on a taut rope from a static anchor reads as rest through its committed velocity, as one resting on the
    // bench does. The control is the same Weight and anchor, unconnected, on the bench, whose contact is already rigid in relax. The
    // rope case first checks that the rope carries the load: it stays within 0.1 m of its hang height, far above the bench. Both cases
    // check the recipe's "no drift over 3 s" pose control, applied to the 2 s after the 1 s settle: the settled height spread stays
    // within the 0.5 mm envelope. That control held before this slice; only the committed velocity failed. Two masses, since the
    // defect is mass-independent while the rope's tension is not.
    [Theory]
    [InlineData(1f, Support.Rope)]
    [InlineData(6.8f, Support.Rope)]
    [InlineData(1f, Support.Bench)]
    public async Task ALoadAtRestOnATautRopeCommitsNoVelocityLikeOneRestingOnTheBench(float mass, Support support)
    {
        var anchor = WorkshopInput.Generic(new("rope_anchor"), new(1), 0f, 6f, -.18f, 0f, 0f, 0f, 1f);
        var tie = DeclaredPorts.Current.LocalPosition(WeightTests.Placed(2, 0, 0, 0, mass), PortId.Tie).Y;
        var hang = 3f - tie;
        var bench = (float)WorkshopConstruction.WorkbenchSurface.Value;
        var weight = WeightTests.Placed(2, 0, support == Support.Rope ? hang : bench, 0, mass);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(anchor, weight), Connections:
            support == Support.Rope
                ? new(new WorkshopConnection(anchor.Id, PortId.Tie, weight.Id, PortId.Tie, WorkshopConnectionDomain.Rope, 3f))
                : WorkshopConnections.Empty);
        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        var reads = await harness.Advance(RestTicks);

        Assert.All(reads, read => Assert.InRange(Height(read), support == Support.Rope ? hang - .1f : bench - RestingEnvelope,
            support == Support.Rope ? hang + .1f : bench + RestingEnvelope));
        var settled = reads.Skip(SettleTicks).Select(Height).ToList();
        Assert.True(settled.Max() - settled.Min() <= RestingEnvelope,
            $"the {mass} kg Weight at rest on the {support} drifted {(settled.Max() - settled.Min()) * 1000f:F3} mm over the settled ticks");
        Assert.All(reads.Skip(SettleTicks), read =>
        {
            Assert.True(read.Bodies.TryGet(weight.Id, out var body));
            Assert.True(MathF.Abs(body.Body.Velocity.Y) <= HeldRestSpeed,
                $"tick {read.Tick.Value}: the {mass} kg Weight at rest on the {support} commits Vy {body.Body.Velocity.Y:E3} m/s");
        });
    }

    // Genericity: the same rule holds a supplied prismatic drive. A 1 kg slider driven up toward a 1.2 m target is stopped 0.5 m up by
    // a rope to its static frame; the drive keeps pushing at its 16 N effort. Once settled it holds short of its target, its rope taut
    // (within the 0.5 mm envelope of the rope's length), and reads as rest through its committed axial velocity (HeldRestSpeed). A
    // rope soft in relax committed T·m_inv/(2ζω + hω²) ≈ 0.6 mm/s here.
    [Fact]
    public void ASuppliedPrismaticDriveHeldByARopeHoldsShortOfItsTargetAndCommitsRest()
    {
        const float rope = .5f;
        var declared = new LawScene(bench: false);
        var frame = declared.StaticBox(new(0, 5, 0), new(.2f, .2f, .2f));
        var moving = declared.Box(new(0, 5, 0), new(.25f, .25f, .25f), 1f, LawMaterial.Domino);
        var joint = declared.Prismatic(frame, moving, ConstraintFrame.Identity, ConstraintFrame.Identity, -.2f, 2f, 3f, .5f,
            ConnectedCollision.Disabled);
        declared.Drive(joint, 1.2f, 1.6f, 8f, 16f, 12f);
        var s = declared.Admit().Scene;
        var span = new TensileSpanDeclaration(new(s.NextIdentity), new(s.NextIdentity + 1), 0, frame, Vector3.Zero, moving, Vector3.Zero,
            rope, true);
        var scene = new PhysicsSceneDeclaration(s.Document, s.NextIdentity + 2, s.Bodies, s.Colliders, s.Materials, [], [],
            prismatics: s.Prismatics, supplyNodes: s.SupplyGraph.Nodes.ToArray(), supplyEdges: s.SupplyGraph.Edges.ToArray(),
            prismaticDrives: s.PrismaticDrives, tensileSpans: [span]);
        var world = new LawWorld(scene, PhysicsGpuAbi.Admission(scene, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1))), SimulationCadence.Hz120);

        var held = new List<LawBody>();
        world.Step(RestTicks, (tick, state) => { if (tick > SettleTicks) held.Add(state[moving]); }, supplied: true);

        Assert.All(held, body => Assert.InRange(body.Position.Y - 5f, rope - RestingEnvelope, rope + RestingEnvelope));
        Assert.All(held, body => Assert.True(MathF.Abs(body.Velocity.Y) <= HeldRestSpeed,
            $"the slider held by its rope commits an axial velocity of {body.Velocity.Y:E3} m/s"));
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
        WorkshopConnection Edge(ulong a, ulong b) => new(new(a), PortId.Tie, new(b), PortId.Tie, WorkshopConnectionDomain.Rope, 1f);
        RopeAttachmentRole Role(RopeTopology.Endpoint endpoint) => endpoint.Body.Value == 1 ? RopeAttachmentRole.Load : RopeAttachmentRole.Guide;
        var route = Assert.Single(RopeTopology.Compile([Edge(2, 1), Edge(3, 2)], Role));
        Assert.False(route.Complete); Assert.Equal(new GpuBodyId(1), route.Attachments[0].Body);
        var completeRoute = Assert.Single(RopeTopology.Compile([Edge(1, 2), Edge(2, 3)],
            endpoint => endpoint.Body.Value == 2 ? RopeAttachmentRole.Guide : RopeAttachmentRole.Load));
        Assert.True(completeRoute.Complete);
        Assert.Equal(3, completeRoute.Attachments.Length);
        Assert.Equal(2, completeRoute.Spans.Length);
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
        var tie = DeclaredPorts.Current.LocalPosition(instance, PortId.Tie);
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
            links.Add(new(a, PortId.Tie, b, PortId.Tie, WorkshopConnectionDomain.Rope, 3f));
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
            Connections: new(new WorkshopConnection(lower.Id, PortId.Tie, upper.Id, PortId.Tie,
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

    private static (WorkshopConstruction Construction, GpuBodyId WeightA, GpuBodyId Pulley1, GpuBodyId Pulley2, GpuBodyId WeightB, float TotalLength)
        TwoPulleyAtwood(float massA = 4f, float massB = 4f, float heightA = 3f, float heightB = 3f, float? totalLength = null)
    {
        var a = WeightTests.Placed(1, -2.4f, heightA, 0f, massA);
        var p1 = WorkshopInput.Generic(new("pulley"), new(2), -2f, 6f, -.12f, 0, 0, 0, 1);
        var p2 = WorkshopInput.Generic(new("pulley"), new(3), 2f, 6f, -.12f, 0, 0, 0, 1);
        var b = WeightTests.Placed(4, 2.4f, heightB, 0f, massB);
        var tieA = DeclaredPorts.Current.LocalPosition(a, PortId.Tie);
        var tieB = DeclaredPorts.Current.LocalPosition(b, PortId.Tie);
        var posA = new Vector3(-2.4f, heightA, 0f) + tieA;
        var posB = new Vector3(2.4f, heightB, 0f) + tieB;
        var guides = new RouteGuideFrame[]
        {
            new(new(-2f, 6f, -.12f), Quaternion.Identity, 0.4f, new(0f, 0f, 0.12f)),
            new(new(2f, 6f, -.12f), Quaternion.Identity, 0.4f, new(0f, 0f, 0.12f))
        };
        var evaluated = RopeGeometry.EvaluateRoute(posA, posB, guides);
        var len = totalLength ?? evaluated.TotalLength;
        var span1 = 4.0f;
        var remaining = MathF.Max(0.1f, len - span1);
        var span0 = remaining * 0.5f;
        var span2 = remaining * 0.5f;
        var link0 = new WorkshopConnection(a.Id, PortId.Tie, p1.Id, PortId.Tie, WorkshopConnectionDomain.Rope, span0);
        var link1 = new WorkshopConnection(p1.Id, PortId.Tie, p2.Id, PortId.Tie, WorkshopConnectionDomain.Rope, span1);
        var link2 = new WorkshopConnection(p2.Id, PortId.Tie, b.Id, PortId.Tie, WorkshopConnectionDomain.Rope, span2);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(a, p1, p2, b),
            Connections: new(link0, link1, link2));
        return (construction, a.Id, p1.Id, p2.Id, b.Id, len);
    }

    [Fact]
    public async Task TwoPulleyAtwoodBalancesEqualWeightsAtRest()
    {
        var (construction, idA, _, _, idB, _) = TwoPulleyAtwood(massA: 5f, massB: 5f, heightA: 3f, heightB: 3f);
        await using var world = await WorkshopIntegrationHarness.Running(construction);
        var initialA = BodyY(world.Committed, idA);
        var initialB = BodyY(world.Committed, idB);

        await world.Advance(120);

        var finalA = BodyY(world.Committed, idA);
        var finalB = BodyY(world.Committed, idB);

        Assert.InRange(MathF.Abs(finalA - initialA), 0f, .005f);
        Assert.InRange(MathF.Abs(finalB - initialB), 0f, .005f);
        Assert.Equal(3, world.Committed.Presentation.Ropes.Length);
        Assert.All(world.Committed.Presentation.Ropes.ToArray(), span =>
        {
            Assert.Equal(RopeIntegrity.Intact, span.Integrity);
        });
    }

    [Fact]
    public async Task TwoPulleyAtwoodAcceleratesUnequalWeightsWithTheoreticalAtwoodDynamics()
    {
        // Mass A = 2 kg, Mass B = 6 kg: theoretical a = g * (6 - 2) / (6 + 2) = 0.5 * 9.81 = 4.905 m/s²
        var (construction, idA, _, _, idB, _) = TwoPulleyAtwood(massA: 2f, massB: 6f, heightA: 2.5f, heightB: 3.5f);
        await using var world = await WorkshopIntegrationHarness.Running(construction);
        var initialA = BodyY(world.Committed, idA);
        var initialB = BodyY(world.Committed, idB);

        await world.Advance(30); // 0.25 seconds at 120 Hz

        var finalA = BodyY(world.Committed, idA);
        var finalB = BodyY(world.Committed, idB);
        var deltaA = finalA - initialA;
        var deltaB = finalB - initialB;

        Assert.True(deltaA > .05f, "Light 2kg weight must rise.");
        Assert.True(deltaB < -.05f, "Heavy 6kg weight must fall.");
        Assert.InRange(MathF.Abs(deltaA + deltaB), 0f, .02f);

        const float expectedA = 4.905f;
        var measuredA = 2f * MathF.Abs(deltaB) / (0.25f * 0.25f);
        Assert.InRange(MathF.Abs(measuredA - expectedA) / expectedA, 0f, 0.10f);

        // Wheel rotation proportional to rope travel Δθ = Δs / 0.4 rad
        var pulleys = world.Committed.Presentation.Guides.ToArray();
        Assert.Equal(2, pulleys.Length);
        var ropeTravel = MathF.Abs(deltaB);
        var expectedAngle = (ropeTravel / 0.4f) % MathF.Tau;
        foreach (var p in pulleys)
        {
            Assert.InRange(MathF.Abs(p.Travel - ropeTravel), 0f, .02f);
            Assert.InRange(MathF.Abs(p.Angle - expectedAngle), 0f, .05f);
        }
    }

    [Fact]
    public void LawSceneAtwoodTenKilogramAndFiveKilogramAcceleratesWithTheoreticalDynamics()
    {
        var scene = new LawScene(bench: false);
        var a = scene.Cylinder(new(-2.4f, 3f, 0f), .2f, .2f, 5f, LawMaterial.Domino, Quaternion.Identity, gravity: -9.81f);
        var p1 = scene.StaticCylinder(new(-2f, 6f, -.12f), .4f, .17f, Quaternion.Identity);
        var p2 = scene.StaticCylinder(new(2f, 6f, -.12f), .4f, .17f, Quaternion.Identity);
        var b = scene.Cylinder(new(2.4f, 3f, 0f), .2f, .2f, 10f, LawMaterial.Domino, Quaternion.Identity, gravity: -9.81f);
        var posA = new Vector3(-2.4f, 3.2f, 0f);
        var posB = new Vector3(2.4f, 3.2f, 0f);
        var guides = new RouteGuideFrame[]
        {
            new(new(-2f, 6f, -.12f), Quaternion.Identity, 0.4f, new(0f, 0f, 0.12f)),
            new(new(2f, 6f, -.12f), Quaternion.Identity, 0.4f, new(0f, 0f, 0.12f))
        };
        var evaluated = RopeGeometry.EvaluateRoute(posA, posB, guides);
        var totalLength = evaluated.TotalLength;
        var compiled = scene.Admit().Scene;
        var span0 = (totalLength - 4f) * .5f;
        var spans = new TensileSpanDeclaration[]
        {
            new(new(1), new(1), 0, a, new(0, .2f, 0), p1, new(0, .4f, .12f), span0, true, 0.4f, new(0f, 0f, 0.12f)),
            new(new(2), new(1), 1, p1, new(0, .4f, .12f), p2, new(0, .4f, .12f), 4f, true, 0.4f, new(0f, 0f, 0.12f)),
            new(new(3), new(1), 2, p2, new(0, .4f, .12f), b, new(0, .2f, 0), span0, true),
        };
        var physicsScene = new PhysicsSceneDeclaration(compiled.Document, compiled.NextIdentity,
            compiled.Bodies, compiled.Colliders, compiled.Materials, [], [], tensileSpans: spans,
            ropeGuides: [new(new(1), p1, 0.4f, new(0f, 0f, 0.12f)), new(new(2), p2, 0.4f, new(0f, 0f, 0.12f))]);
        var world = new LawWorld(physicsScene, PhysicsGpuAbi.Admission(physicsScene, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1))), SimulationCadence.Hz120);

        var initialA = world.Initial(a).Position.Y;
        var initialB = world.Initial(b).Position.Y;
        var final = world.Step(30);
        var finalA = final[a].Position.Y;
        var finalB = final[b].Position.Y;

        var deltaA = finalA - initialA;
        var deltaB = finalB - initialB;
        Assert.True(deltaA > .05f, "Light 5kg weight must rise.");
        Assert.True(deltaB < -.05f, "Heavy 10kg weight must fall.");
        Assert.InRange(MathF.Abs(deltaA + deltaB), 0f, .01f);
        const float expectedA = 9.81f * (10f - 5f) / (10f + 5f); // 3.27 m/s²
        var measuredA = 2f * MathF.Abs(deltaB) / (0.25f * 0.25f);
        Assert.InRange(MathF.Abs(measuredA - expectedA) / expectedA, 0f, 0.10f);
    }

    [Fact]
    public async Task SinglePulleyRedirectionSupportsSuspendedWeight()
    {
        var anchor = WorkshopInput.Generic(new("rope_anchor"), new(1), -2f, 6f, -.18f, 0, 0, 0, 1);
        var pulley = WorkshopInput.Generic(new("pulley"), new(2), 0f, 6f, -.12f, 0, 0, 0, 1);
        var weight = WeightTests.Placed(3, 0.4f, 3f, 0f, 4f);
        var tieWeight = DeclaredPorts.Current.LocalPosition(weight, PortId.Tie);
        var posAnchor = new Vector3(-2f, 6f, 0f);
        var posWeight = new Vector3(0.4f, 3f, 0f) + tieWeight;
        var guides = new RouteGuideFrame[]
        {
            new(new(0f, 6f, -.12f), Quaternion.Identity, 0.4f, new(0f, 0f, 0.12f))
        };
        var evaluated = RopeGeometry.EvaluateRoute(posAnchor, posWeight, guides);
        var totalLength = evaluated.TotalLength;
        var span0 = Vector3.Distance(posAnchor, new Vector3(0f, 6.4f, 0f));
        var span1 = totalLength - span0;
        var link0 = new WorkshopConnection(anchor.Id, PortId.Tie, pulley.Id, PortId.Tie, WorkshopConnectionDomain.Rope, span0);
        var link1 = new WorkshopConnection(pulley.Id, PortId.Tie, weight.Id, PortId.Tie, WorkshopConnectionDomain.Rope, span1);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(anchor, pulley, weight),
            Connections: new(link0, link1));

        await using var world = await WorkshopIntegrationHarness.Running(construction);
        var freeWeight = WeightTests.Placed(4, 3f, 3f, 0f, 4f);
        await using var control = await WorkshopIntegrationHarness.Running(new WorkshopConstruction(new(2),
            WorkshopCadenceSettings.Default(), new(freeWeight)));

        var initialY = BodyY(world.Committed, weight.Id);
        await world.Advance(120);
        await control.Advance(120);

        var finalY = BodyY(world.Committed, weight.Id);
        var controlY = BodyY(control.Committed, freeWeight.Id);

        Assert.InRange(MathF.Abs(finalY - initialY), 0f, .02f);
        Assert.True(controlY < 1f, "Unconnected control weight must fall freely.");
    }

    [Fact]
    public async Task AtwoodSlackTransitionOnFloorContactReleasesTension()
    {
        var shelf = WorkshopInput.Generic(new("wall"), new(5), 2.4f, 2.0f, 0f, 0, 0, 0, 1,
            new([new(new("width"), 1.5f), new(new("height"), .5f), new(new("thickness"), 1.5f)]));
        var (construction, idA, _, _, idB, _) = TwoPulleyAtwood(massA: 2f, massB: 6f, heightA: 2f, heightB: 2.8f);
        construction = construction with { Instances = construction.Instances.With(shelf) };

        await using var world = await WorkshopIntegrationHarness.Running(construction);

        // Advance 40 ticks: rope is taut, Atwood acceleration in progress
        await world.Advance(40);
        Assert.Equal(RopeState.Taut, world.Committed.Presentation.Ropes[0].State);

        // Advance to 60 ticks: weight B has contacted the shelf at y ~ 2.25m, rope transitions to slack
        await world.Advance(20);
        var yB60 = BodyY(world.Committed, idB);
        Assert.InRange(yB60, 2.24f, 2.27f);
        var rope60 = world.Committed.Presentation.Ropes[0];
        Assert.Equal(RopeState.Slack, rope60.State);
        Assert.True(rope60.CurrentLength < rope60.MaximumLength);
        var yA60 = BodyY(world.Committed, idA);

        // Advance to 80 ticks: lighter weight A continues upward on momentum while rope is slack
        await world.Advance(20);
        var rope80 = world.Committed.Presentation.Ropes[0];
        Assert.Equal(RopeState.Slack, rope80.State);
        var yA80 = BodyY(world.Committed, idA);
        Assert.True(yA80 > yA60 + .1f, "Lighter weight must continue rising on momentum while rope is slack.");

        // Advance to 100 ticks: lighter weight A peaks and falls back downward under gravity
        await world.Advance(20);
        var rope100 = world.Committed.Presentation.Ropes[0];
        Assert.Equal(RopeState.Slack, rope100.State);
        var yA100 = BodyY(world.Committed, idA);
        Assert.True(yA100 < yA80, "Lighter weight must fall back downward under gravity after reaching apex.");
        var yB100 = BodyY(world.Committed, idB);
        Assert.InRange(yB100, 2.24f, 2.27f);
    }

    [Fact]
    public async Task AtwoodCutSpanReleasesTensionAtomicallyAndWeightsFallFreely()
    {
        var (construction, idA, _, _, idB, _) = TwoPulleyAtwood(massA: 5f, massB: 5f, heightA: 3.5f, heightB: 3.5f);
        await using var world = await WorkshopIntegrationHarness.Running(construction);
        await world.Advance(30);

        var spanToCut = world.Committed.Presentation.Ropes[1].Id;
        await world.Require(WorkshopCommandKind.CutRope, cut: spanToCut);
        await world.Advance(1);

        var cutRope = world.Committed.Presentation.Ropes.ToArray().Single(r => r.Id == spanToCut);
        Assert.Equal(RopeIntegrity.Cut, cutRope.Integrity);
        Assert.Equal(RopeState.Open, cutRope.State);

        await world.Advance(60);

        var yA = BodyY(world.Committed, idA);
        var yB = BodyY(world.Committed, idB);
        Assert.True(yA < 2.5f, "Weight A must fall freely after cut.");
        Assert.True(yB < 2.5f, "Weight B must fall freely after cut.");
    }

    [Fact]
    public async Task TwoPulleyRouteSurvivesResetAndSaveLoad()
    {
        var (construction, idA, _, _, idB, _) = TwoPulleyAtwood(massA: 2f, massB: 6f, heightA: 2.5f, heightB: 3.5f);
        await using var world = await WorkshopIntegrationHarness.Running(construction);
        await world.Advance(30);

        var movedB = BodyY(world.Committed, idB);
        Assert.True(movedB < 3.4f);

        await world.Reset();

        var resetA = BodyY(world.Committed, idA);
        var resetB = BodyY(world.Committed, idB);
        Assert.InRange(resetA, 2.495f, 2.505f);
        Assert.InRange(resetB, 3.495f, 3.505f);

        var saved = new WorkshopSavedConstruction(world.Construction, new(10));
        var bytes = WorkshopSaveCodec.Encode(saved);
        var loaded = WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes);

        Assert.Equal(construction.Connections, loaded.Construction.Connections);
        Assert.Equal(4, loaded.Construction.Instances.Count);

        await using var loadedWorld = await WorkshopIntegrationHarness.Running(loaded.Construction);
        await loadedWorld.Advance(30);
        var loadedMovedB = BodyY(loadedWorld.Committed, idB);
        Assert.InRange(MathF.Abs(loadedMovedB - movedB), 0f, .01f);
    }

    [Theory]
    [InlineData(0.04f)]
    [InlineData(200.5f)]
    public void MalformedGuideLengthsRefuseAtomically(float length)
    {
        WorkshopConnection Edge(ulong a, ulong b, float len) =>
            new(new(a), PortId.Tie, new(b), PortId.Tie, WorkshopConnectionDomain.Rope, len);
        RopeAttachmentRole Role(RopeTopology.Endpoint endpoint) =>
            endpoint.Body.Value is 2 or 3 ? RopeAttachmentRole.Guide : RopeAttachmentRole.Load;

        Assert.Throws<WorkshopEditRejectedException>(() =>
            RopeTopology.Compile([Edge(1, 2, 1f), Edge(2, 3, length), Edge(3, 4, 1f)], Role));
    }

    [Fact]
    public void MalformedGuideTopologyBranchingCycleAndGuideEndpointsRefuse()
    {
        WorkshopConnection Edge(ulong a, ulong b) =>
            new(new(a), PortId.Tie, new(b), PortId.Tie, WorkshopConnectionDomain.Rope, 1f);
        RopeAttachmentRole Role(RopeTopology.Endpoint endpoint) =>
            endpoint.Body.Value is 2 or 3 ? RopeAttachmentRole.Guide : RopeAttachmentRole.Load;

        var incompleteRoute = Assert.Single(RopeTopology.Compile([Edge(1, 2)], Role));
        Assert.False(incompleteRoute.Complete);

        Assert.Throws<WorkshopEditRejectedException>(() =>
            RopeTopology.Compile([Edge(1, 2), Edge(2, 3), Edge(2, 4)], Role));

        Assert.Throws<WorkshopEditRejectedException>(() =>
            RopeTopology.Compile([Edge(2, 3), Edge(3, 2)], Role));
    }

    [Fact]
    public async Task WorkshopDeclaredPresentationAdvancesPulleyWheelRotationAngle()
    {
        var pulley = WorkshopInput.Generic(new("pulley"), new(1), 0f, 0f, 0f, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(pulley));
        var reads = await WorkshopIntegrationHarness.Observe(construction, 1);
        var read = reads[0];
        var pres = new PhysicsPresentationRead(read.Presentation.Actuators.ToArray(), read.Presentation.Loads.ToArray(),
            read.Presentation.Logic.ToArray(), read.Presentation.Ropes.ToArray(), [new(new(1), 0.8f, 2.0f)]);
        var response = new WorkshopResponse(default, WorkshopResponseKind.Read, new(WorkshopCommandOutcome.Applied, WorkshopRejection.None),
            WorkshopSimulationPhase.Running, read with { Presentation = pres }, new(101, 206), new(1), new(1), new(1), new(1), new(read.Tick.Value));
        var compiled = CompiledSceneFixture.Compile(construction);
        var evaluator = new WorkshopDeclaredPresentation(compiled, response);
        var samples = evaluator.Advance(AnimationTestTime.Seconds(read.Tick.Value / 120.0));
        var wheelSample = Assert.Single(samples, s => s.Owner == new GpuBodyId(1) && s.Binding == 0);
        var angle = BitConverter.UInt32BitsToSingle(wheelSample.Value.CanonicalBits);
        Assert.Equal(2.0f, angle);
    }

    private static float BodyY(WorkshopRead read, GpuBodyId id)
    {
        Assert.True(read.Bodies.TryGet(id, out var value));
        return (value.Body.Cell.Y + value.Body.Local.Y) / 16f;
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
