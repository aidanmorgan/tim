using System;
using System.Buffers.Binary;
using System.Globalization;

namespace CuriousContraptions.Gpu;

public enum PhysicsStateVersion : uint { GenericMechanical = 8 }
public enum PhysicsCandidateStatus : uint { Committed, Invalid }
public enum PhysicsFailure : uint { None, InvalidDeclaration, Domain, ContactBudget, RootBudget, UnsupportedPair, Arithmetic, MotionCapacity }
public enum PhysicsMotionPhase : uint { Free, Supported }
public enum ResidencePhase : uint { Outside, Dwelling, Qualified }
public readonly record struct ResidenceRead(GpuSensorId Id, ResidencePhase Phase, uint OccurrenceCount,
    uint StartOrdinal, Half StartPhase, uint EventOrdinal, Half EventPhase);

public readonly record struct PhysicsCandidateRead(PhysicsBodyReadSet Bodies, PhysicsMotionRead Motion);

/// <summary>One bounded generic scene/state ABI.</summary>
public static partial class PhysicsGpuAbi
{
    public const int HeaderBytes = 128;
    public const int BodyBytes = 128;
    // Body record: id 0..8, motion 8..12, cell 16..28, local remainder (Half) 32..38, rotation (Half) 40..48, committed linear
    // velocity m/s (f32) 48..60 and angular velocity rad/s (f32) 60..72 (the mutable range 16..72); immutable declarations after:
    // mass 72, linear drag 74, gravity 76..82, local centre of mass 82..88 (Half), principal frame 88, inertia 96/104/112, collider slot 120.
    public const int BodyVelocityOffset = 48;
    public const int BodyAngularVelocityOffset = 60;
    public const int BodyMassOffset = 72;
    public const int BodyDragOffset = 74;
    public const int BodyGravityOffset = 76;
    public const int BodyCentreOfMassOffset = 82;
    public const int ColliderBytes = 96;
    public const int MaterialBytes = 32;
    public const int SensorBytes = 128;
    public const int GuideBytes = 64;
    public const int TriggerBytes = 64;
    public const int BodiesOffset = HeaderBytes;
    public const int CollidersOffset = BodiesOffset + PhysicsSceneDeclaration.BodyCapacity * BodyBytes;
    public const int MaterialsOffset = CollidersOffset + PhysicsSceneDeclaration.ColliderCapacity * ColliderBytes;
    public const int SensorsOffset = MaterialsOffset + PhysicsSceneDeclaration.MaterialCapacity * MaterialBytes;
    public const int GuidesOffset = SensorsOffset + PhysicsSceneDeclaration.SensorCapacity * SensorBytes;
    public const int TriggersOffset = GuidesOffset + PhysicsSceneDeclaration.GuideCapacity * GuideBytes;
    public const int ContactWorkBytes = 64;
    public const int ContactWorksOffset = TriggersOffset + PhysicsSceneDeclaration.TriggerCapacity * TriggerBytes;
    public const int WorkOccurrencesOffset = ContactWorksOffset + PhysicsSceneDeclaration.ContactWorkCapacity * ContactWorkBytes;
    public const int WorkOccurrenceBytes = 32;
    // Orientation sensors: declaration (id, body slot, initial rotation, cos half-angle) in the first 32 bytes, sticky fired state after.
    public const int OrientationSensorBytes = 64;
    public const int OrientationSensorsOffset = WorkOccurrencesOffset + PhysicsContactWorkRead.OccurrenceCapacity * WorkOccurrenceBytes;
    public const int MotionOffset = OrientationSensorsOffset + PhysicsSceneDeclaration.OrientationSensorCapacity * OrientationSensorBytes;
    public const int CacheOffset = MotionOffset + PhysicsMotionRead.ByteLength;
    public const int CacheCapacity = 888 * 4;
    public const int CacheBytes = 32;
    public const int ByteLength = CacheOffset + CacheCapacity * CacheBytes;
    public const uint NoBody = uint.MaxValue;
    // A binary eighth-second primary segment bounds elapsed-value quantization.
    public const uint PrimarySegmentSteps = WorkshopCadenceSettings.PhysicalFrequency / 8;

