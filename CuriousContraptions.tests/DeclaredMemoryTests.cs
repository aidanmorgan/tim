using System.Text.Json;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>C-Memory (9A.41 + 9A.43): the declared pulse count and memory capabilities, the nodes they compile to, the deleted record
/// layout's saves (pinned base-encoder bytes) at both decode boundaries, and the authored content key the counter record carried.</summary>
public sealed class DeclaredMemoryTests
{
    // ---------- the declared capabilities select the nodes ----------

    [Fact]
    public void TheCounterAndTheLatchDeclareTheirCapabilitiesAndNothingElseDoes()
    {
        var counter = DeclaredPhysics.Of(PartId.Counter).Signal!;
        Assert.Equal(new CuriousContraptions.Abstractions.Signal.SignalPulseCountDescriptor("target_count"), counter.PulseCount);
        Assert.Null(counter.Memory);
        var latch = DeclaredPhysics.Of(PartId.Latch).Signal!;
        Assert.Equal(new CuriousContraptions.Abstractions.Signal.SignalMemoryDescriptor(CuriousContraptions.Abstractions.PortSocket.SetIn,
            CuriousContraptions.Abstractions.PortSocket.ResetIn), latch.Memory);
        Assert.Null(latch.PulseCount);
        // Exactly these two packages declare either capability.
        Assert.Equal([PartId.Counter, PartId.Latch], SaveFixture.Elements.Where(part => DeclaredPhysics.Of(part).Signal is { } signal &&
            (signal.PulseCount is not null || signal.Memory is not null)).OrderBy(part => part.Value, StringComparer.Ordinal));
    }

    // The node a declared capability compiles to, field for field: the counter's target is its parameter (1 and 9, the bounds), the
    // latch is one bistable node, and a declaration with neither (the lamp, the plate) compiles no memory node here.
    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    public void ADeclaredPulseCountCompilesItsCounterNodeAtItsTarget(int target)
    {
        var counter = Assert.IsType<WorkshopGenericInstance>(PlacedParts.Counter(new(7), 0, 1, 0, 0, 0, 0, 1, target));
        var node = WorkshopActivationCompiler.MemoryNode(counter, DeclaredPhysics.Of(PartId.Counter).Signal!);
        Assert.Equal(new ActivationNodeDeclaration(new(7), new(7), ActivationNodeKind.Counter, default, 0, default, default, (uint)target), node);
        var compiled = Assert.Single(WorkshopActivationCompiler.Compile(new(new(1), WorkshopCadenceSettings.Default(), new(counter))).Nodes.ToArray());
        Assert.Equal(node, compiled);
    }

    [Fact]
    public void ADeclaredMemoryCompilesOneStateLatchNodeAndOtherDeclarationsNone()
    {
        var latch = Assert.IsType<WorkshopGenericInstance>(PlacedParts.Latch(new(4), 0, 1, 0, 0, 0, 0, 1));
        Assert.Equal(new ActivationNodeDeclaration(new(4), new(4), ActivationNodeKind.StateLatch, default),
            WorkshopActivationCompiler.MemoryNode(latch, DeclaredPhysics.Of(PartId.Latch).Signal!));
        var lamp = Assert.IsType<WorkshopGenericInstance>(PlacedParts.Lamp(new(5), 0, 1, 0, 0, 0, 0, 1));
        Assert.Null(WorkshopActivationCompiler.MemoryNode(lamp, DeclaredPhysics.Of(PartId.SignalLamp).Signal!));
        var plate = Assert.IsType<WorkshopGenericInstance>(PlacedParts.Plate(new(6), 0, 1, 0, .5f));
        Assert.Null(WorkshopActivationCompiler.MemoryNode(plate, DeclaredPhysics.Of(PartId.PressurePlate).Signal!));
    }

    // ---------- 9A.43: no type whitelist is left ----------

    /// <summary>An instance type the engine has never seen: it validates itself (counted) and declares the latch's colliders.</summary>
    private sealed record UnlistedInstance(GpuBodyId Id) : IWorkshopInstance
    {
        public int Validations { get; private set; }
        public PartId PartId => PartId.Latch;
        public CellOrigin Cell => new(0, 16, 0);
        public LocalPosition Local => default;
        public CanonicalRotation Rotation => CanonicalRotation.Identity;
        public bool Locked => false;
        public IReadOnlyList<ExtractedCollider> DeclaredColliders => DeclaredPhysics.Colliders(PartId);
        public void Validate() => Validations++;
    }

