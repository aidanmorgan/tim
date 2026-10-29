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
            supplied ? Source : default, Bench(2.5f, supplied).Instances.Identities[Owner].Colliders[0]), node);
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

    private static byte[] Save(IReadOnlyList<(string Key, float Value)> parameters) => PayloadInstanceTests.GenericSave("clock", parameters);

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
        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopAuthoringFlatBuffers.MapInstance(record)).Reason);
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
        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopAuthoringFlatBuffers.MapInstance(record)).Reason);
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

    [Theory]
    [InlineData("clock_0_1")]
    [InlineData("clock_12")]
    [InlineData("clock_locked_rotated")]
    [InlineData("bench_counter_gate")]
    [InlineData("counter_4")]
    [InlineData("counter_3")]
    [InlineData("counter_8")]
    [InlineData("generic_ball")]
    public void ARecordPathSaveIsRefusedTypedAtBothBoundaries(string name)
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
        Assert.False(string.IsNullOrWhiteSpace(SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, saveBytes)).Message));
        var fault = Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(commandBytes));
        Assert.Equal(WireFaultReason.OutOfRange, fault.Reason);
        Assert.False(string.IsNullOrWhiteSpace(fault.Message));
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
