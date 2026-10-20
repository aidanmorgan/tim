// Story 9.4b D4 regression: the construction decode boundary for every declared Pressure Plate / Counter / Clock setting.
// Adopted from the Murdoch-2 review probe MurdochDecodeBoundaryTests.cs (sha256 f02202eb…); the assertions are unchanged.
// The class is renamed for what it proves, the part identity is the typed PartId (its string only at the wire boundary)
// and the truncation diagnostic line is renamed.
//
// Every declared setting is decoded through all three shipped construction decoders:
//   FB  WorkshopWireFlatBuffers.DecodeConstructionRecord (save record and Construct command record),
//   SAV WorkshopSaveCodec.Decode (the IndexedDB save boundary, wraps FB),
//   BIN WorkshopWire.ReadConstruction (binary construction; reached from FlatBuffers Construct commands that carry
//       ConstructionArray, WorkshopWireFlatBuffers.cs DecodeCommand, and from WorkshopWire.DecodeCommand).
// Expected (independently derived from WorkshopPressurePlate [0.1, 16] kg, WorkshopCounter whole [1, 9],
// WorkshopClock [0.1, 12] s and spec D4 "missing or zero declared settings reject; a fractional count rejects"):
//   accept exactly the admitted values and decode them bit-for-bit; reject everything else with an ArgumentException
//   (never OverflowException, InvalidCastException, IndexOutOfRange or NullReference); the decoders agree.
// Positive controls use the identical hand-built record, so a rejection cannot come from a malformed fixture.
using System;
using System.Buffers.Binary;
using System.Linq;
using CuriousContraptions.Gpu;
using Xunit;

namespace CuriousContraptions.Tests;

public sealed class ConstructionDecodeBoundaryTests
{
    public enum Part { PressurePlate, Counter, Clock }

    private static PartId Identity(Part part) => part switch
    {
        Part.PressurePlate => PartId.PressurePlate, Part.Counter => PartId.Counter, Part.Clock => PartId.Clock,
        _ => throw new ArgumentOutOfRangeException(nameof(part)),
    };

    // 2^31 and 2^31 - 1 are the same f32 (2147483648f); 2147483520f is the largest f32 below 2^31.
    private const float TwoTo31 = 2147483648f;
    private const float BelowTwoTo31 = 2147483520f;

    private static bool Admitted(Part part, float value) => part switch
    {
        Part.PressurePlate => float.IsFinite(value) && value >= .1f && value <= 16f,
        Part.Counter => float.IsFinite(value) && value == MathF.Round(value) && value >= 1f && value <= 9f,
        Part.Clock => float.IsFinite(value) && value >= .1f && value <= 12f,
        _ => throw new ArgumentOutOfRangeException(nameof(part)),
    };

    public static TheoryData<Part, float> FloatBoundaries()
    {
        var data = new TheoryData<Part, float>();
        float[] common =
        [
            0f, -0f, 2.5f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity, TwoTo31, (float)int.MaxValue,
            BelowTwoTo31, -TwoTo31, float.Epsilon, float.MaxValue, float.MinValue, 1f, 3f, 9f, 9.5f, 10f,
            BitConverter.Int32BitsToSingle(0x7F800001), // signalling-pattern NaN
            BitConverter.Int32BitsToSingle(unchecked((int)0xFFC00000)), // negative quiet NaN
        ];
        foreach (var part in Enum.GetValues<Part>())
        {
            foreach (var value in common) data.Add(part, value);
            var (min, max) = part switch { Part.PressurePlate => (.1f, 16f), Part.Clock => (.1f, 12f), _ => (1f, 9f) };
            data.Add(part, min); data.Add(part, max);
            data.Add(part, MathF.BitDecrement(min)); data.Add(part, MathF.BitIncrement(max));
            if (part == Part.Counter) { data.Add(part, MathF.BitDecrement(9f)); data.Add(part, MathF.BitIncrement(1f)); }
        }
        return data;
    }

    // ---------- FlatBuffers record (FB) and save (SAV) ----------

