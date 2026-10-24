using System;
using System.Numerics;

namespace CuriousContraptions.Gpu;

/// <summary>Story 9R.4: the contact-load table, after the prismatic rows and before the collider tail; its row count is a world
/// header word (Story 9R.5b). Each row is the compiled f32 declaration followed by the tick's committed phase and measured f32 mass.</summary>
public static partial class PhysicsGpuAbi
{
    public const int ContactLoadSensorBytes = 64;
    // Row: identity U64 at 0, frame body slot, sensing-face collider slot, f32 footprint (min x, max x, min z, max z), f32 local
    // normal, f32 minimum alignment, f32 maximum gap, f32 minimum mass; then the state: phase U32 and measured f32 mass.
    public const int ContactLoadFrameOffset = 8;
    public const int ContactLoadFaceOffset = 12;
    public const int ContactLoadFootprintOffset = 16;
    public const int ContactLoadMinimumXOffset = ContactLoadFootprintOffset;
    public const int ContactLoadMaximumXOffset = 20;
    public const int ContactLoadMinimumZOffset = 24;
    public const int ContactLoadMaximumZOffset = 28;
    public const int ContactLoadNormalOffset = 32;
    public const int ContactLoadAlignmentOffset = 44;
    public const int ContactLoadGapOffset = 48;
    public const int ContactLoadMinimumMassOffset = 52;
    public const int ContactLoadDeclarationBytes = 56;
    public const int ContactLoadPhaseOffset = 56;
    public const int ContactLoadMassOffset = 60;

    private static void WriteContactLoadAdmission(Span<byte> data, PhysicsSceneDeclaration scene)
    {
        for (var i = 0; i < scene.ContactLoadSensors.Length; i++)
        {
            var sensor = scene.ContactLoadSensors[i];
            var record = data.Slice(scene.World.ContactLoadOffset(i), ContactLoadSensorBytes);
            U64(record, RecordIdentityOffset, sensor.Id.Value);
            U32(record, ContactLoadFrameOffset, BodySlot(scene, sensor.Frame));
            U32(record, ContactLoadFaceOffset, (uint)scene.Layout.GetColliderSlot(sensor.Face));
            WriteEnergy(record, ContactLoadMinimumXOffset, sensor.MinimumX); WriteEnergy(record, ContactLoadMaximumXOffset, sensor.MaximumX);
            WriteEnergy(record, ContactLoadMinimumZOffset, sensor.MinimumZ); WriteEnergy(record, ContactLoadMaximumZOffset, sensor.MaximumZ);
            F32Vector(record, ContactLoadNormalOffset, sensor.LocalNormal.X, sensor.LocalNormal.Y, sensor.LocalNormal.Z);
            WriteEnergy(record, ContactLoadAlignmentOffset, sensor.MinimumAlignment);
            WriteEnergy(record, ContactLoadGapOffset, sensor.MaximumGap);
            WriteEnergy(record, ContactLoadMinimumMassOffset, sensor.MinimumMass);
            // Admitted state: Empty with no measured mass (both zero bytes).
        }
    }

    /// <summary>The number of compiled contact-load rows in a world.</summary>
    public static int ReadContactLoadCount(ReadOnlySpan<byte> data)
    {
        Header(data);
        return Layout(data).ContactLoads;
    }

    /// <summary>One compiled contact-load row, its frame and face resolved to identities and validated against the world.</summary>
    public static ContactLoadSensorDeclaration ReadContactLoadDeclaration(ReadOnlySpan<byte> data, int index)
    {
        Header(data);
        var layout = Layout(data);
        if (index < 0 || index >= layout.ContactLoads) throw new ArgumentException("Invalid contact-load row.");
        var record = data.Slice(layout.ContactLoadOffset(index), ContactLoadSensorBytes);
        var frame = R32(record, ContactLoadFrameOffset); var face = R32(record, ContactLoadFaceOffset);
        if (frame >= R32(data, HeaderBodyCountOffset) || BodyMotion(data, frame) != RigidMotionKind.Static ||
            face >= R32(data, HeaderColliderCountOffset) ||
            R32(data, layout.ColliderOffset(checked((int)face)) + ColliderBodyOffset) != frame)
            throw new ArgumentException("Contact-load row names an unowned frame or face.");
        var result = new ContactLoadSensorDeclaration(new(R64(record, RecordIdentityOffset)), new(R64(data, layout.BodyOffset(checked((int)frame)))),
            new(R64(data, layout.ColliderOffset(checked((int)face)))),
            RF(record, ContactLoadMinimumXOffset), RF(record, ContactLoadMaximumXOffset),
            RF(record, ContactLoadMinimumZOffset), RF(record, ContactLoadMaximumZOffset),
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
        var record = data.Slice(Layout(data).ContactLoadOffset(index), ContactLoadSensorBytes);
        var phase = (ContactLoadPhase)R32(record, ContactLoadPhaseOffset); var mass = RF(record, ContactLoadMassOffset);
        if (!Enum.IsDefined(phase) || !float.IsFinite(mass) || mass < 0 || mass > 4096 || declaration.PhaseOf(mass) != phase)
            throw new ArgumentException("Invalid committed contact-load phase or mass.");
        return new(declaration.Id, declaration.Frame, declaration.Face, mass, phase);
    }

    private static void ValidateContactLoadCandidate(ReadOnlySpan<byte> candidate, ReadOnlySpan<byte> source)
    {
        // The header comparison already pinned the population; each row's declaration stays fixed.
        var layout = Layout(source);
        for (var i = 0; i < layout.ContactLoads; i++)
        {
            var offset = layout.ContactLoadOffset(i);
            if (!candidate.Slice(offset, ContactLoadDeclarationBytes).SequenceEqual(source.Slice(offset, ContactLoadDeclarationBytes)))
                throw new ArgumentException("Contact-load declaration changed.");
            _ = ReadContactLoadSensor(candidate, i);
        }
    }
}
