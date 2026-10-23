// Story 9.4b D4 regression, carried onto the one codec by Story 9A.2: the construction decode boundary for every declared
// Pressure Plate / Counter / Clock setting.
//
// Every declared setting is decoded through both shipped construction decoders:
//   FB  WorkshopWireFlatBuffers.DecodeConstructionRecord (the Construct command record);
//   SAV WorkshopSaveCodec.Decode (the browser save boundary: structural pre-validation, then the same record decoder).
// The Counter's target is the whole uint32 field target_count (Story 9A.2); the clock's interval is the f32 threshold and the plate's
// minimum mass is its generic instance's declared f32 minimum_mass parameter (C-Plate). Expected (independently derived from the
// plate's declared [0.1, 16] kg (decision h), WorkshopCounter [1, 9] and
// WorkshopClock [0.1, 12] s, and "a missing declared setting rejects; nothing is defaulted or coerced"): accept exactly the
// admitted values and decode them bit-for-bit; reject everything else. The command record rejects with the domain's argument
// guard or the wire's typed fault; the save rejects with its typed refusal and nothing else. Positive controls use the identical
// hand-built record, so a rejection cannot come from a malformed fixture.
using System;
using CuriousContraptions.Gpu;
using Xunit;

namespace CuriousContraptions.Tests;

public sealed class ConstructionDecodeBoundaryTests
{
    public enum Part { PressurePlate, Clock }

    // 2^31 and 2^31 - 1 are the same f32 (2147483648f); 2147483520f is the largest f32 below 2^31.
    private const float TwoTo31 = 2147483648f;
    private const float BelowTwoTo31 = 2147483520f;

