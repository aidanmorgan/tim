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

    // Old Bumper at strength 8: its strength float sits in the parameters slot, so the verifier refuses the buffer. At strength 0 that
    // float is elided: it decodes as a parameterless generic Bumper and its missing declared strength is refused. Old Springboard: its
    // stiffness and damping land in capacity and maximum power, typed fields a generic record cannot carry, and its parameters are
    // missing. Old Battery: its enable byte sits in the target-count slot, so the verifier refuses it. Old Domino and Counter: their half
    // extents and target count lie beyond the current table. Old Switch and resized Ramp: their parameters lie beyond the table.
    [Theory]
    [InlineData("bumper strength 8", WorkshopSaveRejection.VerifierRejected, WireFaultReason.VerifierRejected)]
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
