using System;
using System.Buffers.Binary;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.Gpu;

public enum PhysicsCandidateStatus : uint { Committed, Invalid }
public enum PhysicsFailure : uint { None, InvalidDeclaration, Domain, ContactBudget, RootBudget, UnsupportedPair, Arithmetic, MotionCapacity }
public enum PhysicsMotionPhase : uint { Free, Supported }
public enum ResidencePhase : uint { Outside, Dwelling, Qualified }
public readonly record struct ResidenceRead(GpuSensorId Id, GpuBodyId Target, ResidencePhase Phase, uint OccurrenceCount,
    uint StartOrdinal, float StartPhase, uint EventOrdinal, float EventPhase)
{
    public void Validate(uint ordinal)
    {
        if(Id.Value==0 || Target.Value==0 || !Enum.IsDefined(Phase) || OccurrenceCount>1 ||
            !PhysicalPhase.IsAdmitted(StartOrdinal,StartPhase) || !PhysicalPhase.IsAdmitted(EventOrdinal,EventPhase) ||
            StartOrdinal>ordinal || EventOrdinal>ordinal ||
            (Phase==ResidencePhase.Outside && (StartOrdinal!=0 || StartPhase!=0)) ||
            (OccurrenceCount==0 && (EventOrdinal!=0 || EventPhase!=0)) ||
            (OccurrenceCount!=0 && Phase==ResidencePhase.Qualified &&
                (EventOrdinal<StartOrdinal || (EventOrdinal==StartOrdinal && EventPhase<StartPhase))))
            throw new ArgumentException("Invalid committed residence state.");
    }
}

public readonly record struct PhysicsCandidateRead(PhysicsBodyReadSet Bodies, PhysicsMotionRead Motion);

/// <summary>One bounded generic scene/state ABI. Every record field is a named offset (PhysicsGpuAbi.Header.cs and below), and
/// every game value is one IEEE-754 f32 (Story 9A.44).</summary>
public static partial class PhysicsGpuAbi
{
    /// <summary>The world header (Story 9R.5b: 144 bytes, every table population among its counts).</summary>
    public const int HeaderBytes = 144;
    public const int BodyBytes = 160;
    // Body: f32 pose32/48, velocities64/76; immutable mass/material lanes start88.
    public const int BodyVelocityOffset = 64;
    public const int BodyAngularVelocityOffset = 76;
    public const int BodyMassOffset = 88;
    public const int BodyDragOffset = 92;
    public const int BodyGravityOffset = 96;
    public const int BodyCentreOfMassOffset = 108;
    // Body record: canonical cell (i32 x3), f32 cell remainder, f32 rotation quaternion, principal frame and moments.
    public const int BodyCellOffset = 16;
    public const int BodyLocalOffset = 32;
    public const int BodyRotationOffset = 48;
    public const int BodyPrincipalFrameOffset = 120;
    /// <summary>Three principal moments, each one f32 in kg m².</summary>
    public const int BodyPrincipalInertiaOffset = 136;
    public const int PrincipalInertiaBytes = 4;
    /// <summary>Contact-work record: declared preload (the store's capacity) and the f32 work still available.</summary>
    public const int ContactWorkPreloadOffset = 36;
    public const int ContactWorkAvailableOffset = 48;
    /// <summary>Contact-work record: the f32 target launch speed and the minimum f32 approach speed that admits a payout.</summary>
    public const int ContactWorkTargetSpeedOffset = 16;
    public const int ContactWorkAdmissionSpeedOffset = 20;
    public const int ContactWorkCooldownOffset = 24;
    public const int ContactWorkOccurrenceCountOffset = 32;
    public const int ColliderBytes = 96;
    public const int MaterialBytes = 32;
    public const int SensorBytes = 96;
    public const int GuideBytes = 96;
    public const int TriggerBytes = 64;
    public const int ContactWorkBytes = 64;
    // Orientation sensors: declaration (id, body slot, initial rotation, cos half-angle) in the first 40 bytes, sticky fired state after.
    public const int OrientationSensorBytes = 64;
    /// <summary>Orientation-sensor record: the declared f32 rotation quaternion (x, y, z, w).</summary>
    public const int OrientationSensorRotationOffset = 16;
    public const uint NoBody = uint.MaxValue;