    private static Google.FlatBuffers.Offset<CuriousContraptions.Wire.ConstructionRecord> BuildRecord(
        Google.FlatBuffers.FlatBufferBuilder builder, Part part, float? threshold)
    {
        var id = builder.CreateString(Identity(part).Value);
        CuriousContraptions.Wire.InstanceRecord.StartInstanceRecord(builder);
        CuriousContraptions.Wire.InstanceRecord.AddId(builder, 1);
        CuriousContraptions.Wire.InstanceRecord.AddPartId(builder, id);
        CuriousContraptions.Wire.InstanceRecord.AddCell(builder, CuriousContraptions.Wire.Vec3i.CreateVec3i(builder, 0, 16, 0));
        CuriousContraptions.Wire.InstanceRecord.AddLocal(builder, CuriousContraptions.Wire.Vec3f.CreateVec3f(builder, 0, 0, 0));
        CuriousContraptions.Wire.InstanceRecord.AddRotation(builder, CuriousContraptions.Wire.Quat4f.CreateQuat4f(builder, 0, 0, 0, 1));
        if (threshold is { } value) CuriousContraptions.Wire.InstanceRecord.AddThreshold(builder, value);
        var instance = CuriousContraptions.Wire.InstanceRecord.EndInstanceRecord(builder);
        var instances = CuriousContraptions.Wire.ConstructionRecord.CreateInstancesVector(builder, [instance]);
        var settings = WorkshopCadenceSettings.Default();
        CuriousContraptions.Wire.ConstructionRecord.StartConstructionRecord(builder);
        CuriousContraptions.Wire.ConstructionRecord.AddRevision(builder, 1);
        CuriousContraptions.Wire.ConstructionRecord.AddSettings(builder, CuriousContraptions.Wire.CadenceSettingsRecord.CreateCadenceSettingsRecord(
            builder, (uint)WorkshopSettingsVersion.SharedMaster, (uint)settings.Simulation, (uint)settings.Physical,
            (uint)settings.Animation, (uint)settings.Presentation, settings.PresentationRate.Numerator, settings.PresentationRate.Denominator));
        CuriousContraptions.Wire.ConstructionRecord.AddInstances(builder, instances);
        return CuriousContraptions.Wire.ConstructionRecord.EndConstructionRecord(builder);
    }

    private static WorkshopConstruction DecodeRecord(Part part, float? threshold)
    {
        var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
        builder.Finish(BuildRecord(builder, part, threshold).Value);
        return WorkshopWireFlatBuffers.DecodeConstructionRecord(CuriousContraptions.Wire.ConstructionRecord.GetRootAsConstructionRecord(
            new Google.FlatBuffers.ByteBuffer(builder.SizedByteArray())));
    }

    private static WorkshopConstruction DecodeSave(Part part, float? threshold)
    {
        var builder = new Google.FlatBuffers.FlatBufferBuilder(1024);
        var record = BuildRecord(builder, part, threshold);
        CuriousContraptions.Wire.SavedConstruction.StartSavedConstruction(builder);
        CuriousContraptions.Wire.SavedConstruction.AddMagic(builder, WorkshopSaveCodec.Magic);
        CuriousContraptions.Wire.SavedConstruction.AddVersion(builder, WorkshopSaveCodec.Version);
        CuriousContraptions.Wire.SavedConstruction.AddNextBodyId(builder, 2);
        CuriousContraptions.Wire.SavedConstruction.AddConstruction(builder, record);
        builder.Finish(CuriousContraptions.Wire.SavedConstruction.EndSavedConstruction(builder).Value);
        return WorkshopSaveCodec.Decode(builder.SizedByteArray()).Construction;
    }

    private static float DecodedSetting(WorkshopConstruction construction, Part part)
    {
        var instance = Assert.Single(construction.Instances);
        return part switch
        {
            Part.PressurePlate => Assert.IsType<WorkshopPressurePlate>(instance).MinimumMass,
            Part.Counter => Assert.IsType<WorkshopCounter>(instance).TargetCount,
            Part.Clock => Assert.IsType<WorkshopClock>(instance).IntervalSeconds,
            _ => throw new ArgumentOutOfRangeException(nameof(part)),
        };
    }

