using CuriousContraptions.Abstractions;
// Story 9.4b D4 regression, carried onto the one codec by Story 9A.2: the construction decode boundary for every declared
// Pressure Plate / Counter / Clock setting.
//
// Every declared setting is decoded through both shipped construction decoders:
//   FB  WorkshopWireFlatBuffers.DecodeConstructionRecord (the Construct command record);
//   SAV WorkshopSaveCodec.Decode (the browser save boundary: structural pre-validation, then the same record decoder).
// The plate's minimum mass, the clock's interval and the counter's target are their generic instances' declared f32 minimum_mass,
// interval_seconds and WholeNumber target_count parameters (C-Plate, C-Clock, C-Memory). Expected (independently derived from the
// plate's declared [0.1, 16] kg (decision h), the counter's declared whole [1, 9] and the clock's declared [0.1, 12] s (CAT-017), and
// "a missing declared setting rejects; nothing is defaulted or coerced"): accept exactly the
// admitted values and decode them bit-for-bit; reject everything else. The command record rejects with the domain's argument
// guard or the wire's typed fault; the save rejects with its typed refusal and nothing else. Positive controls use the identical
// hand-built record, so a rejection cannot come from a malformed fixture.
using System;
using CuriousContraptions.Gpu;
using Xunit;

namespace CuriousContraptions.Tests;

public sealed class ConstructionDecodeBoundaryTests
{
    public enum Part { PressurePlate, Clock, Counter }

    // 2^31 and 2^31 - 1 are the same f32 (2147483648f); 2147483520f is the largest f32 below 2^31.
    private const float TwoTo31 = 2147483648f;
    private const float BelowTwoTo31 = 2147483520f;

    private static bool Admitted(Part part, float value) => part switch
    {
        Part.PressurePlate => float.IsFinite(value) && value >= .1f && value <= 16f,
        Part.Clock => float.IsFinite(value) && value >= .1f && value <= 12f,
        Part.Counter => float.IsFinite(value) && value == MathF.Truncate(value) && value >= 1f && value <= 9f,
        _ => throw new ArgumentOutOfRangeException(nameof(part)),
    };

    public static TheoryData<Part, float> FloatBoundaries()
    {
        var data = new TheoryData<Part, float>();
        float[] common =
        [
            // (float)int.MaxValue is TwoTo31 bit for bit, so it is not a row of its own (xUnit refuses duplicate IDs); float.NaN is the
            // negative quiet NaN 0xFFC00000 on .NET, so the explicit NaN rows are the other encodings.
            0f, -0f, 2.5f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity, TwoTo31,
            BelowTwoTo31, -TwoTo31, float.Epsilon, float.MaxValue, float.MinValue, 1f, 3f, 9f, 9.5f, 10f,
            BitConverter.Int32BitsToSingle(0x7F800001), // signalling-pattern NaN
            BitConverter.Int32BitsToSingle(0x7FC00000), // positive quiet NaN
        ];
        foreach (var part in Enum.GetValues<Part>())
        {
            foreach (var value in common) data.Add(part, value);
            var (min, max) = part switch { Part.PressurePlate => (.1f, 16f), Part.Counter => (1f, 9f), _ => (.1f, 12f) };
            // The counter's bounds 1 and 9 are already common rows (no duplicate theory IDs).
            if (part != Part.Counter) { data.Add(part, min); data.Add(part, max); }
            data.Add(part, MathF.BitDecrement(min)); data.Add(part, MathF.BitIncrement(max));
        }
        return data;
    }

    // ---------- the hand-built record ----------

