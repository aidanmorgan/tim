using System.Security.Cryptography;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>C-Clock (9A.42, D1–D5): the System Clock is a generic instance whose node is selected by its package's declared oscillator.
/// The behaviour guards (period, pause, reset, supply, presentation, save/load) are <see cref="ClockGuardTests"/>, written on the record
/// path and unchanged; these facts cover what only the declared path has: the compiled node's exact identity, the declared parameter's
/// refusals at both decode boundaries, and the refusal of every record-path save.</summary>
public sealed class DeclaredOscillatorTests
{
    private static readonly GpuBodyId Source = new(1), Owner = new(2);

    private static WorkshopConstruction Bench(float seconds, bool supplied) => new(new(2), WorkshopCadenceSettings.Default(),
        new(PlacedParts.Battery(Source, -3, 3, 0), PlacedParts.Clock(Owner, 0, 3, 0, 0, 0, 0, 1, seconds)),
        Connections: supplied ? new(new WorkshopConnection(Source, WorkshopSocket.Supply, Owner, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical)) : new());

    // The declared capability selects the oscillator node with every field the record arm set: the owner's identity, the interval in
    // whole ticks, the PowerIn route's one upstream source (none when unsupplied) and the frame's first collider. Each value is stated.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheDeclaredOscillatorCompilesItsExactNode(bool supplied)
    {
        var node = Assert.Single(WorkshopActivationCompiler.Compile(Bench(2.5f, supplied)).Nodes.ToArray());
        Assert.Equal(new ActivationNodeDeclaration(new(2), Owner, ActivationNodeKind.Oscillator, default, 0, default, default, 0, 300,
            supplied ? Source : default, new GpuColliderId(WorkshopPhysicsCompiler.PartIdentities(Owner) + 1)), node);
    }