    public static byte[] Admission(PhysicsSceneDeclaration scene, SimulationEpoch epoch, WorkshopGpuProfile profile)
    {
        ArgumentNullException.ThrowIfNull(scene); profile.Validate();
        if (epoch.Value == 0) throw new ArgumentException("A world epoch is required.");
        var bytes = new byte[ByteLength]; var data = bytes.AsSpan();
        U32(data, 0, (uint)PhysicsStateVersion.GenericMechanical);
        U32(data, 12, (uint)scene.Bodies.Length); U32(data, 16, (uint)scene.Colliders.Length);
        U32(data, 20, (uint)scene.Materials.Length); U32(data, 24, (uint)scene.Sensors.Length);
        U32(data, 28, 0); U32(data, 96, (uint)scene.Guides.Length); U32(data, 100, (uint)scene.Triggers.Length);
        U32(data, 104, (uint)scene.ContactWorks.Length); U32(data, 116, (uint)scene.OrientationSensors.Length);
        U64(data, 32, epoch.Value); U32(data, 48, (uint)profile.Cadence);
        U32(data, 52, (uint)profile.Physical); U64(data, 56, profile.Revision.Value);
        U64(data, 64, scene.Document.Low); U64(data, 72, scene.Document.High); U64(data, 80, scene.NextIdentity);
        for (var i = 0; i < scene.Bodies.Length; i++)
        {
            var body = scene.Bodies[i]; var record = data.Slice(BodiesOffset + i * BodyBytes, BodyBytes);
            U64(record, 0, body.Id.Value); U32(record, 8, (uint)body.Motion);
            Cell(record, 16, body.Cell); Local(record, 32, body.Local); Rotation(record, 40, body.Rotation);
            F32Vector(record, BodyVelocityOffset, body.Velocity.X, body.Velocity.Y, body.Velocity.Z);
            F32Vector(record, BodyAngularVelocityOffset, body.AngularVelocity.X, body.AngularVelocity.Y, body.AngularVelocity.Z);
            H(record, BodyMassOffset, body.Mass.Value); H(record, BodyDragOffset, body.LinearDrag.Value);
            Vector(record, BodyGravityOffset, body.Gravity.X, body.Gravity.Y, body.Gravity.Z);
            if (body.Motion == RigidMotionKind.Dynamic)
            {
                U32(data,28,R32(data,28)+1);
                for (var collider=0; collider<scene.Colliders.Length; collider++)
                    if (scene.Colliders[collider].Body==body.Id)
                    {
                        var properties=RigidMassProperties.Compile(body,scene.Colliders[collider]);
                        Vector(record,BodyCentreOfMassOffset,properties.LocalCentreOfMass.X,properties.LocalCentreOfMass.Y,properties.LocalCentreOfMass.Z);
                        Rotation(record,88,properties.PrincipalFrame);
                        Principal(record,96,properties.X); Principal(record,104,properties.Y); Principal(record,112,properties.Z);
                        U32(record,120,(uint)collider);
                    }
            }
        }
        for (var i = 0; i < scene.Colliders.Length; i++)
        {
            var collider = scene.Colliders[i]; var record = data.Slice(CollidersOffset + i * ColliderBytes, ColliderBytes);
            U64(record, 0, collider.Id.Value); U32(record, 8, BodySlot(scene, collider.Body));
            U32(record, 12, MaterialSlot(scene, collider.Material)); U32(record, 16, (uint)collider.Shape);
            Pose(record, 24, collider.Pose); H(record, 40, collider.Radius.Value);
            Vector(record, 42, collider.HalfExtents.X, collider.HalfExtents.Y, collider.HalfExtents.Z);
        }
        for (var i = 0; i < scene.Materials.Length; i++)
        {
            var material = scene.Materials[i]; var record = data.Slice(MaterialsOffset + i * MaterialBytes, MaterialBytes);
            U64(record, 0, material.Id.Value); H(record, 8, material.Restitution.Value);
            H(record, 10, material.BounceThreshold.Value); H(record, 12, material.Friction.Value);
            H(record, 14, material.RollingResistance.Value);
        }
        for (var i = 0; i < scene.Sensors.Length; i++)
        {
            var sensor = scene.Sensors[i]; var record = data.Slice(SensorsOffset + i * SensorBytes, SensorBytes);
            U64(record, 0, sensor.Id.Value); U32(record, 8, BodySlot(scene, sensor.Frame));
            U32(record, 12, BodySlot(scene, sensor.Target)); U32(record, 16, (uint)sensor.Participation);
            Pose(record, 24, sensor.Pose);
            Vector(record, 40, sensor.Minimum.X, sensor.Minimum.Y, sensor.Minimum.Z);
            Vector(record, 48, sensor.Maximum.X, sensor.Maximum.Y, sensor.Maximum.Z);
            H(record, 56, sensor.SpeedLimit.Value); H(record, 58, sensor.Dwell.Value);
        }
        for (var i = 0; i < scene.Guides.Length; i++)
        {
            var guide = scene.Guides[i]; var record = data.Slice(GuidesOffset + i * GuideBytes, GuideBytes);
            U64(record, 0, guide.Id.Value); U32(record, 8, BodySlot(scene, guide.Frame));
            U32(record, 12, BodySlot(scene, guide.Target)); Pose(record, 16, guide.Pose);
            Vector(record, 32, guide.Minimum.X, guide.Minimum.Y, guide.Minimum.Z);
            Vector(record, 40, guide.Maximum.X, guide.Maximum.Y, guide.Maximum.Z);
            H(record, 48, guide.MaximumAcceleration.Value); H(record, 50, guide.SupportHeight.Value);
            H(record, 52, guide.SupportMargin.Value);
        }
        for (var i = 0; i < scene.Triggers.Length; i++)
        {
            var trigger = scene.Triggers[i]; var record = data.Slice(TriggersOffset + i * TriggerBytes, TriggerBytes);
            U64(record, 0, trigger.Id.Value); U32(record, 8, BodySlot(scene, trigger.Owner));
            U32(record, 12, (uint)trigger.Targets.Kind); H(record, 16, trigger.Threshold.Value);
            U32(record, 24, trigger.Targets.Kind == BodyTargetKind.NamedBody ? BodySlot(scene, trigger.Targets.Body) : NoBody);
        }
        for (var i = 0; i < scene.ContactWorks.Length; i++)
        {
            var work = scene.ContactWorks[i]; var record = data.Slice(ContactWorksOffset + i * ContactWorkBytes, ContactWorkBytes);
            U64(record, 0, work.Id.Value); U32(record, 8, BodySlot(scene, work.Owner)); U32(record,12,(uint)work.Targets.Kind);
            U32(record,28,work.Targets.Kind==BodyTargetKind.NamedBody ? BodySlot(scene,work.Targets.Body) : NoBody);
            H(record, 16, work.TargetSpeed.Value); H(record, 18, work.InitialEnergy.Value); H(record, 20, work.Threshold.Value);
            U32(record, 24, work.CooldownPhysicalSteps); H(record, 48, work.InitialEnergy.Value);
        }
        for (var i = 0; i < scene.OrientationSensors.Length; i++)
        {
            var sensor = scene.OrientationSensors[i];
            var record = data.Slice(OrientationSensorsOffset + i * OrientationSensorBytes, OrientationSensorBytes);
            U64(record, 0, sensor.Id.Value); U32(record, 8, BodySlot(scene, sensor.Body));
            Rotation(record, 16, sensor.Initial); H(record, 24, sensor.Threshold.CosineHalfAngle);
        }
        var occurrence=0;
        for (var work=0; work<scene.ContactWorks.Length; work++)
            foreach (var body in scene.Bodies)
                if (body.Motion==RigidMotionKind.Dynamic && scene.ContactWorks[work].Targets.Contains(body.Id))
                {
                    var slot=data.Slice(WorkOccurrencesOffset+occurrence++*WorkOccurrenceBytes,WorkOccurrenceBytes);
                    BinaryPrimitives.WriteUInt16LittleEndian(slot,(ushort)work); U64(slot,4,body.Id.Value);
                }
        U32(data,108,(uint)occurrence);
        var pair=0;
        for (var first=0; first<scene.Colliders.Length; first++)
            for (var second=first+1; second<scene.Colliders.Length; second++)
            {
                var firstBody=scene.Bodies[(int)BodySlot(scene,scene.Colliders[first].Body)];
                var secondBody=scene.Bodies[(int)BodySlot(scene,scene.Colliders[second].Body)];
                if (firstBody.Id==secondBody.Id || (firstBody.Motion==RigidMotionKind.Static && secondBody.Motion==RigidMotionKind.Static)) continue;
                var firstScale=firstBody.Motion==RigidMotionKind.Dynamic ? Math.ILogB((double)firstBody.Mass.Value)+1 : int.MinValue;
                var secondScale=secondBody.Motion==RigidMotionKind.Dynamic ? Math.ILogB((double)secondBody.Mass.Value)+1 : int.MinValue;
                for (var point=0; point<4; point++)
                {
                    if (pair>=CacheCapacity) throw new ArgumentException("Contact cache capacity exceeded.");
                    var slot=data.Slice(CacheOffset+pair++*CacheBytes,CacheBytes);
                    BinaryPrimitives.WriteUInt16LittleEndian(slot,(ushort)first);
                    BinaryPrimitives.WriteUInt16LittleEndian(slot[2..],(ushort)second);
                    BinaryPrimitives.WriteInt32LittleEndian(slot[28..],Math.Max(firstScale,secondScale));
                }
            }
        U32(data,112,(uint)pair);
        return bytes;
    }

