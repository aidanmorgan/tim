using System.Buffers.Binary;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;
using Google.FlatBuffers;
using Wire = CuriousContraptions.Wire;

namespace CuriousContraptions.Tests;

public sealed class BodyOwnershipTests
{
    public enum Damage { RootNotSelfOwned, MissingOwner, NonRootOwner, ZeroOwner, DuplicateBody,
        CaptureFrame, CaptureHinge, CaptureExclusion, ObserverHinge, SourceFrame, SourceExclusion, SourceCoordinate }

    [Theory]
    [InlineData(Damage.RootNotSelfOwned)]
    [InlineData(Damage.MissingOwner)]
    [InlineData(Damage.NonRootOwner)]
    [InlineData(Damage.ZeroOwner)]
    [InlineData(Damage.DuplicateBody)]
    [InlineData(Damage.CaptureFrame)]
    [InlineData(Damage.CaptureHinge)]
    [InlineData(Damage.CaptureExclusion)]
    [InlineData(Damage.ObserverHinge)]
    [InlineData(Damage.SourceFrame)]
    [InlineData(Damage.SourceExclusion)]
    [InlineData(Damage.SourceCoordinate)]
    public async Task MalformedCompiledOwnershipRefusesAtomically(Damage damage)
    {
        var other = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("windmill"), new(3), 3, 8, 0, 0, 0, 0, 1);
        var shaftFan = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("fan"), new(4), 0, 12, 0, 0, 0, 0, 1,
            FanIntegrationTests.Fan(FanIntegrationTests.Supply.Shaft).Parameters);
        var construction = TennisBallTests.Scene(FanIntegrationTests.Fan(), WindmillIntegrationTests.Windmill(), other, shaftFan);
        await using var h = await WorkshopIntegrationHarness.Building(construction);
        var before = h.Committed;
        var installed = h.Simulation.Scene;
        var events = h.Records.Count;
        var command = h.Command(WorkshopCommandKind.Construct, construction with { Revision = new(3) });
        var bytes = WorkshopWireFlatBuffers.EncodeCommand(command);
        var wire = new Candidate(bytes);
        var physics = command.Scene!.Physics;
        var foreignHinge = h.Construction.Instances.Identities[other.Id].Hinges[0];
        var foreignCollider = physics.Colliders.ToArray().First(c => c.Body == other.Id).Id;
        var root = Array.FindIndex(physics.Bodies.ToArray(), b => b.Id == new GpuBodyId(2));
        var child = Array.FindIndex(physics.Bodies.ToArray(), b => b.Id == h.Construction.Instances.Identities[new(2)].Hinges[0].Body);
        switch (damage)
        {
            case Damage.RootNotSelfOwned: wire.Scalar(wire.Row("bodies", root), WireSchemaLayout.VtableOffset<Wire.CapabilityRigidBodyDeclarationRecord>("owner"), other.Id.Value); break;
            case Damage.MissingOwner: wire.Scalar(wire.Row("bodies", child), WireSchemaLayout.VtableOffset<Wire.CapabilityRigidBodyDeclarationRecord>("owner"), ulong.MaxValue); break;
            case Damage.NonRootOwner: wire.Scalar(wire.Row("bodies", child), WireSchemaLayout.VtableOffset<Wire.CapabilityRigidBodyDeclarationRecord>("owner"), foreignHinge.Body.Value); break;
            case Damage.ZeroOwner: wire.Scalar(wire.Row("bodies", child), WireSchemaLayout.VtableOffset<Wire.CapabilityRigidBodyDeclarationRecord>("owner"), 0); break;
            case Damage.DuplicateBody: wire.Scalar(wire.Row("bodies", child), WireSchemaLayout.VtableOffset<Wire.CapabilityRigidBodyDeclarationRecord>("id"), 2); break;
            case Damage.CaptureFrame: wire.Scalar(wire.Row("rotary_airflow_captures"), WireSchemaLayout.VtableOffset<Wire.RotaryAirflowCaptureDeclarationRecord>("frame"), other.Id.Value); break;
            case Damage.CaptureHinge: wire.Scalar(wire.Row("rotary_airflow_captures"), WireSchemaLayout.VtableOffset<Wire.RotaryAirflowCaptureDeclarationRecord>("coordinate"), foreignHinge.Joint.Value); break;
            case Damage.CaptureExclusion: wire.VectorValue(wire.Row("rotary_airflow_captures"), WireSchemaLayout.VtableOffset<Wire.RotaryAirflowCaptureDeclarationRecord>("internal_occluders"), foreignCollider.Value); break;
            case Damage.ObserverHinge: wire.Scalar(wire.Row("coordinate_travel_observers"), WireSchemaLayout.VtableOffset<Wire.CoordinateTravelDeclarationRecord>("coordinate"), foreignHinge.Joint.Value); break;
            case Damage.SourceFrame: wire.Scalar(wire.Row("airflow_sources"), WireSchemaLayout.VtableOffset<Wire.AirflowSourceDeclarationRecord>("frame"), other.Id.Value); break;
            case Damage.SourceExclusion: wire.VectorValue(wire.Row("airflow_sources"), WireSchemaLayout.VtableOffset<Wire.AirflowSourceDeclarationRecord>("internal_occluders"), foreignCollider.Value); break;
            case Damage.SourceCoordinate:
                var sourceIndex = Array.FindIndex(physics.AirflowSources.ToArray(), source => source.Owner == shaftFan.Id);
                wire.Scalar(wire.Row("airflow_sources", sourceIndex), WireSchemaLayout.VtableOffset<Wire.AirflowSourceDeclarationRecord>("coordinate"), foreignHinge.Joint.Value);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(damage));
        }
        var fault = Assert.Throws<WireFaultException>(() => h.Router.Dispatch(bytes));
        Assert.Equal(WireFaultReason.OutOfRange, fault.Reason);
        Assert.Same(installed, h.Simulation.Scene);
        Assert.Equal(before.Epoch, h.Committed.Epoch);
        Assert.Equal(before.Revision, h.Committed.Revision);
        Assert.Equal(before.Tick, h.Committed.Tick);
        Assert.Equal(before.EnergyStores[0], h.Committed.EnergyStores[0]);
        Assert.Equal(before.CoordinateTravelObservers[0], h.Committed.CoordinateTravelObservers[0]);
        Assert.Equal(events, h.Records.Count);
        foreach (var body in physics.Bodies.ToArray().Where(body => body.Motion == RigidMotionKind.Dynamic))
        {
            Assert.True(before.Bodies.TryGet(body.Id, out var prior));
            Assert.True(h.Committed.Bodies.TryGet(body.Id, out var current));
            Assert.Equal(prior, current);
        }
        var installedResult = (await h.Dispatch(command)).Result;
        Assert.True(installedResult.Outcome == WorkshopCommandOutcome.Applied,
            installedResult + "; " + string.Join("\n", h.Simulation.Diagnostics.Snapshot()));
        await h.Run(); await h.Advance(120);
        Assert.True(h.Committed.Airflow[0].Work.Value > 0);
    }

    [Fact]
    public async Task ConsistentlyReauthoredChildOwnershipIsAValidReplacement()
    {
        var other = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("windmill"), new(3), 3, 8, 0, 0, 0, 0, 1);
        var construction = TennisBallTests.Scene(WindmillIntegrationTests.Windmill(), other);
        await using var h = await WorkshopIntegrationHarness.Building(construction);
        var child = h.Construction.Instances.Identities[new(2)].Hinges[0].Body;
        var command = h.Command(WorkshopCommandKind.Construct, construction with { Revision = new(3) });
        var original = command.Scene!;
        var physics = original.Physics;
        var bodies = physics.Bodies.ToArray().Select(body => body.Id == child ?
            body with { Owner = other.Id, AngularVelocity = new(2, 0, 0) } : body).ToArray();
        var captures = physics.RotaryAirflowCaptures.ToArray().Select(capture => capture.Owner == new GpuBodyId(2) ?
            capture with { Owner = other.Id, Frame = other.Id,
                InternalOccluders = [.. physics.Colliders.ToArray().Where(c => c.Body == child || c.Body == other.Id).Select(c => c.Id)] } : capture).ToArray();
        var observers = physics.CoordinateTravelObservers.ToArray().Select(observer => observer.Owner == new GpuBodyId(2) ?
            observer with { Owner = other.Id, Threshold = .5f } : observer).ToArray();
        var replacement = new PhysicsSceneDeclaration(physics.Document, physics.NextIdentity, bodies, physics.Colliders,
            physics.Materials, [], [], revolutes: physics.Revolutes, rotaryAirflowCaptures: captures,
            coordinateTravelObservers: observers);
        command = command with { Scene = new WorkshopCompiledScene(original.Identity, original.Revision, original.Settings,
            replacement, original.Activation, [], original.Owners, []) };
        var installedResult = (await h.Dispatch(command)).Result;
        Assert.True(installedResult.Outcome == WorkshopCommandOutcome.Applied,
            installedResult + "; " + string.Join("\n", h.Simulation.Diagnostics.Snapshot()));
        Assert.Equal(other.Id, h.Simulation.Scene.Physics.Bodies.ToArray().Single(b => b.Id == child).Owner);
        await h.Run(); await h.Advance(120);
        Assert.True(h.Committed.Bodies.TryGet(child, out var moving));
        Assert.True(moving.AngularVelocity.X > 0);
        var observed = Enumerable.Range(0, h.Committed.CoordinateTravelObservers.Count)
            .Select(i => h.Committed.CoordinateTravelObservers[i]).Single(row => row.Threshold == .5f);
        Assert.Equal(other.Id, observed.Owner);
        Assert.Equal(CoordinateTravelPhase.Reached, observed.Phase);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CyclicGuidesAndRedundantRotorSupportsDoNotDetermineOwnership(bool reverse)
    {
        var scene = new LawScene(false);
        var a = scene.Box(new(-1, 5, 0), new(.1f), 1, LawMaterial.Domino, velocity: new(3, 1, 0), gravity: 0);
        var b = scene.Box(new(0, 5, 0), new(.1f), 1, LawMaterial.Domino, gravity: 0);
        var c = scene.Box(new(1, 5, 0), new(.1f), 1, LawMaterial.Domino, gravity: 0);
        var pairs = new List<(GpuBodyId A, GpuBodyId B, float Arm)> { (a, b, .5f), (b, c, .5f), (a, c, 1f) };
        if (reverse) pairs.Reverse();
        foreach (var pair in pairs)
            scene.Prismatic(pair.A, pair.B, new(new(pair.Arm, 0, 0), System.Numerics.Quaternion.Identity),
                new(new(-pair.Arm, 0, 0), System.Numerics.Quaternion.Identity), -2, 2, 0, 0, ConnectedCollision.Disabled);
        var support = scene.StaticBox(new(5, 8, 0), new(.05f));
        var rotor = scene.Sphere(new(5, 8, 0), .05f, 1, LawMaterial.Domino, default, new(0, 0, 2), 0, 0);
        scene.Own(rotor, support);
        scene.DeclareMassFrame(rotor, new(.245f));
        var hinge = scene.Revolute(support, rotor, ConstraintFrame.Identity, ConstraintFrame.Identity,
            -MathF.PI, MathF.PI, 0, travel: RevoluteTravel.Continuous);
        var redundantSupport = scene.StaticBox(new(5, 8, 0), new(.05f));
        scene.Revolute(redundantSupport, rotor, ConstraintFrame.Identity, ConstraintFrame.Identity,
            -MathF.PI, MathF.PI, 0, travel: RevoluteTravel.Continuous);
        scene.RotaryCapture(support, support, hinge, 1);
        var primitive = scene.Admit().Scene;
        var bodies = primitive.Bodies.ToArray();
        if (reverse) Array.Reverse(bodies);
        await using var h = await WorkshopIntegrationHarness.Building(TennisBallTests.Scene(WindmillIntegrationTests.Windmill()));
        var command = h.Command(WorkshopCommandKind.Construct, h.Construction with { Revision = new(3) });
        var prior = command.Scene!;
        var foreignFrame = Assert.Single(primitive.RotaryAirflowCaptures.ToArray()) with { Frame = redundantSupport };
        Assert.ThrowsAny<ArgumentException>(() => new PhysicsSceneDeclaration(prior.Physics.Document, primitive.NextIdentity,
            bodies, primitive.Colliders, primitive.Materials, [], [], prismatics: primitive.Prismatics,
            revolutes: primitive.Revolutes, rotaryAirflowCaptures: [foreignFrame]));
        var physics = new PhysicsSceneDeclaration(prior.Physics.Document, primitive.NextIdentity + 1,
            bodies, primitive.Colliders, primitive.Materials, [], [], prismatics: primitive.Prismatics,
            revolutes: primitive.Revolutes, rotaryAirflowCaptures: primitive.RotaryAirflowCaptures,
            coordinateTravelObservers: [new(new(primitive.NextIdentity), support, hinge, .5f)]);
        command = command with { Scene = new WorkshopCompiledScene(prior.Identity, prior.Revision, prior.Settings,
            physics, prior.Activation, [], bodies.Where(body => body.Id == body.Owner).Select(body => body.Id), []) };
        var installedResult = (await h.Dispatch(command)).Result;
        Assert.True(installedResult.Outcome == WorkshopCommandOutcome.Applied,
            installedResult + "; " + string.Join("\n", h.Simulation.Diagnostics.Snapshot()));
        await h.Run(); await h.Advance(120);
        foreach (var (id, initialX) in new[] { (a, -1f), (b, 0f), (c, 1f) })
        {
            Assert.True(h.Committed.Bodies.TryGet(id, out var body));
            var x = (body.Body.Cell.X + body.Body.Local.X) / 16f;
            // Same 2 cm visible-clearance envelope as the compatible guide regression.
            Assert.InRange(x - initialX, .98f, 1.02f);
            Assert.InRange((body.Body.Cell.Z + body.Body.Local.Z) / 16f, -.02f, .02f);
        }
        Assert.Equal(CoordinateTravelPhase.Reached, h.Committed.CoordinateTravelObservers[0].Phase);
        Assert.Single(h.Of(SimulationEventKind.CoordinateTravelReached));
    }

    private sealed class Candidate
    {
        private readonly byte[] _bytes;
        private readonly ByteBuffer _buffer;
        private readonly Table _scene;

        public Candidate(byte[] bytes)
        {
            _bytes = bytes;
            _buffer = new(bytes);
            var envelope = new Table(4 + _buffer.GetInt(4), _buffer);
            _scene = new(envelope.__indirect(envelope.bb_pos + envelope.__offset(WireSchemaLayout.VtableOffset<Wire.WorkshopCommand>("scene"))), _buffer);
        }

        public Table Row(string field, int index = 0)
        {
            var vector = _scene.__vector(_scene.__offset(WireSchemaLayout.VtableOffset<Wire.CompiledSceneRecord>(field)));
            return new(_scene.__indirect(vector + index * sizeof(int)), _buffer);
        }

        public void Scalar(Table row, int fieldOffset, ulong value)
        {
            var offset = row.__offset(fieldOffset);
            Assert.NotEqual(0, offset);
            BinaryPrimitives.WriteUInt64LittleEndian(_bytes.AsSpan(row.bb_pos + offset), value);
        }

        public void VectorValue(Table row, int fieldOffset, ulong value) =>
            BinaryPrimitives.WriteUInt64LittleEndian(_bytes.AsSpan(row.__vector(row.__offset(fieldOffset))), value);
    }
}
