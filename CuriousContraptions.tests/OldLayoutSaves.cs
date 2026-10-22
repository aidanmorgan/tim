using CuriousContraptions.Gpu;
using CuriousContraptions.Wire;
using Google.FlatBuffers;

namespace CuriousContraptions.Tests;

/// <summary>Stored saves of the pre-C-Bumper+Spring InstanceRecord (24 fields, 3fce7a03): slots 10–12 stiffness/damping/
/// initial_compression, 13–16 battery, 17–19 strength/reference_mass/preload, 20 half_extents, 21 friction, 22 target_count and
/// 23 parameters. Encoded by raw slot index, in the old encoder's field order, so the bytes do not depend on the current generated
/// accessors (on the record path they equalled the old production encoder byte for byte:
/// evidence-c-bumper-spring/baseline/old-layout-equality.txt).</summary>
internal static class OldLayoutSaves
{
    private const int OldFieldCount = 24;

    /// <summary>One f32/u8/u32 scalar or Vec3f struct at an old slot.</summary>
    internal readonly record struct Slot(int Index, float Value = 0f, byte? Byte = null, uint? Unsigned = null, System.Numerics.Vector3? Vector = null);

    internal static byte[] Save(string part, IReadOnlyList<Slot> slots, IReadOnlyList<(string Key, float Value)>? parameters = null)
    {
        var builder = new FlatBufferBuilder(512);
        return SaveFixture.Finish(builder, Record(builder, part, slots, parameters), 2);
    }

    /// <summary>The same old-layout construction carried by a Construct command (the worker's command decode boundary).</summary>
    internal static byte[] Command(string part, IReadOnlyList<Slot> slots, IReadOnlyList<(string Key, float Value)>? parameters = null)
    {
        var builder = new FlatBufferBuilder(512);
        var bytes = new byte[RuntimeSessionId.ByteLength];
        new RuntimeSessionId(1, 2).WriteTo(bytes);
        var session = Wire.WorkshopResponse.CreateSessionVectorBlock(builder, bytes);
        var record = Record(builder, part, slots, parameters);
        Wire.WorkshopCommand.StartWorkshopCommand(builder);
        Wire.WorkshopCommand.AddSequence(builder, 1);
        Wire.WorkshopCommand.AddKind(builder, CommandKind.Construct);
        Wire.WorkshopCommand.AddEpoch(builder, 1);
        Wire.WorkshopCommand.AddRevision(builder, 1);
        Wire.WorkshopCommand.AddRevisionKind(builder, (uint)ExpectedRevisionKind.Exact);
        Wire.WorkshopCommand.AddSession(builder, session);
        Wire.WorkshopCommand.AddCadence(builder, 1);
        Wire.WorkshopCommand.AddProjection(builder, 1);
        Wire.WorkshopCommand.AddConstructionRecord(builder, record);
        builder.FinishSizePrefixed(Wire.WorkshopCommand.EndWorkshopCommand(builder).Value, WorkshopWireFlatBuffers.CommandIdentifier);
        return builder.SizedByteArray();
    }

    private static Offset<ConstructionRecord> Record(FlatBufferBuilder builder, string part, IReadOnlyList<Slot> slots,
        IReadOnlyList<(string Key, float Value)>? parameters)
    {
        var name = builder.CreateString(part);
        VectorOffset? parameterVector = null;
        if (parameters is not null)
            parameterVector = InstanceRecord.CreateParametersVector(builder, parameters.Select(p =>
                ParameterRecord.CreateParameterRecord(builder, builder.CreateString(p.Key), p.Value)).ToArray());
        builder.StartTable(OldFieldCount);
        if (parameterVector is { } vector) builder.AddOffset(23, vector.Value, 0);
        builder.AddUlong(0, 1, 0);
        builder.AddOffset(1, name.Value, 0);
        builder.AddBool(2, false, false);
        builder.AddStruct(3, Vec3i.CreateVec3i(builder, 0, 32, 0).Value, 0);
        builder.AddStruct(4, Vec3f.CreateVec3f(builder, 0, 0, 0).Value, 0);
        builder.AddStruct(5, Quat4f.CreateQuat4f(builder, 0, 0, 0, 1).Value, 0);
        foreach (var slot in slots)
        {
            if (slot.Vector is { } v) builder.AddStruct(slot.Index, Vec3f.CreateVec3f(builder, v.X, v.Y, v.Z).Value, 0);
            else if (slot.Byte is { } b) builder.AddByte(slot.Index, b, 0);
            else if (slot.Unsigned is { } u) builder.AddUint(slot.Index, u, 0);
            else builder.AddFloat(slot.Index, slot.Value, 0);
        }
        var instance = new Offset<InstanceRecord>(builder.EndTable());
        var instances = ConstructionRecord.CreateInstancesVector(builder, [instance]);
        var settings = WorkshopCadenceSettings.Default();
        ConstructionRecord.StartConstructionRecord(builder);
        ConstructionRecord.AddRevision(builder, 1);
        ConstructionRecord.AddSettings(builder, CadenceSettingsRecord.CreateCadenceSettingsRecord(builder, (uint)settings.Simulation,
            (uint)settings.Physical, (uint)settings.Animation, (uint)settings.Presentation, settings.PresentationRate.Numerator,
            settings.PresentationRate.Denominator));
        ConstructionRecord.AddInstances(builder, instances);
        return ConstructionRecord.EndConstructionRecord(builder);
    }

    /// <summary>One old record: its element and the old slots and parameters it stored.</summary>
    internal sealed record OldRecord(string Name, string Part, Slot[] Slots, (string Key, float Value)[]? Parameters = null)
    {
        public byte[] Save() => OldLayoutSaves.Save(Part, Slots, Parameters);
        public byte[] Command() => OldLayoutSaves.Command(Part, Slots, Parameters);
    }

    /// <summary>Each pre-conversion record kind this slice's schema change moves, with the old values it stored.</summary>
    internal static IEnumerable<OldRecord> Records()
    {
        yield return new("bumper strength 8", "bumper", [new(17, 8f), new(18, 1f), new(19, 32f)]);
        yield return new("bumper strength 0", "bumper", [new(18, 1f)]);
        yield return new("spring default", "spring", [new(10, 400f), new(11, .2f)]);
        yield return new("spring charged", "spring", [new(10, 400f), new(11, .2f), new(12, .2f)]);
        yield return new("battery default", "battery", [new(13, 3600f), new(14, 120f), new(15, 1f), new(16, Byte: 1)]);
        yield return new("domino", "domino", [new(20, Vector: new((float)(Half).125, (float)(Half).55, (float)(Half).325)),
            new(6, (float)(Half).4), new(7, (float)(Half).05), new(21, (float)(Half).6)]);
        yield return new("counter", "counter", [new(22, Unsigned: 3)]);
        yield return new("switch threshold", "switch", [], [("trigger_threshold", 1.5f)]);
        yield return new("resized ramp", "ramp", [], [("length", 3.123457f), ("width", 1.234567f)]);
    }
}