    // With the last record converted the instance collection admits an instance by its own validation (and identity uniqueness), not
    // by a list of record types: an unlisted instance type validates without the former "Unsupported authored instance declaration".
    // Control: a duplicate identity is still refused.
    [Fact]
    public void TheInstanceCollectionHasNoTypeWhitelist()
    {
        var unlisted = new UnlistedInstance(new(3));
        new WorkshopInstances(PlacedParts.Latch(new(1), 0, 1, 0, 0, 0, 0, 1), unlisted).Validate();
        Assert.Equal(1, unlisted.Validations);
        Assert.Equal("Authored body identities must be unique.", Assert.Throws<ArgumentException>(() =>
            new WorkshopInstances(unlisted, new UnlistedInstance(new(3))).Validate()).Message);
    }

    // ---------- saves written by the record path (base fc7f7cac) ----------

    // Each was written by fc7f7cac's own production save and command encoders (evidence-c-memory/baseline/old-saves, sha256 in
    // old-saves.sha256) and loaded there. C-Memory deleted target_count, so the 8-field table's slot 7 is the parameters offset: a
    // counter's target there is read as an offset, which the verifier refuses for every target except 4 and 8, which pass it as an
    // offset to an empty vector and leave the generic counter without its declared target. A base-encoder generic record wrote its
    // (empty) parameters vector in slot 8, beyond the table. Every refusal is typed at both boundaries; nothing is migrated. A latch
    // record held only its pose and lock, so it loads as the generic latch with the same pose and lock.
    private const string VerifierSave = "This save cannot be loaded: The FlatBuffers verifier refused a CCWS buffer.";
    private const string VerifierCommand = "The FlatBuffers verifier refused a CCWC buffer.";
    private const string TargetSave = "This save cannot be loaded: Invalid declared parameters.";
    private const string TargetCommand = "Invalid declared parameters.";
    private const string BeyondSave = "This save cannot be loaded: an instance record holds a field the current record does not define.";
    private const string BeyondCommand = "An instance record holds a field the current record does not define.";

