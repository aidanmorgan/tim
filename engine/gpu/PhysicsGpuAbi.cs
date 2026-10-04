using System;
using System.Buffers.Binary;
using System.Globalization;

namespace CuriousContraptions.Gpu;

public enum PhysicsStateVersion : uint { GenericMechanical = 4 }
public enum PhysicsCandidateStatus : uint { Committed, Invalid }
public enum PhysicsFailure : uint { None, InvalidDeclaration, Domain, ContactBudget, RootBudget, ContactResidual, UnsupportedPair, Arithmetic, MotionCapacity }
public enum PhysicsMotionPhase : uint { Free, Supported }
public enum ResidencePhase : uint { Outside, Dwelling, Qualified }
public readonly record struct ResidenceRead(GpuSensorId Id, ResidencePhase Phase, uint OccurrenceCount,
    uint StartOrdinal, Half StartPhase, uint EventOrdinal, Half EventPhase);
public readonly record struct PhysicsBodyRead(CanonicalBody Body, CanonicalRotation Rotation, AngularVelocity AngularVelocity);

/// <summary>One bounded generic scene/state ABI. Physical arithmetic lives only in the matching WGSL module.</summary>
public static class PhysicsGpuAbi
{
    public const int HeaderBytes = 128;
    public const int BodyBytes = 256;
    public const int ColliderBytes = 96;
    public const int MaterialBytes = 32;
    public const int SensorBytes = 128;
    public const int GuideBytes = 64;
    public const int BodiesOffset = HeaderBytes;
    public const int CollidersOffset = BodiesOffset + PhysicsSceneDeclaration.BodyCapacity * BodyBytes;
    public const int MaterialsOffset = CollidersOffset + PhysicsSceneDeclaration.ColliderCapacity * ColliderBytes;
    public const int SensorsOffset = MaterialsOffset + PhysicsSceneDeclaration.MaterialCapacity * MaterialBytes;
    public const int GuidesOffset = SensorsOffset + PhysicsSceneDeclaration.SensorCapacity * SensorBytes;
    public const int MotionOffset = GuidesOffset + PhysicsSceneDeclaration.GuideCapacity * GuideBytes;
    public const int ByteLength = MotionOffset + PhysicsMotionRead.ByteLength;
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
        U32(data, 28, NoBody); U32(data, 96, (uint)scene.Guides.Length);
        U64(data, 32, epoch.Value); U32(data, 48, (uint)profile.Cadence);
        U32(data, 52, (uint)profile.Physical); U64(data, 56, profile.Revision.Value);
        U64(data, 64, scene.Document.Low); U64(data, 72, scene.Document.High); U64(data, 80, scene.NextIdentity);
        for (var i = 0; i < scene.Bodies.Length; i++)
        {
            var body = scene.Bodies[i]; var record = data.Slice(BodiesOffset + i * BodyBytes, BodyBytes);
            U64(record, 0, body.Id.Value); U32(record, 8, (uint)body.Motion);
            Cell(record, 16, body.Cell); Local(record, 32, body.Local); Rotation(record, 40, body.Rotation);
            Vector(record, 48, body.Velocity.X, body.Velocity.Y, body.Velocity.Z);
            Vector(record, 56, body.AngularVelocity.X, body.AngularVelocity.Y, body.AngularVelocity.Z);
            H(record, 64, body.Mass.Value); H(record, 66, body.LinearDrag.Value);
            Vector(record, 68, body.Gravity.X, body.Gravity.Y, body.Gravity.Z);
            Cell(record, 80, body.Cell); Local(record, 96, body.Local);
            Vector(record, 104, body.Velocity.X, body.Velocity.Y, body.Velocity.Z);
            Rotation(record, 112, body.Rotation);
            Vector(record, 120, body.AngularVelocity.X, body.AngularVelocity.Y, body.AngularVelocity.Z);
            if (body.Motion == RigidMotionKind.Dynamic) U32(data, 28, (uint)i);
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

    public static PhysicsBodyRead? ReadDynamicBody(ReadOnlySpan<byte> data)
    {
        if (ReadFailure(data) != PhysicsFailure.None) throw new ArgumentException("Candidate is not committed.");
        var profile = ReadProfile(data); var epoch = R64(data, 32); var tick = R64(data, 40);
        if (epoch == 0 || tick > profile.RunTickLimit ||
            R32(data, 88) != checked((uint)(tick * profile.Substeps)))
            throw new ArgumentException("Invalid generic physical time.");
        var slot = R32(data, 28);
        if (slot == NoBody) return null;
        if (slot >= R32(data, 12)) throw new ArgumentException("Dynamic slot is invalid.");
        var record = data.Slice(BodiesOffset + checked((int)slot) * BodyBytes, BodyBytes);
        if ((RigidMotionKind)R32(record, 8) != RigidMotionKind.Dynamic ||
            !Enum.IsDefined((PhysicsMotionPhase)R32(record, 128)) || R32(record, 132) > 4)
            throw new ArgumentException("Invalid dynamic body state.");
        var body = new CanonicalBody(new(R64(record, 0)), epoch, tick,
            ReadCell(record, 16), ReadLocal(record, 32),
            new(RH(record, 48), RH(record, 50), RH(record, 52)));
        body.Validate();
        if ((double)body.Velocity.X * (double)body.Velocity.X +
            (double)body.Velocity.Y * (double)body.Velocity.Y +
            (double)body.Velocity.Z * (double)body.Velocity.Z > 4)
            throw new ArgumentException("Committed velocity exceeds the vector bound.");
        var rotation = ReadRotation(record, 40); rotation.Validate();
        var angular = new AngularVelocity(RH(record, 56), RH(record, 58), RH(record, 60));
        PhysicsDeclarationBounds.Vector(angular.X, angular.Y, angular.Z, (Half)64);
        return new(body, rotation, angular);
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


    /// <summary>Required before committing a GPU result; selected reads alone do not qualify a candidate.</summary>
    public static PhysicsMotionRead ValidateCandidate(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> source, SimulationTick expectedTick)
    {
        if (ReadFailure(candidate) != PhysicsFailure.None || ReadFailure(source) != PhysicsFailure.None)
            throw new ArgumentException("Only a successful candidate can commit.");
        var profile = ReadProfile(source);
        if (expectedTick.Value > profile.RunTickLimit || R64(candidate, 40) != expectedTick.Value ||
            R32(candidate, 88) != checked((uint)(expectedTick.Value * profile.Substeps)) ||
            !candidate[..4].SequenceEqual(source[..4]) ||
            !candidate[12..40].SequenceEqual(source[12..40]) ||
            !candidate[48..88].SequenceEqual(source[48..88]) ||
            !candidate[96..100].SequenceEqual(source[96..100]))
            throw new ArgumentException("Candidate identity, scene counts or physical profile changed.");
        var bodyCount = checked((int)R32(source, 12)); var dynamicSlot = R32(source, 28);
        var dynamicCount = 0;
        for (var i = 0; i < bodyCount; i++)
        {
            var offset = BodiesOffset + i * BodyBytes;
            var actual = candidate.Slice(offset, BodyBytes); var previous = source.Slice(offset, BodyBytes);
            if ((RigidMotionKind)R32(previous, 8) == RigidMotionKind.Static)
            {
                if (!actual.SequenceEqual(previous)) throw new ArgumentException("Static body changed.");
                continue;
            }
            dynamicCount++;
            if (dynamicSlot != (uint)i || !actual[..16].SequenceEqual(previous[..16]) ||
                !actual[64..80].SequenceEqual(previous[64..80]) ||
                !AllZero(actual[28..32]) || !AllZero(actual[38..40]) ||
                !AllZero(actual[54..56]) || !AllZero(actual[62..64]) ||
                !AllZero(actual[110..112]) || !AllZero(actual[126..128]) ||
                !AllZero(actual[140..160]) || !AllZero(actual[224..256]))
                throw new ArgumentException("Dynamic identity, force declaration or padding changed.");
            var start = R32(actual, 92); var phase = RH(actual, 102);
            if (!AdmittedTime(start, phase, R32(candidate, 88)) ||
                R32(candidate, 88) - start > PrimarySegmentSteps ||
                (R32(candidate, 88) - start == PrimarySegmentSteps && phase < (Half)0))
                throw new ArgumentException("Invalid primary segment age.");
            var segment = new CanonicalBody(new(R64(actual, 0)), R64(candidate, 32), expectedTick.Value,
                ReadCell(actual, 80), ReadLocal(actual, 96),
                new(RH(actual, 104), RH(actual, 106), RH(actual, 108)));
            segment.Validate();
            if ((double)segment.Velocity.X * (double)segment.Velocity.X +
                (double)segment.Velocity.Y * (double)segment.Velocity.Y +
                (double)segment.Velocity.Z * (double)segment.Velocity.Z > 4)
                throw new ArgumentException("Segment launch exceeds the vector bound.");
            ReadRotation(actual, 112).Validate();
            PhysicsDeclarationBounds.Vector(RH(actual, 120), RH(actual, 122), RH(actual, 124), (Half)64);
            var contacts = R32(actual, 132);
            if (contacts > 4 || !AllZero(actual.Slice(160 + checked((int)contacts) * 16, checked((int)(4 - contacts)) * 16)))
                throw new ArgumentException("Invalid contact manifold padding.");
            for (var c = 0; c < contacts; c++)
            {
                var contact = actual.Slice(160 + c * 16, 16);
                if (R32(contact, 0) >= R32(source, 16) || !Half.IsFinite(RH(contact, 8)) ||
                    RH(contact, 8) < (Half)0 || !Half.IsFinite(RH(contact, 10)) ||
                    !Half.IsFinite(RH(contact, 12)) || !AllZero(contact[14..16]))
                    throw new ArgumentException("Invalid contact state.");
                var collider = source.Slice(CollidersOffset + checked((int)R32(contact, 0)) * ColliderBytes, ColliderBytes);
                var owner = R32(collider, 8);
                if (owner >= bodyCount || owner == dynamicSlot ||
                    (RigidMotionKind)R32(source.Slice(BodiesOffset + checked((int)owner) * BodyBytes), 8) != RigidMotionKind.Static ||
                    !ValidFeature((ColliderShapeKind)R32(collider, 16), R32(contact, 4)))
                    throw new ArgumentException("Contact feature is not an admitted static feature.");
            }
        }
        if (dynamicCount != (dynamicSlot == NoBody ? 0 : 1) ||
            !AllZero(candidate.Slice(BodiesOffset + bodyCount * BodyBytes,
                (PhysicsSceneDeclaration.BodyCapacity - bodyCount) * BodyBytes)) ||
            !candidate[CollidersOffset..SensorsOffset].SequenceEqual(source[CollidersOffset..SensorsOffset]))
            throw new ArgumentException("Candidate changed geometry, materials or unused body slots.");
        var body = ReadDynamicBody(candidate);
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
        if (!candidate[GuidesOffset..MotionOffset].SequenceEqual(source[GuidesOffset..MotionOffset]))
            throw new ArgumentException("Guide declaration changed.");
        if (expectedTick.Value != 0 && (R32(candidate, MotionOffset + 4) != profile.Substeps ||
            R32(candidate, MotionOffset + 12) != R32(candidate, 88)))
            throw new ArgumentException("Motion profile differs from its committed world.");
        return PhysicsMotionRead.Decode(candidate[MotionOffset..], body?.Body, expectedTick);
    }

    private static bool ValidFeature(ColliderShapeKind shape, uint feature)
    {
        if (shape == ColliderShapeKind.Plane) return feature == 0;
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
const STATE_VERSION:u32={(uint)PhysicsStateVersion.GenericMechanical}u;
const STATUS_COMMITTED:u32={(uint)PhysicsCandidateStatus.Committed}u;
const STATUS_INVALID:u32={(uint)PhysicsCandidateStatus.Invalid}u;
const FAILURE_NONE:u32={(uint)PhysicsFailure.None}u;
const FAILURE_DECLARATION:u32={(uint)PhysicsFailure.InvalidDeclaration}u;
const FAILURE_DOMAIN:u32={(uint)PhysicsFailure.Domain}u;
const FAILURE_CONTACT_BUDGET:u32={(uint)PhysicsFailure.ContactBudget}u;
const FAILURE_ROOT_BUDGET:u32={(uint)PhysicsFailure.RootBudget}u;
const FAILURE_RESIDUAL:u32={(uint)PhysicsFailure.ContactResidual}u;
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
            (R64(data, 64) == 0 && R64(data, 72) == 0) || R64(data, 80) == 0 ||
            !AllZero(data[100..128]))
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
