using CuriousContraptions.Abstractions.Signal;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Story 9A.32 (D1-D2): the Domino is a generic instance whose orientation sensor and OrientationSource node are selected by
/// its package's declared threshold, compiled through the one shared helper
/// (<see cref="WorkshopPhysicsCompiler.OrientationSensorFor"/>) and only while the owner is wired.</summary>
public sealed class DeclaredOrientationTests
{
    private static readonly double Upright = (double)WorkshopConstruction.WorkbenchSurface.Value + .55;
    private static WorkshopConnection Signal(GpuBodyId source, GpuBodyId target) =>
        new(source, WorkshopSocket.ActivationOut, target, WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation);
    private static WorkshopConstruction Bench(IWorkshopInstance[] instances, params WorkshopConnection[] links) =>
        new(new(2), WorkshopCadenceSettings.Default(), new(instances), Connections: new(links));

    // A wired tile turned 30° about its height compiles exactly the helper's sensor on its own body, measured from that admitted
    // rotation, and an OrientationSource node naming it. Control: a wired Impact switch (an ActivationOut without the declaration)
    // compiles no orientation sensor and its own contact node.
    [Fact]
    public void TheDeclaredThresholdSelectsTheSensorAndNodeOfAWiredTile()
    {
        var declared = Assert.NotNull(DeclaredPhysics.Of(PartId.Domino).Signal?.Orientation);
        Assert.Equal(new SignalOrientationDescriptor(45f), declared);
        var half = 15 * Math.PI / 180;
        var tile = Assert.IsType<WorkshopGenericInstance>(PlacedParts.Domino(new(1), 0, Upright, 0, 0, Math.Sin(half), 0, Math.Cos(half)));
        var bench = Bench([tile, PlacedParts.Lamp(new(2), 3, 1, 0, 0, 0, 0, 1)], Signal(tile.Id, new(2)));
        var sensor = Assert.Single(WorkshopPhysicsCompiler.Compile(bench, new(1, 2)).OrientationSensors.ToArray());
        Assert.Equal(WorkshopPhysicsCompiler.OrientationSensorFor(tile, declared), sensor);
        Assert.Equal((WorkshopPhysicsCompiler.OrientationSensor(tile.Id), tile.Id, tile.Rotation), (sensor.Id, sensor.Body, sensor.Initial));
        Assert.NotEqual(CanonicalRotation.Identity, sensor.Initial);
        // The Domino's declared 45° compiles to the f32 cosine of 22.5°, rounded once from its exact value.
        Assert.Equal(BitConverter.SingleToUInt32Bits((float)Math.Cos(Math.PI / 8)), BitConverter.SingleToUInt32Bits(sensor.Threshold.CosineHalfAngle));
        var node = Assert.Single(WorkshopActivationCompiler.Compile(bench).Nodes.ToArray(), row => row.Owner == tile.Id);
        Assert.Equal((ActivationNodeKind.OrientationSource, new GpuOrientationSensorId(sensor.Id.Value)), (node.Kind, node.Sensor));

        var hit = WorkshopInput.Generic(PartId.ImpactSwitch, new(3), -3, 1, 0, 0, 0, 0, 1);
        var control = Bench([hit, PlacedParts.Lamp(new(2), 3, 1, 0, 0, 0, 0, 1)], Signal(hit.Id, new(2)));
        Assert.Empty(WorkshopPhysicsCompiler.Compile(control, new(1, 2)).OrientationSensors.ToArray());
        Assert.Equal(ActivationNodeKind.ContactSource, Assert.Single(WorkshopActivationCompiler.Compile(control).Nodes.ToArray(), row => row.Owner == hit.Id).Kind);
    }