    private static readonly Dictionary<string, (string Save, string Command)> OldSaves = new()
    {
        ["counter_1"] = ("xAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAAfAAAAAAAAAAAAAAAMACgAIAAIAAAABAAMAAAAJAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAQAAABgAAAAUAEAANAAwAAAAJAAYAAgAAAAEABQAAAABAAAAAAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAAAAwAAAAAAAAABAAAAABAAAAAAAAAAAAAAAHAAAAY291bnRlcgA=",
            "FAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAAMQAAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAoACAACAAAAAQADAAAACQAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAEAAAAYAAAAFAA8ADQAMAAAACQAGAAIAAAABAAUAAAAAQAAAAAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAAAAAAAAMAAAAAAAAAAMAAAAAQAAAAAAAAAHAAAAY291bnRlcgAQAAAAAQAAAAAAAAACAAAAAAAAAA=="),
        ["counter_2"] = ("xAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAAfAAAAAAAAAAAAAAAMACgAIAAIAAAABAAMAAAAJAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAQAAABgAAAAUAEAANAAwAAAAJAAYAAgAAAAEABQAAAACAAAAAAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAAAAwAAAAAAAAABAAAAABAAAAAAAAAAAAAAAHAAAAY291bnRlcgA=",
            "FAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAAMQAAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAoACAACAAAAAQADAAAACQAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAEAAAAYAAAAFAA8ADQAMAAAACQAGAAIAAAABAAUAAAAAgAAAAAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAAAAAAAAMAAAAAAAAAAMAAAAAQAAAAAAAAAHAAAAY291bnRlcgAQAAAAAQAAAAAAAAACAAAAAAAAAA=="),
        ["counter_3"] = ("xAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAAfAAAAAAAAAAAAAAAMACgAIAAIAAAABAAMAAAAJAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAQAAABgAAAAUAEAANAAwAAAAJAAYAAgAAAAEABQAAAADAAAAAAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAAAAwAAAAAAAAABAAAAABAAAAAAAAAAAAAAAHAAAAY291bnRlcgA=",
            "FAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAAMQAAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAoACAACAAAAAQADAAAACQAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAEAAAAYAAAAFAA8ADQAMAAAACQAGAAIAAAABAAUAAAAAwAAAAAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAAAAAAAAMAAAAAAAAAAMAAAAAQAAAAAAAAAHAAAAY291bnRlcgAQAAAAAQAAAAAAAAACAAAAAAAAAA=="),
        ["counter_4"] = ("xAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAAfAAAAAAAAAAAAAAAMACgAIAAIAAAABAAMAAAAJAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAQAAABgAAAAUAEAANAAwAAAAJAAYAAgAAAAEABQAAAAEAAAAAAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAAAAwAAAAAAAAABAAAAABAAAAAAAAAAAAAAAHAAAAY291bnRlcgA=",
            "FAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAAMQAAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAoACAACAAAAAQADAAAACQAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAEAAAAYAAAAFAA8ADQAMAAAACQAGAAIAAAABAAUAAAABAAAAAAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAAAAAAAAMAAAAAAAAAAMAAAAAQAAAAAAAAAHAAAAY291bnRlcgAQAAAAAQAAAAAAAAACAAAAAAAAAA=="),
        ["counter_5"] = ("xAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAAfAAAAAAAAAAAAAAAMACgAIAAIAAAABAAMAAAAJAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAQAAABgAAAAUAEAANAAwAAAAJAAYAAgAAAAEABQAAAAFAAAAAAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAAAAwAAAAAAAAABAAAAABAAAAAAAAAAAAAAAHAAAAY291bnRlcgA=",
            "FAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAAMQAAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAoACAACAAAAAQADAAAACQAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAEAAAAYAAAAFAA8ADQAMAAAACQAGAAIAAAABAAUAAAABQAAAAAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAAAAAAAAMAAAAAAAAAAMAAAAAQAAAAAAAAAHAAAAY291bnRlcgAQAAAAAQAAAAAAAAACAAAAAAAAAA=="),
        ["counter_6"] = ("xAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAAfAAAAAAAAAAAAAAAMACgAIAAIAAAABAAMAAAAJAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAQAAABgAAAAUAEAANAAwAAAAJAAYAAgAAAAEABQAAAAGAAAAAAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAAAAwAAAAAAAAABAAAAABAAAAAAAAAAAAAAAHAAAAY291bnRlcgA=",
            "FAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAAMQAAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAoACAACAAAAAQADAAAACQAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAEAAAAYAAAAFAA8ADQAMAAAACQAGAAIAAAABAAUAAAABgAAAAAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAAAAAAAAMAAAAAAAAAAMAAAAAQAAAAAAAAAHAAAAY291bnRlcgAQAAAAAQAAAAAAAAACAAAAAAAAAA=="),
        ["counter_7"] = ("xAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAAfAAAAAAAAAAAAAAAMACgAIAAIAAAABAAMAAAAJAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAQAAABgAAAAUAEAANAAwAAAAJAAYAAgAAAAEABQAAAAHAAAAAAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAAAAwAAAAAAAAABAAAAABAAAAAAAAAAAAAAAHAAAAY291bnRlcgA=",
            "FAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAAMQAAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAoACAACAAAAAQADAAAACQAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAEAAAAYAAAAFAA8ADQAMAAAACQAGAAIAAAABAAUAAAABwAAAAAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAAAAAAAAMAAAAAAAAAAMAAAAAQAAAAAAAAAHAAAAY291bnRlcgAQAAAAAQAAAAAAAAACAAAAAAAAAA=="),
        ["counter_8"] = ("xAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAAfAAAAAAAAAAAAAAAMACgAIAAIAAAABAAMAAAAJAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAQAAABgAAAAUAEAANAAwAAAAJAAYAAgAAAAEABQAAAAIAAAAAAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAAAAwAAAAAAAAABAAAAABAAAAAAAAAAAAAAAHAAAAY291bnRlcgA=",
            "FAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAAMQAAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAoACAACAAAAAQADAAAACQAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAEAAAAYAAAAFAA8ADQAMAAAACQAGAAIAAAABAAUAAAACAAAAAAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAAAAAAAAMAAAAAAAAAAMAAAAAQAAAAAAAAAHAAAAY291bnRlcgAQAAAAAQAAAAAAAAACAAAAAAAAAA=="),
        ["counter_9"] = ("xAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAAfAAAAAAAAAAAAAAAMACgAIAAIAAAABAAMAAAAJAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAQAAABgAAAAUAEAANAAwAAAAJAAYAAgAAAAEABQAAAAJAAAAAAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAAAAwAAAAAAAAABAAAAABAAAAAAAAAAAAAAAHAAAAY291bnRlcgA=",
            "FAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAAMQAAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAoACAACAAAAAQADAAAACQAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAEAAAAYAAAAFAA8ADQAMAAAACQAGAAIAAAABAAUAAAACQAAAAAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAAAAAAAAMAAAAAAAAAAMAAAAAQAAAAAAAAAHAAAAY291bnRlcgAQAAAAAQAAAAAAAAACAAAAAAAAAA=="),
        ["counter_7_locked_rotated"] = ("zAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAAfAAAAAAAAAAAAAAAMACwAIAAIAAAABAAMAAAAKAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAAAAAAEAAAAYAAAAFABEADgANAAzACQAGAAIAAAABAAUAAAABwAAAAAAAAD0BDU/AAAAAPQENT8AAAAAAAAAAAAAAAAoAAAAMAAAAOz///8AAAABEAAAAAEAAAAAAAAAAAAAAAcAAABjb3VudGVyAA==",
            "HAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAAMwAAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAsACAACAAAAAQADAAAACgAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAAAAAABAAAAGAAAABQAQAA4ADQAMwAkABgACAAAAAQAFAAAAAcAAAAAAAAA9AQ1PwAAAAD0BDU/AAAAAAAAAAAAAAAAKAAAADAAAADs////AAAAAQwAAAABAAAAAAAAAAcAAABjb3VudGVyABAAAAABAAAAAAAAAAIAAAAAAAAA"),
        ["latch"] = ("vAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAAfAAAAAAAAAAAAAAAMACgAIAAIAAAABAAMAAAAJAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAQAAABQAAAAQADwAMAAsAAAAIAAUAAQAEAAAAAAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAAAAAAAAMAAAAAAAAAAQAAAAAQAAAAAAAAAAAAAABQAAAGxhdGNoAAAA",
            "DAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAALwAAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAoACAACAAAAAQADAAAACQAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAEAAAAUAAAAEAA4ADAALAAAACAAFAAEABAAAAAAAAAAAAAAAAAAAAAAAIA/AAAAAAAAAAAAAAAAAAAAADAAAAAAAAAADAAAAAEAAAAAAAAABQAAAGxhdGNoAAAAEAAAAAEAAAAAAAAAAgAAAAAAAAA="),
        ["latch_locked_rotated"] = ("xAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAAfAAAAAAAAAAAAAAAMACwAIAAIAAAABAAMAAAAKAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAAAAAAEAAAAUAAAAEABAADQAMAAvACAAFAAEABAAAAAAAAAA9AQ1PwAAAAD0BDU/AAAAAAAAAAAAAAAAKAAAADAAAADs////AAAAARAAAAABAAAAAAAAAAAAAAAFAAAAbGF0Y2gAAAA=",
            "FAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAAMQAAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAsACAACAAAAAQADAAAACgAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAAAAAABAAAAFAAAABAAPAA0ADAALwAgABQABAAQAAAAAAAAAPQENT8AAAAA9AQ1PwAAAAAAAAAAAAAAACgAAAAwAAAA7P///wAAAAEMAAAAAQAAAAAAAAAFAAAAbGF0Y2gAAAAQAAAAAQAAAAAAAAACAAAAAAAAAA=="),
        ["generic_lamp"] = ("zAAAABQAAABDQ1dTAAAAAAgAFAAIAAQACAAAABwAAAAfAAAAAAAAAAAAAAAMACgAIAAIAAAABAAMAAAAJAAAAAIAAAABAAAAAgAAAAIAAAA8AAAAAQAAAAIAAAAAAAAAAQAAABwAAAAAABYAQAAwACwAAAAgABQABAAAAAAAPAAWAAAAAAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAAAAwAAAAAAAAABgAAAABAAAAAAAAAAAAAAAEAAAAAAAAAAQAAABsYW1wAAAAAA==",
            "HAEAACQAAABDQ1dDHABEADgANAAoACAAHAAYABAACAAAAAAAAAAEABwAAABMAAAAAQAAAAAAAAABAAAAAAAAAMwAAAACAAAAAQAAAAAAAAABAAAAAAAAAAAAAAABAAAAAQAAAAAAAAAAAAAADAAoACAACAAAAAQADAAAACQAAAACAAAAAQAAAAIAAAACAAAAPAAAAAEAAAACAAAAAAAAAAEAAAAcAAAAAAAWADwAMAAsAAAAIAAUAAQAAAAAADgAFgAAAAAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAAAAAAAAMAAAAAAAAAAUAAAAAQAAAAAAAAAEAAAAAAAAAAQAAABsYW1wAAAAABAAAAABAAAAAAAAAAIAAAAAAAAA"),
    };

