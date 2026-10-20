using System;
using System.Buffers.Binary;
using System.Globalization;

namespace CuriousContraptions.Gpu;

public enum PhysicsStateVersion : uint { GenericMechanical = 12 }
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
    public const int BodyBytes = 160;
    // Body: f32 pose32/48, velocities64/76; immutable mass/material lanes start88.
    public const int BodyVelocityOffset = 64;
    public const int BodyAngularVelocityOffset = 76;
    public const int BodyMassOffset = 88;
    public const int BodyDragOffset = 90;
    public const int BodyGravityOffset = 92;
    public const int BodyCentreOfMassOffset = 98;
    // Body record: canonical cell (i32 x3), f32 cell remainder, f32 rotation quaternion, principal frame and moments.
    public const int BodyCellOffset = 16;
    public const int BodyLocalOffset = 32;
    public const int BodyRotationOffset = 48;
    public const int BodyPrincipalFrameOffset = 104;
    /// <summary>Three principal moments, each a Half mantissa and an Int32 binary exponent in an 8-byte slot.</summary>
    public const int BodyPrincipalInertiaOffset = 120;
    public const int PrincipalInertiaBytes = 8;
    public const int PrincipalExponentOffset = 4;
    /// <summary>Contact-work record: declared preload (the store's capacity) and the f32 work still available.</summary>
    public const int ContactWorkPreloadOffset = 36;
    public const int ContactWorkAvailableOffset = 48;
    /// <summary>Contact-work record: the f32 target launch speed and the minimum f32 approach speed that admits a payout.</summary>
    public const int ContactWorkTargetSpeedOffset = 16;
    public const int ContactWorkAdmissionSpeedOffset = 20;
    public const int ContactWorkCooldownOffset = 24;
    public const int ContactWorkOccurrenceCountOffset = 32;
    /// <summary>Work-occurrence record: the owner collider slot (u16), the last qualified occurrence's sequence and physical-step
    /// ordinal, and its f32 approach speed.</summary>
    public const int WorkOccurrenceColliderOffset = 2;
    public const int WorkOccurrenceSequenceOffset = 12;
    public const int WorkOccurrenceOrdinalOffset = 16;
    public const int WorkOccurrenceApproachOffset = 22;
    /// <summary>Work-occurrence record: the binary16 event phase, the f32 work debited, the effect byte, and the zero padding from 31.</summary>
    public const int WorkOccurrencePhaseOffset = 20;
    public const int WorkOccurrenceDebitOffset = 26;
    public const int WorkOccurrenceEffectOffset = 30;
    public const int WorkOccurrencePaddingOffset = 31;
    public const int ColliderBytes = 96;
    public const int MaterialBytes = 32;
    public const int SensorBytes = 128;
    public const int GuideBytes = 64;
    public const int TriggerBytes = 64;
    public const int BodiesOffset = HeaderBytes;
    public const int MaterialsOffset = BodiesOffset + PhysicsSceneDeclaration.BodyCapacity * BodyBytes;
    public const int SensorsOffset = MaterialsOffset + PhysicsSceneDeclaration.MaterialCapacity * MaterialBytes;
    public const int GuidesOffset = SensorsOffset + PhysicsSceneDeclaration.SensorCapacity * SensorBytes;
    public const int TriggersOffset = GuidesOffset + PhysicsSceneDeclaration.GuideCapacity * GuideBytes;
    public const int ContactWorkBytes = 64;
    public const int ContactWorksOffset = TriggersOffset + PhysicsSceneDeclaration.TriggerCapacity * TriggerBytes;
    public const int WorkOccurrencesOffset = ContactWorksOffset + PhysicsSceneDeclaration.ContactWorkCapacity * ContactWorkBytes;
    public const int WorkOccurrenceBytes = 32;
    // Orientation sensors: declaration (id, body slot, initial rotation, cos half-angle) in the first 32 bytes, sticky fired state after.
    public const int OrientationSensorBytes = 64;
    /// <summary>Orientation-sensor record: the declared f32 rotation quaternion (x, y, z, w).</summary>
    public const int OrientationSensorRotationOffset = 16;
    public const int OrientationSensorsOffset = WorkOccurrencesOffset + PhysicsContactWorkRead.OccurrenceCapacity * WorkOccurrenceBytes;
    public const int MotionOffset = OrientationSensorsOffset + PhysicsSceneDeclaration.OrientationSensorCapacity * OrientationSensorBytes;
    /// <summary>Story 9R.2: the collider table is the buffer's tail, after every fixed-capacity table, sized by the scene's own
    /// collider count; there is no collider ceiling.</summary>
    public const int CollidersOffset = ContactLoadRecordsOffset + PhysicsSceneDeclaration.ContactLoadSensorCapacity * ContactLoadSensorBytes;
    public const uint NoBody = uint.MaxValue;

    /// <summary>The one layout authority: the exact world length for a scene with this many colliders.</summary>
    public static int ByteLength(int colliders)
    {
        if (colliders < 0) throw new ArgumentException("A collider count cannot be negative.", nameof(colliders));
        return checked(CollidersOffset + colliders * ColliderBytes);
    }

    /// <summary>The exact length a world buffer must have, derived from its own header's collider count.</summary>
    public static int ByteLength(ReadOnlySpan<byte> header)
    {
        if (header.Length < HeaderBytes) throw new ArgumentException("A physics world is shorter than its header.");
        var length = CollidersOffset + (long)R32(header, HeaderColliderCountOffset) * ColliderBytes;
        if (length > int.MaxValue) throw new ArgumentException("Unsupported generic physics record.");
        return (int)length;
    }
    // A binary eighth-second primary segment bounds elapsed-value quantization.
    public const uint PrimarySegmentSteps = WorkshopCadenceSettings.PhysicalFrequency / 8;

    public static byte[] Admission(PhysicsSceneDeclaration scene, SimulationEpoch epoch, WorkshopGpuProfile profile)
    {
        ArgumentNullException.ThrowIfNull(scene); profile.Validate();
        if (epoch.Value == 0) throw new ArgumentException("A world epoch is required.");
        var bytes = new byte[ByteLength(scene.Colliders.Length)]; var data = bytes.AsSpan();
        U32(data, HeaderVersionOffset, (uint)PhysicsStateVersion.GenericMechanical);
        U32(data, HeaderBodyCountOffset, (uint)scene.Bodies.Length); U32(data, HeaderColliderCountOffset, (uint)scene.Colliders.Length);
        U32(data, HeaderMaterialCountOffset, (uint)scene.Materials.Length); U32(data, HeaderSensorCountOffset, (uint)scene.Sensors.Length);
        U32(data, HeaderDynamicCountOffset, 0); U32(data, HeaderGuideCountOffset, (uint)scene.Guides.Length);
        U32(data, HeaderTriggerCountOffset, (uint)scene.Triggers.Length);
        U32(data, HeaderContactWorkCountOffset, (uint)scene.ContactWorks.Length);
        U32(data, HeaderOrientationSensorCountOffset, (uint)scene.OrientationSensors.Length);
        U64(data, HeaderEpochOffset, epoch.Value); U32(data, HeaderCadenceOffset, (uint)profile.Cadence);
        U32(data, HeaderPhysicalProfileOffset, (uint)profile.Physical); U64(data, HeaderRevisionOffset, profile.Revision.Value);
        U64(data, HeaderDocumentLowOffset, scene.Document.Low); U64(data, HeaderDocumentHighOffset, scene.Document.High);
        U64(data, HeaderNextIdentityOffset, scene.NextIdentity);
        for (var i = 0; i < scene.Bodies.Length; i++)
        {
            var body = scene.Bodies[i]; var record = data.Slice(scene.Layout.GetBodyOffset(body.Id), BodyBytes);
            U64(record, 0, body.Id.Value); U32(record, 8, (uint)body.Motion);
            Cell(record, BodyCellOffset, body.Cell); Local(record, BodyLocalOffset, body.Local); Rotation(record, BodyRotationOffset, body.Rotation);
            F32Vector(record, BodyVelocityOffset, body.Velocity.X, body.Velocity.Y, body.Velocity.Z);
            F32Vector(record, BodyAngularVelocityOffset, body.AngularVelocity.X, body.AngularVelocity.Y, body.AngularVelocity.Z);
            H(record, BodyMassOffset, body.Mass.Value); H(record, BodyDragOffset, body.LinearDrag.Value);
            Vector(record, BodyGravityOffset, body.Gravity.X, body.Gravity.Y, body.Gravity.Z);
            if (body.Motion == RigidMotionKind.Dynamic)
            {
                U32(data,HeaderDynamicCountOffset,R32(data,HeaderDynamicCountOffset)+1);
                for (var collider=0; collider<scene.Colliders.Length; collider++)
                    if (scene.Colliders[collider].Body==body.Id)
                    {
                        var properties=RigidMassProperties.Compile(body,scene.Colliders[collider]);
                        Vector(record,BodyCentreOfMassOffset,properties.LocalCentreOfMass.X,properties.LocalCentreOfMass.Y,properties.LocalCentreOfMass.Z);
                        Rotation(record,BodyPrincipalFrameOffset,properties.PrincipalFrame);
                        Principal(record,BodyPrincipalInertiaOffset,properties.X);
                        Principal(record,BodyPrincipalInertiaOffset+PrincipalInertiaBytes,properties.Y);
                        Principal(record,BodyPrincipalInertiaOffset+2*PrincipalInertiaBytes,properties.Z);
                        U32(record,144,(uint)collider);
                    }
            }
        }
        for (var i = 0; i < scene.Colliders.Length; i++)
        {
            var collider = scene.Colliders[i]; var record = data.Slice(scene.Layout.GetColliderOffset(collider.Id), ColliderBytes);
            U64(record, 0, collider.Id.Value); U32(record, 8, BodySlot(scene, collider.Body));
            U32(record, 12, MaterialSlot(scene, collider.Material)); U32(record, 16, (uint)collider.Shape);
            Pose(record, 24, collider.Pose); H(record, 48, collider.Radius.Value);
            Vector(record, 50, collider.HalfExtents.X, collider.HalfExtents.Y, collider.HalfExtents.Z);
        }
        for (var i = 0; i < scene.Materials.Length; i++)
        {
            var material = scene.Materials[i]; var record = data.Slice(scene.Layout.GetMaterialOffset(material.Id), MaterialBytes);
            U64(record, 0, material.Id.Value); H(record, 8, material.Restitution.Value);
            H(record, 10, material.BounceThreshold.Value); H(record, 12, material.Friction.Value);
            H(record, 14, material.RollingResistance.Value);
        }
        for (var i = 0; i < scene.Sensors.Length; i++)
        {
            var sensor = scene.Sensors[i]; var record = data.Slice(scene.Layout.GetSensorOffset(sensor.Id), SensorBytes);
            U64(record, 0, sensor.Id.Value); U32(record, 8, BodySlot(scene, sensor.Frame));
            U32(record, 12, BodySlot(scene, sensor.Target)); U32(record, 16, (uint)sensor.Participation);
            Pose(record, 24, sensor.Pose);
            Vector(record, 48, sensor.Minimum.X, sensor.Minimum.Y, sensor.Minimum.Z);
            Vector(record, 56, sensor.Maximum.X, sensor.Maximum.Y, sensor.Maximum.Z);
            H(record, 64, sensor.SpeedLimit.Value); H(record, 66, sensor.Dwell.Value);
        }
        for (var i = 0; i < scene.Guides.Length; i++)
        {
            var guide = scene.Guides[i]; var record = data.Slice(scene.Layout.GetGuideOffset(guide.Id), GuideBytes);
            U64(record, 0, guide.Id.Value); U32(record, 8, BodySlot(scene, guide.Frame));
            U32(record, 12, BodySlot(scene, guide.Target)); Pose(record, 16, guide.Pose);
            Vector(record, 40, guide.Minimum.X, guide.Minimum.Y, guide.Minimum.Z);
            Vector(record, 48, guide.Maximum.X, guide.Maximum.Y, guide.Maximum.Z);
            H(record, 56, guide.MaximumAcceleration.Value); H(record, 58, guide.SupportHeight.Value);
            H(record, 60, guide.SupportMargin.Value);
        }
        for (var i = 0; i < scene.Triggers.Length; i++)
        {
            var trigger = scene.Triggers[i]; var record = data.Slice(scene.Layout.GetTriggerOffset(trigger.Id), TriggerBytes);
            U64(record, 0, trigger.Id.Value); U32(record, TriggerOwnerOffset, BodySlot(scene, trigger.Owner));
            U32(record, TriggerTargetKindOffset, (uint)trigger.Targets.Kind); H(record, TriggerThresholdOffset, trigger.Threshold.Value);
            U32(record, TriggerNamedTargetOffset, trigger.Targets.Kind == BodyTargetKind.NamedBody ? BodySlot(scene, trigger.Targets.Body) : NoBody);
        }
        AdmitContactWorks(data, scene);
        for (var i = 0; i < scene.OrientationSensors.Length; i++)
        {
            var sensor = scene.OrientationSensors[i];
            var record = data.Slice(scene.Layout.GetOrientationSensorOffset(sensor.Id), OrientationSensorBytes);
            U64(record, 0, sensor.Id.Value); U32(record, 8, BodySlot(scene, sensor.Body));
            Rotation(record, OrientationSensorRotationOffset, sensor.Initial); H(record, 32, sensor.Threshold.CosineHalfAngle);
        }
        WriteElectricalAdmission(data, scene);
        WritePrismaticAdmission(data, scene);
        WriteContactLoadAdmission(data, scene);
        return bytes;
    }

    public static WorkshopGpuProfile ReadProfile(ReadOnlySpan<byte> data)
    {
        Header(data);
        var profile = new WorkshopGpuProfile((SimulationCadence)R32(data, HeaderCadenceOffset),
            (PhysicalStepProfile)R32(data, HeaderPhysicalProfileOffset), new(R64(data, HeaderRevisionOffset)));
        profile.Validate(); return profile;
    }

    public static PhysicsFailure ReadFailure(ReadOnlySpan<byte> data)
    {
        Header(data);
        var status = (PhysicsCandidateStatus)R32(data, HeaderStatusOffset); var failure = (PhysicsFailure)R32(data, HeaderFailureOffset);
        if (!Enum.IsDefined(status) || !Enum.IsDefined(failure) ||
            (status == PhysicsCandidateStatus.Committed) != (failure == PhysicsFailure.None))
            throw new ArgumentException("Invalid generic candidate status.");
        return failure;
    }

    public static PhysicsBodyReadSet ReadDynamicBodies(ReadOnlySpan<byte> data)
    {
        if (ReadFailure(data) != PhysicsFailure.None) throw new ArgumentException("Candidate is not committed.");
        var profile = ReadProfile(data); var epoch = R64(data, HeaderEpochOffset); var tick = R64(data, HeaderTickOffset);
        if (epoch == 0 || tick > profile.RunTickLimit ||
            R32(data, HeaderSourceOrdinalOffset) != checked((uint)(tick * profile.Substeps)))
            throw new ArgumentException("Invalid generic physical time.");
        Span<PhysicsBodyRead> values = stackalloc PhysicsBodyRead[PhysicsBodyReadSet.Capacity];
        var count = 0;
        for (var slot = 0; slot < R32(data, HeaderBodyCountOffset); slot++)
        {
            var record = data.Slice(BodiesOffset + slot * BodyBytes, BodyBytes);
            var motion = (RigidMotionKind)R32(record, 8);
            if (!Enum.IsDefined(motion)) throw new ArgumentException("Undefined rigid motion.");
            if (motion == RigidMotionKind.Static) continue;
            if (count == values.Length) throw new ArgumentException("Dynamic body capacity exceeded.");
            // The read set validates every value: non-finite or out-of-envelope f32 velocity (|v| > 64 m/s, |w| > 128 rad/s) rejects the read.
            var body = new CanonicalBody(new(R64(record, 0)), epoch, tick,
                ReadCell(record, BodyCellOffset), ReadLocal(record, BodyLocalOffset),
                new(RF(record, BodyVelocityOffset), RF(record, BodyVelocityOffset + 4), RF(record, BodyVelocityOffset + 8)));
            values[count++] = new(body, ReadRotation(record, BodyRotationOffset),
                new(RF(record, BodyAngularVelocityOffset), RF(record, BodyAngularVelocityOffset + 4), RF(record, BodyAngularVelocityOffset + 8)),
                ReadCentreOfMass(record));
        }
        var result = new PhysicsBodyReadSet(values[..count]);
        result.ValidateTime(new(epoch), new(tick)); return result;
    }

    public static ResidenceRead ReadSensor(ReadOnlySpan<byte> data, int slot)
    {
        if (ReadFailure(data) != PhysicsFailure.None || slot < 0 || (uint)slot >= R32(data, HeaderSensorCountOffset))
            throw new ArgumentException("Invalid sensor read.");
        var record = data.Slice(SensorsOffset + slot * SensorBytes, SensorBytes);
        var phase = (ResidencePhase)R32(record, 72);
        var result = new ResidenceRead(new(R64(record, 0)), phase, R32(record, 84),
            R32(record, 76), RH(record, 80), R32(record, 88), RH(record, 92));
        if (!Enum.IsDefined(phase) || result.Id.Value == 0 || result.OccurrenceCount > 1 ||
            !Half.IsFinite(result.StartPhase) || result.StartPhase < (Half)(-2048) || result.StartPhase >= (Half)2048 ||
            !Half.IsFinite(result.EventPhase) || result.EventPhase < (Half)(-2048) || result.EventPhase >= (Half)2048 ||
            !AdmittedTime(result.StartOrdinal, result.StartPhase, R32(data, HeaderSourceOrdinalOffset)) ||
            !AdmittedTime(result.EventOrdinal, result.EventPhase, R32(data, HeaderSourceOrdinalOffset)) ||
            (phase == ResidencePhase.Outside && (result.StartOrdinal != 0 || !PhysicsDeclarationBounds.Zero(result.StartPhase))) ||
            (result.OccurrenceCount == 0 && (result.EventOrdinal != 0 || !PhysicsDeclarationBounds.Zero(result.EventPhase))) ||
            (result.OccurrenceCount != 0 && phase == ResidencePhase.Qualified && Before(result.EventOrdinal, result.EventPhase, result.StartOrdinal, result.StartPhase)) ||
            !AllZero(record[82..84]) || !AllZero(record[94..128]))
            throw new ArgumentException("Invalid committed residence state.");
        return result;
    }


    public static ContactTriggerRead ReadTrigger(ReadOnlySpan<byte> data, int slot)
    {
        if (ReadFailure(data) != PhysicsFailure.None || slot < 0 || (uint)slot >= R32(data, HeaderTriggerCountOffset))
            throw new ArgumentException("Invalid contact trigger read.");
        var record = data.Slice(TriggersOffset + slot * TriggerBytes, TriggerBytes);
        var owner = R32(record, TriggerOwnerOffset); var kind = (BodyTargetKind)R32(record, TriggerTargetKindOffset);
        var named = R32(record, TriggerNamedTargetOffset); var target = R32(record, TriggerBodyOffset); var count = R32(record, TriggerCountOffset);
        var bodyCount = R32(data, HeaderBodyCountOffset);

        var threshold = RH(record, TriggerThresholdOffset);
        if (R64(record, 0) == 0 || owner >= bodyCount ||
            R32(record, TriggerTargetKindOffset) is < (uint)BodyTargetKind.NamedBody or > (uint)BodyTargetKind.AllDynamic || !Enum.IsDefined(kind) ||
            (kind == BodyTargetKind.NamedBody ? (named >= bodyCount || BodyMotion(data, named) != RigidMotionKind.Dynamic) || named == owner : named != NoBody) ||
            BodyMotion(data, owner) != RigidMotionKind.Static ||
            !Half.IsFinite(threshold) || threshold < (Half)0 || threshold > (Half)64 ||
            !AllZero(record[TriggerThresholdPaddingOffset..TriggerNamedTargetOffset]) || !AllZero(record[TriggerNamedPaddingOffset..TriggerCountOffset]) ||
            !AllZero(record[TriggerPaddingOffset..]) || count > 1)
            throw new ArgumentException("Invalid contact trigger declaration or padding.");
        var collider = default(GpuColliderId); var targetId = default(GpuBodyId);
        if (count == 0)
        {
            if (!AllZero(record[TriggerColliderOffset..TriggerPaddingOffset])) throw new ArgumentException("An absent impact retained event data.");
        }
        else
        {
            var colliderSlot = R32(record, TriggerColliderOffset);
            if ((target >= bodyCount || BodyMotion(data, target) != RigidMotionKind.Dynamic) || (kind == BodyTargetKind.NamedBody && target != named) ||
                colliderSlot >= R32(data, HeaderColliderCountOffset) ||
                R32(data, CollidersOffset + checked((int)colliderSlot) * ColliderBytes + ColliderBodyOffset) != owner ||
                !AdmittedTime(R32(record, TriggerOrdinalOffset), RH(record, TriggerPhaseOffset), R32(data, HeaderSourceOrdinalOffset)) ||
                !Half.IsFinite(RH(record, TriggerApproachOffset)) || RH(record, TriggerApproachOffset) < threshold || RH(record, TriggerApproachOffset) > (Half)4096)
                throw new ArgumentException("Invalid qualifying impact identity, time or normal speed.");
            collider = new(R64(data, CollidersOffset + checked((int)colliderSlot) * ColliderBytes));
            targetId = new(R64(data, BodiesOffset + checked((int)target) * BodyBytes));
        }
        return new(new(R64(record, 0)), new(R64(data, BodiesOffset + checked((int)owner) * BodyBytes)),
            targetId, count, collider, R32(record, TriggerOrdinalOffset), RH(record, TriggerPhaseOffset), new(RH(record, TriggerApproachOffset)));
    }

    public static OrientationSensorRead ReadOrientationSensor(ReadOnlySpan<byte> data, int slot)
    {
        if (ReadFailure(data) != PhysicsFailure.None || slot < 0 || (uint)slot >= R32(data, HeaderOrientationSensorCountOffset))
            throw new ArgumentException("Invalid orientation sensor read.");
        var record = data.Slice(OrientationSensorsOffset + slot * OrientationSensorBytes, OrientationSensorBytes);
        var body = R32(record, 8); var phase = (OrientationSensorPhase)R32(record, 40);
        if (R64(record, 0) == 0 || body >= R32(data, HeaderBodyCountOffset) || !Enum.IsDefined(phase) ||
            !AllZero(record[12..16]) || !AllZero(record[34..40]) || !AllZero(record[50..]))
            throw new ArgumentException("Invalid orientation sensor declaration or padding.");
        var bodyRecord = data.Slice(BodiesOffset + checked((int)body) * BodyBytes, BodyBytes);
        if ((RigidMotionKind)R32(bodyRecord, 8) != RigidMotionKind.Dynamic)
            throw new ArgumentException("Orientation sensor body is not dynamic.");
        ReadRotation(record, OrientationSensorRotationOffset).Validate();
        new OrientationThreshold(RH(record, 32)).Validate();
        var ordinal = R32(record, 44); var eventPhase = RH(record, 48);
        // Endpoint-sampled: a fired sensor names one substep boundary inside the admitted time; an armed one holds no event.
        if (phase == OrientationSensorPhase.Armed
            ? ordinal != 0 || !PhysicsDeclarationBounds.Zero(eventPhase)
            : ordinal == 0 || !PhysicsDeclarationBounds.Zero(eventPhase) || !AdmittedTime(ordinal, eventPhase, R32(data, HeaderSourceOrdinalOffset)))
            throw new ArgumentException("Invalid orientation sensor event.");
        var colliderSlot = R32(bodyRecord, 144);
        if (colliderSlot >= R32(data, HeaderColliderCountOffset) || R32(data, CollidersOffset + checked((int)colliderSlot) * ColliderBytes + 8) != body)
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
        // The header's identity, scene counts and profile are fixed; status, failure, tick, ordinal and capture change.
        // Both worlds passed Header() against their own length, so equal collider counts (compared below) mean equal lengths.
        if (expectedTick.Value > profile.RunTickLimit || R64(candidate, HeaderTickOffset) != expectedTick.Value ||
            candidate.Length != source.Length ||
            R32(candidate, HeaderSourceOrdinalOffset) != checked((uint)(expectedTick.Value * profile.Substeps)) ||
            !candidate[..HeaderStatusOffset].SequenceEqual(source[..HeaderStatusOffset]) ||
            !candidate[HeaderBodyCountOffset..HeaderTickOffset].SequenceEqual(source[HeaderBodyCountOffset..HeaderTickOffset]) ||
            !candidate[HeaderCadenceOffset..HeaderSourceOrdinalOffset].SequenceEqual(source[HeaderCadenceOffset..HeaderSourceOrdinalOffset]) ||
            !candidate[HeaderGuideCountOffset..HeaderBytes].SequenceEqual(source[HeaderGuideCountOffset..HeaderBytes]))
            throw new ArgumentException("Candidate identity, scene counts or physical profile changed.");
        var bodyCount=checked((int)R32(source,HeaderBodyCountOffset)); var dynamicCount=0;
        for (var i=0; i<bodyCount; i++)
        {
            var actual=candidate.Slice(BodiesOffset+i*BodyBytes,BodyBytes);
            var previous=source.Slice(BodiesOffset+i*BodyBytes,BodyBytes);
            var motion=(RigidMotionKind)R32(previous,BodyMotionOffset);
            if (motion==RigidMotionKind.Static)
            {
                if (!actual.SequenceEqual(previous)) throw new ArgumentException("Static body changed.");
                continue;
            }
            if (motion!=RigidMotionKind.Dynamic) throw new ArgumentException("Undefined rigid motion.");
            dynamicCount++;
            if (!actual[..BodyCellOffset].SequenceEqual(previous[..BodyCellOffset]) || !actual[BodyMassOffset..].SequenceEqual(previous[BodyMassOffset..]) ||
                !AllZero(actual[28..32]) || !AllZero(actual[44..48]))
                throw new ArgumentException("Dynamic immutable declaration or padding changed.");
            ReadMassProperties(actual).Validate();
        }
        if (dynamicCount!=R32(source,HeaderDynamicCountOffset) || dynamicCount>PhysicsBodyReadSet.Capacity ||
            !AllZero(candidate.Slice(BodiesOffset+bodyCount*BodyBytes,(PhysicsSceneDeclaration.BodyCapacity-bodyCount)*BodyBytes)) ||
            !candidate[MaterialsOffset..SensorsOffset].SequenceEqual(source[MaterialsOffset..SensorsOffset]) ||
            !candidate[CollidersOffset..].SequenceEqual(source[CollidersOffset..]))
            throw new ArgumentException("Candidate changed body population, geometry or material.");
        var bodies = ReadDynamicBodies(candidate);
        uint captured = 0;
        var sensorCount = checked((int)R32(source, HeaderSensorCountOffset));
        for (var i = 0; i < sensorCount; i++)
        {
            var offset = SensorsOffset + i * SensorBytes;
            if (!candidate.Slice(offset, 72).SequenceEqual(source.Slice(offset, 72)))
                throw new ArgumentException("Sensor declaration changed.");
            var current = ReadSensor(candidate, i); var previous = ReadSensor(source, i);
            if ((SensorParticipation)R32(source, offset + 16) == SensorParticipation.Disabled &&
                (current.Phase != ResidencePhase.Outside || current.OccurrenceCount != 0))
                throw new ArgumentException("A disabled sensor produced residence.");
            var sourceOrdinal = R32(source, HeaderSourceOrdinalOffset);
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
        if (R32(candidate, HeaderCapturedCountOffset) != captured ||
            !AllZero(candidate[(SensorsOffset + sensorCount * SensorBytes)..GuidesOffset]))
            throw new ArgumentException("Capture count or unused sensor slots changed.");
        if (!candidate[GuidesOffset..TriggersOffset].SequenceEqual(source[GuidesOffset..TriggersOffset]))
            throw new ArgumentException("Guide declaration changed.");
        var triggerCount = checked((int)R32(source, HeaderTriggerCountOffset));
        for (var i = 0; i < triggerCount; i++)
        {
            var offset = TriggersOffset + i * TriggerBytes;
            if (!candidate.Slice(offset, 32).SequenceEqual(source.Slice(offset, 32)))
                throw new ArgumentException("Contact trigger declaration changed.");
            var trigger = ReadTrigger(candidate, i);
            if (trigger.OccurrenceCount != 0 && (expectedTick.Value == 0 ||
                Before(trigger.EventOrdinal, trigger.EventPhase, R32(source, HeaderSourceOrdinalOffset), (Half)0)))
                throw new ArgumentException("Impact event does not belong to this candidate interval.");
        }
        if (!AllZero(candidate[(TriggersOffset + triggerCount * TriggerBytes)..ContactWorksOffset]))
            throw new ArgumentException("Unused contact trigger slots changed.");
        ValidateContactWorkCandidate(candidate, source, expectedTick);
        var orientationCount = checked((int)R32(source, HeaderOrientationSensorCountOffset));
        for (var i = 0; i < orientationCount; i++)
        {
            var offset = OrientationSensorsOffset + i * OrientationSensorBytes;
            if (!candidate.Slice(offset, 40).SequenceEqual(source.Slice(offset, 40)))
                throw new ArgumentException("Orientation sensor declaration changed.");
            // At admission the body record still holds the admitted rotation: the sensor's initial pose must be exactly that.
            // Later ticks keep the declaration bytes equal to the source, so the invariant carries forward.
            if (expectedTick.Value == 0 && !candidate.Slice(offset + 16, 16).SequenceEqual(
                    candidate.Slice(BodiesOffset + checked((int)R32(candidate, offset + 8)) * BodyBytes + 48, 16)))
                throw new ArgumentException("Orientation sensor initial pose differs from its body's admitted rotation.");
            var current = ReadOrientationSensor(candidate, i); var previous = ReadOrientationSensor(source, i);
            // Sticky within a world: a fired sensor never regresses or re-emits; a new event belongs to this interval.
            if (previous.Phase == OrientationSensorPhase.Fired)
            {
                if (current != previous) throw new ArgumentException("A fired orientation sensor regressed or emitted again.");
            }
            else if (current.Phase == OrientationSensorPhase.Fired && (expectedTick.Value == 0 || current.EventOrdinal <= R32(source, HeaderSourceOrdinalOffset)))
                throw new ArgumentException("Orientation event does not belong to this candidate interval.");
        }
        if (!AllZero(candidate[(OrientationSensorsOffset + orientationCount * OrientationSensorBytes)..MotionOffset]))
            throw new ArgumentException("Unused orientation sensor slots changed.");
        if (expectedTick.Value != 0 && (R32(candidate, MotionOffset + 4) != profile.Substeps ||
            R32(candidate, MotionOffset + 12) != R32(candidate, HeaderSourceOrdinalOffset)))
            throw new ArgumentException("Motion profile differs from its committed world.");
        ValidateElectricalCandidate(candidate,source);
        ValidatePrismaticCandidate(candidate,source);
        ValidateContactLoadCandidate(candidate,source);
        return new(bodies, PhysicsMotionRead.Decode(candidate.Slice(MotionOffset,PhysicsMotionRead.ByteLength), bodies, expectedTick));
    }


    private static void Principal(Span<byte> bytes,int offset,PrincipalInertia value)
    {
        value.Validate(); H(bytes,offset,value.Mantissa);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[(offset+PrincipalExponentOffset)..],value.Exponent);
    }
    private static RigidMassProperties ReadMassProperties(ReadOnlySpan<byte> record) =>
        new(ReadCentreOfMass(record),ReadRotation(record,BodyPrincipalFrameOffset),
            ReadPrincipal(record,BodyPrincipalInertiaOffset),ReadPrincipal(record,BodyPrincipalInertiaOffset+PrincipalInertiaBytes),
            ReadPrincipal(record,BodyPrincipalInertiaOffset+2*PrincipalInertiaBytes));
    private static MetreVector ReadCentreOfMass(ReadOnlySpan<byte> record) =>
        new(RH(record,BodyCentreOfMassOffset),RH(record,BodyCentreOfMassOffset+2),RH(record,BodyCentreOfMassOffset+4));
    private static PrincipalInertia ReadPrincipal(ReadOnlySpan<byte> record,int offset)
    {
        if (!AllZero(record.Slice(offset+2,2))) throw new ArgumentException("Nonzero inertia padding.");
        var value=new PrincipalInertia(RH(record,offset),BinaryPrimitives.ReadInt32LittleEndian(record[(offset+PrincipalExponentOffset)..]));
        value.Validate(); return value;
    }
    private static bool AdmittedTime(uint ordinal, Half phase, uint end) =>
        Half.IsFinite(phase) && phase >= (Half)(-2048) && phase < (Half)2048 &&
        ordinal <= end && (ordinal != 0 || phase >= (Half)0) && (ordinal != end || phase <= (Half)0);
    private static bool Before(uint a, Half ap, uint b, Half bp) => a < b || (a == b && ap < bp);

    public static string ShaderPreamble() => FormattableString.Invariant($@"
enable f16;
const PHYSICAL_RATE:f16={WorkshopCadenceSettings.PhysicalFrequency}h;
const PRIMARY_SEGMENT_STEPS:u32={PrimarySegmentSteps}u;
const BODY_CAPACITY:u32={PhysicsSceneDeclaration.BodyCapacity}u;
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
        if (data.Length < HeaderBytes || data.Length != ByteLength(data) ||
            R32(data, HeaderVersionOffset) != (uint)PhysicsStateVersion.GenericMechanical ||
            R32(data, HeaderBodyCountOffset) > PhysicsSceneDeclaration.BodyCapacity ||
            R32(data, HeaderMaterialCountOffset) > PhysicsSceneDeclaration.MaterialCapacity ||
            R32(data, HeaderSensorCountOffset) > PhysicsSceneDeclaration.SensorCapacity ||
            R32(data, HeaderGuideCountOffset) > PhysicsSceneDeclaration.GuideCapacity ||
            R32(data, HeaderTriggerCountOffset) > PhysicsSceneDeclaration.TriggerCapacity ||
            R32(data, HeaderContactWorkCountOffset) > PhysicsSceneDeclaration.ContactWorkCapacity ||
            (R64(data, HeaderDocumentLowOffset) == 0 && R64(data, HeaderDocumentHighOffset) == 0) || R64(data, HeaderNextIdentityOffset) == 0 ||
            R32(data, HeaderDynamicCountOffset) > PhysicsBodyReadSet.Capacity ||
            R32(data, HeaderWorkOccurrenceCountOffset) > PhysicsContactWorkRead.OccurrenceCapacity ||
            R32(data, HeaderReservedOffset) != 0 ||
            R32(data, HeaderOrientationSensorCountOffset) > PhysicsSceneDeclaration.OrientationSensorCapacity ||
            R32(data, HeaderElectricalSourceCountOffset) > ElectricalSupplyPlan.SourceCapacity ||
            R32(data, HeaderElectricalBindingCountOffset) > WorkshopConnections.Capacity)
            throw new ArgumentException("Unsupported generic physics record.");
    }
    private static bool AllZero(ReadOnlySpan<byte> bytes) => bytes.IndexOfAnyExcept((byte)0) < 0;
    private static uint BodySlot(PhysicsSceneDeclaration scene, GpuBodyId id) => (uint)scene.Layout.GetBodySlot(id);
    private static uint MaterialSlot(PhysicsSceneDeclaration scene, GpuMaterialId id) => (uint)scene.Layout.GetMaterialSlot(id);
    private static void Pose(Span<byte> bytes, int offset, RigidLocalPose pose)
    {
        Vector(bytes, offset, pose.Translation.X, pose.Translation.Y, pose.Translation.Z);
        Rotation(bytes, offset + 8, pose.Rotation);
    }
    private static void Rotation(Span<byte> bytes, int offset, CanonicalRotation value)
    {
        F32Vector(bytes, offset, value.X, value.Y, value.Z); BinaryPrimitives.WriteSingleLittleEndian(bytes[(offset+12)..], value.W);
    }
    private static CanonicalRotation ReadRotation(ReadOnlySpan<byte> bytes, int offset) =>
        new(BinaryPrimitives.ReadSingleLittleEndian(bytes[offset..]), BinaryPrimitives.ReadSingleLittleEndian(bytes[(offset+4)..]), BinaryPrimitives.ReadSingleLittleEndian(bytes[(offset+8)..]), BinaryPrimitives.ReadSingleLittleEndian(bytes[(offset+12)..]));
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
        F32Vector(bytes, offset, value.X, value.Y, value.Z);
    private static LocalPosition ReadLocal(ReadOnlySpan<byte> bytes, int offset) =>
        new(BinaryPrimitives.ReadSingleLittleEndian(bytes[offset..]), BinaryPrimitives.ReadSingleLittleEndian(bytes[(offset+4)..]), BinaryPrimitives.ReadSingleLittleEndian(bytes[(offset+8)..]));
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
