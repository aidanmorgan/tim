using CuriousContraptions.Gpu;
using CuriousContraptions.Wire;
using Google.FlatBuffers;

namespace CuriousContraptions.Tests;

/// <summary>Stored saves of two earlier InstanceRecord layouts, encoded by raw slot index in the old encoder's field order, so the bytes
/// do not depend on the current generated accessors.
/// <list type="bullet">
/// <item><see cref="Layout.PreLaunch"/> (24 fields, 3fce7a03, before C-Bumper+Spring): slots 10–12 stiffness/damping/
/// initial_compression, 13–16 battery, 17–19 strength/reference_mass/preload, 20 half_extents, 21 friction, 22 target_count and
/// 23 parameters. On the record path these equalled the old production encoder byte for byte
/// (evidence-c-bumper-spring/baseline/old-layout-equality.txt).</item>
/// <item><see cref="Layout.PreSourceStore"/> (18 fields, f27cb71d, before Story 9A.39): slots 10–13 battery capacity/maximum_power/
/// initial_fraction/enabled, 14 half_extents, 15 friction, 16 target_count and 17 parameters. Their bytes equal that commit's
/// production encoder output (sha256 in evidence-9a-39/baseline/capture/old-saves.sha256).</item>
/// </list></summary>
internal static class OldLayoutSaves
{
    /// <summary>One old table layout and the construction values its pinned saves were written with.</summary>
    internal sealed record Layout(int FieldCount, int ParametersSlot, int CellY, ulong NextBodyId)
    {
        public static Layout PreLaunch { get; } = new(24, 23, 32, 2);
        public static Layout PreSourceStore { get; } = new(18, 17, 16, 3);
        /// <summary>The 14-field table of 2065d3c5 (before Story 9A.32): 6 mass, 7 bounce, 8 length, 9 threshold, 10 half_extents,
        /// 11 friction, 12 target_count, 13 parameters.</summary>
        public static Layout PreDomino { get; } = new(14, 13, 16, 2);
    }

    /// <summary>One f32/u8/u32 scalar or Vec3f struct at an old slot.</summary>
    internal readonly record struct Slot(int Index, float Value = 0f, byte? Byte = null, uint? Unsigned = null, System.Numerics.Vector3? Vector = null);

    internal static byte[] Save(Layout layout, string part, IReadOnlyList<Slot> slots, IReadOnlyList<(string Key, float Value)>? parameters = null)
    {
        var builder = new FlatBufferBuilder(512);
        return SaveFixture.Finish(builder, Record(builder, layout, part, slots, parameters), layout.NextBodyId);
    }

