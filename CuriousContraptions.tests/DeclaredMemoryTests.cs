using CuriousContraptions.Abstractions;
using System.Text.Json;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>C-Memory (9A.41 + 9A.43): the declared pulse count and memory capabilities, the nodes they compile to, the deleted record
/// layout's saves (pinned base-encoder bytes) at both decode boundaries, and the authored content key the counter record carried.</summary>
// Historical command fixtures are obsolete authored envelopes and refuse structurally before instance fields.
// Current compiled field boundaries are exercised by CompiledSceneBoundaryTests; save checks retain their field-level controls.
public sealed class DeclaredMemoryTests
{
    // ---------- the declared capabilities select the nodes ----------

    [Fact]
    public void TheCounterAndTheLatchDeclareIndependentMemoryOperations()
    {
        var counter = DeclaredPhysics.Of(new PartId("counter")).Signal!;
        Assert.Equal(new CuriousContraptions.Abstractions.Signal.SignalPulseCountDescriptor(new("signal"), new("target_count")), Assert.Single(counter.Nodes));
        var latch = DeclaredPhysics.Of(new PartId("latch")).Signal!;
        Assert.Equal(new CuriousContraptions.Abstractions.Signal.SignalMemoryDescriptor(new("signal")), Assert.Single(latch.Nodes));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    public void ADeclaredPulseCountCompilesItsCounterNodeAtItsTarget(int target)
    {
        var counter = PlacedParts.Counter(new(7), 0, 1, 0, 0, 0, 0, 1, target);
        var bench = new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(counter));
        var node = Assert.Single(WorkshopActivationCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, bench).Nodes.ToArray());
        Assert.Equal(ActivationNodeKind.Counter, node.Kind);
        Assert.Equal((uint)target, node.TargetCount);
        Assert.Equal(counter.Id, node.Owner);
        Assert.Equal(bench.Instances.Identities[counter.Id].Signals[new("signal")].Node, node.Id);
        Assert.NotEqual(node.Owner.Value, node.Id.Value);
    }

    [Fact]
    public void ADeclaredMemoryCompilesOneStateLatchNode()
    {
        var latch = PlacedParts.Latch(new(4), 0, 1, 0, 0, 0, 0, 1);
        var node = Assert.Single(WorkshopActivationCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, new(new(1), WorkshopCadenceSettings.Default(), new(latch))).Nodes.ToArray());
        Assert.Equal(ActivationNodeKind.StateLatch, node.Kind);
        Assert.Equal(latch.Id, node.Owner);
    }

    // ---------- 9A.43: no type whitelist is left ----------

    /// <summary>An instance type the engine has never seen: it validates its authored configuration and declares the latch's colliders.</summary>
    private sealed record UnlistedInstance(GpuBodyId Id, bool Refuse = false) : IWorkshopInstance
    {
        public PartId PartId => new PartId("latch");
        public CellOrigin Cell => new(0, 16, 0);
        public LocalPosition Local => default;
        public CanonicalRotation Rotation => CanonicalRotation.Identity;
        public bool Locked => false;
        public IReadOnlyList<ExtractedCollider> DeclaredColliders(CuriousContraptions.Bundle.IBundleServices bundle) => DeclaredPhysics.Colliders(bundle, PartId);
        public void Validate(CuriousContraptions.Bundle.IBundleServices bundle)
        {
            if (Refuse) throw new ArgumentException("Invalid unfamiliar instance configuration.");
        }
    }

    // With the last record converted the instance collection admits an instance by its own validation (and identity uniqueness), not
    // by a list of record types: an unlisted instance type validates without the former "Unsupported authored instance declaration".
    // Control: a duplicate identity is still refused.
    [Fact]
    public void TheInstanceCollectionHasNoTypeWhitelist()
    {
        var unlisted = new UnlistedInstance(new(3));
        new WorkshopInstances(PlacedParts.Latch(new(1), 0, 1, 0, 0, 0, 0, 1), unlisted).Validate(RuntimeTestServices.Bundle);
        Assert.Equal("Invalid unfamiliar instance configuration.", Assert.Throws<ArgumentException>(() =>
            new WorkshopInstances(new UnlistedInstance(new(4), Refuse: true)).Validate(RuntimeTestServices.Bundle)).Message);
        Assert.Equal("Authored body identities must be unique.", Assert.Throws<ArgumentException>(() =>
            new WorkshopInstances(unlisted, new UnlistedInstance(new(3))).Validate(RuntimeTestServices.Bundle)).Message);
    }

    // ---------- saves written by the record path (base fc7f7cac) ----------

    // Each was written by fc7f7cac's own production save and command encoders (evidence-c-memory/baseline/old-saves, sha256 in
    // old-saves.sha256) and loaded there. C-Memory deleted target_count, so the 8-field table's slot 7 is the parameters offset: a
    // counter's target there is read as an offset, which the verifier refuses for every target except 4 and 8, which pass it as an
    // offset to an empty vector and leave the generic counter without its declared target. A base-encoder generic record wrote its
    // (empty) parameters vector in slot 8, beyond the table. Every refusal is typed at both boundaries; nothing is migrated. A latch
    // record held only its pose and lock, so it loads as the generic latch with the same pose and lock.
    private const string BeyondSave = "This save cannot be loaded: an instance record holds a field the current record does not define.";

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
    [InlineData("counter_1")]
    [InlineData("counter_2")]
    [InlineData("counter_3")]
    [InlineData("counter_4")]
    [InlineData("counter_5")]
    [InlineData("counter_6")]
    [InlineData("counter_7")]
    [InlineData("counter_8")]
    [InlineData("counter_9")]
    [InlineData("counter_7_locked_rotated")]
    public void ARecordPathCounterSaveIsRefusedTypedAtBothBoundaries(string name)
    {
        var (save, command) = Bytes(name);
        var rejected = SaveFixture.Refused(WorkshopSaveRejection.OutOfRange,
            () => WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements, save));
        Assert.False(string.IsNullOrWhiteSpace(rejected.Message));
        var fault = Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(command));
        Assert.Equal(WireFaultReason.VerifierRejected, fault.Reason);
        Assert.False(string.IsNullOrWhiteSpace(fault.Message));
    }

    // A base-encoder parameterless generic record (the lamp) wrote an empty parameters vector in slot 8, beyond the 8-field table.
    [Fact]
    public void ARecordPathParameterlessGenericSaveIsRefusedForItsFieldBeyondTheTable()
    {
        var (save, command) = Bytes("generic_lamp");
        Assert.Equal(BeyondSave, SaveFixture.Refused(WorkshopSaveRejection.OutOfRange, () => WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements, save)).Message);
        var fault = Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(command));
        Assert.Equal(WireFaultReason.VerifierRejected, fault.Reason);
        Assert.False(string.IsNullOrWhiteSpace(fault.Message));
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
            ? PlacedParts.Latch(new(1), 2.5f, 3, -1.25f, 0, (float)(Math.Sqrt(.5)), 0, (float)(Math.Sqrt(.5)), locked: true)
            : PlacedParts.Latch(new(1), 0, 3, 0, 0, 0, 0, 1);
        Assert.Equal(expected, Assert.Single(WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements, save).Construction.Instances));
        Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(command));
    }

    // ---------- authored content: the record's target_count key is gone ----------

    private static string CounterContent(bool withStaleKey) =>
        "{\"part\":\"counter\",\"cell\":[0,48,0],\"local\":[0,0,0],\"rotation\":[0,0,0,1],\"locked\":false," +
        (withStaleKey ? "\"target_count\":3," : "") +
        "\"parameters\":{\"target_count\":3},\"assistance\":[[0,0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0,0],[0,0,0,0,0,0,0,0,0,0,0]]," +
        "\"assistance_bindings\":[]}";

    // A content instance carrying the deleted target_count key is refused as an unknown field, never ignored; the same instance
    // without it parses to the counter's generic instance with its declared target (control).
    [Fact]
    public void AnAuthoredInstanceWithTheDeletedTargetCountKeyIsRefused()
    {
        var refusal = Assert.Throws<InvalidDataException>(() =>
            AuthoredInstanceReader.Parse(RuntimeTestServices.Bundle, JsonDocument.Parse(CounterContent(withStaleKey: true)).RootElement, "fixture"));
        Assert.Contains("missing or unknown field", refusal.Message, StringComparison.Ordinal);
        var template = AuthoredInstanceReader.Parse(RuntimeTestServices.Bundle, JsonDocument.Parse(CounterContent(withStaleKey: false)).RootElement, "fixture");
        var counter = Assert.IsType<WorkshopGenericInstance>(template.Create(RuntimeTestServices.Bundle, new(1), new(1f)));
        Assert.Equal((new PartId("counter"), 3f), (counter.PartId, counter.Parameters[new ParameterId("target_count")]));
    }
}