    private static void AssertRejectedAsArgument(Func<object> decode, string label)
    {
        var error = Record.Exception(decode);
        Assert.True(error is not null, $"{label}: decoded instead of rejecting");
        Assert.True(error is ArgumentException, $"{label}: rejected with {error!.GetType().Name} ({error.Message}), not an ArgumentException");
    }

    [Theory]
    [MemberData(nameof(FloatBoundaries))]
    public void FlatBuffersRecordAcceptsExactlyTheAdmittedSetting(Part part, float value)
    {
        var label = $"FB {part} {value:R} (0x{BitConverter.SingleToInt32Bits(value):X8})";
        if (Admitted(part, value))
            Assert.Equal(BitConverter.SingleToInt32Bits(value), BitConverter.SingleToInt32Bits(DecodedSetting(DecodeRecord(part, value), part)));
        else AssertRejectedAsArgument(() => DecodeRecord(part, value), label);
    }

    [Theory]
    [MemberData(nameof(FloatBoundaries))]
    public void SaveCodecAcceptsExactlyTheAdmittedSetting(Part part, float value)
    {
        var label = $"SAV {part} {value:R}";
        if (Admitted(part, value))
            Assert.Equal(BitConverter.SingleToInt32Bits(value), BitConverter.SingleToInt32Bits(DecodedSetting(DecodeSave(part, value), part)));
        else AssertRejectedAsArgument(() => DecodeSave(part, value), label);
    }

    [Theory]
    [InlineData(Part.PressurePlate)]
    [InlineData(Part.Counter)]
    [InlineData(Part.Clock)]
    public void MissingSettingRejectsInEveryFlatBuffersPath(Part part)
    {
        AssertRejectedAsArgument(() => DecodeRecord(part, null), $"FB {part} missing");
        AssertRejectedAsArgument(() => DecodeSave(part, null), $"SAV {part} missing");
    }

    // Positive control for the fixture itself: the same builder with a canonical value decodes (so every rejection above is
    // caused by the value, not by an incomplete hand-built record).
    [Theory]
    [InlineData(Part.PressurePlate, .5f)]
    [InlineData(Part.Counter, 3f)]
    [InlineData(Part.Clock, 1f)]
    public void HandBuiltRecordIsAValidFixture(Part part, float canonical)
    {
        Assert.Equal(canonical, DecodedSetting(DecodeRecord(part, canonical), part));
        Assert.Equal(canonical, DecodedSetting(DecodeSave(part, canonical), part));
    }

    // ---------- Binary construction (BIN) ----------

    private static IWorkshopInstance Canonical(Part part) => part switch
    {
        Part.PressurePlate => WorkshopInput.PressurePlate(new(1), 0, 1, 0, 0, 0, 0, 1, minimumMass: .5f),
        Part.Counter => WorkshopInput.Counter(new(1), 0, 1, 0, 0, 0, 0, 1, targetCount: 3),
        Part.Clock => WorkshopInput.Clock(new(1), 0, 1, 0, 0, 0, 0, 1, intervalSeconds: 1f),
        _ => throw new ArgumentOutOfRangeException(nameof(part)),
    };

    private const int SettingOffset = WorkshopWire.ConstructionHeaderBytes + 112; // instance slot 0, declared setting