    // The helper compiles any admitted declaration to the f32 cosine of its own half angle: the threshold is read from the
    // declaration, never assumed. Its admitted range is the declared 4-179°; beyond it the shared threshold law refuses.
    [Fact]
    public void TheHelperCompilesEachDeclaredThresholdToItsOwnCosine()
    {
        var tile = PlacedParts.Domino(new(1), 0, Upright, 0, 0, 0, 0, 1);
        Assert.Equal(BitConverter.SingleToUInt32Bits((float)Math.Cos(Math.PI / 12)),
            BitConverter.SingleToUInt32Bits(WorkshopPhysicsCompiler.OrientationSensorFor(tile, new(30f)).Threshold.CosineHalfAngle));
        foreach (var degrees in new[] { 4f, 30f, 45f, 179f })
            Assert.Equal(OrientationThreshold.Degrees(degrees), WorkshopPhysicsCompiler.OrientationSensorFor(tile, new(degrees)).Threshold);
        Assert.NotEqual(WorkshopPhysicsCompiler.OrientationSensorFor(tile, new(30f)).Threshold, WorkshopPhysicsCompiler.OrientationSensorFor(tile, new(45f)).Threshold);
        Assert.Throws<ArgumentOutOfRangeException>(() => WorkshopPhysicsCompiler.OrientationSensorFor(tile, new(MathF.BitDecrement(4f))));
        Assert.Throws<ArgumentOutOfRangeException>(() => WorkshopPhysicsCompiler.OrientationSensorFor(tile, new(float.NaN)));
    }

    // ---------- D6: saves written by the base encoder (2065d3c5) whose decoded meaning moved are refused, never misread ----------