    private static Google.FlatBuffers.Offset<CuriousContraptions.Wire.ConstructionRecord> BuildRecord(
        Google.FlatBuffers.FlatBufferBuilder builder, PartId part, float? length, (string Key, float Value)? parameter = null)
    {
        var id = builder.CreateString(part.Value);
        Google.FlatBuffers.VectorOffset? parameters = parameter is { } declared
            ? CuriousContraptions.Wire.InstanceRecord.CreateParametersVector(builder,
                [CuriousContraptions.Wire.ParameterRecord.CreateParameterRecord(builder, builder.CreateString(declared.Key), declared.Value)])
            : null;
        CuriousContraptions.Wire.InstanceRecord.StartInstanceRecord(builder);
        if (parameters is { } vector) CuriousContraptions.Wire.InstanceRecord.AddParameters(builder, vector);
        CuriousContraptions.Wire.InstanceRecord.AddId(builder, 1);
        CuriousContraptions.Wire.InstanceRecord.AddPartId(builder, id);
        CuriousContraptions.Wire.InstanceRecord.AddCell(builder, CuriousContraptions.Wire.Vec3i.CreateVec3i(builder, 0, 16, 0));
        CuriousContraptions.Wire.InstanceRecord.AddLocal(builder, CuriousContraptions.Wire.Vec3f.CreateVec3f(builder, 0, 0, 0));
        CuriousContraptions.Wire.InstanceRecord.AddRotation(builder, CuriousContraptions.Wire.Quat4f.CreateQuat4f(builder, 0, 0, 0, 1));
        if (length is { } value) CuriousContraptions.Wire.InstanceRecord.AddLength(builder, value);
        var instance = CuriousContraptions.Wire.InstanceRecord.EndInstanceRecord(builder);
        var instances = CuriousContraptions.Wire.ConstructionRecord.CreateInstancesVector(builder, [instance]);
        var settings = WorkshopCadenceSettings.Default();
        CuriousContraptions.Wire.ConstructionRecord.StartConstructionRecord(builder);
        CuriousContraptions.Wire.ConstructionRecord.AddRevision(builder, 1);
        CuriousContraptions.Wire.ConstructionRecord.AddSettings(builder, CuriousContraptions.Wire.CadenceSettingsRecord.CreateCadenceSettingsRecord(
            builder, (uint)settings.Simulation, (uint)settings.Physical,
            (uint)settings.Animation, (uint)settings.Presentation, settings.PresentationRate.Numerator, settings.PresentationRate.Denominator));
        CuriousContraptions.Wire.ConstructionRecord.AddInstances(builder, instances);
        return CuriousContraptions.Wire.ConstructionRecord.EndConstructionRecord(builder);
    }

    private static WorkshopConstruction DecodeRecord(PartId part, float? length, (string, float)? parameter = null)
    {
        var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
        builder.Finish(BuildRecord(builder, part, length, parameter).Value);
        return WorkshopWireFlatBuffers.DecodeConstructionRecord(CuriousContraptions.Wire.ConstructionRecord.GetRootAsConstructionRecord(
            new Google.FlatBuffers.ByteBuffer(builder.SizedByteArray())));
    }

    private static WorkshopConstruction DecodeSave(PartId part, float? length, (string, float)? parameter = null)
    {
        var builder = new Google.FlatBuffers.FlatBufferBuilder(1024);
        return WorkshopSaveCodec.Decode(SaveFixture.Elements,SaveFixture.Finish(builder, BuildRecord(builder, part, length, parameter), 2)).Construction;
    }

    // C-Plate and C-Clock: each setting is its generic instance's declared parameter (absent when the value is).
    private static (PartId Part, string Key) Declared(Part part) => part switch
    {
        Part.PressurePlate => (PartId.PressurePlate, "minimum_mass"),
        Part.Clock => (PartId.Clock, "interval_seconds"),
        Part.Counter => (PartId.Counter, "target_count"),
        _ => throw new ArgumentOutOfRangeException(nameof(part)),
    };

    private static WorkshopConstruction DecodeRecord(Part part, float? value) =>
        DecodeRecord(Declared(part).Part, null, parameter: value is { } setting ? (Declared(part).Key, setting) : null);

    private static WorkshopConstruction DecodeSave(Part part, float? value) =>
        DecodeSave(Declared(part).Part, null, parameter: value is { } setting ? (Declared(part).Key, setting) : null);

    private static float DecodedSetting(WorkshopConstruction construction, Part part)
    {
        var instance = Assert.Single(construction.Instances);
        Assert.Equal(Declared(part).Part, instance.PartId);
        return Assert.IsType<WorkshopGenericInstance>(instance).Parameters[new ParameterId(Declared(part).Key)];
    }

    /// <summary>The Construct command's record: the domain guard (ArgumentException) or the wire's typed fault, nothing else.</summary>
    private static void AssertRecordRejected(Func<object> decode, string label)
    {
        var error = Record.Exception(decode);
        Assert.True(error is not null, $"{label}: decoded instead of rejecting");
        Assert.True(error is ArgumentException or WireFaultException, $"{label}: rejected with {error!.GetType().Name} ({error.Message})");
    }