    private static bool Admitted(Part part, float value) => part switch
    {
        Part.PressurePlate => float.IsFinite(value) && value >= .1f && value <= 16f,
        Part.Clock => float.IsFinite(value) && value >= .1f && value <= 12f,
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
            var (min, max) = part switch { Part.PressurePlate => (.1f, 16f), _ => (.1f, 12f) };
            data.Add(part, min); data.Add(part, max);
            data.Add(part, MathF.BitDecrement(min)); data.Add(part, MathF.BitIncrement(max));
        }
        return data;
    }

    // ---------- the hand-built record ----------

    private static Google.FlatBuffers.Offset<CuriousContraptions.Wire.ConstructionRecord> BuildRecord(
        Google.FlatBuffers.FlatBufferBuilder builder, PartId part, float? threshold, uint? targetCount, float? minimumMass = null)
    {
        var id = builder.CreateString(part.Value);
        Google.FlatBuffers.VectorOffset? parameters = minimumMass is { } mass
            ? CuriousContraptions.Wire.InstanceRecord.CreateParametersVector(builder,
                [CuriousContraptions.Wire.ParameterRecord.CreateParameterRecord(builder, builder.CreateString("minimum_mass"), mass)])
            : null;
        CuriousContraptions.Wire.InstanceRecord.StartInstanceRecord(builder);
        if (parameters is { } vector) CuriousContraptions.Wire.InstanceRecord.AddParameters(builder, vector);
        CuriousContraptions.Wire.InstanceRecord.AddId(builder, 1);
        CuriousContraptions.Wire.InstanceRecord.AddPartId(builder, id);
        CuriousContraptions.Wire.InstanceRecord.AddCell(builder, CuriousContraptions.Wire.Vec3i.CreateVec3i(builder, 0, 16, 0));
        CuriousContraptions.Wire.InstanceRecord.AddLocal(builder, CuriousContraptions.Wire.Vec3f.CreateVec3f(builder, 0, 0, 0));
        CuriousContraptions.Wire.InstanceRecord.AddRotation(builder, CuriousContraptions.Wire.Quat4f.CreateQuat4f(builder, 0, 0, 0, 1));
        if (threshold is { } value) CuriousContraptions.Wire.InstanceRecord.AddThreshold(builder, value);
        if (targetCount is { } count) CuriousContraptions.Wire.InstanceRecord.AddTargetCount(builder, count);
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

    private static WorkshopConstruction DecodeRecord(PartId part, float? threshold, uint? targetCount = null, float? minimumMass = null)
    {
        var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
        builder.Finish(BuildRecord(builder, part, threshold, targetCount, minimumMass).Value);
        return WorkshopWireFlatBuffers.DecodeConstructionRecord(CuriousContraptions.Wire.ConstructionRecord.GetRootAsConstructionRecord(
            new Google.FlatBuffers.ByteBuffer(builder.SizedByteArray())));
    }

    private static WorkshopConstruction DecodeSave(PartId part, float? threshold, uint? targetCount = null, float? minimumMass = null)
    {
        var builder = new Google.FlatBuffers.FlatBufferBuilder(1024);
        return WorkshopSaveCodec.Decode(SaveFixture.Elements,SaveFixture.Finish(builder, BuildRecord(builder, part, threshold, targetCount, minimumMass), 2)).Construction;
    }

    // C-Plate: the plate's setting is its generic instance's declared minimum_mass parameter; the clock's is the record threshold.
    private static WorkshopConstruction DecodeRecord(Part part, float? value) => part switch
    {
        Part.PressurePlate => DecodeRecord(PartId.PressurePlate, null, minimumMass: value),
        Part.Clock => DecodeRecord(PartId.Clock, value),
        _ => throw new ArgumentOutOfRangeException(nameof(part)),
    };

    private static WorkshopConstruction DecodeSave(Part part, float? value) => part switch
    {
        Part.PressurePlate => DecodeSave(PartId.PressurePlate, null, minimumMass: value),
        Part.Clock => DecodeSave(PartId.Clock, value),
        _ => throw new ArgumentOutOfRangeException(nameof(part)),
    };

    private static float DecodedSetting(WorkshopConstruction construction, Part part)
    {
        var instance = Assert.Single(construction.Instances);
        return part switch
        {
            Part.PressurePlate => Assert.IsType<WorkshopGenericInstance>(instance).Parameters[new ParameterId("minimum_mass")],
            Part.Clock => Assert.IsType<WorkshopClock>(instance).IntervalSeconds,
            _ => throw new ArgumentOutOfRangeException(nameof(part)),
        };
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

    [Theory]
    [InlineData(0u)]
    [InlineData(1u)]
    [InlineData(9u)]
    [InlineData(10u)]
    [InlineData((uint)int.MaxValue)]
    [InlineData((uint)int.MaxValue + 1)]
    [InlineData(uint.MaxValue)]
    public void CounterTargetIsAWholeFieldAcceptedExactlyInItsRange(uint count)
    {
        if (count is >= 1 and <= 9)
        {
            Assert.Equal((int)count, Assert.IsType<WorkshopCounter>(Assert.Single(DecodeRecord(PartId.Counter, null, count).Instances)).TargetCount);
            Assert.Equal((int)count, Assert.IsType<WorkshopCounter>(Assert.Single(DecodeSave(PartId.Counter, null, count).Instances)).TargetCount);
            return;
        }
        AssertRecordRejected(() => DecodeRecord(PartId.Counter, null, count), $"FB counter {count}");
        Assert.Equal(count == 0 ? WorkshopSaveRejection.MissingField : WorkshopSaveRejection.OutOfRange,
            Assert.Throws<WorkshopSaveRejectedException>(() => DecodeSave(PartId.Counter, null, count)).Reason);
    }

    /// <summary>The counter's target is the whole field, never the former f32 threshold: a threshold alone is a missing target.</summary>
    [Fact]
    public void ACounterThresholdIsNotItsTarget()
    {
        Assert.Equal(WireFaultReason.MissingField,
            Assert.Throws<WireFaultException>(() => DecodeRecord(PartId.Counter, 3f)).Reason);
        Assert.Equal(WorkshopSaveRejection.MissingField, Assert.Throws<WorkshopSaveRejectedException>(() => DecodeSave(PartId.Counter, 3f)).Reason);
    }

    [Theory]
    [InlineData(Part.PressurePlate)]
    [InlineData(Part.Clock)]
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
                    WorkshopInput.Clock(new(2), 1, 1, 0, 0, 0, 0, 1, intervalSeconds: 2f))),
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