    // Each beside one Basketball, written by 2065d3c5's production save and command encoders (evidence-9a-32/baseline/capture/old-saves,
    // sha256 in old-saves.sha256). Removing mass, bounce, half_extents and friction renumbered the later slots: the old Domino's mass
    // and bounce land in length and target_count, typed fields its generic record cannot carry. C-Clock (9-field table) re-derived the
    // rest: the old plate and clock threshold (slot 9) now lies beyond the table, so each record lacks its declared parameter and is
    // refused out of range at both boundaries (at 9A.32 slot 9 was the parameters slot and the verifier refused the buffer); the old
    // pipe length (slot 8) now sits in the parameters slot, so the verifier refuses the buffer at both boundaries (at 9A.32 it sat in
    // target_count and the pipe's length read absent: OutOfRange and MissingField). C-Memory (8-field table, no target_count): the old
    // Domino's bounce (slot 7) is now the parameters offset, so the verifier refuses it; the old pipe length (slot 8) now lies beyond the
    // table and its current length slot (the old mass, absent) reads 0: the save refuses it out of range and the command as a missing
    // length; the plate and clock keep their refusals.
    private const string DominoSave = "VAEAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAAJAAAAAAAAAAAAAAAMACwAIAAIAAAABAAMAAAAKAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAAAAAAIAAACQAAAAJAAAACAAPAAwACwAAAAgABQABAAAAAAAAAAAAAAAAAAAADgAIAAAAAAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAAAwAAAAIAAAAAAAAAAUAAAAAgAAAAAAAAAEAAAAAAAAAAQAAABiYWxsAAAAABwAVABIAEQAAAA4ACwAHAAMAAgAAAAAABAABAAcAAAAAKAZPwDATD0AwMw+AAAAPgDADD8AYKY+AAAAAAAAAAAAAAAAAACAPwAAAADhesy+AAAAAAAAAAACAAAAAAAAABAAAAABAAAAAAAAAAAAAAAGAAAAZG9taW5vAAA=";
    private const string DominoCommand = "pAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAAFQBAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAsACAACAAAAAQADAAAACgAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAAAAAACAAAAkAAAACQAAAAgADwAMAAsAAAAIAAUAAQAAAAAAAAAAAAAAAAAAAA4ACAAAAAAAAAAAAAAAAAAAAAAAIA/AAAAAAAAAAAAAAAAMAAAACAAAAAAAAAAFAAAAAIAAAAAAAAABAAAAAAAAAAEAAAAYmFsbAAAAAAcAFAASABEAAAAOAAsABwADAAIAAAAAAAQAAQAHAAAAACgGT8AwEw9AMDMPgAAAD4AwAw/AGCmPgAAAAAAAAAAAAAAAAAAgD8AAAAA4XrMvgAAAAAAAAAAAgAAAAAAAAAMAAAAAQAAAAAAAAAGAAAAZG9taW5vAAAQAAAAAQAAAAAAAAACAAAAAAAAAA==";
    private const string PlateSave = "RAEAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAAJAAAAAAAAAAAAAAAMACwAIAAIAAAABAAMAAAAKAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAAAAAAIAAACMAAAAJAAAACAAPAAwACwAAAAgABQABAAAAAAAAAAAAAAAAAAAADgAIAAAAAAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAAAwAAAAIAAAAAAAAAAUAAAAAgAAAAAAAAAEAAAAAAAAAAQAAABiYWxsAAAAABgAQAA0ADAAAAAkABgACAAAAAAAAAAEABgAAAAAAKA/AAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAAAAQAAAAAAAAABAAAAABAAAAAAAAAAAAAAAOAAAAcHJlc3N1cmVfcGxhdGUAAA==";
    private const string PlateCommand = "lAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAAEQBAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAsACAACAAAAAQADAAAACgAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAAAAAACAAAAjAAAACQAAAAgADwAMAAsAAAAIAAUAAQAAAAAAAAAAAAAAAAAAAA4ACAAAAAAAAAAAAAAAAAAAAAAAIA/AAAAAAAAAAAAAAAAMAAAACAAAAAAAAAAFAAAAAIAAAAAAAAABAAAAAAAAAAEAAAAYmFsbAAAAAAYADwANAAwAAAAJAAYAAgAAAAAAAAABAAYAAAAAACgPwAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAAAAAAAAEAAAAAAAAAAMAAAAAQAAAAAAAAAOAAAAcHJlc3N1cmVfcGxhdGUAABAAAAABAAAAAAAAAAIAAAAAAAAA";
    private const string ClockSave = "PAEAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAAJAAAAAAAAAAAAAAAMACwAIAAIAAAABAAMAAAAKAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAAAAAAIAAACMAAAAJAAAACAAPAAwACwAAAAgABQABAAAAAAAAAAAAAAAAAAAADgAIAAAAAAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAAAwAAAAIAAAAAAAAAAUAAAAAgAAAAAAAAAEAAAAAAAAAAQAAABiYWxsAAAAABgAQAA0ADAAAAAkABgACAAAAAAAAAAEABgAAAAAAMA/AAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAAAAQAAAAAAAAABAAAAABAAAAAAAAAAAAAAAFAAAAY2xvY2sAAAA=";
    private const string ClockCommand = "jAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAADwBAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAsACAACAAAAAQADAAAACgAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAAAAAACAAAAjAAAACQAAAAgADwAMAAsAAAAIAAUAAQAAAAAAAAAAAAAAAAAAAA4ACAAAAAAAAAAAAAAAAAAAAAAAIA/AAAAAAAAAAAAAAAAMAAAACAAAAAAAAAAFAAAAAIAAAAAAAAABAAAAAAAAAAEAAAAYmFsbAAAAAAYADwANAAwAAAAJAAYAAgAAAAAAAAABAAYAAAAAADAPwAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAAAAAAAAEAAAAAAAAAAMAAAAAQAAAAAAAAAFAAAAY2xvY2sAAAAQAAAAAQAAAAAAAAACAAAAAAAAAA==";
    private const string PipeSave = "PAEAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAAJAAAAAAAAAAAAAAAMACwAIAAIAAAABAAMAAAAKAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAAAAAAIAAACMAAAAJAAAACAAQAAwACwAAAAgABQABAAAAAAAAAAAAAAAAAAAADwAIAAAAAAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAAAwAAAAIAAAAAAAAAAYAAAAAgAAAAAAAAAAAAAABAAAAAAAAAAEAAAAYmFsbAAAFgBAADQAMAAAACQAGAAIAAAAAAAEABYAAAAAADBAAAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAAAAQAAAAAAAAABAAAAABAAAAAAAAAAAAAAAEAAAAcGlwZQAAAAA=";
    private const string PipeCommand = "jAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAADwBAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAsACAACAAAAAQADAAAACgAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAAAAAACAAAAjAAAACQAAAAgAEAAMAAsAAAAIAAUAAQAAAAAAAAAAAAAAAAAAAA8ACAAAAAAAAAAAAAAAAAAAAAAAIA/AAAAAAAAAAAAAAAAMAAAACAAAAAAAAAAGAAAAAIAAAAAAAAAAAAAAAQAAAAAAAAABAAAAGJhbGwAABYAPAA0ADAAAAAkABgACAAAAAAABAAWAAAAAAAwQAAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAAAAAAAAEAAAAAAAAAAMAAAAAQAAAAAAAAAEAAAAcGlwZQAAAAAQAAAAAQAAAAAAAAACAAAAAAAAAA==";

