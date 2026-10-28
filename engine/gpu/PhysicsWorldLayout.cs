using System;
using System.Buffers.Binary;

namespace CuriousContraptions.Gpu;

/// <summary>
/// Story 9R.5b: the one layout authority for a physical world. Every table is exactly as long as its compiled population, and
/// every population is a count in the world header, so the offsets and the world's length derive from the header alone. There
/// is no fixed table capacity: memory is the only bound (owner decision, 20 Oct 2026).
/// Table order: bodies, materials, residence sensors, guides, triggers, contact works, orientation sensors,
/// the motion payload, electrical sources, electrical bindings, prismatic joints, contact-load rows, colliders.
/// </summary>
public readonly record struct PhysicsWorldLayout(int Bodies, int Colliders, int Materials, int Sensors, int Dynamic, int Guides,
    int Triggers, int ContactWorks, int OrientationSensors, int ElectricalSources, int ElectricalBindings,
    int Prismatics, int ContactLoads)
{
    /// <summary>The body table directly follows the header in every world.</summary>
    public static int BodiesOffset => PhysicsGpuAbi.HeaderBytes;
    public int MaterialsOffset => After(BodiesOffset, Bodies, PhysicsGpuAbi.BodyBytes);
    public int SensorsOffset => After(MaterialsOffset, Materials, PhysicsGpuAbi.MaterialBytes);
    public int GuidesOffset => After(SensorsOffset, Sensors, PhysicsGpuAbi.SensorBytes);
    public int TriggersOffset => After(GuidesOffset, Guides, PhysicsGpuAbi.GuideBytes);
    public int ContactWorksOffset => After(TriggersOffset, Triggers, PhysicsGpuAbi.TriggerBytes);
    public int OrientationSensorsOffset => After(ContactWorksOffset, ContactWorks, PhysicsGpuAbi.ContactWorkBytes);
    public int MotionOffset => After(OrientationSensorsOffset, OrientationSensors, PhysicsGpuAbi.OrientationSensorBytes);
    /// <summary>The motion payload: room for every dynamic body's pieces at the largest admitted substep count.</summary>
    public int MotionBytes => PhysicsMotionRead.ByteLength(Dynamic);
    public int ElectricalSourcesOffset => After(MotionOffset, 1, MotionBytes);
    public int ElectricalBindingsOffset => After(ElectricalSourcesOffset, ElectricalSources, PhysicsGpuAbi.ElectricalSourceBytes);
    public int PrismaticsOffset => After(ElectricalBindingsOffset, ElectricalBindings, PhysicsGpuAbi.ElectricalBindingBytes);
    public int ContactLoadsOffset => After(PrismaticsOffset, Prismatics, PhysicsGpuAbi.PrismaticBytes);
    public int CollidersOffset => After(ContactLoadsOffset, ContactLoads, PhysicsGpuAbi.ContactLoadSensorBytes);
    /// <summary>The exact world length.</summary>
    public int ByteLength => After(CollidersOffset, Colliders, PhysicsGpuAbi.ColliderBytes);

    public int BodyOffset(int slot) => Row(BodiesOffset, slot, Bodies, PhysicsGpuAbi.BodyBytes);
    public int MaterialOffset(int slot) => Row(MaterialsOffset, slot, Materials, PhysicsGpuAbi.MaterialBytes);
    public int SensorOffset(int slot) => Row(SensorsOffset, slot, Sensors, PhysicsGpuAbi.SensorBytes);
    public int GuideOffset(int slot) => Row(GuidesOffset, slot, Guides, PhysicsGpuAbi.GuideBytes);
    public int TriggerOffset(int slot) => Row(TriggersOffset, slot, Triggers, PhysicsGpuAbi.TriggerBytes);
    public int ContactWorkOffset(int slot) => Row(ContactWorksOffset, slot, ContactWorks, PhysicsGpuAbi.ContactWorkBytes);
    public int OrientationSensorOffset(int slot) => Row(OrientationSensorsOffset, slot, OrientationSensors, PhysicsGpuAbi.OrientationSensorBytes);
    public int ElectricalSourceOffset(int slot) => Row(ElectricalSourcesOffset, slot, ElectricalSources, PhysicsGpuAbi.ElectricalSourceBytes);
    public int ElectricalBindingOffset(int slot) => Row(ElectricalBindingsOffset, slot, ElectricalBindings, PhysicsGpuAbi.ElectricalBindingBytes);
    public int PrismaticOffset(int slot) => Row(PrismaticsOffset, slot, Prismatics, PhysicsGpuAbi.PrismaticBytes);
    public int ContactLoadOffset(int slot) => Row(ContactLoadsOffset, slot, ContactLoads, PhysicsGpuAbi.ContactLoadSensorBytes);
    public int ColliderOffset(int slot) => Row(CollidersOffset, slot, Colliders, PhysicsGpuAbi.ColliderBytes);

    /// <summary>The layout a world header declares. Every count is a non-negative population; a world whose tables cannot be
    /// addressed (longer than an array can hold) is rejected.</summary>
    public static PhysicsWorldLayout Read(ReadOnlySpan<byte> header)
    {
        if (header.Length < PhysicsGpuAbi.HeaderBytes) throw new ArgumentException("A physics world is shorter than its header.");
        var layout = new PhysicsWorldLayout(Count(header, PhysicsGpuAbi.HeaderBodyCountOffset), Count(header, PhysicsGpuAbi.HeaderColliderCountOffset),
            Count(header, PhysicsGpuAbi.HeaderMaterialCountOffset), Count(header, PhysicsGpuAbi.HeaderSensorCountOffset),
            Count(header, PhysicsGpuAbi.HeaderDynamicCountOffset), Count(header, PhysicsGpuAbi.HeaderGuideCountOffset),
            Count(header, PhysicsGpuAbi.HeaderTriggerCountOffset), Count(header, PhysicsGpuAbi.HeaderContactWorkCountOffset),
            Count(header, PhysicsGpuAbi.HeaderOrientationSensorCountOffset),
            Count(header, PhysicsGpuAbi.HeaderElectricalSourceCountOffset), Count(header, PhysicsGpuAbi.HeaderElectricalBindingCountOffset),
            Count(header, PhysicsGpuAbi.HeaderPrismaticCountOffset), Count(header, PhysicsGpuAbi.HeaderContactLoadCountOffset));
        _ = layout.ByteLength;
        return layout;
    }

    /// <summary>Writes every population count into a world header.</summary>
    public void WriteCounts(Span<byte> header)
    {
        Write(header, PhysicsGpuAbi.HeaderBodyCountOffset, Bodies); Write(header, PhysicsGpuAbi.HeaderColliderCountOffset, Colliders);
        Write(header, PhysicsGpuAbi.HeaderMaterialCountOffset, Materials); Write(header, PhysicsGpuAbi.HeaderSensorCountOffset, Sensors);
        Write(header, PhysicsGpuAbi.HeaderDynamicCountOffset, Dynamic); Write(header, PhysicsGpuAbi.HeaderGuideCountOffset, Guides);
        Write(header, PhysicsGpuAbi.HeaderTriggerCountOffset, Triggers); Write(header, PhysicsGpuAbi.HeaderContactWorkCountOffset, ContactWorks);
        Write(header, PhysicsGpuAbi.HeaderOrientationSensorCountOffset, OrientationSensors);
        Write(header, PhysicsGpuAbi.HeaderElectricalSourceCountOffset, ElectricalSources);
        Write(header, PhysicsGpuAbi.HeaderElectricalBindingCountOffset, ElectricalBindings);
        Write(header, PhysicsGpuAbi.HeaderPrismaticCountOffset, Prismatics); Write(header, PhysicsGpuAbi.HeaderContactLoadCountOffset, ContactLoads);
    }

    private static int Count(ReadOnlySpan<byte> header, int offset)
    {
        var value = BinaryPrimitives.ReadUInt32LittleEndian(header[offset..]);
        if (value > int.MaxValue) throw new WorkshopStorageException("Physical world storage is not representable.");
        return (int)value;
    }
    private static void Write(Span<byte> header, int offset, int value) =>
        BinaryPrimitives.WriteUInt32LittleEndian(header[offset..], checked((uint)value));
    private static int After(int offset, int count, int bytes)
    {
        if (count < 0) throw new ArgumentException("A table population cannot be negative.");
        var end = offset + (long)count * bytes;
        if (end > Array.MaxLength) throw new WorkshopStorageException("Physical world storage is not representable.");
        return (int)end;
    }
    private static int Row(int offset, int slot, int count, int bytes)
    {
        if ((uint)slot >= (uint)count) throw new ArgumentOutOfRangeException(nameof(slot));
        return offset + slot * bytes;
    }
}