    public static WorkshopGpuProfile ReadProfile(ReadOnlySpan<byte> data)
    {
        Header(data);
        var profile = new WorkshopGpuProfile((SimulationCadence)R32(data, 48),
            (PhysicalStepProfile)R32(data, 52), new(R64(data, 56)));
        profile.Validate(); return profile;
    }

    public static PhysicsFailure ReadFailure(ReadOnlySpan<byte> data)
    {
        Header(data);
        var status = (PhysicsCandidateStatus)R32(data, 4); var failure = (PhysicsFailure)R32(data, 8);
        if (!Enum.IsDefined(status) || !Enum.IsDefined(failure) ||
            (status == PhysicsCandidateStatus.Committed) != (failure == PhysicsFailure.None))
            throw new ArgumentException("Invalid generic candidate status.");
        return failure;
    }

    public static PhysicsBodyReadSet ReadDynamicBodies(ReadOnlySpan<byte> data)
    {
        if (ReadFailure(data) != PhysicsFailure.None) throw new ArgumentException("Candidate is not committed.");
        var profile = ReadProfile(data); var epoch = R64(data, 32); var tick = R64(data, 40);
        if (epoch == 0 || tick > profile.RunTickLimit ||
            R32(data, 88) != checked((uint)(tick * profile.Substeps)))
            throw new ArgumentException("Invalid generic physical time.");
        Span<PhysicsBodyRead> values = stackalloc PhysicsBodyRead[PhysicsBodyReadSet.Capacity];
        var count = 0;
        for (var slot = 0; slot < R32(data, 12); slot++)
        {
            var record = data.Slice(BodiesOffset + slot * BodyBytes, BodyBytes);
            var motion = (RigidMotionKind)R32(record, 8);
            if (!Enum.IsDefined(motion)) throw new ArgumentException("Undefined rigid motion.");
            if (motion == RigidMotionKind.Static) continue;
            if (count == values.Length) throw new ArgumentException("Dynamic body capacity exceeded.");
            // The read set validates every value: non-finite or out-of-envelope f32 velocity (|v| > 64 m/s, |w| > 128 rad/s) rejects the read.
            var body = new CanonicalBody(new(R64(record, 0)), epoch, tick,
                ReadCell(record, 16), ReadLocal(record, 32),
                new(RF(record, BodyVelocityOffset), RF(record, BodyVelocityOffset + 4), RF(record, BodyVelocityOffset + 8)));
            values[count++] = new(body, ReadRotation(record, 40),
                new(RF(record, BodyAngularVelocityOffset), RF(record, BodyAngularVelocityOffset + 4), RF(record, BodyAngularVelocityOffset + 8)),
                ReadCentreOfMass(record));
        }
        var result = new PhysicsBodyReadSet(values[..count]);
        result.ValidateTime(new(epoch), new(tick)); return result;
    }

