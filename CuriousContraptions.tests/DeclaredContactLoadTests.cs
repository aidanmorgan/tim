using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Signal;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>C-Plate (9A.20 D1-D5): the Pressure Plate is a generic instance whose contact-load row, ActivationOut level node and release
/// hold come from its package's declared contact-load sensor, compiled through one shared helper
/// (<see cref="WorkshopPhysicsCompiler.ContactLoadSensorFor"/>), for every plate, wired or not. Expected values are stated literally
/// from CAT-052 §2, owner decision (h) and the Q3 override,.</summary>
public sealed class DeclaredContactLoadTests
{
    private static readonly double PlateY = (double)WorkshopConstruction.WorkbenchSurface.Value + .22;
    private static WorkshopConstruction Bench(IWorkshopInstance[] instances, params WorkshopConnection[] links) =>
        new(new(2), WorkshopCadenceSettings.Default(), new(instances), Connections: new(links));

    // The declared top region retains x/z footprint margins and strict normal/gap
    // filters; top and ring membership is separate from that region anchor.
    [Fact]
    public void TheDeclaredSensorCompilesItsRegionAndMembersForEveryPlate()
    {
        var declared = Assert.NotNull(DeclaredPhysics.Of(PartId.PressurePlate).Signal?.ContactLoad);
        Assert.Equal(new SignalContactLoadDescriptor("minimum_mass", 1, System.Numerics.Vector3.UnitY, 1f - 1e-6f, 1e-4f, 1e-6f, .1f, declared.SupportingColliders), declared);
        Assert.Equal([1, 7], declared.SupportingColliders);
        var face = CuriousContraptions.Bundle.BundleGeometry.Colliders(DeclaredPhysics.Of(PartId.PressurePlate))[1];
        Assert.Equal((CuriousContraptions.Abstractions.ColliderShapeKind.Box, .9f, .05f, .9f), (face.ShapeKind, face.HalfExtents.X, face.HalfExtents.Y, face.HalfExtents.Z));

        var plate = Assert.IsType<WorkshopGenericInstance>(PlacedParts.Plate(new(3), -4, (float)(PlateY), 0, 2f));
        var row = Assert.Single(WorkshopPhysicsCompiler.Compile(Bench([plate]), new(1, 2)).ContactLoadSensors.ToArray());
        var identities = Bench([plate]).Instances.Identities[plate.Id];
        Assert.Equal(new[] { identities.Colliders[1], identities.Colliders[7] }, row.SupportingColliders.ToArray());
        Assert.Equal((Bench([plate]).Instances.Identities[plate.Id].ContactLoad, plate.Id, Bench([plate]).Instances.Identities[plate.Id].Colliders[1]),
            (row.Id, row.Frame, row.Region));
        Assert.Equal((-.9f - .0001f, .9f + .0001f, -.9f - .000001f, .9f + .000001f, 1f - .000001f, .0001f, 2f),
            (row.MinimumX, row.MaximumX, row.MinimumZ, row.MaximumZ, row.MinimumAlignment, row.MaximumGap, row.MinimumMass));
        Assert.Equal(System.Numerics.Vector3.UnitY, row.LocalNormal);
        var node = Assert.Single(WorkshopActivationCompiler.Compile(Bench([plate])).Nodes.ToArray());
        Assert.Equal((ActivationNodeKind.ContactLoadSource, row.Id, 12u), (node.Kind, node.ContactLoad, node.ReleaseHoldTicks));
    }

    // The helper reads the owner's own minimum mass at each admitted end and between; two plates keep their own rows in instance order.
    private static readonly float[] Masses = [.1f, 2f, 16f, .5f];
    [Fact]
    public void EachPlateCompilesItsOwnMinimumMassInInstanceOrder()
    {
        var plates = Masses.Select((mass, i) => PlacedParts.Plate(new((ulong)i + 1), (float)(3 * i), (float)(PlateY), 0, (float)(mass))).ToArray();
        var rows = WorkshopPhysicsCompiler.Compile(Bench(plates), new(1, 2)).ContactLoadSensors.ToArray();
        Assert.Equal(plates.Select(plate => plate.Id), rows.Select(row => row.Frame));
        Assert.Equal(Masses, rows.Select(row => row.MinimumMass));
    }