    /// <summary>Story 9R.5b: every table offset of a world, derived from its own header's populations (no fixed capacity).</summary>
    public static PhysicsWorldLayout Layout(ReadOnlySpan<byte> world) => PhysicsWorldLayout.Read(world);

    /// <summary>The exact length a world buffer must have, derived from its own header's populations.</summary>
    public static int ByteLength(ReadOnlySpan<byte> header) => PhysicsWorldLayout.Read(header).ByteLength;
    // A binary eighth-second primary segment bounds elapsed-value quantization.
    public const uint PrimarySegmentSteps = WorkshopCadenceSettings.PhysicalFrequency / 8;

    public static byte[] Admission(PhysicsSceneDeclaration scene, SimulationEpoch epoch, WorkshopGpuProfile profile)
    {
        ArgumentNullException.ThrowIfNull(scene); profile.Validate();
        if (epoch.Value == 0) throw new ArgumentException("A world epoch is required.");
        var bytes = new byte[scene.World.ByteLength]; var data = bytes.AsSpan();
        U32(data, HeaderReservedOffset, 0);
        scene.World.WriteCounts(data);
        U64(data, HeaderEpochOffset, epoch.Value); U32(data, HeaderCadenceOffset, (uint)profile.Cadence);
        U32(data, HeaderPhysicalProfileOffset, (uint)profile.Physical); U64(data, HeaderRevisionOffset, profile.Revision.Value);
        U64(data, HeaderDocumentLowOffset, scene.Document.Low); U64(data, HeaderDocumentHighOffset, scene.Document.High);
        U64(data, HeaderNextIdentityOffset, scene.NextIdentity);
        for (var i = 0; i < scene.Bodies.Length; i++)
        {
            var body = scene.Bodies[i]; var record = data.Slice(scene.Layout.GetBodyOffset(body.Id), BodyBytes);
            U64(record, RecordIdentityOffset, body.Id.Value); U32(record, BodyMotionOffset, (uint)body.Motion);
            U32(record,BodyPayloadOffset,(uint)body.Payload);
            Cell(record, BodyCellOffset, body.Cell); Local(record, BodyLocalOffset, body.Local); Rotation(record, BodyRotationOffset, body.Rotation);
            F32Vector(record, BodyVelocityOffset, body.Velocity.X, body.Velocity.Y, body.Velocity.Z);
            F32Vector(record, BodyAngularVelocityOffset, body.AngularVelocity.X, body.AngularVelocity.Y, body.AngularVelocity.Z);
            F32(record, BodyMassOffset, body.Mass.Value); F32(record, BodyDragOffset, body.LinearDrag.Value);
            F32Vector(record, BodyGravityOffset, body.Gravity.X, body.Gravity.Y, body.Gravity.Z);
            if (body.Motion == RigidMotionKind.Dynamic)
            {
                var properties = body.MassProperties;
                F32Vector(record, BodyCentreOfMassOffset, properties.LocalCentreOfMass.X, properties.LocalCentreOfMass.Y, properties.LocalCentreOfMass.Z);
                Rotation(record, BodyPrincipalFrameOffset, properties.PrincipalFrame);
                Principal(record, BodyPrincipalInertiaOffset, properties.X);
                Principal(record, BodyPrincipalInertiaOffset + PrincipalInertiaBytes, properties.Y);
                Principal(record, BodyPrincipalInertiaOffset + 2 * PrincipalInertiaBytes, properties.Z);
            }
        }
        for (var i = 0; i < scene.Colliders.Length; i++)
        {
            var collider = scene.Colliders[i]; var record = data.Slice(scene.Layout.GetColliderOffset(collider.Id), ColliderBytes);
            U64(record, RecordIdentityOffset, collider.Id.Value); U32(record, ColliderBodyOffset, BodySlot(scene, collider.Body));
            U32(record, ColliderMaterialOffset, MaterialSlot(scene, collider.Material)); U32(record, ColliderShapeOffset, (uint)collider.Shape);
            Pose(record, ColliderPoseOffset, collider.Pose); F32(record, ColliderRadiusOffset, collider.Radius.Value);
            F32Vector(record, ColliderHalfExtentsOffset, collider.HalfExtents.X, collider.HalfExtents.Y, collider.HalfExtents.Z);
        }
        for (var i = 0; i < scene.Materials.Length; i++)
        {
            var material = scene.Materials[i]; var record = data.Slice(scene.Layout.GetMaterialOffset(material.Id), MaterialBytes);
            U64(record, RecordIdentityOffset, material.Id.Value); F32(record, MaterialRestitutionOffset, material.Restitution.Value);
            F32(record, MaterialBounceThresholdOffset, material.BounceThreshold.Value); F32(record, MaterialFrictionOffset, material.Friction.Value);
            F32(record, MaterialRollingResistanceOffset, material.RollingResistance.Value);
        }
        for (var i = 0; i < scene.Sensors.Length; i++)
        {
            var sensor = scene.Sensors[i]; var record = data.Slice(scene.Layout.GetSensorOffset(sensor.Id), SensorBytes);
            U64(record, RecordIdentityOffset, sensor.Id.Value); U32(record, SensorFrameOffset, BodySlot(scene, sensor.Frame));
            U32(record, SensorTargetOffset, sensor.Targets.Kind==BodyTargetKind.NamedBody ? BodySlot(scene,sensor.Targets.Body) : NoBody);
            U32(record,SensorTargetKindOffset,(uint)sensor.Targets.Kind); U32(record, SensorParticipationOffset, (uint)sensor.Participation);
            Pose(record, SensorPoseOffset, sensor.Pose);
            F32Vector(record, SensorMinimumOffset, sensor.Minimum.X, sensor.Minimum.Y, sensor.Minimum.Z);
            F32Vector(record, SensorMaximumOffset, sensor.Maximum.X, sensor.Maximum.Y, sensor.Maximum.Z);
            F32(record, SensorSpeedLimitOffset, sensor.SpeedLimit.Value); F32(record, SensorDwellOffset, sensor.Dwell.Value);
        }
        for (var i = 0; i < scene.Guides.Length; i++)
        {
            var guide = scene.Guides[i]; var record = data.Slice(scene.Layout.GetGuideOffset(guide.Id), GuideBytes);
            U64(record, RecordIdentityOffset, guide.Id.Value); U32(record, GuideFrameOffset, BodySlot(scene, guide.Frame));
            U32(record, GuideTargetOffset, guide.Targets.Kind==BodyTargetKind.NamedBody ? BodySlot(scene,guide.Targets.Body) : NoBody);
            U32(record,GuideTargetKindOffset,(uint)guide.Targets.Kind); Pose(record, GuidePoseOffset, guide.Pose);
            F32Vector(record, GuideMinimumOffset, guide.Minimum.X, guide.Minimum.Y, guide.Minimum.Z);
            F32Vector(record, GuideMaximumOffset, guide.Maximum.X, guide.Maximum.Y, guide.Maximum.Z);
            F32(record, GuideMaximumAccelerationOffset, guide.MaximumAcceleration.Value); F32(record, GuideSupportHeightOffset, guide.SupportHeight.Value);
            F32(record, GuideSupportMarginOffset, guide.SupportMargin.Value);
        }
        for (var i = 0; i < scene.Triggers.Length; i++)
        {
            var trigger = scene.Triggers[i]; var record = data.Slice(scene.Layout.GetTriggerOffset(trigger.Id), TriggerBytes);
            U64(record, RecordIdentityOffset, trigger.Id.Value); U32(record, TriggerOwnerOffset, BodySlot(scene, trigger.Owner));
            U32(record, TriggerTargetKindOffset, (uint)trigger.Targets.Kind); F32(record, TriggerThresholdOffset, trigger.Threshold.Value);
            U32(record, TriggerNamedTargetOffset, trigger.Targets.Kind == BodyTargetKind.NamedBody ? BodySlot(scene, trigger.Targets.Body) : NoBody);
        }
        AdmitContactWorks(data, scene);
        for (var i = 0; i < scene.OrientationSensors.Length; i++)
        {
            var sensor = scene.OrientationSensors[i];
            var record = data.Slice(scene.Layout.GetOrientationSensorOffset(sensor.Id), OrientationSensorBytes);
            U64(record, RecordIdentityOffset, sensor.Id.Value); U32(record, OrientationSensorBodyOffset, BodySlot(scene, sensor.Body));
            Rotation(record, OrientationSensorRotationOffset, sensor.Initial); F32(record, OrientationSensorCosineOffset, sensor.Threshold.CosineHalfAngle);
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
        var layout = Layout(data);
        // Exactly the world's declared dynamic population; the read set owns this array.
        var values = new PhysicsBodyRead[layout.Dynamic];
        var count = 0;
        for (var slot = 0; slot < layout.Bodies; slot++)
        {
            var record = data.Slice(layout.BodyOffset(slot), BodyBytes);
            var motion = (RigidMotionKind)R32(record, BodyMotionOffset);
            if (!Enum.IsDefined(motion)) throw new ArgumentException("Undefined rigid motion.");
            if (motion == RigidMotionKind.Static) continue;
            if (count == values.Length) throw new ArgumentException("More dynamic bodies than the world header declares.");
            // The read set validates every value: non-finite or out-of-envelope f32 velocity (|v| > 64 m/s, |w| > 128 rad/s) rejects the read.
            var body = new CanonicalBody(new(R64(record, RecordIdentityOffset)), epoch, tick,
                ReadCell(record, BodyCellOffset), ReadLocal(record, BodyLocalOffset), ReadLinearVelocity(record, BodyVelocityOffset));
            values[count++] = new(body, ReadRotation(record, BodyRotationOffset), ReadAngularVelocity(record, BodyAngularVelocityOffset),
                ReadCentreOfMass(record));
        }
        if (count != values.Length) throw new ArgumentException("Fewer dynamic bodies than the world header declares.");
        var result = PhysicsBodyReadSet.Adopt(values);
        result.ValidateTime(new(epoch), new(tick)); return result;
    }


    public static ContactTriggerRead ReadTrigger(ReadOnlySpan<byte> data, int slot)
    {
        if (ReadFailure(data) != PhysicsFailure.None || slot < 0 || (uint)slot >= R32(data, HeaderTriggerCountOffset))
            throw new ArgumentException("Invalid contact trigger read.");
        var layout = Layout(data);
        var record = data.Slice(layout.TriggerOffset(slot), TriggerBytes);
        var owner = R32(record, TriggerOwnerOffset); var kind = (BodyTargetKind)R32(record, TriggerTargetKindOffset);
        var named = R32(record, TriggerNamedTargetOffset); var target = R32(record, TriggerBodyOffset); var count = R32(record, TriggerCountOffset);
        var bodyCount = R32(data, HeaderBodyCountOffset);

        var threshold = RF(record, TriggerThresholdOffset);
        if (R64(record, RecordIdentityOffset) == 0 || owner >= bodyCount ||
            R32(record, TriggerTargetKindOffset) is < (uint)BodyTargetKind.NamedBody or > (uint)BodyTargetKind.AllDynamic || !Enum.IsDefined(kind) ||
            (kind == BodyTargetKind.NamedBody ? (named >= bodyCount || BodyMotion(data, named) != RigidMotionKind.Dynamic) || named == owner : named != NoBody) ||
            BodyMotion(data, owner) != RigidMotionKind.Static ||
            !float.IsFinite(threshold) || threshold < 0f || threshold > 64f ||
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
                R32(data, layout.ColliderOffset(checked((int)colliderSlot)) + ColliderBodyOffset) != owner ||
                !AdmittedTime(R32(record, TriggerOrdinalOffset), RF(record, TriggerPhaseOffset), R32(data, HeaderSourceOrdinalOffset)) ||
                !float.IsFinite(RF(record, TriggerApproachOffset)) || RF(record, TriggerApproachOffset) < threshold || RF(record, TriggerApproachOffset) > 4096f)
                throw new ArgumentException("Invalid qualifying impact identity, time or normal speed.");
            collider = new(R64(data, layout.ColliderOffset(checked((int)colliderSlot))));
            targetId = new(R64(data, layout.BodyOffset(checked((int)target))));
        }
        return new(new(R64(record, RecordIdentityOffset)), new(R64(data, layout.BodyOffset(checked((int)owner)))),
            targetId, count, collider, R32(record, TriggerOrdinalOffset), RF(record, TriggerPhaseOffset), new(RF(record, TriggerApproachOffset)));
    }