    /// <summary>The same old-layout construction carried by a Construct command (the worker's command decode boundary).</summary>
    internal static byte[] Command(Layout layout, string part, IReadOnlyList<Slot> slots, IReadOnlyList<(string Key, float Value)>? parameters = null)
    {
        var builder = new FlatBufferBuilder(512);
        var bytes = new byte[RuntimeSessionId.ByteLength];
        new RuntimeSessionId(1, 2).WriteTo(bytes);
        var session = Wire.WorkshopResponse.CreateSessionVectorBlock(builder, bytes);
        var record = Record(builder, layout, part, slots, parameters);
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

    private static Offset<ConstructionRecord> Record(FlatBufferBuilder builder, Layout layout, string part, IReadOnlyList<Slot> slots,
        IReadOnlyList<(string Key, float Value)>? parameters)
    {
        var name = builder.CreateString(part);
        VectorOffset? parameterVector = null;
        if (parameters is not null)
            parameterVector = InstanceRecord.CreateParametersVector(builder, parameters.Select(p =>
                ParameterRecord.CreateParameterRecord(builder, builder.CreateString(p.Key), p.Value)).ToArray());
        builder.StartTable(layout.FieldCount);
        if (parameterVector is { } vector) builder.AddOffset(layout.ParametersSlot, vector.Value, 0);
        builder.AddUlong(0, 1, 0);
        builder.AddOffset(1, name.Value, 0);
        builder.AddBool(2, false, false);
        builder.AddStruct(3, Vec3i.CreateVec3i(builder, 0, layout.CellY, 0).Value, 0);
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
        public Layout Layout { get; init; } = Layout.PreLaunch;
        public byte[] Save() => OldLayoutSaves.Save(Layout, Part, Slots, Parameters);
        public byte[] Command() => OldLayoutSaves.Command(Layout, Part, Slots, Parameters);
    }

    private static (string Key, float Value)[] Declared(PartId part) =>
        InstanceParameterCollection.Defaults(part).Select(pair => (pair.Key.Value, pair.Value)).ToArray();

    /// <summary>Each pre-9A.39 record kind whose meaning the source-store change moves (the battery's four fields left the table, so
    /// half_extents, friction, target_count and parameters moved down four slots), with the values f27cb71d's production encoder
    /// stored for evidence-9a-39/baseline/capture/old-saves.</summary>
    internal static IEnumerable<OldRecord> PreSourceStoreRecords()
    {
        var layout = Layout.PreSourceStore;
        yield return new("battery_default", "battery", [new(10, 3600f), new(11, 120f), new(12, 1f), new(13, Byte: 1)]) { Layout = layout };
        yield return new("battery_disabled", "battery", [new(10, 3600f), new(11, 120f), new(12, 1f)]) { Layout = layout };
        yield return new("battery_custom_120_30_half", "battery", [new(10, 120f), new(11, 30f), new(12, .5f), new(13, Byte: 1)]) { Layout = layout };
        yield return new("battery_custom_disabled_zero", "battery", [new(10, 600f), new(11, 10f)]) { Layout = layout };
        yield return new("domino", "domino", [new(14, Vector: OldDominoHalfExtents),
            new(6, OldDominoMass), new(7, OldDominoBounce), new(15, OldDominoFriction)]) { Layout = layout };
        yield return new("counter", "counter", [new(16, Unsigned: 3)]) { Layout = layout };
        foreach (var (name, part) in new[] { ("impact_switch", PartId.ImpactSwitch), ("ramp", PartId.Ramp), ("bumper", PartId.PinballBumper),
                     ("receiver", PartId.Receiver), ("springboard", PartId.Springboard), ("wall", PartId.Wall), ("hold_timer", new PartId("hold_timer")) })
            yield return new(name, part.Value, [], Declared(part)) { Layout = layout };
    }

    /// <summary>2065d3c5 records whose parameters vector (slot 13) lies beyond the current 9-field table: a zero-parameter domino and
    /// lamp each carrying an undeclared parameter, and a bare Basketball with the empty parameters vector the base encoder wrote for
    /// every generic instance.</summary>
    internal static IEnumerable<OldRecord> PreDominoRecords()
    {
        var layout = Layout.PreDomino;
        yield return new("domino with a parameter", "domino", [], [("mass", .5f)]) { Layout = layout };
        yield return new("lamp with a parameter", "lamp", [], [("radius", .5f)]) { Layout = layout };
        yield return new("bare ball, empty parameters vector", "ball", [], []) { Layout = layout };
    }

    /// <summary>The old Domino record's material as its encoder stored it: the half-precision-rounded 0.125 × 0.55 × 0.325 m, 0.4 kg,
    /// bounce 0.05 and friction 0.6, widened to f32 (exact constants, so the old bytes depend on no current narrowing).</summary>
    internal static readonly System.Numerics.Vector3 OldDominoHalfExtents = new(.125f, .5498046875f, .324951171875f);
    internal const float OldDominoMass = .39990234375f, OldDominoBounce = .04998779296875f, OldDominoFriction = .60009765625f;

    /// <summary>Each pre-conversion record kind C-Bumper+Spring's schema change moved, with the old values it stored.</summary>
    internal static IEnumerable<OldRecord> Records()
    {
        yield return new("bumper strength 8", "bumper", [new(17, 8f), new(18, 1f), new(19, 32f)]);
        yield return new("bumper strength 0", "bumper", [new(18, 1f)]);
        yield return new("spring default", "spring", [new(10, 400f), new(11, .2f)]);
        yield return new("spring charged", "spring", [new(10, 400f), new(11, .2f), new(12, .2f)]);
        yield return new("battery default", "battery", [new(13, 3600f), new(14, 120f), new(15, 1f), new(16, Byte: 1)]);
        yield return new("domino", "domino", [new(20, Vector: OldDominoHalfExtents),
            new(6, OldDominoMass), new(7, OldDominoBounce), new(21, OldDominoFriction)]);
        yield return new("counter", "counter", [new(22, Unsigned: 3)]);
        yield return new("switch threshold", "switch", [], [("trigger_threshold", 1.5f)]);
        yield return new("resized ramp", "ramp", [], [("length", 3.123457f), ("width", 1.234567f)]);
    }
}