    public static ResidenceRead ReadSensor(ReadOnlySpan<byte> data, int slot)
    {
        if (ReadFailure(data) != PhysicsFailure.None || slot < 0 || (uint)slot >= R32(data, 24))
            throw new ArgumentException("Invalid sensor read.");
        var record = data.Slice(SensorsOffset + slot * SensorBytes, SensorBytes);
        var phase = (ResidencePhase)R32(record, 64);
        var result = new ResidenceRead(new(R64(record, 0)), phase, R32(record, 76),
            R32(record, 68), RH(record, 72), R32(record, 80), RH(record, 84));
        if (!Enum.IsDefined(phase) || result.Id.Value == 0 || result.OccurrenceCount > 1 ||
            !Half.IsFinite(result.StartPhase) || result.StartPhase < (Half)(-2048) || result.StartPhase >= (Half)2048 ||
            !Half.IsFinite(result.EventPhase) || result.EventPhase < (Half)(-2048) || result.EventPhase >= (Half)2048 ||
            !AdmittedTime(result.StartOrdinal, result.StartPhase, R32(data, 88)) ||
            !AdmittedTime(result.EventOrdinal, result.EventPhase, R32(data, 88)) ||
            (phase == ResidencePhase.Outside && (result.StartOrdinal != 0 || !PhysicsDeclarationBounds.Zero(result.StartPhase))) ||
            (result.OccurrenceCount == 0 && (result.EventOrdinal != 0 || !PhysicsDeclarationBounds.Zero(result.EventPhase))) ||
            (result.OccurrenceCount != 0 && phase == ResidencePhase.Qualified && Before(result.EventOrdinal, result.EventPhase, result.StartOrdinal, result.StartPhase)) ||
            !AllZero(record[74..76]) || !AllZero(record[86..128]))
            throw new ArgumentException("Invalid committed residence state.");
        return result;
    }


    public static ContactTriggerRead ReadTrigger(ReadOnlySpan<byte> data, int slot)
    {
        if (ReadFailure(data) != PhysicsFailure.None || slot < 0 || (uint)slot >= R32(data, 100))
            throw new ArgumentException("Invalid contact trigger read.");
        var record = data.Slice(TriggersOffset + slot * TriggerBytes, TriggerBytes);
        var owner = R32(record, 8); var kind = (BodyTargetKind)R32(record, 12);
        var named = R32(record, 24); var target = R32(record, 48); var count = R32(record, 32);
        var bodyCount = R32(data, 12);

        if (R64(record, 0) == 0 || owner >= bodyCount || R32(record, 12) is < 1 or > 2 || !Enum.IsDefined(kind) ||
            (kind == BodyTargetKind.NamedBody ? (named >= bodyCount || (RigidMotionKind)R32(data, BodiesOffset + checked((int)named) * BodyBytes + 8) != RigidMotionKind.Dynamic) || named == owner : named != NoBody) ||
            (RigidMotionKind)R32(data, BodiesOffset + checked((int)owner) * BodyBytes + 8) != RigidMotionKind.Static ||
            !Half.IsFinite(RH(record, 16)) || RH(record, 16) < (Half)0 || RH(record, 16) > (Half)64 ||
            !AllZero(record[18..24]) || !AllZero(record[28..32]) || !AllZero(record[52..]) || count > 1)
            throw new ArgumentException("Invalid contact trigger declaration or padding.");
        var collider = default(GpuColliderId); var targetId = default(GpuBodyId);
        if (count == 0)
        {
            if (!AllZero(record[36..52])) throw new ArgumentException("An absent impact retained event data.");
        }
        else
        {
            var colliderSlot = R32(record, 36);
            if ((target >= bodyCount || (RigidMotionKind)R32(data, BodiesOffset + checked((int)target) * BodyBytes + 8) != RigidMotionKind.Dynamic) || (kind == BodyTargetKind.NamedBody && target != named) ||
                colliderSlot >= R32(data, 16) ||
                R32(data, CollidersOffset + checked((int)colliderSlot) * ColliderBytes + 8) != owner ||
                !AdmittedTime(R32(record, 40), RH(record, 44), R32(data, 88)) ||
                !Half.IsFinite(RH(record, 46)) || RH(record, 46) < RH(record, 16) || RH(record, 46) > (Half)4096)
                throw new ArgumentException("Invalid qualifying impact identity, time or normal speed.");
            collider = new(R64(data, CollidersOffset + checked((int)colliderSlot) * ColliderBytes));
            targetId = new(R64(data, BodiesOffset + checked((int)target) * BodyBytes));
        }
        return new(new(R64(record, 0)), new(R64(data, BodiesOffset + checked((int)owner) * BodyBytes)),
            targetId, count, collider, R32(record, 40), RH(record, 44), new(RH(record, 46)));
    }