    public static OrientationSensorRead ReadOrientationSensor(ReadOnlySpan<byte> data, int slot)
    {
        if (ReadFailure(data) != PhysicsFailure.None || slot < 0 || (uint)slot >= R32(data, HeaderOrientationSensorCountOffset))
            throw new ArgumentException("Invalid orientation sensor read.");
        var layout = Layout(data);
        var record = data.Slice(layout.OrientationSensorOffset(slot), OrientationSensorBytes);
        var body = R32(record, OrientationSensorBodyOffset); var phase = (OrientationSensorPhase)R32(record, OrientationSensorPhaseOffset);
        if (R64(record, RecordIdentityOffset) == 0 || body >= R32(data, HeaderBodyCountOffset) || !Enum.IsDefined(phase) ||
            !AllZero(record[OrientationSensorBodyPaddingOffset..OrientationSensorRotationOffset]) ||
            !AllZero(record[OrientationSensorCosinePaddingOffset..OrientationSensorPhaseOffset]) || !AllZero(record[OrientationSensorPaddingOffset..]))
            throw new ArgumentException("Invalid orientation sensor declaration or padding.");
        var bodyRecord = data.Slice(layout.BodyOffset(checked((int)body)), BodyBytes);
        if ((RigidMotionKind)R32(bodyRecord, BodyMotionOffset) != RigidMotionKind.Dynamic)
            throw new ArgumentException("Orientation sensor body is not dynamic.");
        ReadRotation(record, OrientationSensorRotationOffset).Validate();
        new OrientationThreshold(RF(record, OrientationSensorCosineOffset)).Validate();
        var ordinal = R32(record, OrientationSensorOrdinalOffset); var eventPhase = RF(record, OrientationSensorEventPhaseOffset);
        // Endpoint-sampled: a fired sensor names one substep boundary inside the admitted time; an armed one holds no event.
        if (phase == OrientationSensorPhase.Armed
            ? ordinal != 0 || !PhysicsDeclarationBounds.Zero(eventPhase)
            : ordinal == 0 || !PhysicsDeclarationBounds.Zero(eventPhase) || !AdmittedTime(ordinal, eventPhase, R32(data, HeaderSourceOrdinalOffset)))
            throw new ArgumentException("Invalid orientation sensor event.");
        var colliderSlot = -1;
        for (var candidate = 0; candidate < layout.Colliders; candidate++)
            if (R32(data, layout.ColliderOffset(candidate) + ColliderBodyOffset) == body)
            {
                colliderSlot = candidate;
                break;
            }
        if (colliderSlot < 0) throw new ArgumentException("Orientation sensor body has no owned geometry.");
        return new(new(R64(record, RecordIdentityOffset)), new(R64(bodyRecord, RecordIdentityOffset)),
            new(R64(data, layout.ColliderOffset(checked((int)colliderSlot)))), phase, ordinal, eventPhase);
    }

