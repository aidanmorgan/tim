using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

/// <summary>Story 9R.4: the contact-load table, after the prismatic rows and before the collider tail. A 16-byte table header
/// holds the row count (the 128-byte world header has no free word). Each row is the compiled f32 declaration followed by the
/// tick's committed phase and measured f32 mass.</summary>
public static partial class PhysicsGpuAbi
{
    public const int ContactLoadSensorsOffset = PrismaticRecordsOffset + PhysicsSceneDeclaration.PrismaticCapacity * PrismaticBytes;
    public const int ContactLoadRecordsOffset = ContactLoadSensorsOffset + 16;
    public const int ContactLoadSensorBytes = 64;
    // Row: identity U64 at 0, frame body slot, sensing-face collider slot, f32 footprint (min x, max x, min z, max z), f32 local
    // normal, f32 minimum alignment, f32 maximum gap, f32 minimum mass; then the state: phase U32 and measured f32 mass.
    public const int ContactLoadFrameOffset = 8;
    public const int ContactLoadFaceOffset = 12;
    public const int ContactLoadFootprintOffset = 16;
    public const int ContactLoadNormalOffset = 32;
    public const int ContactLoadAlignmentOffset = 44;
    public const int ContactLoadGapOffset = 48;
    public const int ContactLoadMinimumMassOffset = 52;
    public const int ContactLoadDeclarationBytes = 56;
    public const int ContactLoadPhaseOffset = 56;
    public const int ContactLoadMassOffset = 60;

    private static void WriteContactLoadAdmission(Span<byte> data, PhysicsSceneDeclaration scene)
    {
        U32(data, ContactLoadSensorsOffset, (uint)scene.ContactLoadSensors.Length);
        for (var i = 0; i < scene.ContactLoadSensors.Length; i++)
        {
            var sensor = scene.ContactLoadSensors[i];
            var record = data.Slice(ContactLoadRecordsOffset + i * ContactLoadSensorBytes, ContactLoadSensorBytes);
            U64(record, 0, sensor.Id.Value);
            U32(record, ContactLoadFrameOffset, BodySlot(scene, sensor.Frame));
            U32(record, ContactLoadFaceOffset, (uint)scene.Layout.GetColliderSlot(sensor.Face));
            WriteEnergy(record, ContactLoadFootprintOffset, sensor.MinimumX); WriteEnergy(record, ContactLoadFootprintOffset + 4, sensor.MaximumX);
            WriteEnergy(record, ContactLoadFootprintOffset + 8, sensor.MinimumZ); WriteEnergy(record, ContactLoadFootprintOffset + 12, sensor.MaximumZ);
            F32Vector(record, ContactLoadNormalOffset, sensor.LocalNormal.X, sensor.LocalNormal.Y, sensor.LocalNormal.Z);
            WriteEnergy(record, ContactLoadAlignmentOffset, sensor.MinimumAlignment);
            WriteEnergy(record, ContactLoadGapOffset, sensor.MaximumGap);
            WriteEnergy(record, ContactLoadMinimumMassOffset, sensor.MinimumMass);
            // Admitted state: Empty with no measured mass (both zero bytes).
        }
    }

    private static uint ContactLoadCount(ReadOnlySpan<byte> data)
    {
        var count = R32(data, ContactLoadSensorsOffset);
        if (count > PhysicsSceneDeclaration.ContactLoadSensorCapacity || !AllZero(data.Slice(ContactLoadSensorsOffset + 4, 12)))
            throw new ArgumentException("Invalid contact-load table.");
        return count;
    }

    /// <summary>The number of compiled contact-load rows in a world.</summary>
    public static int ReadContactLoadCount(ReadOnlySpan<byte> data)
    {
        Header(data);
        return (int)ContactLoadCount(data);
    }

    /// <summary>One compiled contact-load row, its frame and face resolved to identities and validated against the world.</summary>
    public static ContactLoadSensorDeclaration ReadContactLoadDeclaration(ReadOnlySpan<byte> data, int index)
    {
        Header(data);
        if (index < 0 || index >= ContactLoadCount(data)) throw new ArgumentException("Invalid contact-load row.");
        var record = data.Slice(ContactLoadRecordsOffset + index * ContactLoadSensorBytes, ContactLoadSensorBytes);
        var frame = R32(record, ContactLoadFrameOffset); var face = R32(record, ContactLoadFaceOffset);
        if (frame >= R32(data, HeaderBodyCountOffset) || BodyMotion(data, frame) != RigidMotionKind.Static ||
            face >= R32(data, HeaderColliderCountOffset) ||
            R32(data, CollidersOffset + checked((int)face) * ColliderBytes + ColliderBodyOffset) != frame)
            throw new ArgumentException("Contact-load row names an unowned frame or face.");
        var result = new ContactLoadSensorDeclaration(new(R64(record, 0)), new(R64(data, BodiesOffset + checked((int)frame) * BodyBytes)),
            new(R64(data, CollidersOffset + checked((int)face) * ColliderBytes)),
            RF(record, ContactLoadFootprintOffset), RF(record, ContactLoadFootprintOffset + 4),
            RF(record, ContactLoadFootprintOffset + 8), RF(record, ContactLoadFootprintOffset + 12),
            new Vector3(RF(record, ContactLoadNormalOffset), RF(record, ContactLoadNormalOffset + 4), RF(record, ContactLoadNormalOffset + 8)),
            RF(record, ContactLoadAlignmentOffset), RF(record, ContactLoadGapOffset), RF(record, ContactLoadMinimumMassOffset));
        result.Validate();
        return result;
    }

    /// <summary>One row's committed measurement. The phase must be exactly the declared comparison of the measured f32 mass.</summary>
    public static ContactLoadSensorRead ReadContactLoadSensor(ReadOnlySpan<byte> data, int index)
    {
        if (ReadFailure(data) != PhysicsFailure.None) throw new ArgumentException("Invalid contact-load read.");
        var declaration = ReadContactLoadDeclaration(data, index);
        var record = data.Slice(ContactLoadRecordsOffset + index * ContactLoadSensorBytes, ContactLoadSensorBytes);
        var phase = (ContactLoadPhase)R32(record, ContactLoadPhaseOffset); var mass = RF(record, ContactLoadMassOffset);
        if (!Enum.IsDefined(phase) || !float.IsFinite(mass) || mass < 0 || mass > 4096 || declaration.PhaseOf(mass) != phase)
            throw new ArgumentException("Invalid committed contact-load phase or mass.");
        return new(declaration.Id, declaration.Frame, declaration.Face, mass, phase);
    }

    private static void ValidateContactLoadCandidate(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> source)
    {
        var count = ContactLoadCount(candidate);
        if (!candidate.Slice(ContactLoadSensorsOffset, 16).SequenceEqual(source.Slice(ContactLoadSensorsOffset, 16)))
            throw new ArgumentException("Contact-load table changed.");
        for (var i = 0; i < count; i++)
        {
            var offset = ContactLoadRecordsOffset + i * ContactLoadSensorBytes;
            if (!candidate.Slice(offset, ContactLoadDeclarationBytes).SequenceEqual(source.Slice(offset, ContactLoadDeclarationBytes)))
                throw new ArgumentException("Contact-load declaration changed.");
            _ = ReadContactLoadSensor(candidate, i);
        }
        if (!AllZero(candidate[(ContactLoadRecordsOffset + (int)count * ContactLoadSensorBytes)..CollidersOffset]))
            throw new ArgumentException("Unused contact-load rows changed.");
    }
}