    public static OrientationSensorRead ReadOrientationSensor(ReadOnlySpan<byte> data, int slot)
    {
        if (ReadFailure(data) != PhysicsFailure.None || slot < 0 || (uint)slot >= R32(data, 116))
            throw new ArgumentException("Invalid orientation sensor read.");
        var record = data.Slice(OrientationSensorsOffset + slot * OrientationSensorBytes, OrientationSensorBytes);
        var body = R32(record, 8); var phase = (OrientationSensorPhase)R32(record, 32);
        if (R64(record, 0) == 0 || body >= R32(data, 12) || !Enum.IsDefined(phase) ||
            !AllZero(record[12..16]) || !AllZero(record[26..32]) || !AllZero(record[42..]))
            throw new ArgumentException("Invalid orientation sensor declaration or padding.");
        var bodyRecord = data.Slice(BodiesOffset + checked((int)body) * BodyBytes, BodyBytes);
        if ((RigidMotionKind)R32(bodyRecord, 8) != RigidMotionKind.Dynamic)
            throw new ArgumentException("Orientation sensor body is not dynamic.");
        ReadRotation(record, 16).Validate();
        new OrientationThreshold(RH(record, 24)).Validate();
        var ordinal = R32(record, 36); var eventPhase = RH(record, 40);
        // Endpoint-sampled: a fired sensor names one substep boundary inside the admitted time; an armed one holds no event.
        if (phase == OrientationSensorPhase.Armed
            ? ordinal != 0 || !PhysicsDeclarationBounds.Zero(eventPhase)
            : ordinal == 0 || !PhysicsDeclarationBounds.Zero(eventPhase) || !AdmittedTime(ordinal, eventPhase, R32(data, 88)))
            throw new ArgumentException("Invalid orientation sensor event.");
        var colliderSlot = R32(bodyRecord, 120);
        if (colliderSlot >= R32(data, 16) || R32(data, CollidersOffset + checked((int)colliderSlot) * ColliderBytes + 8) != body)
            throw new ArgumentException("Orientation sensor body has no owned collider.");
        return new(new(R64(record, 0)), new(R64(bodyRecord, 0)),
            new(R64(data, CollidersOffset + checked((int)colliderSlot) * ColliderBytes)), phase, ordinal, eventPhase);
    }

