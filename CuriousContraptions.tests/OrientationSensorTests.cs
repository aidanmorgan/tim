using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>The orientation-threshold sensor is declaration data compiled only for a wired Domino; the network routes it like any other source.</summary>
public sealed class OrientationSensorTests
{
    private const double Upright = -0.46 + .55;
    private static readonly WorkshopGpuProfile Profile = new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1));
    private static readonly WorkshopConnection Wire = new(new(2), WorkshopSocket.ActivationOut, new(3), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);
    private static WorkshopConstruction Construction(bool wired = true) => new(new(1), WorkshopCadenceSettings.Default(),
        new(WorkshopInput.Basketball(new(1), -.4, 3, 0, 0, 0, 0, 1),
            WorkshopInput.Domino(new(2), 0, Upright, 0, 0, 0, 0, 1),
            WorkshopInput.Lamp(new(3), 3, 1, 0, 0, 0, 0, 1)),
        Connections: wired ? new(Wire) : WorkshopConnections.Empty);
    private static GpuColliderId ColliderOf(PhysicsSceneDeclaration scene, GpuBodyId body) => scene.Colliders.ToArray().Single(c => c.Body == body).Id;
    private static Span<byte> Record(byte[] bytes, int slot) => bytes.AsSpan(PhysicsGpuAbi.OrientationSensorsOffset + slot * PhysicsGpuAbi.OrientationSensorBytes, PhysicsGpuAbi.OrientationSensorBytes);
    private static void Fire(byte[] bytes, int slot, uint ordinal)
    {
        var record = Record(bytes, slot);
        BinaryPrimitives.WriteUInt32LittleEndian(record[32..], 1); BinaryPrimitives.WriteUInt32LittleEndian(record[36..], ordinal);
    }

    // A tick-N candidate over an unchanged world: header time advanced and a minimal valid motion description (bodies at rest).
    private static byte[] Candidate(byte[] source, uint tick)
    {
        var bytes = (byte[])source.Clone(); var span = bytes.AsSpan();
        const uint steps = 4; var ordinal = tick * steps;
        BinaryPrimitives.WriteUInt64LittleEndian(span[40..], tick);
        BinaryPrimitives.WriteUInt32LittleEndian(span[88..], ordinal);
        var motion = span.Slice(PhysicsGpuAbi.MotionOffset, PhysicsMotionRead.ByteLength); motion.Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(motion[4..], steps);
        BinaryPrimitives.WriteUInt32LittleEndian(motion[8..], ordinal - steps);
        BinaryPrimitives.WriteUInt32LittleEndian(motion[12..], ordinal);
        var count = 0;
        var bodyCount = BinaryPrimitives.ReadUInt32LittleEndian(span[12..]);
        for (var slot = 0; slot < bodyCount; slot++)
        {
            var record = span.Slice(PhysicsGpuAbi.BodiesOffset + slot * PhysicsGpuAbi.BodyBytes, PhysicsGpuAbi.BodyBytes);
            if ((RigidMotionKind)BinaryPrimitives.ReadUInt32LittleEndian(record[8..]) != RigidMotionKind.Dynamic) continue;
            for (var s = 0u; s < steps; s++)
            {
                var piece = motion.Slice(PhysicsMotionRead.HeaderBytes + count++ * PhysicsMotionRead.PieceBytes, PhysicsMotionRead.PieceBytes);
                BinaryPrimitives.WriteUInt32LittleEndian(piece, (uint)PhysicsMotionKind.FreePolynomial);
                BinaryPrimitives.WriteUInt32LittleEndian(piece[4..], ordinal - steps + s);
                BinaryPrimitives.WriteUInt32LittleEndian(piece[8..], ordinal - steps + s + 1);
                BinaryPrimitives.WriteUInt32LittleEndian(piece[12..], ordinal - steps + s);
                BinaryPrimitives.WriteUInt16LittleEndian(piece[22..], BitConverter.HalfToUInt16Bits((Half)480));
                record[..8].CopyTo(piece[24..]); record[16..28].CopyTo(piece[32..]);
                record[32..38].CopyTo(piece[48..]); record[40..48].CopyTo(piece[56..]);
            }
        }
        BinaryPrimitives.WriteUInt32LittleEndian(motion, (uint)count);
        return bytes;
    }

    [Fact]
    public void SensorAndSourceNodeCompileOnlyForAWiredDomino()
    {
        var unwired = WorkshopPhysicsCompiler.Compile(Construction(false), new(1, 2));
        Assert.Empty(unwired.OrientationSensors.ToArray());
        Assert.Equal(1, (int)WorkshopActivationCompiler.Compile(Construction(false)).Clear().Count);
        var scene = WorkshopPhysicsCompiler.Compile(Construction(), new(1, 2));
        var sensor = Assert.Single(scene.OrientationSensors.ToArray());
        Assert.Equal(WorkshopPhysicsCompiler.OrientationSensor(new(2)), sensor.Id);
        Assert.Equal(new GpuBodyId(2), sensor.Body);
        Assert.Equal(CanonicalRotation.Identity, sensor.Initial);
        Assert.Equal(WorkshopDomino.ActivationThreshold, sensor.Threshold);
        Assert.Equal((Half)Math.Cos(Math.PI / 8), sensor.Threshold.CosineHalfAngle);
        Assert.Empty(scene.Triggers.ToArray());
        var clear = WorkshopActivationCompiler.Compile(Construction()).Clear();
        Assert.Equal(2, (int)clear.Count);
        Assert.Equal(new GpuBodyId(2), clear[0].Owner); Assert.Equal(ActivationPhase.Clear, clear[0].Phase);
        Assert.Equal(new GpuBodyId(3), clear[1].Owner);
    }

    [Fact]
    public void ThresholdDeclarationAndNodeRulesReject()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OrientationThreshold.Degrees(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => OrientationThreshold.Degrees(3.9));
        Assert.Throws<ArgumentOutOfRangeException>(() => OrientationThreshold.Degrees(179.5));
        Assert.Throws<ArgumentOutOfRangeException>(() => OrientationThreshold.Degrees(180));
        OrientationThreshold.Degrees(4).Validate(); OrientationThreshold.Degrees(179).Validate();
        Assert.True(OrientationThreshold.Degrees(4).CosineHalfAngle < (Half)1);
        Assert.Throws<ArgumentOutOfRangeException>(() => OrientationThreshold.Degrees(double.NaN));
        Assert.Throws<ArgumentException>(() => new OrientationThreshold((Half)1).Validate());
        Assert.Throws<ArgumentException>(() => new OrientationThreshold((Half)0).Validate());
        Assert.Throws<ArgumentException>(() => new OrientationThreshold(Half.NaN).Validate());
        OrientationThreshold.Degrees(45).Validate();
        var scene = WorkshopPhysicsCompiler.Compile(Construction(), new(1, 2));
        var sensor = scene.OrientationSensors[0];
        PhysicsSceneDeclaration Replace(params OrientationSensorDeclaration[] values) => new(scene.Document, scene.NextIdentity,
            scene.Bodies, scene.Colliders, scene.Materials, scene.Sensors, scene.Guides, scene.Triggers, scene.ContactWorks, values);
        Assert.Equal(sensor, Replace(sensor).OrientationSensors[0]);
        Assert.Throws<ArgumentException>(() => Replace(sensor with { Body = new(3) }));
        Assert.Throws<ArgumentException>(() => Replace(sensor with { Body = new(99) }));
        Assert.Throws<ArgumentException>(() => Replace(sensor with { Id = new(scene.NextIdentity) }));
        Assert.Throws<ArgumentException>(() => Replace(sensor with { Id = new(sensor.Body.Value) }));
        Assert.Throws<ArgumentException>(() => Replace(sensor, sensor with { Id = new(sensor.Id.Value + 1) }));
        Assert.Throws<ArgumentException>(() => Replace(sensor with { Initial = new((Half)1, (Half)1, (Half)0, (Half)0) }));
        // A valid rotation that is not the body's admitted rotation is rejected too: the angle is measured from the admitted pose.
        Assert.Throws<ArgumentException>(() => Replace(sensor with { Initial = new((Half)0, (Half)Math.Sqrt(.5), (Half)0, (Half)Math.Sqrt(.5)) }));
        Assert.Throws<ArgumentException>(() => Replace(sensor with { Threshold = new((Half)1) }));
        // Node declarations: an orientation source names exactly its sensor and never a trigger; nothing routes into a source.
        var owner = new GpuBodyId(2); var sensorId = new GpuOrientationSensorId(sensor.Id.Value);
        var lamp = new ActivationNodeDeclaration(new(3), new(3), ActivationNodeKind.Latch, default);
        _ = new ActivationNetwork([new(new(2), owner, ActivationNodeKind.OrientationSource, default, 0, sensorId), lamp], [new(new(2), new(3))]);
        Assert.Throws<ArgumentException>(() => new ActivationNetwork([new(new(2), owner, ActivationNodeKind.OrientationSource, default), lamp], []));
        Assert.Throws<ArgumentException>(() => new ActivationNetwork([new(new(2), owner, ActivationNodeKind.OrientationSource, new(5), 0, sensorId), lamp], []));
        Assert.Throws<ArgumentException>(() => new ActivationNetwork([new(new(2), owner, ActivationNodeKind.ContactSource, new(5), 0, sensorId), lamp], []));
        Assert.Throws<ArgumentException>(() => new ActivationNetwork([new(new(2), owner, ActivationNodeKind.Latch, default, 0, sensorId), lamp], []));
        Assert.Throws<ArgumentException>(() => new ActivationNetwork([new(new(2), owner, ActivationNodeKind.OrientationSource, default, 0, sensorId), lamp], [new(new(3), new(2))]));
        Assert.Throws<ArgumentException>(() => new ActivationNetwork([new(new(2), owner, ActivationNodeKind.OrientationSource, default, 0, sensorId),
            new(new(4), new(4), ActivationNodeKind.OrientationSource, default, 0, sensorId)], []));
    }

    [Fact]
    public void AdmissionWritesTheSensorAndCandidateValidationEnforcesStickyOnce()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Construction(), new(1, 2));
        var bytes = PhysicsGpuAbi.Admission(scene, new(1), Profile);
        Assert.Equal(1u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(116)));
        Assert.Equal(PhysicsGpuAbi.OrientationSensorsOffset + PhysicsSceneDeclaration.OrientationSensorCapacity * PhysicsGpuAbi.OrientationSensorBytes, PhysicsGpuAbi.MotionOffset);
        PhysicsGpuAbi.ValidateCandidate(bytes, bytes, new(0));
        var armed = new OrientationSensorRead(scene.OrientationSensors[0].Id, new(2), ColliderOf(scene, new(2)), OrientationSensorPhase.Armed, 0, (Half)0);
        Assert.Equal(armed, PhysicsGpuAbi.ReadOrientationSensor(bytes, 0));
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadOrientationSensor(bytes, 1));
        Assert.Equal(BitConverter.HalfToUInt16Bits((Half)Math.Cos(Math.PI / 8)), BinaryPrimitives.ReadUInt16LittleEndian(Record(bytes, 0)[24..]));
        // Every byte of the sensor table is covered at admission: declaration, state, padding and the unused slots.
        for (var offset = PhysicsGpuAbi.OrientationSensorsOffset; offset < PhysicsGpuAbi.MotionOffset; offset++)
        {
            var changed = (byte[])bytes.Clone(); changed[offset] ^= 1;
            Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(changed, bytes, new(0)));
        }
        var turned = (byte[])bytes.Clone();
        BinaryPrimitives.WriteUInt16LittleEndian(Record(turned, 0)[18..], BitConverter.HalfToUInt16Bits((Half)Math.Sqrt(.5)));
        BinaryPrimitives.WriteUInt16LittleEndian(Record(turned, 0)[22..], BitConverter.HalfToUInt16Bits((Half)Math.Sqrt(.5)));
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(turned, turned, new(0)));
        // After admission the body rotates freely; the declaration bytes, not the live rotation, carry the admitted pose.
        var rotating = Candidate(bytes, 1);
        var sensedBody = PhysicsGpuAbi.BodiesOffset + checked((int)BinaryPrimitives.ReadUInt32LittleEndian(Record(rotating, 0)[8..])) * PhysicsGpuAbi.BodyBytes;
        BinaryPrimitives.WriteUInt16LittleEndian(rotating.AsSpan(sensedBody + 44), BitConverter.HalfToUInt16Bits((Half).25));
        BinaryPrimitives.WriteUInt16LittleEndian(rotating.AsSpan(sensedBody + 46), BitConverter.HalfToUInt16Bits((Half)Math.Sqrt(1 - .0625)));
        Assert.Equal(OrientationSensorPhase.Armed, PhysicsGpuAbi.ReadOrientationSensor(rotating, 0).Phase);
        var still = Candidate(bytes, 1);
        PhysicsGpuAbi.ValidateCandidate(still, bytes, new(1));
        // A new event must lie inside the candidate interval: at tick 2 (source end ordinal 4) ordinals 3 and 4 reject, 5 is admitted.
        foreach (var ordinal in new uint[] { 3, 4 })
        {
            var late = Candidate(still, 2); Fire(late, 0, ordinal);
            Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(late, still, new(2)));
        }
        var inside = Candidate(still, 2); Fire(inside, 0, 5);
        PhysicsGpuAbi.ValidateCandidate(inside, still, new(2));
        var fired = Candidate(bytes, 1); Fire(fired, 0, 3);
        PhysicsGpuAbi.ValidateCandidate(fired, bytes, new(1));
        Assert.Equal(armed with { Phase = OrientationSensorPhase.Fired, EventOrdinal = 3 }, PhysicsGpuAbi.ReadOrientationSensor(fired, 0));
        foreach (var ordinal in new uint[] { 0, 5 })
        {
            var outside = Candidate(bytes, 1); Fire(outside, 0, ordinal);
            Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(outside, bytes, new(1)));
        }
        var phased = Candidate(bytes, 1); Fire(phased, 0, 3); phased[PhysicsGpuAbi.OrientationSensorsOffset + 41] = 0x3c;
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(phased, bytes, new(1)));
        // Sticky: the next interval keeps the same event; regression, re-emission and a changed declaration all reject.
        var kept = Candidate(fired, 2);
        PhysicsGpuAbi.ValidateCandidate(kept, fired, new(2));
        var regressed = Candidate(fired, 2); Record(regressed, 0)[32..].Clear();
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(regressed, fired, new(2)));
        var reemitted = Candidate(fired, 2); Fire(reemitted, 0, 7);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(reemitted, fired, new(2)));
        var redeclared = Candidate(fired, 2); Record(redeclared, 0)[24] ^= 1;
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(redeclared, fired, new(2)));
        // A fresh admission rearms: the sensor is armed again in the new world.
        var reset = PhysicsGpuAbi.Admission(scene, new(2), Profile);
        Assert.Equal(armed, PhysicsGpuAbi.ReadOrientationSensor(reset, 0));
    }

    [Fact]
    public void NetworkLatchesTheDominoThenTheLampOnceAndRejectsRegressionOrForeignReads()
    {
        var construction = Construction();
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var network = WorkshopActivationCompiler.Compile(construction);
        var sensor = scene.OrientationSensors[0];
        var collider = ColliderOf(scene, new(2));
        var fired = new OrientationSensorRead(sensor.Id, new(2), collider, OrientationSensorPhase.Fired, 3, (Half)0);
        var clear = network.Clear();
        var active = network.Consume(clear, network.ClearTimers(), [], new(1), [fired]).Activations;
        Assert.Equal(ActivationPhase.Clear, clear[0].Phase); Assert.Equal(ActivationPhase.Clear, clear[1].Phase);
        var domino = active[0]; var lamp = active[1];
        Assert.Equal(ActivationPhase.Latched, domino.Phase); Assert.Equal(ActivationPhase.Latched, lamp.Phase);
        Assert.Equal(ActivationOccurrenceKind.Orientation, domino.Kind);
        Assert.Equal(sensor.Id.Value, domino.Source.Value); Assert.Equal(new GpuBodyId(2), domino.Body); Assert.Equal(collider, domino.Collider);
        Assert.Equal(3u, domino.EventOrdinal); Assert.Equal((Half)0, domino.ApproachSpeed.Value); Assert.Equal(new ActivationNodeId(2), domino.Emitter);
        Assert.Equal(domino.Occurrence, lamp.Occurrence);
        // The committed stream repeats the sticky read; the latch does not change, and it never regresses.
        var replay = network.Consume(active, network.ClearTimers(), [], new(2), [fired]).Activations;
        Assert.Equal(domino, replay[0]); Assert.Equal(lamp, replay[1]);
        Assert.Throws<ArgumentException>(() => network.Consume(active, network.ClearTimers(), [], new(2), [fired with { Phase = OrientationSensorPhase.Armed, EventOrdinal = 0 }]));
        Assert.Throws<ArgumentException>(() => network.Consume(active, network.ClearTimers(), [], new(2), [fired with { EventOrdinal = 7 }]));
        Assert.Throws<ArgumentException>(() => network.Consume(clear, network.ClearTimers(), [], new(1), [fired with { Body = new(1) }]));
        Assert.Throws<ArgumentException>(() => network.Consume(clear, network.ClearTimers(), [], new(1), [fired with { Id = new(99) }]));
        Assert.Throws<ArgumentException>(() => network.Consume(clear, network.ClearTimers(), [], new(1), [fired, fired]));
        Assert.Throws<ArgumentException>(() => network.Consume(clear, network.ClearTimers(), [], new(1), []));
        Assert.Throws<ArgumentException>(() => network.Consume(clear, network.ClearTimers(), [], new(1), [fired with { EventOrdinal = 9 }]));
        Assert.Equal(ActivationPhase.Clear, network.Consume(clear, network.ClearTimers(), [], new(1), [fired with { Phase = OrientationSensorPhase.Armed, EventOrdinal = 0 }]).Activations[0].Phase);
        // Read contract: the latched occurrence must name the declared sensor on its owner; foreign kinds, causes and scenes reject.
        network.ValidateRead(active, network.ClearTimers(), new(1), 4, scene);
        Assert.Throws<ArgumentException>(() => network.ValidateRead(active, network.ClearTimers(), new(0), 4, scene));
        Assert.Throws<ArgumentException>(() => network.ValidateRead(active, network.ClearTimers(), new(1), 4, WorkshopPhysicsCompiler.Compile(Construction(false), new(1, 2))));
        foreach (var changed in new[]
        {
            domino with { Kind = ActivationOccurrenceKind.Contact }, domino with { Source = new(99) }, domino with { Body = new(1) },
            domino with { Collider = new(99) }, domino with { ApproachSpeed = new((Half)1) }, domino with { EventOrdinal = 2, CauseOrdinal = 2 }
        }) Assert.Throws<ArgumentException>(() => network.ValidateRead(new([changed, lamp]), network.ClearTimers(), new(1), 4, scene));
        Assert.Throws<ArgumentException>(() => network.ValidateRead(new([domino, clear[1]]), network.ClearTimers(), new(1), 4, scene));
        // The activation wire carries the new kind in its existing layout.
        var wire = new byte[WorkshopActivationWire.ByteLength]; WorkshopActivationWire.Write(active, wire);
        Assert.Equal((uint)ActivationOccurrenceKind.Orientation, BinaryPrimitives.ReadUInt32LittleEndian(wire.AsSpan(52)));
        var decoded = WorkshopActivationWire.Read(wire, 2);
        Assert.Equal(domino, decoded[0]); Assert.Equal(lamp, decoded[1]);
        Assert.Equal(ActivationPhase.Clear, network.Clear()[0].Phase);
    }

    [Fact]
    public void ARotatedDominoCompilesItsAdmittedRotationAsTheSensorInitialPose()
    {
        var turned = WorkshopInput.Domino(new(2), 0, Upright, 0, 0, Math.Sqrt(.5), 0, Math.Sqrt(.5));
        var construction = Construction() with { Instances = new(WorkshopInput.Basketball(new(1), -.4, 3, 0, 0, 0, 0, 1), turned, WorkshopInput.Lamp(new(3), 3, 1, 0, 0, 0, 0, 1)) };
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));
        var sensor = Assert.Single(scene.OrientationSensors.ToArray());
        Assert.Equal(turned.Rotation, sensor.Initial);
        Assert.NotEqual(CanonicalRotation.Identity, sensor.Initial);
        var bytes = PhysicsGpuAbi.Admission(scene, new(1), Profile);
        PhysicsGpuAbi.ValidateCandidate(bytes, bytes, new(0));
        Assert.Equal(BitConverter.HalfToUInt16Bits(turned.Rotation.Y), BinaryPrimitives.ReadUInt16LittleEndian(Record(bytes, 0)[18..]));
        Assert.Equal(OrientationSensorPhase.Armed, PhysicsGpuAbi.ReadOrientationSensor(bytes, 0).Phase);
    }

    [Fact]
    public void OrientationCauseCarriesThroughADelayToTheLamp()
    {
        var delayed = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
            new(WorkshopInput.Basketball(new(1), -.4, 3, 0, 0, 0, 0, 1), WorkshopInput.Domino(new(2), 0, Upright, 0, 0, 0, 0, 1),
                WorkshopInput.Lamp(new(3), 3, 1, 0, 0, 0, 0, 1), WorkshopInput.Delay(new(4), 0, 1, 0, 0, 0, 0, 1, DelayDuration.Default)),
            Connections: new(new WorkshopConnection(new(2), WorkshopSocket.ActivationOut, new(4), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation),
                new WorkshopConnection(new(4), WorkshopSocket.ActivationOut, new(3), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation)));
        var scene = WorkshopPhysicsCompiler.Compile(delayed, new(1, 2));
        var network = WorkshopActivationCompiler.Compile(delayed);
        var sensor = scene.OrientationSensors[0];
        var fired = new OrientationSensorRead(sensor.Id, new(2), ColliderOf(scene, new(2)), OrientationSensorPhase.Fired, 11, (Half)0);
        var counting = network.Consume(network.Clear(), network.ClearTimers(), [], new(3), [fired]);
        Assert.Equal(ActivationTimerPhase.Counting, counting.Timers[0].Phase);
        Assert.Equal(ActivationOccurrenceKind.Orientation, counting.Timers[0].Input.Kind);
        Assert.Equal(ActivationPhase.Clear, counting.Activations[1].Phase);
        network.ValidateRead(counting.Activations, counting.Timers, new(3), 4, scene);
        var due = network.Consume(counting.Activations, counting.Timers, [], new(123), [fired]);
        var lamp = due.Activations[1]; // nodes are ordered by id: Domino 2, Lamp 3, Delay 4
        Assert.Equal(new GpuBodyId(3), lamp.Owner);
        Assert.Equal(ActivationPhase.Latched, lamp.Phase);
        Assert.Equal(ActivationOccurrenceKind.TimerElapsed, lamp.Kind);
        Assert.Equal(sensor.Id.Value, lamp.Source.Value);
        Assert.Equal(new ActivationNodeId(4), lamp.Emitter);
        Assert.Equal(123u * 4, lamp.EventOrdinal);
        network.ValidateRead(due.Activations, due.Timers, new(123), 4, scene);
        var wire = new byte[WorkshopTimerWire.ByteLength]; WorkshopTimerWire.Write(due.Timers, wire);
        Assert.Equal(due.Timers[0], WorkshopTimerWire.Read(wire, 1)[0]);
        Assert.Equal(ActivationOccurrenceKind.Orientation, WorkshopTimerWire.Read(wire, 1)[0].Input.Kind);
    }

    [Fact]
    public void WiringIntoADominoRejectsAndNineWiredSourcesExceedTheActivationTable()
    {
        var intoDomino = new WorkshopConnection(new(3), WorkshopSocket.ActivationOut, new(2), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);
        Assert.Throws<ArgumentException>(() => (Construction(false) with { Connections = new(intoDomino) }).Validate());
        var items = new List<IWorkshopInstance>();
        for (var i = 1; i <= 8; i++) items.Add(WorkshopInput.Domino(new((ulong)i), -6 + i, Upright, 0, 0, 0, 0, 1));
        items.Add(WorkshopInput.Lamp(new(9), 4, 1, 0, 0, 0, 0, 1));
        var instances = new WorkshopInstances([.. items]);
        instances.Validate();
        WorkshopConnection Link(int source) => new(new((ulong)source), WorkshopSocket.ActivationOut, new(9), WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);
        var seven = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), instances, Connections: new([.. Enumerable.Range(1, 7).Select(Link)]));
        seven.Validate();
        Assert.Equal(ActivationNetwork.Capacity, (int)WorkshopActivationCompiler.Compile(seven).Clear().Count);
        Assert.Equal(7, WorkshopPhysicsCompiler.Compile(seven, new(1, 2)).OrientationSensors.Length);
        var eight = seven with { Connections = seven.Connections.With(Link(8)) };
        var error = Assert.Throws<WorkbenchFullException>(() => eight.Validate());
        Assert.Equal(WorkbenchTable.ActivationNodes, error.Table);
        Assert.Equal("Workbench is full: activation table", error.Message);
        Assert.Throws<WorkbenchFullException>(() => WorkshopActivationCompiler.Compile(eight));
    }
}