    /// <summary>Required before committing a GPU result; selected reads alone do not qualify a candidate.</summary>
    public static PhysicsCandidateRead ValidateCandidate(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> source, SimulationTick expectedTick, PhysicsInteractionSnapshot candidateInteractions, PhysicsInteractionSnapshot sourceInteractions)
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
        // Equal header populations: one layout addresses both worlds, and every table is exactly its population (Story 9R.5b).
        var layout = Layout(source);
        var bodyCount = layout.Bodies; var dynamicCount = 0;
        for (var i = 0; i < bodyCount; i++)
        {
            var actual = candidate.Slice(layout.BodyOffset(i), BodyBytes);
            var previous = source.Slice(layout.BodyOffset(i), BodyBytes);
            var motion = (RigidMotionKind)R32(previous, BodyMotionOffset);
            if (motion == RigidMotionKind.Static)
            {
                if (!actual.SequenceEqual(previous)) throw new ArgumentException("Static body changed.");
                continue;
            }
            if (motion != RigidMotionKind.Dynamic) throw new ArgumentException("Undefined rigid motion.");
            dynamicCount++;
            if (!actual[..BodyCellOffset].SequenceEqual(previous[..BodyCellOffset]) || !actual[BodyMassOffset..].SequenceEqual(previous[BodyMassOffset..]) ||
                !AllZero(actual[BodyCellPaddingOffset..BodyLocalOffset]) || !AllZero(actual[BodyLocalPaddingOffset..BodyRotationOffset]))
                throw new ArgumentException("Dynamic immutable declaration or padding changed.");
            ReadMassProperties(actual).Validate();
        }
        if (dynamicCount != layout.Dynamic ||
            !candidate[layout.MaterialsOffset..layout.SensorsOffset].SequenceEqual(source[layout.MaterialsOffset..layout.SensorsOffset]) ||
            !candidate[layout.CollidersOffset..].SequenceEqual(source[layout.CollidersOffset..]))
            throw new ArgumentException("Candidate changed body population, geometry or material.");
        // Every fixed row's zero padding is checked in the candidate itself (at admission, tick 0, too): an unchanged nonzero byte
        // passes the row comparisons above, so only these checks see it.
        for (var i = 0; i < bodyCount; i++)
        {
            var body = candidate.Slice(layout.BodyOffset(i), BodyBytes);
            if (!Enum.IsDefined((PayloadPolicy)R32(body,BodyPayloadOffset)) ||
                ((RigidMotionKind)R32(body,BodyMotionOffset)==RigidMotionKind.Static && (PayloadPolicy)R32(body,BodyPayloadOffset)!=PayloadPolicy.None) ||
                !AllZero(body[BodyMotionPaddingOffset..BodyCellOffset]) || !AllZero(body[BodyPaddingOffset..]))
                throw new ArgumentException("Nonzero body padding.");
        }
        for (var i = 0; i < layout.Colliders; i++)
        {
            var collider = candidate.Slice(layout.ColliderOffset(i), ColliderBytes);
            var sensingRow = R32(collider, ColliderContactLoadOffset);
            if (sensingRow > layout.ContactLoads || (sensingRow != 0 &&
                R32(candidate, layout.ContactLoadOffset((int)sensingRow - 1) + ContactLoadFrameOffset) != R32(collider, ColliderBodyOffset)))
                throw new ArgumentException("Collider names a foreign sensing region.");
            if (!AllZero(collider[ColliderPaddingOffset..]))
                throw new ArgumentException("Nonzero collider padding.");
        }
        for (var i = 0; i < layout.Materials; i++)
            if (!AllZero(candidate.Slice(layout.MaterialOffset(i) + MaterialPaddingOffset, MaterialBytes - MaterialPaddingOffset)))
                throw new ArgumentException("Nonzero material padding.");
        for (var i = 0; i < layout.Sensors; i++)
            if (!AllZero(candidate.Slice(layout.SensorOffset(i) + SensorParticipationPaddingOffset, SensorPoseOffset - SensorParticipationPaddingOffset)))
                throw new ArgumentException("Nonzero sensor padding.");
        var bodies = ReadDynamicBodies(candidate);
        ValidateCaptureCandidate(candidate,source,candidateInteractions,sourceInteractions);
        if (!candidate[layout.GuidesOffset..layout.TriggersOffset].SequenceEqual(source[layout.GuidesOffset..layout.TriggersOffset]))
            throw new ArgumentException("Guide declaration changed.");
        for (var i = 0; i < layout.Triggers; i++)
        {
            var offset = layout.TriggerOffset(i);
            if (!candidate.Slice(offset, TriggerCountOffset).SequenceEqual(source.Slice(offset, TriggerCountOffset)))
                throw new ArgumentException("Contact trigger declaration changed.");
            var trigger = ReadTrigger(candidate, i);
            if (trigger.OccurrenceCount != 0 && (expectedTick.Value == 0 ||
                Before(trigger.EventOrdinal, trigger.EventPhase, R32(source, HeaderSourceOrdinalOffset), 0f)))
                throw new ArgumentException("Impact event does not belong to this candidate interval.");
        }
        ValidateContactWorkCandidate(candidate, source, expectedTick, candidateInteractions, sourceInteractions);
        for (var i = 0; i < layout.OrientationSensors; i++)
        {
            var offset = layout.OrientationSensorOffset(i);
            if (!candidate.Slice(offset, OrientationSensorDeclarationBytes).SequenceEqual(source.Slice(offset, OrientationSensorDeclarationBytes)))
                throw new ArgumentException("Orientation sensor declaration changed.");
            // At admission the body record still holds the admitted rotation: the sensor's initial pose must be exactly that.
            // Later ticks keep the declaration bytes equal to the source, so the invariant carries forward.
            if (expectedTick.Value == 0 && !candidate.Slice(offset + OrientationSensorRotationOffset, RotationBytes).SequenceEqual(
                    candidate.Slice(layout.BodyOffset(checked((int)R32(candidate, offset + OrientationSensorBodyOffset))) + BodyRotationOffset, RotationBytes)))
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
        if (expectedTick.Value != 0 && (R32(candidate, layout.MotionOffset + PhysicsMotionRead.SubstepsOffset) != profile.Substeps ||
            R32(candidate, layout.MotionOffset + PhysicsMotionRead.LastOrdinalOffset) != R32(candidate, HeaderSourceOrdinalOffset)))
            throw new ArgumentException("Motion profile differs from its committed world.");
        ValidateElectricalCandidate(candidate, source, candidateInteractions);
        ValidatePrismaticCandidate(candidate, source);
        ValidateContactLoadCandidate(candidate, source);
        return new(bodies, PhysicsMotionRead.Decode(candidate.Slice(layout.MotionOffset, layout.MotionBytes), bodies, expectedTick));
    }