    /// <summary>Required before committing a GPU result; selected reads alone do not qualify a candidate.</summary>
    public static PhysicsCandidateRead ValidateCandidate(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> source, SimulationTick expectedTick)
    {
        if (ReadFailure(candidate) != PhysicsFailure.None || ReadFailure(source) != PhysicsFailure.None)
            throw new ArgumentException("Only a successful candidate can commit.");
        var profile = ReadProfile(source);
        if (expectedTick.Value > profile.RunTickLimit || R64(candidate, 40) != expectedTick.Value ||
            R32(candidate, 88) != checked((uint)(expectedTick.Value * profile.Substeps)) ||
            !candidate[..4].SequenceEqual(source[..4]) ||
            !candidate[12..40].SequenceEqual(source[12..40]) ||
            !candidate[48..88].SequenceEqual(source[48..88]) ||
            !candidate[96..120].SequenceEqual(source[96..120]))
            throw new ArgumentException("Candidate identity, scene counts or physical profile changed.");
        var bodyCount=checked((int)R32(source,12)); var dynamicCount=0;
        for (var i=0; i<bodyCount; i++)
        {
            var actual=candidate.Slice(BodiesOffset+i*BodyBytes,BodyBytes);
            var previous=source.Slice(BodiesOffset+i*BodyBytes,BodyBytes);
            var motion=(RigidMotionKind)R32(previous,8);
            if (motion==RigidMotionKind.Static)
            {
                if (!actual.SequenceEqual(previous)) throw new ArgumentException("Static body changed.");
                continue;
            }
            if (motion!=RigidMotionKind.Dynamic) throw new ArgumentException("Undefined rigid motion.");
            dynamicCount++;
            if (!actual[..16].SequenceEqual(previous[..16]) || !actual[BodyMassOffset..].SequenceEqual(previous[BodyMassOffset..]) ||
                !AllZero(actual[28..32]) || !AllZero(actual[38..40]))
                throw new ArgumentException("Dynamic immutable declaration or padding changed.");
            ReadMassProperties(actual).Validate();
        }
        if (dynamicCount!=R32(source,28) || dynamicCount>PhysicsBodyReadSet.Capacity ||
            !AllZero(candidate.Slice(BodiesOffset+bodyCount*BodyBytes,(PhysicsSceneDeclaration.BodyCapacity-bodyCount)*BodyBytes)) ||
            !candidate[CollidersOffset..SensorsOffset].SequenceEqual(source[CollidersOffset..SensorsOffset]))
            throw new ArgumentException("Candidate changed body population, geometry or material.");
        var bodies = ReadDynamicBodies(candidate);
        uint captured = 0;
        var sensorCount = checked((int)R32(source, 24));
        for (var i = 0; i < sensorCount; i++)
        {
            var offset = SensorsOffset + i * SensorBytes;
            if (!candidate.Slice(offset, 64).SequenceEqual(source.Slice(offset, 64)))
                throw new ArgumentException("Sensor declaration changed.");
            var current = ReadSensor(candidate, i); var previous = ReadSensor(source, i);
            if ((SensorParticipation)R32(source, offset + 16) == SensorParticipation.Disabled &&
                (current.Phase != ResidencePhase.Outside || current.OccurrenceCount != 0))
                throw new ArgumentException("A disabled sensor produced residence.");
            var sourceOrdinal = R32(source, 88);
            if (current.OccurrenceCount != 0 && Before(current.EventOrdinal, current.EventPhase, sourceOrdinal, (Half)0))
                throw new ArgumentException("New qualification predates the candidate interval.");
            var retainedEpisode = previous.Phase != ResidencePhase.Outside &&
                current.StartOrdinal == previous.StartOrdinal && current.StartPhase == previous.StartPhase;
            if (current.Phase != ResidencePhase.Outside &&
                Before(current.StartOrdinal, current.StartPhase, sourceOrdinal, (Half)0) && !retainedEpisode)
                throw new ArgumentException("Residence invented an earlier episode.");
            if (current.Phase == ResidencePhase.Qualified && current.OccurrenceCount == 0 &&
                !(retainedEpisode && previous.Phase == ResidencePhase.Qualified))
                throw new ArgumentException("A new qualification omitted its occurrence.");
            if (retainedEpisode && previous.Phase == ResidencePhase.Qualified &&
                (current.Phase == ResidencePhase.Dwelling || current.OccurrenceCount != 0))
                throw new ArgumentException("A retained qualified episode regressed or emitted again.");
            captured += current.OccurrenceCount;
        }
        if (R32(candidate, 92) != captured ||
            !AllZero(candidate[(SensorsOffset + sensorCount * SensorBytes)..GuidesOffset]))
            throw new ArgumentException("Capture count or unused sensor slots changed.");
        if (!candidate[GuidesOffset..TriggersOffset].SequenceEqual(source[GuidesOffset..TriggersOffset]))
            throw new ArgumentException("Guide declaration changed.");
        var triggerCount = checked((int)R32(source, 100));
        for (var i = 0; i < triggerCount; i++)
        {
            var offset = TriggersOffset + i * TriggerBytes;
            if (!candidate.Slice(offset, 32).SequenceEqual(source.Slice(offset, 32)))
                throw new ArgumentException("Contact trigger declaration changed.");
            var trigger = ReadTrigger(candidate, i);
            if (trigger.OccurrenceCount != 0 && (expectedTick.Value == 0 ||
                Before(trigger.EventOrdinal, trigger.EventPhase, R32(source, 88), (Half)0)))
                throw new ArgumentException("Impact event does not belong to this candidate interval.");
        }
        if (!AllZero(candidate[(TriggersOffset + triggerCount * TriggerBytes)..ContactWorksOffset]))
            throw new ArgumentException("Unused contact trigger slots changed.");
        ValidateContactWorkCandidate(candidate, source, expectedTick);
        var orientationCount = checked((int)R32(source, 116));
        for (var i = 0; i < orientationCount; i++)
        {
            var offset = OrientationSensorsOffset + i * OrientationSensorBytes;
            if (!candidate.Slice(offset, 32).SequenceEqual(source.Slice(offset, 32)))
                throw new ArgumentException("Orientation sensor declaration changed.");
            // At admission the body record still holds the admitted rotation: the sensor's initial pose must be exactly that.
            // Later ticks keep the declaration bytes equal to the source, so the invariant carries forward.
            if (expectedTick.Value == 0 && !candidate.Slice(offset + 16, 8).SequenceEqual(
                    candidate.Slice(BodiesOffset + checked((int)R32(candidate, offset + 8)) * BodyBytes + 40, 8)))
                throw new ArgumentException("Orientation sensor initial pose differs from its body's admitted rotation.");
            var current = ReadOrientationSensor(candidate, i); var previous = ReadOrientationSensor(source, i);
            // Sticky within a world: a fired sensor never regresses or re-emits; a new event belongs to this interval.
            if (previous.Phase == OrientationSensorPhase.Fired)
            {
                if (current != previous) throw new ArgumentException("A fired orientation sensor regressed or emitted again.");
            }
            else if (current.Phase == OrientationSensorPhase.Fired && (expectedTick.Value == 0 || current.EventOrdinal <= R32(source, 88)))
                throw new ArgumentException("Orientation event does not belong to this candidate interval.");
        }
        if (!AllZero(candidate[(OrientationSensorsOffset + orientationCount * OrientationSensorBytes)..MotionOffset]))
            throw new ArgumentException("Unused orientation sensor slots changed.");
        if (expectedTick.Value != 0 && (R32(candidate, MotionOffset + 4) != profile.Substeps ||
            R32(candidate, MotionOffset + 12) != R32(candidate, 88)))
            throw new ArgumentException("Motion profile differs from its committed world.");
        ValidateCache(candidate,source);
        return new(bodies, PhysicsMotionRead.Decode(candidate.Slice(MotionOffset,PhysicsMotionRead.ByteLength), bodies, expectedTick));
    }