    [Theory]
    [MemberData(nameof(FloatBoundaries))]
    public void FlatBuffersRecordAcceptsExactlyTheAdmittedSetting(Part part, float value)
    {
        var label = $"FB {part} {value:R} (0x{BitConverter.SingleToInt32Bits(value):X8})";
        if (Admitted(part, value))
            Assert.Equal(BitConverter.SingleToInt32Bits(value), BitConverter.SingleToInt32Bits(DecodedSetting(DecodeRecord(part,value), part)));
        else AssertRecordRejected(() => DecodeRecord(part,value), label);
    }

    [Theory]
    [MemberData(nameof(FloatBoundaries))]
    public void SaveCodecAcceptsExactlyTheAdmittedSetting(Part part, float value)
    {
        if (Admitted(part, value))
            Assert.Equal(BitConverter.SingleToInt32Bits(value), BitConverter.SingleToInt32Bits(DecodedSetting(DecodeSave(part,value), part)));
        else
            Assert.Equal(float.IsFinite(value) ? WorkshopSaveRejection.OutOfRange : WorkshopSaveRejection.NonFinite,
                Assert.Throws<WorkshopSaveRejectedException>(() => DecodeSave(part,value)).Reason);
    }

    /// <summary>C-Memory: the counter's target is its declared parameter, never a typed record field (the target_count field and the
    /// former threshold are deleted): a counter record carrying a length is a generic instance with a typed field, refused OutOfRange at
    /// both boundaries even beside an admitted target (the same record without the length decodes: the control).</summary>
    [Fact]
    public void ACounterCarryingALengthIsRefusedBesideAnAdmittedTarget()
    {
        Assert.Equal(WireFaultReason.OutOfRange,
            Assert.Throws<WireFaultException>(() => DecodeRecord(PartId.Counter, 3f, ("target_count", 3f))).Reason);
        Assert.Equal(WorkshopSaveRejection.OutOfRange,
            Assert.Throws<WorkshopSaveRejectedException>(() => DecodeSave(PartId.Counter, 3f, ("target_count", 3f))).Reason);
        Assert.Equal(3f, DecodedSetting(DecodeRecord(PartId.Counter, null, ("target_count", 3f)), Part.Counter));
        Assert.Equal(3f, DecodedSetting(DecodeSave(PartId.Counter, null, ("target_count", 3f)), Part.Counter));
    }

    [Theory]
    [InlineData(Part.PressurePlate)]
    [InlineData(Part.Clock)]
    [InlineData(Part.Counter)]
    public void MissingSettingRejectsInEveryFlatBuffersPath(Part part)
    {
        AssertRecordRejected(() => DecodeRecord(part,null), $"FB {part} missing");
        Assert.Equal(WorkshopSaveRejection.OutOfRange, Assert.Throws<WorkshopSaveRejectedException>(() => DecodeSave(part,null)).Reason);
    }

    // Positive control for the fixture itself: the same builder with a canonical value decodes (so every rejection above is
    // caused by the value, not by an incomplete hand-built record).
    [Theory]
    [InlineData(Part.PressurePlate, .5f)]
    [InlineData(Part.Clock, 1f)]
    [InlineData(Part.Counter, 3f)]
    public void HandBuiltRecordIsAValidFixture(Part part, float canonical)
    {
        Assert.Equal(canonical, DecodedSetting(DecodeRecord(part,canonical), part));
        Assert.Equal(canonical, DecodedSetting(DecodeSave(part,canonical), part));
    }

    /// <summary>Every truncation of a save is its typed Truncated refusal: the size prefix makes a cut save unloadable, so no
    /// truncation decodes (not even one that only cut trailing padding).</summary>
    [Fact]
    public void EveryTruncatedSaveIsTheTruncatedRefusal()
    {
        var saved = new WorkshopSavedConstruction(
            new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
                new WorkshopInstances(PlacedParts.Plate(new(1), 0, 1, 0, .5f),
                    PlacedParts.Clock(new(2), 1, 1, 0, 0, 0, 0, 1, intervalSeconds: 2f))),
            new(3));
        var save = WorkshopSaveCodec.Encode(saved);
        Assert.Equal(2, WorkshopSaveCodec.Decode(SaveFixture.Elements,save).Construction.Instances.Count);
        for (var n = 0; n < save.Length; n++)
        {
            var length = n;
            Assert.Equal(WorkshopSaveRejection.Truncated,
                Assert.Throws<WorkshopSaveRejectedException>(() => WorkshopSaveCodec.Decode(SaveFixture.Elements,save.AsSpan(0, length))).Reason);
        }
    }
}