    // D1: the hold is ActivationTimerDeclaration.Duration(0.1 s, cadence): 6, 12 and 24 ticks at 60, 120 and 240 Hz.
    [Theory]
    [InlineData(SimulationCadence.Hz60, 6u)]
    [InlineData(SimulationCadence.Hz120, 12u)]
    [InlineData(SimulationCadence.Hz240, 24u)]
    public void TheHoldIsATenthOfASecondAtEachCadence(SimulationCadence cadence, uint ticks)
    {
        Assert.Equal(ticks, ActivationTimerDeclaration.Duration(.1f, cadence));
        var bench = Bench([PlacedParts.Plate(new(1), 0, (float)(PlateY), 0, .5f)]) with { Settings = WorkshopCadenceSettings.Default() with { Simulation = cadence } };
        Assert.Equal(ticks, Assert.Single(WorkshopActivationCompiler.Compile(bench).Nodes.ToArray()).ReleaseHoldTicks);
    }

    // Negative control (static box, no capability): a Wall sized to the face's footprint compiles no row and no node.
    [Fact]
    public void AStaticBoxWithoutTheDeclarationCompilesNoRowAndNoNode()
    {
        var wall = WorkshopInput.Generic(PartId.Wall, new(1), 0, (float)(PlateY), 0, 0, 0, 0, 1,
            new([new(new("width"), 1.8f), new(new("height"), .4f), new(new("thickness"), 1.8f)]));
        Assert.Null(DeclaredPhysics.Of(PartId.Wall).Signal?.ContactLoad);
        Assert.Empty(WorkshopPhysicsCompiler.Compile(Bench([wall]), new(1, 2)).ContactLoadSensors.ToArray());
        Assert.Empty(WorkshopActivationCompiler.Compile(Bench([wall])).Nodes.ToArray());
    }

    // ---------- D5: saves ----------

    // 0.1, 0.5, 2.0 and 16 round-trip numerically through the production save and command encoders.
    [Theory]
    [InlineData(.1f)]
    [InlineData(.5f)]
    [InlineData(2f)]
    [InlineData(16f)]
    public void AdmittedMinimumMassesRoundTripAtBothBoundaries(float minimumMass)
    {
        var bench = Bench([PlacedParts.Plate(new(1), 0, (float)(PlateY), 0, (float)(minimumMass))]);
        var saved = WorkshopSaveCodec.Decode(SaveFixture.Elements, WorkshopSaveCodec.Encode(new WorkshopSavedConstruction(bench, new(2)))).Construction;
        var command = WorkshopWireFlatBuffers.DecodeCommand(WorkshopWireFlatBuffers.EncodeCommand(new WorkshopCommand(new(1),
            WorkshopCommandKind.Construct, new(1), new(1), bench, Session: new(1, 2), Cadence: new(1), Projection: new(1)))).Construction!.Value;
        foreach (var decoded in new[] { saved, command })
        {
            var plate = Assert.IsType<WorkshopGenericInstance>(Assert.Single(decoded.Instances));
            Assert.Equal(minimumMass, plate.Parameters[new ParameterId("minimum_mass")]);
        }
    }