    private static void Principal(Span<byte> bytes, int offset, PrincipalInertia value)
    {
        value.Validate(); F32(bytes, offset, value.Moment);
    }
    private static RigidMassProperties ReadMassProperties(ReadOnlySpan<byte> record) =>
        new(ReadCentreOfMass(record), ReadRotation(record, BodyPrincipalFrameOffset),
            ReadPrincipal(record, BodyPrincipalInertiaOffset), ReadPrincipal(record, BodyPrincipalInertiaOffset + PrincipalInertiaBytes),
            ReadPrincipal(record, BodyPrincipalInertiaOffset + 2 * PrincipalInertiaBytes));
    private static MetreVector ReadCentreOfMass(ReadOnlySpan<byte> record) =>
        new(RF(record, BodyCentreOfMassOffset), RF(record, BodyCentreOfMassOffset + sizeof(float)), RF(record, BodyCentreOfMassOffset + 2 * sizeof(float)));
    private static PrincipalInertia ReadPrincipal(ReadOnlySpan<byte> record, int offset)
    {
        var value = new PrincipalInertia(RF(record, offset));
        value.Validate(); return value;
    }
    /// <summary>A physical time: an ordinal and its f32 phase in [−2048, 2048) of the ordinal's 4096 phase units, inside the
    /// committed interval.</summary>
    private static bool AdmittedTime(uint ordinal, float phase, uint end) =>
        PhysicalPhase.IsAdmitted(ordinal, phase) && ordinal <= end && (ordinal != end || phase <= 0f);
    private static bool Before(uint a, float ap, uint b, float bp) => a < b || (a == b && ap < bp);