    private static byte[] Binary(Part part)
    {
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new WorkshopInstances(Canonical(part)));
        var bytes = new byte[WorkshopWire.ConstructionBytes(construction)];
        WorkshopWire.WriteConstruction(bytes, construction);
        return bytes;
    }

    [Theory]
    [MemberData(nameof(FloatBoundaries))]
    public void BinaryDecoderAgreesWithTheFlatBuffersDecoder(Part part, float value)
    {
        var bytes = Binary(part);
        // A Counter f32 whose bit pattern is itself an admitted Int32 count (float.Epsilon is 0x00000001) is not a confusion case.
        if (part == Part.Counter && !Admitted(part, value) && BitConverter.SingleToInt32Bits(value) is >= 1 and <= 9) return;
        // The binary layout stores the Counter as an Int32 count and the others as f32. A non-integral or non-finite f32
        // cannot be an Int32 count, so for the Counter the f32 bit pattern itself is written (bit-pattern confusion).
        if (part == Part.Counter && Admitted(part, value)) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(SettingOffset), (int)value);
        else if (part == Part.Counter) BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(SettingOffset), BitConverter.SingleToInt32Bits(value));
        else BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(SettingOffset), value);
        var label = $"BIN {part} {value:R}";
        var binaryAccepts = Record.Exception(() => WorkshopWire.ReadConstruction(bytes)) is null;
        var flatAccepts = Record.Exception(() => DecodeRecord(part, value)) is null;
        if (part != Part.Counter || Admitted(part, value))
            Assert.True(binaryAccepts == flatAccepts, $"{label}: binary accepts={binaryAccepts}, FlatBuffers accepts={flatAccepts}");
        if (Admitted(part, value))
            Assert.Equal(BitConverter.SingleToInt32Bits(value), BitConverter.SingleToInt32Bits(DecodedSetting(WorkshopWire.ReadConstruction(bytes), part)));
        else AssertRejectedAsArgument(() => WorkshopWire.ReadConstruction(bytes), label);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    [InlineData(1)]
    [InlineData(9)]
    public void BinaryCounterIntegerBoundaries(int count)
    {
        var bytes = Binary(Part.Counter);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(SettingOffset), count);
        if (count is >= 1 and <= 9) Assert.Equal(count, Assert.IsType<WorkshopCounter>(Assert.Single(WorkshopWire.ReadConstruction(bytes).Instances)).TargetCount);
        else AssertRejectedAsArgument(() => WorkshopWire.ReadConstruction(bytes), $"BIN counter {count}");
    }

    // A rejected binary construction carried by a Construct command rejects the whole command (atomic, no partial world).
    [Theory]
    [InlineData(Part.PressurePlate)]
    [InlineData(Part.Counter)]
    [InlineData(Part.Clock)]
    public void ZeroSettingRejectsTheBinaryConstructCommand(Part part)
    {
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), new(1),
            new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new WorkshopInstances(Canonical(part))),
            Session: new(1, 2), Cadence: new(1), Projection: new(1));
        var wire = WorkshopWire.Encode(command);
        var at = WorkshopWire.CommandHeaderBytes + SettingOffset;
        Assert.NotNull(WorkshopWire.DecodeCommand(wire).Construction); // control: the unmodified command decodes
        BinaryPrimitives.WriteInt32LittleEndian(wire.AsSpan(at), 0);
        AssertRejectedAsArgument(() => WorkshopWire.DecodeCommand(wire), $"BIN command {part} zero");
    }

    // A truncated save either rejects with ArgumentException or (when only trailing padding was cut) decodes to exactly the
    // saved machine. It never decodes to a different machine and never escapes as another exception type. Accepted
    // truncation lengths are printed: they show the save codec has no length or verifier integrity (no FlatBuffers Verifier).
    [Fact]
    public void EveryTruncatedSaveRejectsOrDecodesTheSameMachine()
    {
        var saved = new WorkshopSavedConstruction(
            new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
                new WorkshopInstances(Canonical(Part.PressurePlate), WorkshopInput.Clock(new(2), 1, 1, 0, 0, 0, 0, 1, intervalSeconds: 2f))),
            new(3));
        var save = WorkshopSaveCodec.Encode(saved);
        Assert.Equal(2, WorkshopSaveCodec.Decode(save).Construction.Instances.Count);
        var accepted = new System.Collections.Generic.List<int>();
        for (var n = 0; n < save.Length; n++)
        {
            var length = n;
            var error = Record.Exception(() => WorkshopSaveCodec.Decode(save.AsSpan(0, length)));
            if (error is not null) { Assert.True(error is ArgumentException, $"length {n}: {error.GetType().Name}"); continue; }
            accepted.Add(n);
            Assert.Equal(save, WorkshopSaveCodec.Encode(WorkshopSaveCodec.Decode(save.AsSpan(0, length))));
        }
        Console.WriteLine($"CONSTRUCTION_SAVE_TRUNCATION save={save.Length} accepted=[{string.Join(",", accepted)}]");
    }
}