    [Theory]
    [InlineData("domino", "c837090c5184533686b7e165fa753b9f9ded17c2c2a95562380582ad2f692fd6", "19536ebaedd02a18fef548613e09f4ab6e315e567f4d663573c6039756d0d17f", WorkshopSaveRejection.VerifierRejected, WireFaultReason.VerifierRejected)]
    [InlineData("plate", "0b0a704d05e5420e29869d5fd95d372f4a18a9f595c56efae40018bf6cfa718b", "a5393bcc3517b35a16bd7b4de41877e969c8bb361b6dcf526f4f367d1beb8ce3", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    [InlineData("clock", "dc2c6e3ba6401c924a1a4c4e88ddaaa9b38b7f112de0f6f713665c7440811f83", "e71d957bd68b5c8706d1f52dcc93f0ad7909bc380ac51a831ad1e99aec56d231", WorkshopSaveRejection.OutOfRange, WireFaultReason.OutOfRange)]
    public void ABaseEncoderSaveWhoseSlotsMovedIsRefusedTypedAtBothBoundaries(string name, string saveSha256, string commandSha256,
        WorkshopSaveRejection save, WireFaultReason command)
    {
        var (saveBytes, commandBytes) = name switch
        {
            "domino" => (Convert.FromBase64String(DominoSave), Convert.FromBase64String(DominoCommand)),
            "plate" => (Convert.FromBase64String(PlateSave), Convert.FromBase64String(PlateCommand)),
            "clock" => (Convert.FromBase64String(ClockSave), Convert.FromBase64String(ClockCommand)),
            _ => throw new ArgumentOutOfRangeException(nameof(name))
        };
        Assert.Equal((saveSha256, commandSha256), (Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(saveBytes)),
            Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(commandBytes))));
        SaveFixture.Refused(save, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, saveBytes));
        Assert.Equal(command, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(commandBytes)).Reason);
    }

    [Fact]
    public void ABaseEncoderPipeSaveIsRefusedTypedAtBothBoundaries()
    {
        var saveBytes = Convert.FromBase64String(PipeSave);
        var commandBytes = Convert.FromBase64String(PipeCommand);
        Assert.Equal(("e011925730a9729676d9530eb932f84a64eba8e2caff5bac9e619cb903bc57f6", "9635d17d38ed20c430b222cd09ef089a96532a1eda606be75a7281030927a7d5"),
            (Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(saveBytes)), Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(commandBytes))));
        // The old length (2.75 m, slot 8) now lies beyond the 8-field table, and the current length slot (the old mass, absent) reads 0.
        Assert.Equal("This save cannot be loaded: Part 1 length 0 is outside [1, 8].",
            SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, saveBytes)).Message);
        var fault = Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(commandBytes));
        Assert.Equal((WireFaultReason.MissingField, "Instance 1 has no length."), (fault.Reason, fault.Message));
    }

    // ---------- a field beyond the current InstanceRecord table is refused, never ignored (coordinator decision 2, 23 Oct 2026) ----------

    public static TheoryData<string> PreDominoRecordNames() => new(OldLayoutSaves.PreDominoRecords().Select(record => record.Name));

    // The 2065d3c5 layout's parameters vector sits in slot 13, beyond the current 8-field table, so its decoder would never see it:
    // a zero-parameter package's record carrying an undeclared parameter there (and any record with that field, the base encoder's
    // empty vector included) is refused with the typed reason at both decode boundaries, after every per-field rule.
    [Theory]
    [MemberData(nameof(PreDominoRecordNames))]
    public void ARecordHoldingAFieldBeyondTheCurrentTableIsRefusedAtBothBoundaries(string name)
    {
        var record = OldLayoutSaves.PreDominoRecords().Single(value => value.Name == name);
        SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, record.Save()));
        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(record.Command())).Reason);
    }

    // The boundary slot: a record that is current in every field but holds one extra field at exactly slot 8 (the first slot beyond
    // the 8-field table) is refused at both boundaries; the same record without it loads (control).
    [Fact]
    public void AFieldAtExactlyTheFirstSlotBeyondTheTableIsRefusedAtBothBoundaries()
    {
        // schemas/workshop_wire.fbs: InstanceRecord holds fields 0-7 (C-Clock deleted the threshold, C-Memory the target count), so
        // slot 8 is the first undefined slot (stated here, not read from the rule's own constant).
        var layout = new OldLayoutSaves.Layout(9, 7, 16, 2);
        OldLayoutSaves.Slot[] extra = [new(8, 1f)];
        SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, OldLayoutSaves.Save(layout, "domino", extra)));
        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() =>
            WorkshopWireFlatBuffers.DecodeCommand(OldLayoutSaves.Command(layout, "domino", extra))).Reason);
        Assert.Equal(PartId.Domino, Assert.Single(WorkshopSaveCodec.Decode(SaveFixture.Elements, OldLayoutSaves.Save(layout, "domino", [])).Construction.Instances).PartId);
        Assert.Equal(PartId.Domino, Assert.Single(WorkshopWireFlatBuffers.DecodeCommand(OldLayoutSaves.Command(layout, "domino", [])).Construction!.Value.Instances).PartId);
    }

    // Controls and the pinned walk: current-encoder records (a zero-parameter domino and lamp, a parametered Ramp whose parameters fill
    // the last slot, a pipe, a counter) hold nothing beyond the table and load at both boundaries; the slots the rule walks reach the
    // same instance tables as the generated accessors.
    [Fact]
    public void CurrentRecordsHoldNothingBeyondTheTableAndTheWalkReachesTheGeneratedInstances()
    {
        var bench = Bench([PlacedParts.Domino(new(1), 0, Upright, 0, 0, 0, 0, 1), PlacedParts.Lamp(new(2), 3, 1, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(PartId.Ramp, new(3), -3, 1, 0, 0, 0, 0, 1), WorkshopInput.Pipe(new(4), 6, 1, 0, 0, 0, 0, 1),
            PlacedParts.Counter(new(5), -6, 1, 0, 0, 0, 0, 1, 3)]);
        var save = WorkshopSaveCodec.Encode(new WorkshopSavedConstruction(bench, new(9)));
        var command = WorkshopWireFlatBuffers.EncodeCommand(new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), new(1), bench,
            Session: new(1, 2), Cadence: new(1), Projection: new(1)));
        Assert.Equal(bench, WorkshopSaveCodec.Decode(SaveFixture.Elements, save).Construction);
        Assert.Equal(bench, WorkshopWireFlatBuffers.DecodeCommand(command).Construction);
        var saved = WorkshopSaveCodec.Open(save);
        Assert.False(WorkshopWireFlatBuffers.HoldsFieldBeyondTheInstanceTable(saved.ByteBuffer, WorkshopWireFlatBuffers.SavedConstructionSlot));
        var bb = saved.ByteBuffer;
        var root = new Google.FlatBuffers.Table(bb.GetInt(bb.Position) + bb.Position, bb);
        var construction = new Google.FlatBuffers.Table(root.__indirect(root.__offset(WorkshopWireFlatBuffers.SavedConstructionSlot) + root.bb_pos), bb);
        var instances = construction.__offset(WorkshopWireFlatBuffers.ConstructionInstancesSlot);
        Assert.Equal(bench.Instances.Count, construction.__vector_len(instances));
        for (var i = 0; i < bench.Instances.Count; i++)
        {
            var walked = new CuriousContraptions.Wire.InstanceRecord().__assign(construction.__indirect(construction.__vector(instances) + i * 4), bb);
            var generated = saved.Construction!.Value.Instances(i)!.Value;
            Assert.Equal((generated.Id, generated.PartId), (walked.Id, walked.PartId));
        }
    }

    // A base-encoder record that uses only slots 0-5 (the latch) loads identically; every base-encoder generic record wrote its
    // parameters vector in slot 13 and is refused by the rule above (evidence-9a-32/candidate/capture/old-save-outcomes.txt).

    // Each wired tile compiles its own sensor (the admitted scene holds them in identity order); an unwired tile between them
    // compiles neither a sensor nor a node.
    [Fact]
    public void EachWiredTileCompilesItsOwnSensorAndAnUnwiredTileNone()
    {
        var bench = Bench([PlacedParts.Domino(new(7), 0, Upright, 0, 0, 0, 0, 1), PlacedParts.Domino(new(5), 2, Upright, 0, 0, 0, 0, 1),
                PlacedParts.Domino(new(3), 4, Upright, 0, 0, 0, 0, 1), PlacedParts.Lamp(new(9), 6, 1, 0, 0, 0, 0, 1)],
            Signal(new(7), new(9)), Signal(new(3), new(9)));
        Assert.Equal([new GpuBodyId(3), new GpuBodyId(7)], WorkshopPhysicsCompiler.Compile(bench, new(1, 2)).OrientationSensors.ToArray().Select(sensor => sensor.Body));
        Assert.DoesNotContain(WorkshopActivationCompiler.Compile(bench).Nodes.ToArray(), node => node.Owner == new GpuBodyId(5));
    }
}