    private static void Header(ReadOnlySpan<byte> data)
    {
        if (data.Length < HeaderBytes || data.Length != ByteLength(data) ||
            R32(data, HeaderReservedOffset) != 0 ||
            (R64(data, HeaderDocumentLowOffset) == 0 && R64(data, HeaderDocumentHighOffset) == 0) || R64(data, HeaderNextIdentityOffset) == 0 ||
            // No capacity bounds a population (Story 9R.5b); only the populations' own relations do.
            R32(data, HeaderDynamicCountOffset) > R32(data, HeaderBodyCountOffset) ||
            !AllZero(data[HeaderPaddingOffset..HeaderBytes]))
            throw new ArgumentException("Unsupported generic physics record.");
    }
    private static bool AllZero(ReadOnlySpan<byte> bytes) => bytes.IndexOfAnyExcept((byte)0) < 0;
    private static uint BodySlot(PhysicsSceneDeclaration scene, GpuBodyId id) => (uint)scene.Layout.GetBodySlot(id);
    private static uint MaterialSlot(PhysicsSceneDeclaration scene, GpuMaterialId id) => (uint)scene.Layout.GetMaterialSlot(id);
    private static void Pose(Span<byte> bytes, int offset, RigidLocalPose pose)
    {
        F32Vector(bytes, offset, pose.Translation.X, pose.Translation.Y, pose.Translation.Z);
        Rotation(bytes, offset + PoseRotationOffset, pose.Rotation);
    }
    private static void Rotation(Span<byte> bytes, int offset, CanonicalRotation value)
    {
        F32Vector(bytes, offset, value.X, value.Y, value.Z); F32(bytes, offset + VectorBytes, value.W);
    }
    private static CanonicalRotation ReadRotation(ReadOnlySpan<byte> bytes, int offset) =>
        new(RF(bytes, offset), RF(bytes, offset + sizeof(float)), RF(bytes, offset + 2 * sizeof(float)), RF(bytes, offset + VectorBytes));
    private static void Cell(Span<byte> bytes, int offset, CellOrigin value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(bytes[offset..], value.X);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[(offset + sizeof(int))..], value.Y);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[(offset + 2 * sizeof(int))..], value.Z);
    }
    private static CellOrigin ReadCell(ReadOnlySpan<byte> bytes, int offset) =>
        new(BinaryPrimitives.ReadInt32LittleEndian(bytes[offset..]),
            BinaryPrimitives.ReadInt32LittleEndian(bytes[(offset + sizeof(int))..]),
            BinaryPrimitives.ReadInt32LittleEndian(bytes[(offset + 2 * sizeof(int))..]));
    private static void Local(Span<byte> bytes, int offset, LocalPosition value) =>
        F32Vector(bytes, offset, value.X, value.Y, value.Z);
    private static LocalPosition ReadLocal(ReadOnlySpan<byte> bytes, int offset) =>
        new(RF(bytes, offset), RF(bytes, offset + sizeof(float)), RF(bytes, offset + 2 * sizeof(float)));
    private static LinearVelocity ReadLinearVelocity(ReadOnlySpan<byte> bytes, int offset) =>
        new(RF(bytes, offset), RF(bytes, offset + sizeof(float)), RF(bytes, offset + 2 * sizeof(float)));
    private static AngularVelocity ReadAngularVelocity(ReadOnlySpan<byte> bytes, int offset) =>
        new(RF(bytes, offset), RF(bytes, offset + sizeof(float)), RF(bytes, offset + 2 * sizeof(float)));
    private static void F32Vector(Span<byte> bytes, int offset, float x, float y, float z)
    {
        F32(bytes, offset, x); F32(bytes, offset + sizeof(float), y); F32(bytes, offset + 2 * sizeof(float), z);
    }
    private static void F32(Span<byte> bytes, int offset, float value) => BinaryPrimitives.WriteSingleLittleEndian(bytes[offset..], value);
    private static float RF(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadSingleLittleEndian(bytes[offset..]);
    private static void U32(Span<byte> bytes, int offset, uint value) =>
        BinaryPrimitives.WriteUInt32LittleEndian(bytes[offset..], value);
    private static void U64(Span<byte> bytes, int offset, ulong value) =>
        BinaryPrimitives.WriteUInt64LittleEndian(bytes[offset..], value);
    private static uint R32(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(bytes[offset..]);
    private static ulong R64(ReadOnlySpan<byte> bytes, int offset) => BinaryPrimitives.ReadUInt64LittleEndian(bytes[offset..]);
}
