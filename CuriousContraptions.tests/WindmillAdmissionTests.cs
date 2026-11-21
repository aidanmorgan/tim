using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class WindmillAdmissionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnfamiliarRepeatedCapturesKeepExactFramesAndHingesWhenReordered(bool reverse)
    {
        var entry = TestBundle.Entry(new("windmill"));
        var template = entry.Mechanical!.Hinges[0];
        template = template with { MovingBody = template.MovingBody with
            { Colliders = [new(CuriousContraptions.Abstractions.ColliderShapeKind.Sphere, new(.05f), Vector3.Zero, Quaternion.Identity, new(0, .3f, 0, .1f), 0)] } };
        var hinges = new[] {
            template with { BaseFrame = template.BaseFrame with { Position = new(-.12f, 0, 2) } },
            template with { BaseFrame = template.BaseFrame with { Position = new(-.12f, -.25f, 0) } },
            template with { BaseFrame = template.BaseFrame with { Position = new(-.12f, .25f, 0) } }
        };
        var firstIndex = reverse ? 2 : 1;
        var secondIndex = reverse ? 1 : 2;
        if (reverse) (hinges[1], hinges[2]) = (hinges[2], hinges[1]);
        var original = entry.Mechanical.RotaryAirflowCaptures[0];
        LocalColliderReference[] exclusions = [new(new(1), new(0)), new(new(2), new(0)), new(new(3), new(0))];
        var first = original with { Id = new(17), Coordinate = new(CoordinateKind.Revolute, firstIndex),
            Samples = [new(new(-.12f, -.27f, 0), .3f), new(new(-.12f, -.23f, 0), .7f)],
            InternalOccluders = exclusions, Response = MechanicalValue.Fixed(1), MaximumPitch = MechanicalValue.Fixed(.3f) };
        var second = original with { Id = new(53), Frame = new(secondIndex + 1),
            Coordinate = new(CoordinateKind.Revolute, secondIndex),
            Samples = [new(new(0, -.02f, 0), .7f), new(new(0, .02f, 0), .3f)],
            InternalOccluders = exclusions, Response = MechanicalValue.Fixed(1.5f), MaximumPitch = MechanicalValue.Fixed(.5f) };
        var part = new PartId("unfamiliar_capture_pair");
        entry = entry with { Metadata = entry.Metadata with { Id = part.Value },
            Footprint = new(4, 3, 0, 3),
            Colliders = [new(CuriousContraptions.Abstractions.ColliderShapeKind.Sphere, new(.05f), new(0, -2, 0), Quaternion.Identity, new(0, .3f, 0, .1f), 0)],
            Ports = [], Events = [],
            Presentation = entry.Presentation with { Bindings = [], Bodies = [
                new("Visual/Unrelated", new(1), Vector3.Zero), new("Visual/First", new(2), Vector3.Zero),
                new("Visual/Second", new(3), Vector3.Zero)] },
            Mechanical = entry.Mechanical with { Hinges = hinges,
                RotaryAirflowCaptures = reverse ? [second, first] : [first, second],
                CoordinateTravelObservers = [] } };
        Assert.Empty(BundleValidator.Validate([new("capture pair", entry)]));
        await using var h = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        using var physicsScope = DeclaredPhysics.Scoped([entry]);
        using var portsScope = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(part, entry.Ports)]));
        var pair = WindmillIntegrationTests.Windmill() with { PartId = part };
        await h.Require(WorkshopCommandKind.Construct, TennisBallTests.Scene(FanIntegrationTests.Fan(), pair));
        await h.Run(); await h.Advance(1200);
        var ids = h.Construction.Instances.Identities[pair.Id];
        float Spin(int index)
        {
            Assert.True(h.Committed.Bodies.TryGet(ids.Hinges[index].Body, out var body));
            return body.AngularVelocity.X;
        }
        Assert.InRange(Spin(firstIndex), 4.4f, 4.6f);
        Assert.InRange(Spin(secondIndex), 6.65f, 6.85f);
        Assert.Equal(0, Spin(0));
        var rows = h.Simulation.Scene.Physics.RotaryAirflowCaptures.ToArray();
        Assert.Equal(ids.Hinges[firstIndex].Joint, rows.Single(r => r.Id == ids.RotaryAirflowCaptures[new(17)]).Coordinate);
        Assert.Equal(ids.Hinges[secondIndex].Body, rows.Single(r => r.Id == ids.RotaryAirflowCaptures[new(53)]).Frame);
        Assert.True(h.Committed.Airflow[0].Work.Value > 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NullObserverCollectionIsABundleRefusalWithOrWithoutEvents(bool withEvent)
    {
        var entry = TestBundle.Entry(new("windmill"));
        entry = entry with { Events = withEvent ? entry.Events : [],
            Mechanical = entry.Mechanical! with { CoordinateTravelObservers = null! } };
        Assert.Contains(BundleValidator.Validate([new("null observers", entry)]),
            error => error.Code == BundleErrorCode.OutOfRange && error.Detail.Contains("CoordinateTravel", StringComparison.Ordinal));
        var empty = entry with { Events = [],
            Mechanical = entry.Mechanical with { CoordinateTravelObservers = [] } };
        Assert.Empty(BundleValidator.Validate([new("empty observers", empty)]));
        Assert.NotEmpty(BundleValidator.Validate([new("unknown observer",
            empty with { Events = TestBundle.Entry(new("windmill")).Events })]));
    }

    public enum CaptureDamage { NullSingle, NullMultiple, DefaultSamples, DefaultExclusions }

    [Theory]
    [InlineData(CaptureDamage.NullSingle)]
    [InlineData(CaptureDamage.NullMultiple)]
    [InlineData(CaptureDamage.DefaultSamples)]
    [InlineData(CaptureDamage.DefaultExclusions)]
    public void InvalidCaptureRowsRefuseBeforeSortingOrSizing(CaptureDamage damage)
    {
        var compiled = CompiledSceneFixture.Compile(TennisBallTests.Scene(WindmillIntegrationTests.Windmill()));
        var physics = compiled.Physics;
        var capture = Assert.Single(physics.RotaryAirflowCaptures.ToArray());
        RotaryAirflowCaptureDeclaration[] captures = damage switch
        {
            CaptureDamage.NullSingle => [null!],
            CaptureDamage.NullMultiple => [capture, null!],
            CaptureDamage.DefaultSamples => [capture with { Samples = default }],
            CaptureDamage.DefaultExclusions => [capture with { InternalOccluders = default }],
            _ => throw new ArgumentOutOfRangeException(nameof(damage))
        };
        Assert.ThrowsAny<ArgumentException>(() => new PhysicsSceneDeclaration(physics.Document, physics.NextIdentity,
            physics.Bodies, physics.Colliders, physics.Materials, [], [], revolutes: physics.Revolutes,
            rotaryAirflowCaptures: captures));
    }

    [Fact]
    public async Task PreResetCandidateEpochCannotSpendOrPublishIntoFreshRun()
    {
        await using var h = await WorkshopIntegrationHarness.Running(
            TennisBallTests.Scene(FanIntegrationTests.Fan(), WindmillIntegrationTests.Windmill()));
        await h.Advance(30);
        var staleEpoch = h.Committed.Epoch;
        await h.Reset(); await h.Run();
        var before = h.Committed;
        var events = h.Records.Count;
        h.ChangeNextCandidate = world =>
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(world.AsSpan(PhysicsGpuAbi.HeaderEpochOffset), staleEpoch.Value);
        Assert.Equal(WorkshopRejection.InvalidRead, (await h.AdvanceOne()).Reason);
        Assert.Equal(before.Epoch, h.Committed.Epoch);
        Assert.Equal(before.Tick, h.Committed.Tick);
        Assert.Equal(before.Revision, h.Committed.Revision);
        foreach (var id in new[] { h.Construction.Instances.Identities[new(2)].Hinges[0].Body })
        {
            Assert.True(before.Bodies.TryGet(id, out var previous));
            Assert.True(h.Committed.Bodies.TryGet(id, out var current));
            Assert.Equal(previous, current);
        }
        Assert.Equal(before.EnergyStores[0], h.Committed.EnergyStores[0]);
        Assert.Equal(before.CoordinateTravelObservers[0], h.Committed.CoordinateTravelObservers[0]);
        Assert.Equal(events, h.Records.Count);
        await h.Reset(); await h.Run(); await h.Advance(360);
        Assert.True(h.Committed.Airflow[0].Work.Value > 0);
        Assert.Single(h.Of(SimulationEventKind.CoordinateTravelReached));
    }

    public enum PortDamage { OutputToOutput, ElectricalDomain, OccupiedInput }

    [Theory]
    [InlineData(PortDamage.OutputToOutput)]
    [InlineData(PortDamage.ElectricalDomain)]
    [InlineData(PortDamage.OccupiedInput)]
    public async Task InvalidPortsLeaveTheInstalledConstructionUntouched(PortDamage damage)
    {
        var conveyor = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("conveyor"), new(3), 0, 4, 4, 0, 0, 0, 1);
        var other = WindmillIntegrationTests.Windmill() with { Id = new(4) };
        var construction = TennisBallTests.Scene(WindmillIntegrationTests.Windmill(), conveyor, other);
        await using var h = await WorkshopIntegrationHarness.Building(construction);
        var good = new WorkshopConnection(new(2), PortId.Drive, conveyor.Id, PortId.DriveIn, WorkshopConnectionDomain.Mechanical);
        var links = damage switch
        {
            PortDamage.OutputToOutput => new[] { good with { Target = other.Id, Input = PortId.Drive } },
            PortDamage.ElectricalDomain => new[] { good with { Domain = WorkshopConnectionDomain.Electrical } },
            PortDamage.OccupiedInput => new[] { good, good with { Source = other.Id } },
            _ => throw new ArgumentOutOfRangeException(nameof(damage))
        };
        var before = h.Committed;
        EditRefusal.Is(WorkshopEditRejection.InvalidConnection, () => h.Command(WorkshopCommandKind.Construct,
            construction with { Revision = new(3), Connections = new(links) }));
        Assert.Equal(construction, h.Construction);
        Assert.Equal(before.Revision, h.Committed.Revision);
        Assert.Equal(before.CoordinateTravelObservers[0], h.Committed.CoordinateTravelObservers[0]);
    }

    public enum ProbePath { ClearBore, Guard, Blade, Miss }

    [Theory]
    [InlineData(ProbePath.ClearBore, .4f, .4f, false)]
    [InlineData(ProbePath.Guard, .78f, .2f, true)]
    [InlineData(ProbePath.Blade, .4f, 0f, true)]
    [InlineData(ProbePath.Miss, 1.1f, 0f, false)]
    public void ActualPackageSolidsContactProbesWhileBoreAndMissRemainClear(ProbePath path, float y, float z, bool contact)
    {
        var probe = TestBundle.Entry(new("tennis"));
        probe = probe with { Mechanical = probe.Mechanical! with
            { MassFrame = MassFrameDeclaration.Constant(.35f, Vector3.Zero, new(.0014f), Quaternion.Identity, 0, Vector3.Zero) } };
        using var physicsScope = DeclaredPhysics.Scoped([probe]);
        // A 6.25 cm probe fits the diagonal bore clearance; zero drag isolates material contact.
        var ball = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("tennis"), new(3), 2, 5 + y, z, 0, 0, 0, 1,
            new([new(new("mass"), .35f), new(new("bounce"), .85f), new(new("radius"), .0625f),
                new(new("buoyancy"), 0), new(new("drag"), 0)]));
        var compiled = CompiledSceneFixture.Compile(TennisBallTests.Scene(WindmillIntegrationTests.Windmill(), ball));
        var guard = Assert.Single(compiled.Physics.Colliders.ToArray(), c => c.Shape == CuriousContraptions.Gpu.ColliderShapeKind.Tube);
        Assert.Equal(.815f, guard.Radius.Value);
        var world = new LawWorld(compiled.Physics, PhysicsGpuAbi.Admission(compiled.Physics, new(1),
            new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1))), SimulationCadence.Hz120);
        world.SetVelocity(ball.Id, new(.5f, 0, 0), Vector3.Zero);
        var touched = false;
        var result = world.Step(480, (_, state) =>
            touched |= Vector3.Distance(state[ball.Id].Velocity, new(.5f, 0, 0)) > .05f);
        // Ten percent of the launch speed discriminates a solid collision from a clear passage.
        Assert.Equal(contact, touched);
        if (!contact) Assert.True(result[ball.Id].Position.X > 3.5f, $"{path} must clear the complete rotor/housing.");
    }

    public enum Damage { EmptySamples, NegativeWeight, WrongTotal, NonFiniteSample, MissingFrame,
        MissingHinge, NonRevolute, ForeignExclusion, DuplicateId, DuplicateWriter, AuthoredDamper }

    [Theory]
    [InlineData(Damage.EmptySamples)]
    [InlineData(Damage.NegativeWeight)]
    [InlineData(Damage.WrongTotal)]
    [InlineData(Damage.NonFiniteSample)]
    [InlineData(Damage.MissingFrame)]
    [InlineData(Damage.MissingHinge)]
    [InlineData(Damage.NonRevolute)]
    [InlineData(Damage.ForeignExclusion)]
    [InlineData(Damage.DuplicateId)]
    [InlineData(Damage.DuplicateWriter)]
    [InlineData(Damage.AuthoredDamper)]
    public void MalformedCaptureCannotEnterBundle(Damage damage)
    {
        var entry = TestBundle.Entry(new("windmill"));
        var capture = Assert.Single(entry.Mechanical!.RotaryAirflowCaptures);
        var changed = damage switch
        {
            Damage.EmptySamples => capture with { Samples = [] },
            Damage.NegativeWeight => capture with { Samples = [new(Vector3.Zero, -1)] },
            Damage.WrongTotal => capture with { Samples = [new(Vector3.Zero, .5f)] },
            Damage.NonFiniteSample => capture with { Samples = [new(new(float.NaN, 0, 0), 1)] },
            Damage.MissingFrame => capture with { Frame = new(99) },
            Damage.MissingHinge => capture with { Coordinate = new(CoordinateKind.Revolute, 99) },
            Damage.NonRevolute => capture with { Coordinate = new(CoordinateKind.Prismatic, 0) },
            Damage.ForeignExclusion => capture with { InternalOccluders = [new(new(99), new(0))] },
            _ => capture
        };
        var captures = damage switch
        {
            Damage.DuplicateId => new[] { changed, changed },
            Damage.DuplicateWriter => new[] { changed, changed with { Id = new(3) } },
            _ => new[] { changed }
        };
        entry = entry with { Mechanical = entry.Mechanical with { RotaryAirflowCaptures = captures } };
        if (damage == Damage.AuthoredDamper)
            entry = entry with { Mechanical = entry.Mechanical with
                { Hinges = [entry.Mechanical.Hinges[0] with { Damping = MechanicalValue.Fixed(1) }] } };
        Assert.NotEmpty(BundleValidator.Validate([new("malformed capture", entry)]));
    }

    [Theory]
    [InlineData(.1f, true)]
    [InlineData(4f, true)]
    [InlineData(0f, false)]
    [InlineData(4.1f, false)]
    [InlineData(float.NaN, false)]
    [InlineData(float.PositiveInfinity, false)]
    public void ResponseAdmitsOnlyCanonicalFiniteRange(float response, bool admitted)
    {
        if (!admitted)
        {
            Assert.Throws<ArgumentException>(() => WindmillIntegrationTests.Windmill(response)
                .Validate(RuntimeTestServices.Bundle));
            return;
        }
        var scene = CompiledSceneFixture.Compile(TennisBallTests.Scene(WindmillIntegrationTests.Windmill(response)));
        var capture = Assert.Single(scene.Physics.RotaryAirflowCaptures.ToArray());
        Assert.Equal(response, capture.Response);
        Assert.Equal(.05f, capture.CutInForce);
        Assert.Equal(12, capture.MaximumRate);
    }

    public enum MissingCapability { Captures, Observers, Samples, Exclusions }

    [Theory]
    [InlineData(MissingCapability.Captures)]
    [InlineData(MissingCapability.Observers)]
    [InlineData(MissingCapability.Samples)]
    [InlineData(MissingCapability.Exclusions)]
    public async Task MissingCurrentWireCapabilitiesRefuseWithoutReplacingInstalledWorld(MissingCapability missing)
    {
        var construction = TennisBallTests.Scene(FanIntegrationTests.Fan(), WindmillIntegrationTests.Windmill());
        await using var h = await WorkshopIntegrationHarness.Building(construction);
        var command = h.Command(WorkshopCommandKind.Construct, construction with { Revision = new(3) });
        var bytes = WorkshopWireFlatBuffers.EncodeCommand(command);
        var buffer = new Google.FlatBuffers.ByteBuffer(bytes);
        var envelope = new Google.FlatBuffers.Table(4 + buffer.GetInt(4), buffer);
        var scene = new Google.FlatBuffers.Table(envelope.__indirect(envelope.bb_pos + envelope.__offset(26)), buffer);
        var field = missing switch { MissingCapability.Observers => 48, MissingCapability.Captures => 47,
            MissingCapability.Samples => 4, MissingCapability.Exclusions => 5, _ => throw new ArgumentOutOfRangeException(nameof(missing)) };
        if (missing is MissingCapability.Samples or MissingCapability.Exclusions)
            scene = new(scene.__indirect(scene.__vector(scene.__offset(4 + 47 * 2))), buffer);
        var vtable = scene.bb_pos - buffer.GetInt(scene.bb_pos);
        Assert.NotEqual(0, scene.__offset(4 + field * 2));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(vtable + 4 + field * 2), 0);
        var before = h.Committed;
        var fault = Assert.Throws<WireFaultException>(() => h.Router.Dispatch(bytes));
        Assert.Contains(fault.Reason, new[] { WireFaultReason.MissingField, WireFaultReason.VerifierRejected });
        Assert.Equal(construction, h.Construction);
        Assert.Equal(before.Revision, h.Committed.Revision);
        Assert.Equal(before.EnergyStores[0], h.Committed.EnergyStores[0]);
        Assert.Equal(before.CoordinateTravelObservers[0], h.Committed.CoordinateTravelObservers[0]);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await h.Dispatch(command)).Result.Outcome);
        await h.Run(); await h.Advance(30);
        Assert.True(WindmillIntegrationTests.Coordinate(h).Speed > 1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidCandidateTravelNeverPublishesAnOccurrenceOrSpendsEnergy(bool falseOccurrence)
    {
        await using var h = await WorkshopIntegrationHarness.Running(
            TennisBallTests.Scene(FanIntegrationTests.Fan(), WindmillIntegrationTests.Windmill()));
        await h.Advance(1);
        var before = h.Committed;
        h.ChangeNextCandidate = world =>
        {
            var row = PhysicsGpuAbi.Layout(world).CoordinateTravelOffset(0);
            var offset = row + (falseOccurrence ? PhysicsGpuAbi.CoordinateTravelOrdinalOffset : PhysicsGpuAbi.CoordinateTravelAccumulatedOffset);
            if (falseOccurrence) System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(world.AsSpan(offset), uint.MaxValue);
            else System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(world.AsSpan(offset), float.NaN);
        };
        Assert.Equal(WorkshopRejection.InvalidRead, (await h.AdvanceOne()).Reason);
        Assert.Equal(before.Tick, h.Committed.Tick);
        Assert.Equal(before.EnergyStores[0], h.Committed.EnergyStores[0]);
        Assert.Equal(before.CoordinateTravelObservers[0], h.Committed.CoordinateTravelObservers[0]);
        Assert.Empty(h.Of(SimulationEventKind.CoordinateTravelReached));
        await h.Reset(); await h.Run(); await h.Advance(360);
        Assert.Single(h.Of(SimulationEventKind.CoordinateTravelReached));
    }
}
