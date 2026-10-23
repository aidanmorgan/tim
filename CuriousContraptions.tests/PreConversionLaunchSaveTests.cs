using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>C-Bumper+Spring removed six InstanceRecord fields (stiffness, damping, initial_compression, strength, reference_mass,
/// preload), renumbering battery, half_extents, friction, target_count and parameters. A pre-conversion record whose meaning moved is
/// refused with a typed reason at both boundaries, the stored save's pre-validation and the worker's command decode, and never misread.
/// There is no migration. The old bytes are the record path's own encoder output (sha256 below, receipt
/// evidence-c-bumper-spring/baseline/old-layout-equality.txt).</summary>
public sealed class PreConversionLaunchSaveTests
{
    private static readonly Dictionary<string, string> RecordPathSha256 = new()
    {
        ["bumper strength 8"] = "6B9A177CA918FB7D9DBA8000E459BD600929EB45D81FED0654F9CAE94AC61327",
        ["bumper strength 0"] = "9A0BD50F65D0E76F4AA1D2375CE495EDCA15DB21480E7DE7C28D15DB0782CCD7",
        ["spring default"] = "E333F6CF4FC5AF45B05A0D107D68D51A482A0268ABC9471155CD0B8333154360",
        ["spring charged"] = "032B79ECC9A9693168C59A7EC27853B3DE12894723AA092E46FF161FB140EB55",
        ["battery default"] = "F125C0C315754889D0439F52E98E09FA8789E5DFF48E97E05F11E8E3A93276FC",
        ["domino"] = "071238776F7B250C80C3D7E353369D40416FE6A130E36A87F36DF162EF722671",
        ["counter"] = "D1400AC3874FC4CB0F66668B89B947D432312F916C795C66EBEFB880EF41C4A0",
        ["switch threshold"] = "18A54E48CC98677ABA4713C6B407A0591F00B40A88B5F8311700B725C88E9238",
        ["resized ramp"] = "4332CC7D996E14C5287E0431D1A4172AE1D07888C5760D879973DD32292C1F25",
    };

    // Re-derived on the 14-field table of Story 9A.39 (the C-Bumper+Spring-era reasons are in that slice's receipts). Old Bumper at
    // strength 8: its strength, reference mass and preload lie beyond the table, so it decodes as a parameterless generic Bumper whose
    // missing declared strength is refused (at C-Bumper+Spring the strength sat in the parameters slot and the verifier refused it). At
    // strength 0 likewise. Old Springboard: its stiffness and damping land in half_extents and friction, typed fields a generic record
    // cannot carry. Old Battery: its capacity float sits in the parameters slot, so the verifier refuses the buffer. Old Domino and Counter: their half extents and target count lie beyond the table. Old Switch and resized Ramp: their
    // parameters lie beyond the table.
    [Theory]
    [InlineData("bumper strength 8", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("bumper strength 0", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("spring default", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("spring charged", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("battery default", WorkshopSaveRejection.VerifierRejected, WireFaultReason.VerifierRejected)]
    [InlineData("domino", WorkshopSaveRejection.MissingField, WireFaultReason.MissingField)]
    [InlineData("counter", WorkshopSaveRejection.MissingField, WireFaultReason.MissingField)]
    [InlineData("switch threshold", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("resized ramp", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    public void APreConversionRecordIsRefusedWithATypedReasonAtBothBoundaries(string name, WorkshopSaveRejection save,
        WireFaultReason command)
    {
        var record = OldLayoutSaves.Records().Single(value => value.Name == name);
        var bytes = record.Save();
        Assert.Equal(RecordPathSha256[name], Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)));
        SaveFixture.Refused(save, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes));
        Assert.Equal(command, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(record.Command())).Reason);
    }

    // Story 9A.39: f27cb71d's records whose meaning moved when the battery's four typed fields left the table. The bytes are that
    // commit's own production encoder output (sha256 pinned from evidence-9a-39/baseline/capture/old-saves.sha256). There is no
    // migration: each is refused with a typed reason at both boundaries, never misread. Old Battery: its capacity, maximum power and
    // initial fraction land in half_extents, friction and target_count; where its enable byte (slot 13) is written it reads as the
    // parameters offset and the verifier refuses the buffer, otherwise the typed fields are refused. Old Domino and Counter: half
    // extents and target count now read beyond their old slots, so they are missing. Every parameterised generic: its parameters lie
    // beyond the table, so its declared parameters are missing.
    [Theory]
    [InlineData("battery_default", "29ca2c042cfed22cb7b54cff4f8ad0c73801e700b362bd22965d7dcd939a2aaf", WorkshopSaveRejection.VerifierRejected, WireFaultReason.VerifierRejected)]
    [InlineData("battery_disabled", "f6814d3103970f0e92ff42cc769a109a6d4ca7a8964bf8bd74ad4751f9f85166", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("battery_custom_120_30_half", "3ca68bb944310a98001d5a013c7f822d1368cddbdbb8a4c92a3a524d72f8b04c", WorkshopSaveRejection.VerifierRejected, WireFaultReason.VerifierRejected)]
    [InlineData("battery_custom_disabled_zero", "bb81a4225332a58134f30b6ef2faaabfffee70c2982b1f87f210926ec323f16d", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("domino", "9715a1b50c2df29b8c1d8b3d6e51d185594ba10e3960967ddd1f507590f96828", WorkshopSaveRejection.MissingField, WireFaultReason.MissingField)]
    [InlineData("counter", "28e34c0cbb55ff4f74e5b6e9ec7f587ba8373edebc4809b676bc8fde5493a70c", WorkshopSaveRejection.MissingField, WireFaultReason.MissingField)]
    [InlineData("impact_switch", "5616915e22c3a5850531aa8081837e123687710cfe36cc69c9192b21129194df", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("ramp", "d0f3d6bbe76738031f2b778d921f95d8955e87ac6d1b56457219fe05601d1c0a", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("bumper", "2609c9eef542ee5ccf282f2529b43804c69f8eef86e96cd8344a1452deb98f90", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("receiver", "e115ed19fa96f287ce4639c7cb4029d000448516a4589f940698eb963a3e19f3", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("springboard", "b341f48004f7a5f24fca755d42912ecaf8f60cbbc820ffc520db2b597ab3b580", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("wall", "8de75e884c98b1090aa39463ce201f7497b9d0317636bf0655beb74d39c82081", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("hold_timer", "ed8ea65636f6b6ab71797325c27460a3eb927e4b0986e1ec354b72fcd1f60b78", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    public void APreSourceStoreRecordIsRefusedWithATypedReasonAtBothBoundaries(string name, string baseEncoderSha256,
        WorkshopSaveRejection save, WireFaultReason command)
    {
        var record = OldLayoutSaves.PreSourceStoreRecords().Single(value => value.Name == name);
        var bytes = record.Save();
        Assert.Equal(baseEncoderSha256, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant());
        SaveFixture.Refused(save, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes));
        Assert.Equal(command, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(record.Command())).Reason);
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