    private static void Principal(Span<byte> bytes,int offset,PrincipalInertia value)
    {
        value.Validate(); H(bytes,offset,value.Mantissa);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[(offset+4)..],value.Exponent);
    }
    private static RigidMassProperties ReadMassProperties(ReadOnlySpan<byte> record) =>
        new(ReadCentreOfMass(record),ReadRotation(record,88),
            ReadPrincipal(record,96),ReadPrincipal(record,104),ReadPrincipal(record,112));
    private static MetreVector ReadCentreOfMass(ReadOnlySpan<byte> record) =>
        new(RH(record,BodyCentreOfMassOffset),RH(record,BodyCentreOfMassOffset+2),RH(record,BodyCentreOfMassOffset+4));
    private static PrincipalInertia ReadPrincipal(ReadOnlySpan<byte> record,int offset)
    {
        if (!AllZero(record.Slice(offset+2,2))) throw new ArgumentException("Nonzero inertia padding.");
        var value=new PrincipalInertia(RH(record,offset),BinaryPrimitives.ReadInt32LittleEndian(record[(offset+4)..]));
        value.Validate(); return value;
    }
    private static void ValidateCache(ReadOnlySpan<byte> candidate,ReadOnlySpan<byte> source)
    {
        var count=checked((int)R32(source,112));
        for (var i=0; i<count; i++)
        {
            var before=source.Slice(CacheOffset+i*CacheBytes,CacheBytes);
            var after=candidate.Slice(CacheOffset+i*CacheBytes,CacheBytes);
            // This external byte boundary checks the same complete record with four integer loads.
            var identityFeature = R64(after, 0);
            var impulses = R64(after, 8);
            var normalPadding = R64(after, 16);
            var enabledExponent = R64(after, 24);
            var enabled = (uint)enabledExponent;
            if ((uint)identityFeature != R32(before, 0) || (uint)(enabledExponent >> 32) != R32(before, 28) ||
                enabled > 1 || (normalPadding >> 48) != 0)
                throw new ArgumentException("Contact cache identity or padding changed.");
            if (enabled == 0)
            {
                if (((identityFeature >> 32) | impulses | normalPadding) != 0) throw new ArgumentException("Inactive contact retained a warm impulse.");
                continue;
            }
            PhysicsDeclarationBounds.Range(RH(after,8),(Half)0,(Half)8192);
            PhysicsDeclarationBounds.Range(RH(after,14),(Half)0,(Half)4096);
            PhysicsDeclarationBounds.Vector(RH(after,10),RH(after,12),(Half)0,(Half)8192);
            PhysicsDeclarationBounds.Vector(RH(after,16),RH(after,18),RH(after,20),(Half)1);
        }
        if (!AllZero(candidate[(CacheOffset+count*CacheBytes)..])) throw new ArgumentException("Unused cache changed.");
    }

    private static bool ValidFeature(ColliderShapeKind shape, uint feature)
    {
        if (shape is ColliderShapeKind.Plane or ColliderShapeKind.Sphere) return feature == 0;
        if (shape != ColliderShapeKind.Box) return false;
        if (feature is >= 64 and <= 69) return true;
        return feature < 64 && feature != 21 && (feature & 3) <= 2 &&
            ((feature >> 2) & 3) <= 2 && ((feature >> 4) & 3) <= 2;
    }

    private static bool AdmittedTime(uint ordinal, Half phase, uint end) =>
        Half.IsFinite(phase) && phase >= (Half)(-2048) && phase < (Half)2048 &&
        ordinal <= end && (ordinal != 0 || phase >= (Half)0) && (ordinal != end || phase <= (Half)0);
    private static bool Before(uint a, Half ap, uint b, Half bp) => a < b || (a == b && ap < bp);

    public static string ShaderPreamble() => FormattableString.Invariant($@"
enable f16;
const WORLD_BYTES:u32={ByteLength}u;
const PHYSICAL_RATE:f16={WorkshopCadenceSettings.PhysicalFrequency}h;
const PRIMARY_SEGMENT_STEPS:u32={PrimarySegmentSteps}u;
const BODY_CAPACITY:u32={PhysicsSceneDeclaration.BodyCapacity}u;
const COLLIDER_CAPACITY:u32={PhysicsSceneDeclaration.ColliderCapacity}u;
const MATERIAL_CAPACITY:u32={PhysicsSceneDeclaration.MaterialCapacity}u;
const SENSOR_CAPACITY:u32={PhysicsSceneDeclaration.SensorCapacity}u;
const GUIDE_CAPACITY:u32={PhysicsSceneDeclaration.GuideCapacity}u;
const TRIGGER_CAPACITY:u32={PhysicsSceneDeclaration.TriggerCapacity}u;
const CONTACT_WORK_CAPACITY:u32={PhysicsSceneDeclaration.ContactWorkCapacity}u;
const ORIENTATION_SENSOR_CAPACITY:u32={PhysicsSceneDeclaration.OrientationSensorCapacity}u;
const STATE_VERSION:u32={(uint)PhysicsStateVersion.GenericMechanical}u;
const STATUS_COMMITTED:u32={(uint)PhysicsCandidateStatus.Committed}u;
const STATUS_INVALID:u32={(uint)PhysicsCandidateStatus.Invalid}u;
const FAILURE_NONE:u32={(uint)PhysicsFailure.None}u;
const FAILURE_DECLARATION:u32={(uint)PhysicsFailure.InvalidDeclaration}u;
const FAILURE_DOMAIN:u32={(uint)PhysicsFailure.Domain}u;
const FAILURE_CONTACT_BUDGET:u32={(uint)PhysicsFailure.ContactBudget}u;
const FAILURE_ROOT_BUDGET:u32={(uint)PhysicsFailure.RootBudget}u;
const FAILURE_PAIR:u32={(uint)PhysicsFailure.UnsupportedPair}u;
const FAILURE_ARITHMETIC:u32={(uint)PhysicsFailure.Arithmetic}u;
const FAILURE_MOTION_CAPACITY:u32={(uint)PhysicsFailure.MotionCapacity}u;
const MOTION_STATIC:u32={(uint)RigidMotionKind.Static}u;
const MOTION_DYNAMIC:u32={(uint)RigidMotionKind.Dynamic}u;
const SHAPE_SPHERE:u32={(uint)ColliderShapeKind.Sphere}u;
const SHAPE_BOX:u32={(uint)ColliderShapeKind.Box}u;
const SHAPE_PLANE:u32={(uint)ColliderShapeKind.Plane}u;
const MOTION_FREE:u32={(uint)PhysicsMotionPhase.Free}u;
const MOTION_SUPPORTED:u32={(uint)PhysicsMotionPhase.Supported}u;
const SENSOR_DISABLED:u32={(uint)SensorParticipation.Disabled}u;
const SENSOR_ENABLED:u32={(uint)SensorParticipation.Enabled}u;
const RESIDENCE_OUTSIDE:u32={(uint)ResidencePhase.Outside}u;
const RESIDENCE_DWELLING:u32={(uint)ResidencePhase.Dwelling}u;
const RESIDENCE_QUALIFIED:u32={(uint)ResidencePhase.Qualified}u;
const CADENCE_60:u32={(uint)SimulationCadence.Hz60}u;
const CADENCE_120:u32={(uint)SimulationCadence.Hz120}u;
const CADENCE_240:u32={(uint)SimulationCadence.Hz240}u;
const PHYSICAL_480:u32={(uint)PhysicalStepProfile.Canonical480Hz}u;
");

    private static void Header(ReadOnlySpan<byte> data)
    {
        if (data.Length != ByteLength || R32(data, 0) != (uint)PhysicsStateVersion.GenericMechanical ||
            R32(data, 12) > PhysicsSceneDeclaration.BodyCapacity ||
            R32(data, 16) > PhysicsSceneDeclaration.ColliderCapacity ||
            R32(data, 20) > PhysicsSceneDeclaration.MaterialCapacity ||
            R32(data, 24) > PhysicsSceneDeclaration.SensorCapacity ||
            R32(data, 96) > PhysicsSceneDeclaration.GuideCapacity ||
            R32(data, 100) > PhysicsSceneDeclaration.TriggerCapacity ||
            R32(data, 104) > PhysicsSceneDeclaration.ContactWorkCapacity ||
            (R64(data, 64) == 0 && R64(data, 72) == 0) || R64(data, 80) == 0 ||
            R32(data,28)>PhysicsBodyReadSet.Capacity || R32(data,108)>PhysicsContactWorkRead.OccurrenceCapacity ||
            R32(data,112)>CacheCapacity || R32(data,116)>PhysicsSceneDeclaration.OrientationSensorCapacity || !AllZero(data[120..128]))
            throw new ArgumentException("Unsupported generic physics record.");
    }
    private static bool AllZero(ReadOnlySpan<byte> bytes) => bytes.IndexOfAnyExcept((byte)0) < 0;
    private static uint BodySlot(PhysicsSceneDeclaration scene, GpuBodyId id)
    {
        for (var i = 0; i < scene.Bodies.Length; i++) if (scene.Bodies[i].Id == id) return (uint)i;
        throw new ArgumentException("Absent body.");
    }
    private static uint MaterialSlot(PhysicsSceneDeclaration scene, GpuMaterialId id)
    {
        for (var i = 0; i < scene.Materials.Length; i++) if (scene.Materials[i].Id == id) return (uint)i;
        throw new ArgumentException("Absent material.");
    }
    private static void Pose(Span<byte> bytes, int offset, RigidLocalPose pose)
    {
        Vector(bytes, offset, pose.Translation.X, pose.Translation.Y, pose.Translation.Z);
        Rotation(bytes, offset + 8, pose.Rotation);
    }
    private static void Rotation(Span<byte> bytes, int offset, CanonicalRotation value)
    {
        Vector(bytes, offset, value.X, value.Y, value.Z); H(bytes, offset + 6, value.W);
    }
    private static CanonicalRotation ReadRotation(ReadOnlySpan<byte> bytes, int offset) =>
        new(RH(bytes, offset), RH(bytes, offset + 2), RH(bytes, offset + 4), RH(bytes, offset + 6));
    private static void Cell(Span<byte> bytes, int offset, CellOrigin value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(bytes[offset..], value.X);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[(offset + 4)..], value.Y);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[(offset + 8)..], value.Z);
    }
    private static CellOrigin ReadCell(ReadOnlySpan<byte> bytes, int offset) =>
        new(BinaryPrimitives.ReadInt32LittleEndian(bytes[offset..]),
            BinaryPrimitives.ReadInt32LittleEndian(bytes[(offset + 4)..]),
            BinaryPrimitives.ReadInt32LittleEndian(bytes[(offset + 8)..]));
    private static void Local(Span<byte> bytes, int offset, LocalPosition value) =>
        Vector(bytes, offset, value.X, value.Y, value.Z);
    private static LocalPosition ReadLocal(ReadOnlySpan<byte> bytes, int offset) =>
        new(RH(bytes, offset), RH(bytes, offset + 2), RH(bytes, offset + 4));
    private static void Vector(Span<byte> bytes, int offset, Half x, Half y, Half z)
    {
        H(bytes, offset, x); H(bytes, offset + 2, y); H(bytes, offset + 4, z);
    }
    private static void F32Vector(Span<byte> bytes, int offset, float x, float y, float z)
    {
        BinaryPrimitives.WriteSingleLittleEndian(bytes[offset..], x);
        BinaryPrimitives.WriteSingleLittleEndian(bytes[(offset + 4)..], y);
        BinaryPrimitives.WriteSingleLittleEndian(bytes[(offset + 8)..], z);
    }
    private static float RF(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadSingleLittleEndian(bytes[offset..]);
    private static void H(Span<byte> bytes, int offset, Half value) =>
        BinaryPrimitives.WriteUInt16LittleEndian(bytes[offset..], BitConverter.HalfToUInt16Bits(value));
    private static Half RH(ReadOnlySpan<byte> bytes, int offset) =>
        BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16LittleEndian(bytes[offset..]));
    private static void U32(Span<byte> bytes, int offset, uint value) =>
        BinaryPrimitives.WriteUInt32LittleEndian(bytes[offset..], value);
    private static void U64(Span<byte> bytes, int offset, ulong value) =>
        BinaryPrimitives.WriteUInt64LittleEndian(bytes[offset..], value);
    private static uint R32(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]);
    private static ulong R64(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadUInt64LittleEndian(bytes[offset..]);
}