    private static (byte[] Save, byte[] Command) Bytes(string name)
    {
        var (save, command) = (Convert.FromBase64String(OldSaves[name].Save), Convert.FromBase64String(OldSaves[name].Command));
        return (save, command);
    }

    // Every admitted target 1–9 of a base-encoder counter (and a locked, rotated target 7) is refused typed at both boundaries.
    [Theory]
    [InlineData("counter_1", false)]
    [InlineData("counter_2", false)]
    [InlineData("counter_3", false)]
    [InlineData("counter_4", true)]
    [InlineData("counter_5", false)]
    [InlineData("counter_6", false)]
    [InlineData("counter_7", false)]
    [InlineData("counter_8", true)]
    [InlineData("counter_9", false)]
    [InlineData("counter_7_locked_rotated", false)]
    public void ARecordPathCounterSaveIsRefusedTypedAtBothBoundaries(string name, bool emptyVector)
    {
        var (save, command) = Bytes(name);
        var (saveReason, saveMessage, commandReason, commandMessage) = emptyVector
            ? (WorkshopSaveRejection.OutOfRange, TargetSave, WireFaultReason.OutOfRange, TargetCommand)
            : (WorkshopSaveRejection.VerifierRejected, VerifierSave, WireFaultReason.VerifierRejected, VerifierCommand);
        Assert.Equal(saveMessage, SaveFixture.Refused(saveReason, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, save)).Message);
        var fault = Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(command));
        Assert.Equal((commandReason, commandMessage), (fault.Reason, fault.Message));
    }

    // A base-encoder parameterless generic record (the lamp) wrote an empty parameters vector in slot 8, beyond the 8-field table.
    [Fact]
    public void ARecordPathParameterlessGenericSaveIsRefusedForItsFieldBeyondTheTable()
    {
        var (save, command) = Bytes("generic_lamp");
        Assert.Equal(BeyondSave, SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(SaveFixture.Elements, save)).Message);
        var fault = Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(command));
        Assert.Equal((WireFaultReason.OutOfRange, BeyondCommand), (fault.Reason, fault.Message));
    }

    // A base-encoder latch record (pose and lock only) loads at both boundaries as the generic latch with that same pose and lock.
    [Theory]
    [InlineData("latch", false)]
    [InlineData("latch_locked_rotated", true)]
    public void ARecordPathLatchSaveLoadsAsTheGenericLatchWithItsPoseAtBothBoundaries(string name,
        bool lockedRotated)
    {
        var (save, command) = Bytes(name);
        var expected = lockedRotated
            ? PlacedParts.Latch(new(1), 2.5, 3, -1.25, 0, Math.Sqrt(.5), 0, Math.Sqrt(.5), locked: true)
            : PlacedParts.Latch(new(1), 0, 3, 0, 0, 0, 0, 1);
        Assert.Equal(expected, Assert.Single(WorkshopSaveCodec.Decode(SaveFixture.Elements, save).Construction.Instances));
        Assert.Equal(expected, Assert.Single(WorkshopWireFlatBuffers.DecodeCommand(command).Construction!.Value.Instances));
    }

    // ---------- authored content: the record's target_count key is gone ----------

    private static string CounterContent(bool withStaleKey) =>
        "{\"part\":\"counter\",\"cell\":[0,48,0],\"local\":[0,0,0],\"rotation\":[0,0,0,1],\"locked\":false,\"scalars\":{\"Length\":0}," +
        (withStaleKey ? "\"target_count\":3," : "") +
        "\"parameters\":{\"target_count\":3},\"assistance\":[[0,0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0,0]]," +
        "\"assistance_bindings\":[]}";

    // A content instance carrying the deleted target_count key is refused as an unknown field, never ignored; the same instance
    // without it parses to the counter's generic instance with its declared target (control).
    [Fact]
    public void AnAuthoredInstanceWithTheDeletedTargetCountKeyIsRefused()
    {
        var refusal = Assert.Throws<InvalidDataException>(() =>
            AuthoredInstanceReader.Parse(JsonDocument.Parse(CounterContent(withStaleKey: true)).RootElement, "fixture"));
        Assert.Contains("missing or unknown field", refusal.Message, StringComparison.Ordinal);
        var template = AuthoredInstanceReader.Parse(JsonDocument.Parse(CounterContent(withStaleKey: false)).RootElement, "fixture");
        var counter = Assert.IsType<WorkshopGenericInstance>(template.Create(new(1), new(1f)));
        Assert.Equal((PartId.Counter, 3f), (counter.PartId, counter.Parameters[new ParameterId("target_count")]));
    }
}