    // D2 (review B1): more than one upstream source on the clock's PowerIn is refused by the oscillator arm itself, with its own message.
    // Two batteries wired straight to the clock's PowerIn never reach that arm: connection validation refuses them first ("This power
    // input already has a supplier.", the case ClockGuardTests.TwoSourcesOnOneClockAreRefused exercises). Here both batteries supply a
    // latch's PowerIn, which admits two suppliers, and its switched Supply feeds the clock, so construction validation admits the bench
    // and only the arm refuses it. Control: one battery through the same latch compiles, that battery the clock's power source.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TwoUpstreamSourcesThroughALatchAreRefusedByTheOscillatorItself(bool twoBatteries)
    {
        GpuBodyId first = new(1), second = new(2), latch = new(3), clock = new(4);
        static WorkshopConnection Wire(GpuBodyId source, GpuBodyId target) =>
            new(source, WorkshopSocket.Supply, target, WorkshopSocket.PowerIn, WorkshopConnectionDomain.Electrical);
        var bench = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(PlacedParts.Battery(first, -6, 3, 0), PlacedParts.Battery(second, -6, 6, 0), PlacedParts.Latch(latch, -3, 3, 0, 0, 0, 0, 1),
                PlacedParts.Clock(clock, 0, 3, 0, 0, 0, 0, 1, 1f)),
            Connections: twoBatteries ? new(Wire(first, latch), Wire(second, latch), Wire(latch, clock)) : new(Wire(first, latch), Wire(latch, clock)));
        bench.Validate();
        if (twoBatteries)
        {
            var refusal = Assert.Throws<WorkshopEditRejectedException>(() => WorkshopActivationCompiler.Compile(bench));
            Assert.Equal((WorkshopEditRejection.PowerInputSupplied, "This source-accounted input requires one upstream source."),
                (refusal.Reason, refusal.Message));
        }
        else
            Assert.Equal(first, Assert.Single(WorkshopActivationCompiler.Compile(bench).Nodes.ToArray(), node => node.Owner == clock).PowerSource);
    }

    // Review L1: a typed length is detected by its bits, so a stored -0 length (forced past the builder's default elision) is a typed
    // field a generic record cannot carry; a stored +0 length is the absent value and the record loads (control).
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ANegativeZeroLengthIsATypedFieldOnAGenericRecord(bool negative)
    {
        var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
        var part = builder.CreateString("clock");
        var parameters = CuriousContraptions.Wire.InstanceRecord.CreateParametersVector(builder,
            [CuriousContraptions.Wire.ParameterRecord.CreateParameterRecord(builder, builder.CreateString("interval_seconds"), 1f)]);
        CuriousContraptions.Wire.InstanceRecord.StartInstanceRecord(builder);
        CuriousContraptions.Wire.InstanceRecord.AddParameters(builder, parameters);
        CuriousContraptions.Wire.InstanceRecord.AddId(builder, 1);
        CuriousContraptions.Wire.InstanceRecord.AddPartId(builder, part);
        CuriousContraptions.Wire.InstanceRecord.AddCell(builder, CuriousContraptions.Wire.Vec3i.CreateVec3i(builder, 0, 48, 0));
        CuriousContraptions.Wire.InstanceRecord.AddLocal(builder, CuriousContraptions.Wire.Vec3f.CreateVec3f(builder, 0, 0, 0));
        CuriousContraptions.Wire.InstanceRecord.AddRotation(builder, CuriousContraptions.Wire.Quat4f.CreateQuat4f(builder, 0, 0, 0, 1));
        builder.ForceDefaults = true;
        CuriousContraptions.Wire.InstanceRecord.AddLength(builder, negative ? -0f : 0f);
        builder.ForceDefaults = false;
        var instance = CuriousContraptions.Wire.InstanceRecord.EndInstanceRecord(builder);
        var settings = WorkshopCadenceSettings.Default();
        var instances = CuriousContraptions.Wire.ConstructionRecord.CreateInstancesVector(builder, [instance]);
        CuriousContraptions.Wire.ConstructionRecord.StartConstructionRecord(builder);
        CuriousContraptions.Wire.ConstructionRecord.AddRevision(builder, 1);
        CuriousContraptions.Wire.ConstructionRecord.AddSettings(builder, CuriousContraptions.Wire.CadenceSettingsRecord.CreateCadenceSettingsRecord(builder,
            (uint)settings.Simulation, (uint)settings.Physical, (uint)settings.Animation, (uint)settings.Presentation,
            settings.PresentationRate.Numerator, settings.PresentationRate.Denominator));
        CuriousContraptions.Wire.ConstructionRecord.AddInstances(builder, instances);
        var bytes = SaveFixture.Finish(builder, CuriousContraptions.Wire.ConstructionRecord.EndConstructionRecord(builder), 2);
        var record = WorkshopSaveCodec.Open(bytes).Construction!.Value.Instances(0)!.Value;
        Assert.Equal(negative ? 0x80000000u : 0u, BitConverter.SingleToUInt32Bits(record.Length));
        if (negative)
        {
            SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes));
            Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.MapInstance(record)).Reason);
        }
        else Assert.Equal(PartId.Clock, Assert.Single(WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes).Construction.Instances).PartId);
    }

    private static byte[] Save(IReadOnlyList<(string Key, float Value)> parameters) => PayloadInstanceTests.GenericSave("clock", null, parameters);

    // C19 at the save and command boundaries: interval_seconds outside [0.1, 12] s (each one-ulp neighbour included), zero, negative,
    // NaN or infinite is refused, NonFinite at the save boundary for a non-finite value and OutOfRange otherwise; the command decoder
    // refuses each OutOfRange. Controls: both ends load bit for bit.
    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(.01f)]
    [InlineData(13f)]
    [InlineData(.099999994f)]
    [InlineData(12.000001f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void AnIntervalOutsideItsRangeIsRefusedAtBothBoundaries(float seconds)
    {
        Assert.True(seconds is not (.1f or 12f));
        var bytes = Save([("interval_seconds", seconds)]);
        SaveFixture.Refused(float.IsFinite(seconds) ? WorkshopSaveRejection.OutOfRange : WorkshopSaveRejection.NonFinite,
            () => WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes));
        var record = WorkshopSaveCodec.Open(bytes).Construction!.Value.Instances(0)!.Value;
        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.MapInstance(record)).Reason);
        foreach (var admitted in new[] { .1f, 12f })
        {
            var loaded = Assert.IsType<WorkshopGenericInstance>(Assert.Single(WorkshopSaveCodec.Decode(SaveFixture.Elements,
                Save([("interval_seconds", admitted)])).Construction.Instances));
            Assert.Equal(BitConverter.SingleToUInt32Bits(admitted), BitConverter.SingleToUInt32Bits(loaded.Parameters[new("interval_seconds")]));
        }
    }

    // An unknown parameter in place of, or beside, the declared interval, and a missing interval, are refused at both boundaries.
    [Theory]
    [InlineData("unknown")]
    [InlineData("extra")]
    [InlineData("missing")]
    public void AnUndeclaredOrMissingParameterIsRefusedAtBothBoundaries(string shape)
    {
        (string, float)[] parameters = shape switch
        {
            "unknown" => [("interval", 1f)],
            "extra" => [("interval_seconds", 1f), ("threshold", 1f)],
            "missing" => [],
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };
        var bytes = Save(parameters);
        SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes));
        var record = WorkshopSaveCodec.Open(bytes).Construction!.Value.Instances(0)!.Value;
        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.MapInstance(record)).Reason);
    }

    // ---------- D5: saves written by the record path's encoders at 14658883 are refused, never misread ----------

    // The bytes are 14658883's own production save and command encoder output (evidence-c-clock/baseline/old-saves, sha256 in
    // old-saves.sha256), each of which loaded there. The record clock wrote its interval in the threshold slot (7), now the typed
    // target_count, so a lone clock is refused as a typed field on a generic instance. In the cat-017 bench the counter's target count
    // (slot 8) is now read as the parameters offset and the verifier refuses the buffer; a lone counter's target 3 is likewise refused
    // by the verifier, while targets 4 and 8 pass it as an offset but leave the counter no target count of its own. Every generic record of that layout wrote its parameters in slot 9, beyond
    // the 9-field table (a bare Basketball's empty vector included). Each refusal is typed at both boundaries; none is migrated.
    // C-Memory's 8-field table (no target_count): a clock's threshold slot 7 is now the parameters offset, so the verifier refuses every
    // clock save; a lone counter's target (old slot 8) is beyond the table and the generic counter it decodes as has no declared target
    // (OutOfRange, before the beyond-the-table check); the cat-017 bench and the bare Basketball keep their refusals.
    private const string Clock01Save = "xAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAALAAAAAAAAAAAAAAAMACgAIAAIAAAABAAMAAAAJAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAQAAABgAAAAUAEAANAAwAAAAJAAYAAgAAAAEABQAAADNzMw9AAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAAAAwAAAAAAAAABAAAAABAAAAAAAAAAAAAAAFAAAAY2xvY2sAAAA=";
    private const string Clock01Command = "FAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAAMQAAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAoACAACAAAAAQADAAAACQAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAEAAAAYAAAAFAA8ADQAMAAAACQAGAAIAAAABAAUAAAAzczMPQAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAAAAAAAAMAAAAAAAAAAMAAAAAQAAAAAAAAAFAAAAY2xvY2sAAAAQAAAAAQAAAAAAAAACAAAAAAAAAA==";
    private const string Clock12Save = "xAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAALAAAAAAAAAAAAAAAMACgAIAAIAAAABAAMAAAAJAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAQAAABgAAAAUAEAANAAwAAAAJAAYAAgAAAAEABQAAAAAAEBBAAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAAAAwAAAAAAAAABAAAAABAAAAAAAAAAAAAAAFAAAAY2xvY2sAAAA=";
    private const string Clock12Command = "FAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAAMQAAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAoACAACAAAAAQADAAAACQAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAEAAAAYAAAAFAA8ADQAMAAAACQAGAAIAAAABAAUAAAAAABAQQAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAAAAAAAAMAAAAAAAAAAMAAAAAQAAAAAAAAAFAAAAY2xvY2sAAAAQAAAAAQAAAAAAAAACAAAAAAAAAA==";
    private const string LockedRotatedSave = "zAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAALAAAAAAAAAAAAAAAMACwAIAAIAAAABAAMAAAAKAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAAAAAAEAAAAYAAAAFABEADgANAAzACQAGAAIAAAABAAUAAAAAAAgQAAAAAD0BDU/AAAAAPQENT8AAAAAAAAAAAAAAAAoAAAAMAAAAOz///8AAAABEAAAAAEAAAAAAAAAAAAAAAUAAABjbG9jawAAAA==";
    private const string LockedRotatedCommand = "HAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAAMwAAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAsACAACAAAAAQADAAAACgAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAAAAAABAAAAGAAAABQAQAA4ADQAMwAkABgACAAAAAQAFAAAAAAAIEAAAAAA9AQ1PwAAAAD0BDU/AAAAAAAAAAAAAAAAKAAAADAAAADs////AAAAAQwAAAABAAAAAAAAAAUAAABjbG9jawAAABAAAAABAAAAAAAAAAIAAAAAAAAA";
    private const string CounterGateSave = "dAMAABQAAABDQ1dTAAAAAAgAFgAIAAQACAAAACAAAAALAAAAAAAAAAAAAAAAAA4AMAAkAAwAAAAIAAQADgAAACwAAACsAAAAAgAAAAEAAAACAAAAAgAAADwAAAABAAAAAgAAAAAAAAAAAAAABAAAAAUAAAAAAAAAAwAAAAAAAAABAAAAAQAAAAQAAAAAAAAAAwAAAAAAAAABAAAAAAAAAAIAAAAHAAAACAAAAAAAAAAEAAAAAAAAAAMAAAAAAAAAAgAAAAcAAAAIAAAAAAAAAAQAAAAAAAAABQAAAAAAAAACAAAABwAAAAgAAAAAAAAABQAAACwCAADAAQAAcAEAAIAAAAAYAAAAFABAADQAMAAAACQAGAAIAAAABAAUAAAAAACAPwAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAACw////MAAAAAAAAAAQAAAABQAAAAAAAAAAAAAABQAAAGNsb2NrAAAAGAA8ADAALAAAACAAFAAEAAAAAAAAADgAGAAAAAAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAABwAAAAMAAAAAAAAACcAAAABAAAAAAAAAAEAAAABAAAAGwAAABIAAAAJAAAAAQAAACs////CAAAAAAA8EILAAAAcG93ZXJfbGltaXQAyP///wgAAAAAAIA/DgAAAGluaXRpYWxfY2hhcmdlAADo////CAAAAAAAgD8HAAAAZW5hYmxlZAAIAAwABAAIAAgAAAAIAAAAAABhRQgAAABjYXBhY2l0eQAAAAAHAAAAYmF0dGVyeQAAABYAQAA0ADAAAAAkABgACAAAAAAABAAWAAAAAwAAAAAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAABAAAAAMAAAAAAAAAAQAAAAAwAAAAAAAAAAAAAABwAAAGNvdW50ZXIAsP///wAAAAAAAAAAAAAAAAAAgD/NzMw+zczMvgAAAAD2////OgAAAAAAAAAYAAAAAgAAAAAAAAAAAAAABAAAAAAAAAAEAAAAYmFsbAAAAAAYAEAAMAAsAAAAIAAUAAQAAAAAAAAAPAAYAAAAAAAAAAAAAACoqAW+Vc99PwAAAAAAAAAAAAAAAAAAAAAwAAAAAAAAABgAAAABAAAAAAAAAAAAAAAEAAAAAAAAAAwAAABwb3dlcmVkX2dhdGUAAAAA";
    private const string CounterGateCommand = "xAMAACQAAABDQ1dDHABGADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABQAAAAAQAAAAAAAAABAAAAAAAAAHQDAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAAAAAOADAAJAAMAAAACAAEAA4AAAAsAAAArAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAAAAAAQAAAAFAAAAAAAAAAMAAAAAAAAAAQAAAAEAAAAEAAAAAAAAAAMAAAAAAAAAAQAAAAAAAAACAAAABwAAAAgAAAAAAAAABAAAAAAAAAADAAAAAAAAAAIAAAAHAAAACAAAAAAAAAAEAAAAAAAAAAUAAAAAAAAAAgAAAAcAAAAIAAAAAAAAAAUAAAAsAgAAwAEAAFgBAABoAAAAGAAAABQAQAA0ADAAAAAkABgACAAAAAQAFAAAAAAAgD8AAAAAAAAAAAAAAAAAAIA/AAAAAAAAAAAAAAAAsP///zAAAAAAAAAAEAAAAAUAAAAAAAAAAAAAAAUAAABjbG9jawAAAGD+//8AAAAAAAAAAAAAAAAAAIA/AAAAAAAAAAAAAAAAcAAAADAAAAAAAAAAnAAAAAQAAAAAAAAABAAAAAQAAABsAAAASAAAACQAAAAEAAAArP///wgAAAAAAPBCCwAAAHBvd2VyX2xpbWl0AMj///8IAAAAAACAPw4AAABpbml0aWFsX2NoYXJnZQAA6P///wgAAAAAAIA/BwAAAGVuYWJsZWQACAAMAAQACAAIAAAACAAAAAAAYUUIAAAAY2FwYWNpdHkAAAAABwAAAGJhdHRlcnkAAAAWAEAANAAwAAAAJAAYAAgAAAAAAAQAFgAAAAMAAAAAAAAAAAAAAAAAAAAAAIA/AAAAAAAAAAAAAAAAQAAAADAAAAAAAAAAEAAAAAMAAAAAAAAAAAAAAAcAAABjb3VudGVyABgAQAAwACwAAAAgABQABAAAAAAAAAA8ABgAAAAAAAAAAAAAAAAAAAAAAIA/zczMPs3MzL4AAAAA9v///zoAAAAAAAAAGAAAAAIAAAAAAAAAAAAAAAQAAAAAAAAABAAAAGJhbGwAAAAAGAA8ADAALAAAACAAFAAEAAAAAAAAADgAGAAAAAAAAAAAAAAAqKgFvlXPfT8AAAAAAAAAAAAAAAAAAAAAMAAAAAAAAAAUAAAAAQAAAAAAAAAEAAAAAAAAAAwAAABwb3dlcmVkX2dhdGUAAAAAEAAAAAEAAAAAAAAAAgAAAAAAAAA=";
    private const string Counter4Save = "zAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAALAAAAAAAAAAAAAAAMACwAIAAIAAAABAAMAAAAKAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAAAAAAEAAAAcAAAAAAAWAEAANAAwAAAAJAAYAAgAAAAAAAQAFgAAAAQAAAAAAAAAAAAAAAAAAAAAAIA/AAAAAAAAAAAAAAAAAAAAADAAAAAAAAAAEAAAAAEAAAAAAAAAAAAAAAcAAABjb3VudGVyAA==";
    private const string Counter4Command = "HAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAAMwAAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAsACAACAAAAAQADAAAACgAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAAAAAABAAAAHAAAAAAAFgA8ADQAMAAAACQAGAAIAAAAAAAEABYAAAAEAAAAAAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAAAAwAAAAAAAAAAwAAAABAAAAAAAAAAcAAABjb3VudGVyABAAAAABAAAAAAAAAAIAAAAAAAAA";
    private const string Counter3Save = "zAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAALAAAAAAAAAAAAAAAMACwAIAAIAAAABAAMAAAAKAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAAAAAAEAAAAcAAAAAAAWAEAANAAwAAAAJAAYAAgAAAAAAAQAFgAAAAMAAAAAAAAAAAAAAAAAAAAAAIA/AAAAAAAAAAAAAAAAAAAAADAAAAAAAAAAEAAAAAEAAAAAAAAAAAAAAAcAAABjb3VudGVyAA==";
    private const string Counter3Command = "HAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAAMwAAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAsACAACAAAAAQADAAAACgAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAAAAAABAAAAHAAAAAAAFgA8ADQAMAAAACQAGAAIAAAAAAAEABYAAAADAAAAAAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAAAAwAAAAAAAAAAwAAAABAAAAAAAAAAcAAABjb3VudGVyABAAAAABAAAAAAAAAAIAAAAAAAAA";
    private const string Counter8Save = "zAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAALAAAAAAAAAAAAAAAMACwAIAAIAAAABAAMAAAAKAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAAAAAAEAAAAcAAAAAAAWAEAANAAwAAAAJAAYAAgAAAAAAAQAFgAAAAgAAAAAAAAAAAAAAAAAAAAAAIA/AAAAAAAAAAAAAAAAAAAAADAAAAAAAAAAEAAAAAEAAAAAAAAAAAAAAAcAAABjb3VudGVyAA==";
    private const string Counter8Command = "HAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAAMwAAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAsACAACAAAAAQADAAAACgAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAAAAAABAAAAHAAAAAAAFgA8ADQAMAAAACQAGAAIAAAAAAAEABYAAAAIAAAAAAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAAAAwAAAAAAAAAAwAAAABAAAAAAAAAAcAAABjb3VudGVyABAAAAABAAAAAAAAAAIAAAAAAAAA";
    private const string BallSave ="zAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAALAAAAAAAAAAAAAAAMACgAIAAIAAAABAAMAAAAJAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAQAAABwAAAAYAEAAMAAsAAAAIAAUAAQAAAAAAAAAPAAYAAAAAAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAAAAwAAAAAAAAABgAAAABAAAAAAAAAAAAAAAEAAAAAAAAAAQAAABiYWxsAAAAAA==";
    private const string BallCommand = "HAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAAMwAAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAoACAACAAAAAQADAAAACQAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAEAAAAcAAAAGAA8ADAALAAAACAAFAAEAAAAAAAAADgAGAAAAAAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAAAAAAAAMAAAAAAAAAAUAAAAAQAAAAAAAAAEAAAAAAAAAAQAAABiYWxsAAAAABAAAAABAAAAAAAAAAIAAAAAAAAA";

    private const string VerifierSave = "This save cannot be loaded: The FlatBuffers verifier refused a CCWS buffer.";
    private const string VerifierCommand = "The FlatBuffers verifier refused a CCWC buffer.";
    private const string TargetSave = "This save cannot be loaded: Invalid declared parameters.";
    private const string TargetCommand = "Invalid declared parameters.";

    [Theory]
    [InlineData("clock_0_1", "f4a493b3f39b89b7d352351fe35196fb2d8c4e46f702faeb91e47f60965e25ab", "18b9590666b4e313ac344a33cbc8254b204e5ca59342877f67065a19e872ca23",
        WorkshopSaveRejection.VerifierRejected, VerifierSave, WireFaultReason.VerifierRejected, VerifierCommand)]
    [InlineData("clock_12", "12856819067e26c572257f4a7275eca6f9cd1d14f3e208d3c03c81f6c67d3aea", "af10719172fcd7c6093c85c2b6e37f252b18fec2d78d177c04d64a9e3a2412be",
        WorkshopSaveRejection.VerifierRejected, VerifierSave, WireFaultReason.VerifierRejected, VerifierCommand)]
    [InlineData("clock_locked_rotated", "7bb2f9594727e48b3ba1311426f9f28e40dc073fc5135ea5f9ee42d6aabeb18f", "e8342cef097e3697db486e9b2d7d6b3c37f64c395f792888a98ddfc7e4e64b73",
        WorkshopSaveRejection.VerifierRejected, VerifierSave, WireFaultReason.VerifierRejected, VerifierCommand)]
    [InlineData("bench_counter_gate", "630b8bb7cc81c7c4440f8a1d5fa957b244d8af0fe5ceb29393777e345f64b5d7", "3fb5d595fd7512ca9b4581b8b69bea6a32268670c06718c8369f5e62b4be86f1",
        WorkshopSaveRejection.VerifierRejected, VerifierSave, WireFaultReason.VerifierRejected, VerifierCommand)]
    [InlineData("counter_4", "30c31d97d4a74f46b892b5779087340c9e592e5b0d32b8c388e5071c67c72567", "86d189336724a5add8c234c923ed7237f25ee6e5ac9862ab3c1aa3814add3565",
        WorkshopSaveRejection.OutOfRange, TargetSave, WireFaultReason.OutOfRange, TargetCommand)]
    [InlineData("counter_3", "45882e833efb5593a8e096ce357a15fce05035ccb9f225f302682104c1b854c9", "278a2ce8210b739eab41c4e0d2221fc1d8c118d4e2e90af4ba8b1c1773864d20",
        WorkshopSaveRejection.OutOfRange, TargetSave, WireFaultReason.OutOfRange, TargetCommand)]
    [InlineData("counter_8", "51bbb19ef3ee46e76a38b879698d6a24c2228d644630ea508be95936d4d67d74", "63a28e4c28d67dbb920a15476294f97413f0138fcf358ac66fce5e02a4c6367d",
        WorkshopSaveRejection.OutOfRange, TargetSave, WireFaultReason.OutOfRange, TargetCommand)]
    [InlineData("generic_ball", "8d819cef4f1f3ab907a728e0b8874e07e3df7d3ab2c37e35c4c56d578ae8c90c", "56c55e27701c6ed85e7d288e39a55f9710ca6f50f256534b894fef0bb50e897c",
        WorkshopSaveRejection.OutOfRange, "This save cannot be loaded: an instance record holds a field the current record does not define.",
        WireFaultReason.OutOfRange, "An instance record holds a field the current record does not define.")]
    public void ARecordPathSaveIsRefusedTypedAtBothBoundaries(string name, string saveSha256, string commandSha256,
        WorkshopSaveRejection saveReason, string saveMessage, WireFaultReason commandReason, string commandMessage)
    {
        var (save, command) = name switch
        {
            "clock_0_1" => (Clock01Save, Clock01Command),
            "clock_12" => (Clock12Save, Clock12Command),
            "clock_locked_rotated" => (LockedRotatedSave, LockedRotatedCommand),
            "bench_counter_gate" => (CounterGateSave, CounterGateCommand),
            "counter_4" => (Counter4Save, Counter4Command),
            "counter_3" => (Counter3Save, Counter3Command),
            "counter_8" => (Counter8Save, Counter8Command),
            "generic_ball" => (BallSave, BallCommand),
            _ => throw new ArgumentOutOfRangeException(nameof(name))
        };
        var (saveBytes, commandBytes) = (Convert.FromBase64String(save), Convert.FromBase64String(command));
        Assert.Equal((saveSha256, commandSha256), (Convert.ToHexStringLower(SHA256.HashData(saveBytes)), Convert.ToHexStringLower(SHA256.HashData(commandBytes))));
        Assert.Equal(saveMessage, SaveFixture.Refused(saveReason, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, saveBytes)).Message);
        var fault = Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(commandBytes));
        Assert.Equal((commandReason, commandMessage), (fault.Reason, fault.Message));
    }

    // Breadth over every admitted counter target: the 14658883 layout (10 fields: 6 length, 7 threshold, 8 target_count, 9 parameters)
    // wrote the target in slot 8, now the parameters offset. Which typed refusal each value gets depends on whether the verifier admits
    // that offset (the pinned base-encoder saves above show both), but every target is refused typed at both boundaries and none loads.
    [Theory]
    [InlineData(1u)]
    [InlineData(2u)]
    [InlineData(3u)]
    [InlineData(4u)]
    [InlineData(5u)]
    [InlineData(6u)]
    [InlineData(7u)]
    [InlineData(8u)]
    [InlineData(9u)]
    public void EveryRecordPathCounterTargetIsRefusedTypedAtBothBoundaries(uint target)
    {
        var layout = new OldLayoutSaves.Layout(10, 9, 48, 11);
        OldLayoutSaves.Slot[] slots = [new(8, Unsigned: target)];
        Assert.Throws<WorkshopSaveRejectedException>(() => WorkshopSaveCodec.Decode(SaveFixture.Elements, OldLayoutSaves.Save(layout, "counter", slots)));
        Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(OldLayoutSaves.Command(layout, "counter", slots)));
    }
}
