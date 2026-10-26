using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>C-Bumper+Spring removed six InstanceRecord fields (stiffness, damping, initial_compression, strength, reference_mass,
/// preload), renumbering battery, half_extents, friction, target_count and parameters. A pre-conversion record whose meaning moved is
/// refused with a typed reason at both boundaries, the stored save's pre-validation and the worker's command decode, and never misread.
/// There is no migration. The old bytes are the record path's own encoder output (fixed input provenance in Fixtures/README-pre-conversion.md; receipt
/// evidence-c-bumper-spring/baseline/old-layout-equality.txt).</summary>
public sealed class PreConversionLaunchSaveTests
{
    private enum FixtureFamily { PreLaunch, PreSourceStore }
    private enum FixtureBoundary { Save, Command }

    private static byte[] Fixture(FixtureFamily family, string name, FixtureBoundary boundary)
    {
        // Closed test domains become strings only at the resource-path boundary.
        var directory = family switch
        {
            FixtureFamily.PreLaunch => "pre-launch",
            FixtureFamily.PreSourceStore => "pre-source-store",
            _ => throw new ArgumentOutOfRangeException(nameof(family))
        };
        var extension = boundary switch
        {
            FixtureBoundary.Save => ".save.base64",
            FixtureBoundary.Command => ".command.base64",
            _ => throw new ArgumentOutOfRangeException(nameof(boundary))
        };
        return Convert.FromBase64String(File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(),
            "CuriousContraptions.tests/Fixtures", directory, name.Replace(' ', '_') + extension)));
    }

    // Re-derived on the 14-field table of Story 9A.39 (the C-Bumper+Spring-era reasons are in that slice's receipts). Old Bumper at
    // strength 8: its strength, reference mass and preload lie beyond the table, so it decodes as a parameterless generic Bumper whose
    // missing declared strength is refused (at C-Bumper+Spring the strength sat in the parameters slot and the verifier refused it). At
    // strength 0 likewise. Old Springboard: its stiffness and damping land in half_extents and friction, typed fields a generic record
    // cannot carry. Old Battery: its capacity float sat in the 14-field table's parameters slot (the verifier refused the buffer); on the
    // 10-field table of Story 9A.32 its four fields lie beyond the table, so it decodes as a parameterless generic Battery whose missing
    // declared parameters are refused. Old Domino: it is a generic tile since 9A.32, and its mass and bounce (slots 6 and 7) land in
    // length and threshold (target_count since C-Clock's 9-field table), typed fields a generic record cannot carry. Old Counter: its
    // target count lies beyond the table. Old Switch and resized Ramp: their parameters lie beyond the table. (C-Clock re-ran these
    // rows on the 9-field table: every reason is unchanged.) C-Memory's 8-field table (no target_count; slot 7 is the parameters
    // offset): the old Domino's bounce float in slot 7 now reads as that offset and the verifier refuses the buffer; the old Counter is
    // a generic counter (its target still beyond the table) whose missing declared target is refused OutOfRange. Every other reason is
    // unchanged.
    [Theory]
    [InlineData("bumper strength 8", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("bumper strength 0", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("spring default", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("spring charged", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("battery default", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("domino", WorkshopSaveRejection.VerifierRejected, WireFaultReason.VerifierRejected)]
    [InlineData("counter", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("switch threshold", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("resized ramp", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    public void APreConversionRecordIsRefusedWithATypedReasonAtBothBoundaries(string name, WorkshopSaveRejection save,
        WireFaultReason command)
    {
        var bytes = Fixture(FixtureFamily.PreLaunch, name, FixtureBoundary.Save);
        SaveFixture.Refused(save, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes));
        Assert.Equal(command, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(Fixture(FixtureFamily.PreLaunch, name, FixtureBoundary.Command))).Reason);
    }

    // Story 9A.39: f27cb71d's records whose meaning moved when the battery's four typed fields left the table. The bytes are that
    // commit's own production encoder output (provenance: evidence-9a-39/baseline/capture/old-saves.sha256). There is no
    // migration: each is refused with a typed reason at both boundaries, never misread. Old Battery: its capacity, maximum power and
    // initial fraction land in half_extents, friction and target_count; where its enable byte (slot 13) is written it reads as the
    // parameters offset and the verifier refuses the buffer, otherwise the typed fields are refused. Re-derived on the 10-field table of
    // Story 9A.32: the Battery's four fields (slots 10–13) lie beyond the table, so every old Battery decodes as a parameterless generic
    // Battery whose missing declared parameters are refused. Old Domino: a generic tile since 9A.32, its mass and bounce (slots 6 and 7)
    // land in length and threshold (target_count since C-Clock), typed fields a generic record cannot carry. Old Counter: its target
    // count reads beyond its old slot, so it is missing. Every parameterised generic: its parameters lie beyond the table, so its
    // declared parameters are missing. (C-Clock re-ran these rows on the 9-field table: every reason is unchanged.)
    // C-Memory's 8-field table: the old Domino's bounce in slot 7 is now the parameters offset (VerifierRejected), and the old Counter
    // is a generic counter missing its declared target (OutOfRange); every other reason is unchanged.
    [Theory]
    [InlineData("battery_default", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("battery_disabled", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("battery_custom_120_30_half", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("battery_custom_disabled_zero", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("domino", WorkshopSaveRejection.VerifierRejected, WireFaultReason.VerifierRejected)]
    [InlineData("counter", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("impact_switch", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("ramp", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("bumper", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("receiver", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("springboard", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("wall", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("hold_timer", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    public void APreSourceStoreRecordIsRefusedWithATypedReasonAtBothBoundaries(string name,
        WorkshopSaveRejection save, WireFaultReason command)
    {
        var bytes = Fixture(FixtureFamily.PreSourceStore, name, FixtureBoundary.Save);
        SaveFixture.Refused(save, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes));
        Assert.Equal(command, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(Fixture(FixtureFamily.PreSourceStore, name, FixtureBoundary.Command))).Reason);
    }

    // Control: the current layout of a Battery (its four declared store parameters) loads at both boundaries, with its values, and a
    // current Battery record carrying any typed record field, an undeclared, missing or duplicate parameter, or an enable index that
    // names no option, is refused atomically at both.
    [Fact]
    public void ACurrentBatteryRecordIsItsDeclaredStoreParametersOnly()
    {
        (string Key, float Value)[] valid = [("capacity", 120f), ("enabled", 0f), ("initial_charge", .5f), ("power_limit", 30f)];
        void Refused(byte[] bytes, string what, WorkshopSaveRejection reason = WorkshopSaveRejection.OutOfRange)
        {
            var save = Assert.Throws<WorkshopSaveRejectedException>(() => WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes));
            Assert.True(save.Reason == reason, $"battery {what}: {save.Reason}");
            var record = WorkshopSaveCodec.Open(bytes).Construction!.Value.Instances(0)!.Value;
            Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.MapInstance(record)).Reason);
        }
        foreach (var field in Enum.GetValues<TypedField>())
            Refused(PayloadInstanceTests.GenericSave("battery", field, valid, 4f), field.ToString());
        Refused(PayloadInstanceTests.GenericSave("battery", null, [.. valid, ("maximum_power", 30f)]), "undeclared parameter");
        Refused(PayloadInstanceTests.GenericSave("battery", null, valid[1..]), "missing parameter");
        Refused(PayloadInstanceTests.GenericSave("battery", null, [.. valid, valid[0]]), "duplicate parameter");
        foreach (var (key, value) in new[] { ("enabled", 2f), ("enabled", .5f), ("enabled", -1f), ("capacity", 59f), ("capacity", 14401f),
                     ("power_limit", 9f), ("power_limit", 481f), ("initial_charge", -.01f), ("initial_charge", 1.01f) })
            Refused(PayloadInstanceTests.GenericSave("battery", null, valid.Select(pair => pair.Key == key ? (key, value) : pair).ToArray()),
                $"{key} = {value}");
        // A non-finite value is the save boundary's NonFinite refusal (the command decode's OutOfRange).
        Refused(PayloadInstanceTests.GenericSave("battery", null, valid.Select(pair => pair.Key == "capacity" ? (pair.Key, float.NaN) : pair).ToArray()),
            "capacity = NaN", WorkshopSaveRejection.NonFinite);
        var bytes = PayloadInstanceTests.GenericSave("battery", null, valid);
        var loaded = Assert.IsType<WorkshopGenericInstance>(Assert.Single(WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes).Construction.Instances));
        Assert.Equal(PlacedParts.Battery(new(1), 0, 2, 0, 120f, 30f, .5f, ElectricalEnable.Disabled), loaded);
        Assert.Equal(loaded, WorkshopWireFlatBuffers.MapInstance(WorkshopSaveCodec.Open(bytes).Construction!.Value.Instances(0)!.Value));
    }

    // A current-layout Bumper or Springboard record is its declared parameters only: any typed record field (all 11, each with valid
    // parameters beside it), an undeclared parameter, a missing one or a duplicate is refused atomically by the save pre-validation and
    // by the command decode's instance mapping alike. Control: the exact declared set loads at both.
    [Theory]
    [InlineData("bumper")]
    [InlineData("spring")]
    public void ACurrentRecordCarryingATypedFieldOrAWrongParameterSetIsRefusedAtBothBoundaries(string part)
    {
        (string Key, float Value)[] valid = part == "bumper" ? [("strength", 8f)]
            : [("stiffness", 400f), ("damping", .2f), ("initial_compression", .2f)];
        void Refused(byte[] bytes, string what)
        {
            var save = Assert.Throws<WorkshopSaveRejectedException>(() => WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes));
            Assert.True(save.Reason == WorkshopSaveRejection.OutOfRange, $"{part} {what}: {save.Reason}");
            var record = WorkshopSaveCodec.Open(bytes).Construction!.Value.Instances(0)!.Value;
            Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.MapInstance(record)).Reason);
        }
        foreach (var field in Enum.GetValues<TypedField>())
            Refused(PayloadInstanceTests.GenericSave(part, field, valid, 4f), field.ToString());
        Refused(PayloadInstanceTests.GenericSave(part, null, [.. valid, ("preload", 1f)]), "undeclared parameter");
        Refused(PayloadInstanceTests.GenericSave(part, null, valid[..^1]), "missing parameter");
        Refused(PayloadInstanceTests.GenericSave(part, null, [.. valid, valid[0]]), "duplicate parameter");
        var bytes = PayloadInstanceTests.GenericSave(part, null, valid);
        var loaded = Assert.IsType<WorkshopGenericInstance>(Assert.Single(WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes).Construction.Instances));
        Assert.Equal(new InstanceParameterCollection(valid.Select(pair => KeyValuePair.Create(new ParameterId(pair.Key), pair.Value))), loaded.Parameters);
        Assert.Equal(loaded, WorkshopWireFlatBuffers.MapInstance(WorkshopSaveCodec.Open(bytes).Construction!.Value.Instances(0)!.Value));
    }

    // Control: a current-layout generic Bumper and Springboard record of the same values loads at both boundaries.
    [Fact]
    public void TheCurrentLayoutOfTheSameRecordsLoads()
    {
        var construction = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(
            PlacedParts.Bumper(new(1), 0, 2, 0), PlacedParts.Springboard(new(2), 3, 2, 0, 0, 0, 0, 1, .2f)));
        var saved = new WorkshopSavedConstruction(construction, new(3));
        Assert.Equal(saved, WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(saved)));
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), new(1), construction,
            Session: new(1, 2), Cadence: new(1), Projection: new(1));
        Assert.Equal(command, WorkshopWireFlatBuffers.DecodeCommand(WorkshopWireFlatBuffers.EncodeCommand(command)));
    }
}