    // A missing, NaN, 0.09 or 16.01 minimum_mass is refused at both boundaries: the save pre-validation (OutOfRange; a NaN is its
    // NonFinite refusal) and the command's instance decoder (OutOfRange). Nothing is clamped or defaulted.
    [Theory]
    [InlineData(null, WorkshopSaveRejection.OutOfRange)]
    [InlineData(float.NaN, WorkshopSaveRejection.NonFinite)]
    [InlineData(.09f, WorkshopSaveRejection.OutOfRange)]
    [InlineData(16.01f, WorkshopSaveRejection.OutOfRange)]
    public void AMissingOrOutOfRangeMinimumMassIsRefusedAtBothBoundaries(float? minimumMass, WorkshopSaveRejection save)
    {
        var bytes = PayloadInstanceTests.GenericSave("pressure_plate", null, minimumMass is { } mass ? [("minimum_mass", mass)] : []);
        SaveFixture.Refused(save, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes));
        var record = WorkshopSaveCodec.Open(bytes).Construction!.Value.Instances(0)!.Value;
        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.MapInstance(record)).Reason);
        // Control: the identical record at 0.5 kg loads at both boundaries.
        var control = PayloadInstanceTests.GenericSave("pressure_plate", null, [("minimum_mass", .5f)]);
        Assert.IsType<WorkshopGenericInstance>(Assert.Single(WorkshopSaveCodec.Decode(SaveFixture.Elements, control).Construction.Instances));
        Assert.IsType<WorkshopGenericInstance>(WorkshopWireFlatBuffers.MapInstance(WorkshopSaveCodec.Open(control).Construction!.Value.Instances(0)!.Value));
    }

    // Saves written by the record path's production encoders at 61ded7ad (evidence-c-plate/baseline/old-saves, sha256 in
    // old-saves.sha256): a plate record carries its minimum mass in the threshold field, a typed field the generic instance cannot
    // carry. Each is refused at the save boundary with the visible message and at the command boundary with the wire fault, never
    // migrated (decision h). C-Clock deleted the threshold (9-field table): the plate's old slot 7 is now target_count, still a typed
    // field, so a lone plate keeps its message; but every generic record of that layout wrote its parameters vector in slot 9, now
    // beyond the table, so in the wired benches the battery (the first parametered record, before the plate) is refused first for its
    // missing declared parameters. Both are typed OutOfRange refusals at both boundaries (evidence-c-clock/candidate old-save
    // outcomes); no record loads. C-Memory deleted target_count (8-field table): the plate's old slot 7 is now the parameters offset, so
    // its float bits read as an offset the verifier refuses, at both boundaries, in every one of these saves; still no record loads.
    private const string PlainSave = "zAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAALAAAAAAAAAAAAAAAMACgAIAAIAAAABAAMAAAAJAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAQAAABgAAAAUAEAANAAwAAAAJAAYAAgAAAAEABQAAAAAAAA/AAAAAAAAAAAAAAAAAACAPwAAAADheiQ+AAAAAAAAAAD8////AAAAABAAAAABAAAAAAAAAAAAAAAOAAAAcHJlc3N1cmVfcGxhdGUAAA==";
    private const string PlainCommand = "HAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAAMwAAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAoACAACAAAAAQADAAAACQAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAEAAAAYAAAAFAA8ADQAMAAAACQAGAAIAAAABAAUAAAAAAAAPwAAAAAAAAAAAAAAAAAAgD8AAAAA4XokPgAAAAAAAAAA/P///wAAAAAMAAAAAQAAAAAAAAAOAAAAcHJlc3N1cmVfcGxhdGUAABAAAAABAAAAAAAAAAIAAAAAAAAA";
    private const string TwoKgSave = "zAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAALAAAAAAAAAAAAAAAMACgAIAAIAAAABAAMAAAAJAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAQAAABgAAAAUAEAANAAwAAAAJAAYAAgAAAAEABQAAAAAAABAAAAAAAAAAAAAAAAAAACAPwAAAADheiQ+AAAAAAAAAAD8////AAAAABAAAAABAAAAAAAAAAAAAAAOAAAAcHJlc3N1cmVfcGxhdGUAAA==";
    private const string TwoKgCommand = "HAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAAMwAAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAoACAACAAAAAQADAAAACQAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAEAAAAYAAAAFAA8ADQAMAAAACQAGAAIAAAABAAUAAAAAAAAQAAAAAAAAAAAAAAAAAAAgD8AAAAA4XokPgAAAAAAAAAA/P///wAAAAAMAAAAAQAAAAAAAAAOAAAAcHJlc3N1cmVfcGxhdGUAABAAAAABAAAAAAAAAAIAAAAAAAAA";
    private const string LampWiredSave = "NAMAABQAAABDQ1dTAAAAAAgAFgAIAAQACAAAACAAAAALAAAAAAAAAAAAAAAAAA4AMAAkAAwAAAAIAAQADgAAACwAAACMAAAAAgAAAAEAAAACAAAAAgAAADwAAAABAAAAAgAAAAAAAAAAAAAAAwAAAAIAAAAAAAAAAwAAAAAAAAACAAAABwAAAAgAAAAAAAAAAwAAAAAAAAABAAAAAAAAAAIAAAAHAAAACAAAAAAAAAADAAAAAAAAAAQAAAAAAAAAAQAAAAEAAAAEAAAAAAAAAAUAAAAMAgAAGAEAAMAAAABYAAAABAAAACD+//8AAAAAAAAAAAAAAAAAAIA/AAAAAK5HIb0AAAAAwP///wMAAAAAAAAAGAAAAAUAAAAAAAAAAAAAAAQAAAAAAAAABAAAAGJhbGwAAAAAcP7//wAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAABgAAAAMAAAAAAAAAAYAAAABAAAAAAAAAAAAAAABAAAAAAAAAAEAAAAbGFtcAAAAAAUAEAANAAwAAAAJAAYAAgAAAAEABQAAAAAAAA/AAAAAAAAAAAAAAAAAACAPwAAAADheiQ+AAAAAMD////8////AAAAABAAAAADAAAAAAAAAAAAAAAOAAAAcHJlc3N1cmVfcGxhdGUAACj///8AAAAAAAAAAAAAAAAAAIA/AAAAAAAAAAAAAAAAMAAAADAAAAAAAAAAoAAAAAIAAAAAAAAAAAAAAAQAAAAEAAAAbAAAAEgAAAAkAAAABAAAAKz///8IAAAAAADwQgsAAABwb3dlcl9saW1pdADI////CAAAAAAAgD8OAAAAaW5pdGlhbF9jaGFyZ2UAAOj///8IAAAAAACAPwcAAABlbmFibGVkAAgADAAEAAgACAAAAAgAAAAAAGFFCAAAAGNhcGFjaXR5AAAAAAcAAABiYXR0ZXJ5ABgAQAAwACwAAAAgABQABAAAAAAAAAA8ABgAAAAAAAAAAAAAAAAAAAAAAIA/AAAAAAAAAAAAAAAAAAAAADAAAAAAAAAAGAAAAAEAAAAAAAAAAAAAAAQAAAAAAAAADAAAAHBvd2VyZWRfZ2F0ZQAAAAA=";
    private const string LampWiredCommand = "nAMAACQAAABDQ1dDHABGADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABQAAAAAQAAAAAAAAABAAAAAAAAAEwDAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAAAAAOADAAJAAMAAAACAAEAA4AAAAsAAAAjAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAAAAAAMAAAACAAAAAAAAAAMAAAAAAAAAAgAAAAcAAAAIAAAAAAAAAAMAAAAAAAAAAQAAAAAAAAACAAAABwAAAAgAAAAAAAAAAwAAAAAAAAAEAAAAAAAAAAEAAAABAAAABAAAAAAAAAAFAAAAJAIAADABAADAAAAAWAAAAAQAAAD4/v//AAAAAAAAAAAAAAAAAACAPwAAAACuRyG9AAAAAMD///8DAAAAAAAAABgAAAAFAAAAAAAAAAAAAAAEAAAAAAAAAAQAAABiYWxsAAAAAEj///8AAAAAAAAAAAAAAAAAAIA/AAAAAAAAAAAAAAAAYAAAADAAAAAAAAAAGAAAAAQAAAAAAAAAAAAAAAQAAAAAAAAABAAAAGxhbXAAAAAAFABAADQAMAAAACQAGAAIAAAABAAUAAAAAAAAPwAAAAAAAAAAAAAAAAAAgD8AAAAA4XokPgAAAADA/////P///wAAAAAQAAAAAwAAAAAAAAAAAAAADgAAAHByZXNzdXJlX3BsYXRlAAAYAEAAMAAsAAAAIAAUAAQAAAAAAAAAPAAYAAAAAAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAADAAAAAwAAAAAAAAAKAAAAACAAAAAAAAAAAAAAAEAAAABAAAAGwAAABIAAAAJAAAAAQAAACs////CAAAAAAA8EILAAAAcG93ZXJfbGltaXQAyP///wgAAAAAAIA/DgAAAGluaXRpYWxfY2hhcmdlAADo////CAAAAAAAgD8HAAAAZW5hYmxlZAAIAAwABAAIAAgAAAAIAAAAAABhRQgAAABjYXBhY2l0eQAAAAAHAAAAYmF0dGVyeQAYADwAMAAsAAAAIAAUAAQAAAAAAAAAOAAYAAAAAAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAAAAwAAAAAAAAABQAAAABAAAAAAAAAAQAAAAAAAAADAAAAHBvd2VyZWRfZ2F0ZQAAAAAQAAAAAQAAAAAAAAACAAAAAAAAAA==";
    private const string TwoPlateSave = "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";
    private const string TwoPlateCommand = "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";

    [Theory]
    [InlineData("two_plate")]
    [InlineData("plain")]
    [InlineData("two_kg")]
    [InlineData("lamp_wired")]
    public void ARecordPathPlateSaveIsRefusedTypedAtBothBoundaries(string name)
    {
        var (save, command) = name switch
        {
            "plain" => (PlainSave, PlainCommand),
            "two_kg" => (TwoKgSave, TwoKgCommand),
            "lamp_wired" => (LampWiredSave, LampWiredCommand),
            "two_plate" => (TwoPlateSave, TwoPlateCommand),
            _ => throw new ArgumentOutOfRangeException(nameof(name))
        };
        var (saveBytes, commandBytes) = (Convert.FromBase64String(save), Convert.FromBase64String(command));
        var refusal = SaveFixture.Refused(WorkshopSaveRejection.VerifierRejected, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, saveBytes));
        var fault = Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(commandBytes));
        Assert.Equal("This save cannot be loaded: The FlatBuffers verifier refused a CCWS buffer.", refusal.Message);
        Assert.Equal((WireFaultReason.VerifierRejected, "The FlatBuffers verifier refused a CCWC buffer."), (fault.Reason, fault.Message));
    }
}
