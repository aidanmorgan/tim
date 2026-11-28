using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using Google.FlatBuffers;
using W = CuriousContraptions.Wire;

namespace CuriousContraptions.Tests;

/// <summary>
/// S5c-2 cross-language ABI conformance (rows S3 to S7): C# production codecs and the TypeScript bindings generated from the one
/// schema file (<c>schemas/workshop_wire.fbs</c>) agree on every root identifier, every command kind, every response kind, every
/// observation payload arm and the save root, in both directions, and refuse malformed frames for the same check.
/// </summary>
public sealed class WireConformanceTests
{
    private const ulong FirstWidePublication = 1UL << 53;
    private const ulong WideExpectedRevision = (1UL << 53) + 1;
    private const float MovingSpeedMetresPerSecond = 3f;
    private const float MovingMidpointMetres = MovingSpeedMetresPerSecond * .5f / PhysicalTime.Frequency;

    private static readonly string Root = twodog.Engine.ResolveProjectDir();
    private static readonly RuntimeSessionId Session = new(0x0102030405060708UL, 0x1112131415161718UL);
    private static readonly byte[] SessionBytes = SessionBytesOf(Session);

    private static byte[] SessionBytesOf(RuntimeSessionId session)
    {
        var bytes = new byte[RuntimeSessionId.ByteLength];
        session.WriteTo(bytes);
        return bytes;
    }

    // ----- S3: root identifiers are generated schema data both sides agree on --------------------------------------------

    [Fact]
    public void EveryRootIdentifierIsGeneratedSchemaDataBothSidesAgreeOn()
    {
        var members = Enum.GetValues<W.WireRootIdentifier>();
        Assert.Equal(4, members.Length);

        const uint probe = 0xfe018000;
        var response = RunConformance([new { op = "identifiers", probe }])[0];
        Assert.True(response.GetProperty("ok").GetBoolean());
        var tsIdentifiers = response.GetProperty("identifiers");

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in members)
        {
            var name = member.ToString();
            var csharpId = WorkshopWireFlatBuffers.FileIdentifier(member);
            Assert.Equal(4, csharpId.Length);
            Assert.All(csharpId, ch => Assert.InRange(ch, (char)0x20, (char)0x7E));
            Assert.True(seenIds.Add(csharpId), $"Duplicate identifier {csharpId} for {name}");
            Assert.Equal(csharpId, tsIdentifiers.GetProperty(name).GetString());
        }
        Assert.Equal(members.Length, tsIdentifiers.EnumerateObject().Count());
        Assert.Equal("\0\u0080\u0001\u00fe", response.GetProperty("probe").GetString());

        var schema = WireSchemaText.Read(Path.Combine(Root, "schemas", "workshop_wire.fbs"));
        Assert.Equal(schema.FileIdentifier, WorkshopWireFlatBuffers.FileIdentifier(W.WireRootIdentifier.SavedConstruction));

        var save = SampleSave();
        var saveBytes = EncodeSave(save);
        RuntimeTestServices.Core.Codecs.Commands.ValidateSaveFrame(saveBytes);

        var refinished = RunConformance([new
        {
            op = "roundtrip",
            root = nameof(W.WireRootIdentifier.SavedConstruction),
            asRoot = nameof(W.WireRootIdentifier.WorkshopCommand),
            hex = Convert.ToHexString(saveBytes)
        }])[0];
        Assert.True(refinished.GetProperty("ok").GetBoolean());
        var wrongBytes = Convert.FromHexString(refinished.GetProperty("hex").GetString()!);
        var fault = Assert.Throws<WireFaultException>(() => RuntimeTestServices.Core.Codecs.Commands.ValidateSaveFrame(wrongBytes));
        Assert.Equal(WireFaultReason.UnknownIdentifier, fault.Reason);
        var rejected = RunConformance([DecodeRequest(W.WireRootIdentifier.SavedConstruction, wrongBytes)])[0];
        Assert.False(rejected.GetProperty("ok").GetBoolean());
        Assert.Equal(fault.Reason, MapTypeScriptErrorToReason(rejected.GetProperty("error").GetString()!));
    }

    public static TheoryData<WorkshopCommandKind, ExpectedRevisionKind> CommandKinds()
    {
        var data = new TheoryData<WorkshopCommandKind, ExpectedRevisionKind>();
        foreach (var kind in Enum.GetValues<WorkshopCommandKind>())
            data.Add(kind, ExpectedRevisionKind.Exact);
        data.Add(WorkshopCommandKind.Run, ExpectedRevisionKind.Any);
        return data;
    }
    public enum ResponseCase { Acknowledgement, RefusedAcknowledgement, AdmissionRead, CommittedRead, MovingRead, PopulatedRead, MemoryAcknowledgement }
    public static TheoryData<ResponseCase> ResponseCases => new(Enum.GetValues<ResponseCase>());
    public static TheoryData<W.ObservationPayload> ObservationArms =>
        new(Enum.GetValues<W.ObservationPayload>().Where(arm => arm != W.ObservationPayload.None));

    [Theory]
    [MemberData(nameof(CommandKinds))]
    public void TypeScriptDecodesEveryCommandCSharpEncoded(WorkshopCommandKind kind, ExpectedRevisionKind revisionKind)
    {
        var command = MakeCommand(kind, revisionKind);
        var value = DecodeInTypeScript(W.WireRootIdentifier.WorkshopCommand, WorkshopWireFlatBuffers.EncodeCommand(command));
        var errors = new List<string>();
        ExpectUInt(errors, "kind", (uint)kind, value);
        ExpectUInt64(errors, "sequence", command.Sequence.Value, value);
        ExpectUInt64(errors, "revision", command.Revision.Value, value);
        ExpectUInt(errors, "revisionKind", (uint)command.RevisionKind, value);
        ExpectUInt64(errors, "cadence", command.Cadence.Value, value);
        ExpectUInt64(errors, "projection", command.Projection.Value, value);
        ExpectBytes(errors, "session", SessionBytes, value);
        if (command.Scene is { } scene)
            ExpectUInt64(errors, "identity", scene.Identity.Value, value.GetProperty("scene"));
        else
            Assert.Equal(JsonValueKind.Null, value.GetProperty("scene").ValueKind);
        if (command.RunControl is { } control)
            ExpectF32(errors, "scalar", control.Value.Scalar, value.GetProperty("runControl"));
        if (command.Target is { } target)
            ExpectUInt64(errors, "sequence", target.Sequence.Value, value.GetProperty("target"));
        if (command.Cut is { } span)
            ExpectUInt64(errors, "cutSpan", span.Value, value);
        Assert.Empty(errors);
    }

    [Theory]
    [MemberData(nameof(ResponseCases))]
    public void TypeScriptDecodesEveryResponseCSharpEncoded(ResponseCase sample)
    {
        var response = ResponseSample(sample);
        if (sample == ResponseCase.CommittedRead)
            Assert.NotEqual(0UL, response.Read.Revision.Value);
        var value = DecodeInTypeScript(W.WireRootIdentifier.WorkshopResponse, WorkshopWireFlatBuffers.EncodeResponse(response));
        var errors = new List<string>();
        CheckResponseFields(errors, response, value);
        Assert.Empty(errors);
    }

    private static void CheckResponseFields(List<string> errors, WorkshopResponse response, JsonElement value)
    {
        ExpectUInt(errors, "kind", (uint)response.Kind, value);
        ExpectUInt(errors, "outcome", (uint)response.Result.Outcome, value);
        ExpectUInt(errors, "reason", (uint)response.Result.Reason, value);
        ExpectUInt64(errors, "sequence", response.Sequence.Value, value);
        ExpectUInt64(errors, "publication", response.Publication.Value, value);
        ExpectUInt64(errors, "revision", response.Read.Revision.Value, value);
        ExpectUInt64(errors, "cadence", response.Cadence.Value, value);
        ExpectUInt64(errors, "projection", response.Projection.Value, value);
        ExpectBytes(errors, "session", SessionBytesOf(response.Session), value);
        var bodies = value.GetProperty("bodies");
        Assert.Equal(response.Read.Bodies.Count, bodies.GetArrayLength());
        if (response.Read.Bodies.Count > 0)
        {
            var body = response.Read.Bodies[0].Body;
            ExpectUInt64(errors, "id", body.Id.Value, bodies[0]);
            ExpectF32(errors, "x", body.Local.X, bodies[0].GetProperty("local"));
        }
        var motion = value.GetProperty("motion");
        if (response.Read.Motion is { } expectedMotion)
            Assert.Equal(expectedMotion.Bytes.Length, motion.GetArrayLength());
        else
            Assert.Equal(JsonValueKind.Null, motion.ValueKind);
        CheckResponsePayloads(errors, response, value);
    }

    private static void CheckResponsePayloads(List<string> errors, WorkshopResponse response, JsonElement value)
    {
        if (response.Scene is { } scene)
            ExpectUInt64(errors, "identity", scene.Identity.Value, value.GetProperty("scene"));
        if (response.Memory is { } memory)
            ExpectUInt64(errors, "usedBefore", (ulong)memory.UsedBefore, value.GetProperty("memory"));
        if (response.Read.Electrical.Count > 0)
        {
            var source = response.Read.Electrical[0];
            var electrical = Assert.Single(value.GetProperty("electrical").EnumerateArray());
            ExpectUInt64(errors, "id", source.Id.Value, electrical);
            ExpectUInt64(errors, "owner", source.Owner.Value, electrical);
            ExpectF32(errors, "remainingBulk", source.Balance.Bulk, electrical);
        }
        if (response.Read.EnergyStores.Count > 0)
        {
            var expected = response.Read.EnergyStores[0];
            var store = Assert.Single(value.GetProperty("energyStores").EnumerateArray());
            ExpectUInt64(errors, "id", expected.Id.Value, store);
            ExpectUInt64(errors, "owner", expected.Owner.Value, store);
            ExpectF32(errors, "bulk", expected.Balance.Bulk, store);
            ExpectF32(errors, "commitReleased", expected.CommitReleased.Value, store);
            ExpectUInt(errors, "transactions", expected.Transactions, store);
        }
        CheckRopeRows(errors, response.Read.Presentation, value);
        var coordinateTravel = value.GetProperty("coordinateTravel");
        Assert.Equal(response.Read.CoordinateTravelObservers.Count, coordinateTravel.GetArrayLength());
        for (var i = 0; i < response.Read.CoordinateTravelObservers.Count; i++)
        {
            var expected = response.Read.CoordinateTravelObservers[i];
            var travel = coordinateTravel[i];
            ExpectUInt64(errors, "id", expected.Id.Value, travel);
            ExpectUInt64(errors, "owner", expected.Owner.Value, travel);
            ExpectF32(errors, "threshold", expected.Threshold, travel);
            ExpectF32(errors, "travel", expected.Travel, travel);
            ExpectUInt(errors, "phase", (uint)expected.Phase, travel);
            ExpectUInt(errors, "ordinal", expected.Ordinal, travel);
        }
        if (!response.Read.Presentation.Loads.IsEmpty)
        {
            var expected = response.Read.Presentation.Loads[0];
            var load = Assert.Single(value.GetProperty("contactLoads").EnumerateArray());
            ExpectUInt64(errors, "sensor", expected.Sensor.Value, load);
            ExpectUInt64(errors, "frame", expected.Frame.Value, load);
            ExpectUInt64(errors, "region", expected.Region.Value, load);
            ExpectF32(errors, "measuredMass", expected.MeasuredMass, load);
            ExpectUInt(errors, "phase", (uint)expected.Phase, load);
        }
    }

    private static void CheckRopeRows(List<string> errors, PhysicsPresentationRead presentation, JsonElement value)
    {
        var rows = value.GetProperty("ropes");
        Assert.Equal(presentation.Ropes.Length, rows.GetArrayLength());
        if (presentation.Ropes.IsEmpty)
            return;
        var expected = presentation.Ropes[0];
        var rope = Assert.Single(rows.EnumerateArray());
        ExpectUInt64(errors, "id", expected.Id.Value, rope);
        ExpectUInt64(errors, "route", expected.Route.Value, rope);
        ExpectUInt64(errors, "bodyA", expected.BodyA.Value, rope);
        ExpectUInt64(errors, "bodyB", expected.BodyB.Value, rope);
        ExpectUInt(errors, "order", expected.Order, rope);
        ExpectUInt(errors, "integrity", (uint)expected.Integrity, rope);
        ExpectUInt(errors, "state", (uint)expected.State, rope);
        ExpectUInt64(errors, "cutSpan", expected.CutSpan.Value, rope);
        ExpectF32(errors, "maximumLength", expected.MaximumLength, rope);
        ExpectF32(errors, "currentLength", expected.CurrentLength, rope);
        ExpectF32(errors, "x", expected.AttachmentA.X, rope.GetProperty("attachmentA"));
        ExpectF32(errors, "x", expected.AttachmentB.X, rope.GetProperty("attachmentB"));
    }

    [Theory]
    [MemberData(nameof(ObservationArms))]
    public void TypeScriptDecodesEveryObservationArmCSharpEncoded(W.ObservationPayload arm)
    {
        var batch = MakeObservationBatch(arm);
        var value = DecodeInTypeScript(W.WireRootIdentifier.WorkshopObservationBatch, WorkshopWireFlatBuffers.EncodeObservations(batch));
        var errors = new List<string>();
        ExpectBytes(errors, "session", SessionBytes, value);
        ExpectUInt(errors, "role", (uint)batch.Role, value);
        var record = Assert.Single(value.GetProperty("records").EnumerateArray());
        ExpectUInt(errors, "kind", (uint)arm, record);
        CheckObservationArm(errors, record, Assert.Single(batch.Records));
        Assert.Empty(errors);
    }

    private static void CheckObservationArm(List<string> errors, JsonElement record, WorkshopDiagnostic diagnostic)
    {
        switch (diagnostic)
        {
            case WorkshopFaultDiagnostic value:
                Assert.Equal(value.Detail, record.GetProperty("fault").GetProperty("detail").GetString());
                break;
            case ClockRecalibrationDiagnostic value:
                ExpectUInt64(errors, "displayEpoch", value.DisplayEpoch, record.GetProperty("recalibration"));
                break;
            case ClockReplyRejectedDiagnostic value:
                Assert.Equal(value.Detail, record.GetProperty("rejectedClock").GetProperty("detail").GetString());
                break;
            case ClockLateRenewalDiagnostic value:
                ExpectUInt64(errors, "count", value.Count, record.GetProperty("lateRenewal"));
                break;
            case ClockWideOffsetDiagnostic value:
                var wide = record.GetProperty("wideOffset");
                ExpectUInt64(errors, "count", value.Count, wide);
                ExpectInt64(errors, "offset", value.Offset, wide);
                ExpectInt64(errors, "admittedLower", value.AdmittedLower, wide);
                ExpectInt64(errors, "admittedUpper", value.AdmittedUpper, wide);
                break;
            case ClockDerivationDiagnostic value:
                ExpectUInt64(errors, "checks", value.Checks, record.GetProperty("derivation"));
                break;
            case AnimationDeferralDiagnostic value:
                ExpectUInt64(errors, "count", value.Count, record.GetProperty("deferral"));
                break;
            case WorkshopDebtDiagnostic value:
                ExpectUInt64(errors, "tick", value.CommittedTick.Value, record.GetProperty("debt"));
                break;
            case WorkshopTickFailureDiagnostic value:
                ExpectUInt(errors, "fault", (uint)value.Fault, record.GetProperty("tickFailure"));
                break;
            case WorkshopTransferDiagnostic value:
                var bytes = record.GetProperty("transfer").GetProperty("response").EnumerateArray()
                    .Select(octet => checked((byte)ReadJsNumber(octet))).ToArray();
                var response = DecodeInTypeScript(W.WireRootIdentifier.WorkshopResponse, bytes);
                CheckResponseFields(errors, value.Response, response);
                break;
            case WorkshopCaptureDiagnostic value:
                ExpectUInt64(errors, "identity", value.Identity, record.GetProperty("capture"));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(diagnostic));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TypeScriptDecodesTheSaveCSharpEncoded(bool authored)
    {
        var save = SampleSave(authored);
        var value = DecodeInTypeScript(W.WireRootIdentifier.SavedConstruction, EncodeSave(save));
        var errors = new List<string>();
        ExpectUInt64(errors, "nextBodyId", save.NextBodyId.Value, value);
        var construction = value.GetProperty("construction");
        var instances = construction.GetProperty("instances");
        Assert.Equal(save.Construction.Instances.Count, instances.GetArrayLength());
        var instance = instances[0];
        ExpectUInt64(errors, "id", save.Construction.Instances[0].Id.Value, instance);
        ExpectF32(errors, "x", save.Construction.Instances[0].Local.X, instance.GetProperty("local"));
        if (authored)
            CheckAuthoredSave(errors, save.Construction, construction);
        Assert.Empty(errors);

        ExpectUInt64(errors, "nextBodyId", 42, value);
        Assert.Contains("nextBodyId", Assert.Single(errors), StringComparison.Ordinal);
    }

    private static void CheckAuthoredSave(List<string> errors, WorkshopConstruction expected, JsonElement construction)
    {
        var puzzle = construction.GetProperty("puzzle");
        Assert.Equal(expected.Puzzle.Id.Value, puzzle.GetProperty("id").GetString());
        ExpectUInt(errors, "placement", (uint)expected.Puzzle.Placement, puzzle);
        ExpectF32(errors, "precision", expected.Puzzle.Precision.Value, puzzle);
        ExpectUInt(errors, "goalKind", (uint)expected.Puzzle.Goal.Kind, puzzle);
        var connections = construction.GetProperty("connections");
        Assert.NotEmpty(expected.Connections);
        Assert.Equal(expected.Connections.Count, connections.GetArrayLength());
        for (var i = 0; i < expected.Connections.Count; i++)
        {
            var link = expected.Connections[i];
            ExpectUInt64(errors, "source", link.Source.Value, connections[i]);
            ExpectUInt64(errors, "target", link.Target.Value, connections[i]);
            ExpectUInt(errors, "domain", (uint)link.Domain, connections[i]);
            ExpectUInt(errors, "fromSocket", SocketIndex(expected.Instances, link.Source, link.Output), connections[i]);
            ExpectUInt(errors, "toSocket", SocketIndex(expected.Instances, link.Target, link.Input), connections[i]);
        }
    }

    private static uint SocketIndex(WorkshopInstances instances, GpuBodyId owner, PortId socket)
    {
        var part = instances.Single(instance => instance.Id == owner).PartId;
        var ports = DeclaredPorts.Current.Of(part).ToArray();
        return checked((uint)Array.FindIndex(ports, port => port.Id == socket));
    }

    [Theory]
    [MemberData(nameof(CommandKinds))]
    public void CSharpDecodesEveryCommandTypeScriptEncoded(WorkshopCommandKind kind, ExpectedRevisionKind revisionKind)
    {
        var command = MakeCommand(kind, revisionKind);
        var (unchanged, changed) = RoundTrip(W.WireRootIdentifier.WorkshopCommand, WorkshopWireFlatBuffers.EncodeCommand(command),
            "sequence", new { i64 = "999" });
        var original = WorkshopWireFlatBuffers.DecodeCommand(unchanged);
        var edited = WorkshopWireFlatBuffers.DecodeCommand(changed);
        Assert.Equivalent(command, original, strict: true);
        Assert.Equivalent(command with { Sequence = new(999) }, edited, strict: true);
        if (kind == WorkshopCommandKind.ConfigureRunControl)
        {
            // The scalar is an admitted -0. CLR equality alone would accept a decoded +0.
            Assert.Equal(0x80000000u, BitConverter.SingleToUInt32Bits(original.RunControl!.Value.Value.Scalar));
            Assert.Equal(0x80000000u, BitConverter.SingleToUInt32Bits(edited.RunControl!.Value.Value.Scalar));
        }
    }

    [Fact]
    public void CSharpRefusesANonzeroAnyRevisionEncodedByTypeScript()
    {
        var command = MakeCommand(WorkshopCommandKind.Run, ExpectedRevisionKind.Any);
        var (unchanged, changed) = RoundTrip(W.WireRootIdentifier.WorkshopCommand, WorkshopWireFlatBuffers.EncodeCommand(command),
            "revision", new { i64 = WideExpectedRevision.ToString(CultureInfo.InvariantCulture) });
        Assert.Equivalent(command, WorkshopWireFlatBuffers.DecodeCommand(unchanged), strict: true);
        var value = DecodeInTypeScript(W.WireRootIdentifier.WorkshopCommand, changed);
        var errors = new List<string>();
        ExpectUInt64(errors, "revision", WideExpectedRevision, value);
        ExpectUInt(errors, "revisionKind", (uint)ExpectedRevisionKind.Any, value);
        Assert.Empty(errors);
        var refusal = Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.DecodeCommand(changed));
        Assert.Equal("Unsupported expected revision.", refusal.Message);
    }

    [Theory]
    [MemberData(nameof(ResponseCases))]
    public void CSharpDecodesEveryResponseTypeScriptEncoded(ResponseCase sample)
    {
        var response = ResponseSample(sample);
        var bytes = WorkshopWireFlatBuffers.EncodeResponse(response);
        var (unchanged, changed) = response.Kind == WorkshopResponseKind.Read
            ? RoundTrip(W.WireRootIdentifier.WorkshopResponse, bytes, "cadence", new { i64 = "19" })
            : RoundTrip(W.WireRootIdentifier.WorkshopResponse, bytes, "phase", (uint)WorkshopSimulationPhase.Paused);
        var expected = response.Kind == WorkshopResponseKind.Read
            ? response with { Cadence = new(19) }
            : response with { Phase = WorkshopSimulationPhase.Paused };
        var midpoint = sample == ResponseCase.MovingRead ? MovingMidpointMetres : 0f;
        AssertResponse(response, WorkshopWireFlatBuffers.DecodeResponse(unchanged), midpoint);
        AssertResponse(expected, WorkshopWireFlatBuffers.DecodeResponse(changed), midpoint);
    }

    [Fact]
    public void TheTypeScriptHarnessExplicitlyRefusesAMotionEdit()
    {
        var frame = WorkshopWireFlatBuffers.EncodeResponse(WireFaultTests.PhysicalRead());
        var result = Assert.Single(RunConformance([new
        {
            op = "roundtrip",
            root = nameof(W.WireRootIdentifier.WorkshopResponse),
            hex = Convert.ToHexString(frame),
            mutate = new { field = "motion", value = Array.Empty<int>() }
        }]));
        Assert.False(result.GetProperty("ok").GetBoolean());
        Assert.Equal("Response motion editing is unsupported by the conformance harness.", result.GetProperty("error").GetString());
    }

    private static void AssertResponse(WorkshopResponse expected, WorkshopResponse actual, float midpointMetres = 0f)
    {
        Assert.Equal(expected with { Read = expected.Read with { Motion = null }, Scene = null },
            actual with { Read = actual.Read with { Motion = null }, Scene = null });
        Assert.Equivalent(expected.Scene, actual.Scene, strict: true);
        Assert.Equal(expected.Read.Motion is not null, actual.Read.Motion is not null);
        if (actual.Read.Motion is not { } motion)
            return;

        // The committed controls carry four free-flight pieces per body.
        Assert.Equal(4 * expected.Read.Bodies.Count, motion.Count);
        Assert.Equal(4u, motion.Substeps);
        Assert.Equal(0u, motion.FirstOrdinal);
        Assert.Equal(4u, motion.LastOrdinal);
        Assert.True(motion.TrySample(new(1), new(1, -2048), out var pose));
        Assert.Equal(new GpuBodyId(1), pose.Id);
        Assert.Equal(default, pose.Cell);
        Assert.Equal(CanonicalRotation.Identity, pose.Rotation);
        // Losing the velocity lane would render the moving body at its piece origin rather than its in-piece position.
        var expectedLocal = new Vector3(MathF.ScaleB(midpointMetres, -(int)CellScale.Metres), 0, 0);
        var errorMetres = MathF.ScaleB((new Vector3(pose.Local.X, pose.Local.Y, pose.Local.Z) - expectedLocal).Length(),
            (int)CellScale.Metres);
        Assert.InRange(errorMetres, 0f, PhysicalTolerance.ContactSeparationMetres);
        Assert.False(motion.TrySample(new(1), new(4), out _));
    }

    [Theory]
    [MemberData(nameof(ObservationArms))]
    public void CSharpDecodesEveryObservationArmTypeScriptEncoded(W.ObservationPayload arm)
    {
        var batch = MakeObservationBatch(arm);
        const string incomplete = "one observation was unavailable";
        var (unchanged, changed) = RoundTrip(W.WireRootIdentifier.WorkshopObservationBatch, WorkshopWireFlatBuffers.EncodeObservations(batch),
            "incomplete", incomplete);
        AssertBatch(batch, WorkshopWireFlatBuffers.DecodeObservations(unchanged));
        AssertBatch(batch with { Incomplete = incomplete }, WorkshopWireFlatBuffers.DecodeObservations(changed));
    }

    private static void AssertBatch(WorkshopObservationBatch expected, WorkshopObservationBatch actual)
    {
        Assert.Equal(expected.Session, actual.Session);
        Assert.Equal(expected.Role, actual.Role);
        Assert.Equal(expected.Incomplete, actual.Incomplete);
        var record = Assert.Single(actual.Records);
        // Use the response's semantic equality, which includes the committed read's collection elements.
        var expectedRecord = Assert.Single(expected.Records);
        if (expectedRecord is WorkshopTransferDiagnostic transfer)
            AssertResponse(transfer.Response, Assert.IsType<WorkshopTransferDiagnostic>(record).Response);
        else
            Assert.Equivalent(expectedRecord, record, strict: true);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CSharpDecodesTheSaveTypeScriptEncoded(bool authored)
    {
        var save = SampleSave(authored);
        var (unchanged, changed) = RoundTrip(W.WireRootIdentifier.SavedConstruction, EncodeSave(save), "nextBodyId", new { i64 = "5000" });
        var original = DecodeSave(unchanged);
        var edited = DecodeSave(changed);
        Assert.Equal(save, original);
        Assert.Equal(save with { NextBodyId = new(5000) }, edited);
        if (!authored)
        {
            // The free-workshop control carries -0 through the canonical f32 wire lane.
            Assert.Equal(0x80000000u, BitConverter.SingleToUInt32Bits(original.Construction.Instances[0].Local.X));
            Assert.Equal(0x80000000u, BitConverter.SingleToUInt32Bits(edited.Construction.Instances[0].Local.X));
        }
    }

    // ----- S6: malformed frames are refused on both sides for the same check ----------------------------------------------

    public enum FrameFault { ShorterThanPrefix, WrongIdentifier, TruncatedByOne, TrailingByte, IncompleteIdentifier, HighBitPrefix }

    public static TheoryData<W.WireRootIdentifier, FrameFault> S6Cases()
    {
        var data = new TheoryData<W.WireRootIdentifier, FrameFault>();
        foreach (var root in Enum.GetValues<W.WireRootIdentifier>())
            foreach (var fault in Enum.GetValues<FrameFault>())
                data.Add(root, fault);
        return data;
    }

    [Theory]
    [MemberData(nameof(S6Cases))]
    public void MalformedFramesAreRefusedOnBothSidesForTheSameCheck(W.WireRootIdentifier root, FrameFault fault)
    {
        var valid = ValidBuffer(root);
        var malformed = ApplyFault(valid, fault, root);

        var expectedReason = fault switch
        {
            FrameFault.ShorterThanPrefix => WireFaultReason.Truncated,
            FrameFault.WrongIdentifier or FrameFault.IncompleteIdentifier => WireFaultReason.UnknownIdentifier,
            FrameFault.TruncatedByOne or FrameFault.HighBitPrefix => WireFaultReason.Truncated,
            FrameFault.TrailingByte => WireFaultReason.TrailingBytes,
            _ => throw new ArgumentOutOfRangeException(nameof(fault))
        };

        DecodeInCSharp(root, valid);
        Assert.Equal(expectedReason, AssertCSharpRefusal(root, malformed));

        var results = RunConformance([DecodeRequest(root, valid), DecodeRequest(root, malformed)]);
        Assert.True(results[0].GetProperty("ok").GetBoolean(), $"{root}: {results[0]}");
        var rejected = results[1];
        Assert.False(rejected.GetProperty("ok").GetBoolean(), $"TypeScript admitted malformed {root} ({fault})");
        var tsError = rejected.GetProperty("error").GetString()!;
        Assert.Equal(expectedReason, MapTypeScriptErrorToReason(tsError));
        if (fault == FrameFault.ShorterThanPrefix)
            Assert.Contains("shorter than its size prefix", tsError, StringComparison.Ordinal);
        if (fault is FrameFault.TruncatedByOne or FrameFault.HighBitPrefix)
            Assert.Contains("truncated", tsError, StringComparison.Ordinal);
    }

    private static WireFaultReason MapTypeScriptErrorToReason(string message)
    {
        if (message.Contains("shorter than its size prefix", StringComparison.Ordinal) ||
            message.Contains("truncated", StringComparison.Ordinal))
            return WireFaultReason.Truncated;
        if (message.Contains("trailing bytes", StringComparison.Ordinal))
            return WireFaultReason.TrailingBytes;
        if (message.Contains("wrong identifier", StringComparison.Ordinal))
            return WireFaultReason.UnknownIdentifier;
        throw new InvalidOperationException($"Unmapped TypeScript frame error: {message}");
    }

    // ----- S7: verifier failures are refused in C# ------------------------------------------------------------------------

    [Fact]
    public void VerifierFailuresAreRefusedInCSharp()
    {
        W.WireRootIdentifier[] runtimeRoots =
            [W.WireRootIdentifier.WorkshopCommand, W.WireRootIdentifier.WorkshopResponse, W.WireRootIdentifier.WorkshopObservationBatch];
        foreach (var root in runtimeRoots)
        {
            var valid = ValidBuffer(root);
            DecodeInCSharp(root, valid);
            var outside = (byte[])valid.Clone();
            BinaryPrimitives.WriteInt32LittleEndian(outside.AsSpan(FlatBufferConstants.SizePrefixLength), outside.Length);
            Assert.Equal(WireFaultReason.VerifierRejected, AssertCSharpRefusal(root, outside));
        }

        // Only the required assistance field is cleared from an otherwise admitted authored save.
        var construction = PuzzleCatalogue.Create(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle,
            new("first_principles"), new(2), WorkshopCadenceSettings.Default(), new(1), new(.5f));
        var expected = new WorkshopSavedConstruction(construction, new(construction.Instances.Max(instance => instance.Id.Value) + 1));
        var save = EncodeSave(expected);
        Assert.Equal(expected, DecodeSave(save));

        ClearRequiredAssistance(save);
        var saveRejection = Assert.Throws<WorkshopSaveRejectedException>(() => DecodeSave(save));
        Assert.Equal(WorkshopSaveRejection.VerifierRejected, saveRejection.Reason);
    }

    private static void ClearRequiredAssistance(byte[] save)
    {
        var schema = WireSchemaText.Read(Path.Combine(Root, "schemas", "workshop_wire.fbs"));
        var root = Follow(FlatBufferConstants.SizePrefixLength);
        var construction = NestedTable(root, nameof(W.SavedConstruction), "construction");
        var puzzle = NestedTable(construction, nameof(W.ConstructionRecord), "puzzle");
        var assistance = VtableEntry(puzzle, nameof(W.PuzzleRecord), "assistance");
        Assert.NotEqual((ushort)0, BinaryPrimitives.ReadUInt16LittleEndian(save.AsSpan(assistance)));
        BinaryPrimitives.WriteUInt16LittleEndian(save.AsSpan(assistance), 0);

        int Follow(int address) => address + BinaryPrimitives.ReadInt32LittleEndian(save.AsSpan(address));

        int VtableEntry(int table, string name, string field)
        {
            var vtable = table - BinaryPrimitives.ReadInt32LittleEndian(save.AsSpan(table));
            return vtable + WireSchemaLayout.VtableOffset(WireSchemaLayout.Slot(schema, name, field));
        }

        int NestedTable(int table, string name, string field)
        {
            var offset = BinaryPrimitives.ReadUInt16LittleEndian(save.AsSpan(VtableEntry(table, name, field)));
            Assert.NotEqual((ushort)0, offset);
            return Follow(table + offset);
        }
    }

    // ----- Helpers & Fixtures ---------------------------------------------------------------------------------------------

    private static JsonElement[] RunConformance(object[] ops) => RunWireTool("wire-conformance.js", ops);

    internal static IEnumerable<string> RunWireSourceGuard(IEnumerable<(string Path, string Text)> sources, string frameAdapter) =>
        RunWireTool("wire-source-guard.js", sources.Select(source => new
        {
            path = source.Path,
            text = source.Text,
            frameAdapter = source.Path == frameAdapter
        }).ToArray()).Select(result =>
            $"{result.GetProperty("path").GetString()}:{result.GetProperty("line").GetInt32()}: {result.GetProperty("reason").GetString()}");

    private static JsonElement[] RunWireTool(string script, object[] request)
    {
        var path = Path.Combine(Root, ".runtime-build/tools/runtime", script);
        var start = new ProcessStartInfo("node")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = Root
        };
        start.ArgumentList.Add(path);
        using var node = Process.Start(start) ?? throw new InvalidOperationException("Node could not be started.");
        node.StandardInput.Write(JsonSerializer.Serialize(request));
        node.StandardInput.Close();
        var stdout = node.StandardOutput.ReadToEndAsync();
        var stderr = node.StandardError.ReadToEndAsync();
        if (!node.WaitForExit(30_000))
        {
            node.Kill();
            Assert.Fail($"{script} did not finish within 30 s.");
        }
        Assert.True(node.ExitCode == 0, $"{script} failed ({node.ExitCode}): {stderr.Result}");
        using var response = JsonDocument.Parse(stdout.Result);
        return response.RootElement.EnumerateArray().Select(value => value.Clone()).ToArray();
    }

    private static object DecodeRequest(W.WireRootIdentifier root, byte[] bytes) =>
        new { op = "decode", root = root.ToString(), hex = Convert.ToHexString(bytes) };

    private static JsonElement DecodeInTypeScript(W.WireRootIdentifier root, byte[] bytes)
    {
        var result = RunConformance([DecodeRequest(root, bytes)])[0];
        Assert.True(result.GetProperty("ok").GetBoolean(), $"{root}: {result}");
        return result.GetProperty("value");
    }

    private static (byte[] Unchanged, byte[] Changed) RoundTrip(W.WireRootIdentifier root, byte[] bytes, string field, object value)
    {
        var rootName = root.ToString();
        var hex = Convert.ToHexString(bytes);
        var results = RunConformance([
            new { op = "roundtrip", root = rootName, hex },
            new { op = "roundtrip", root = rootName, hex, mutate = new { field, value } }
        ]);
        return (ReencodedBytes(results[0], root), ReencodedBytes(results[1], root));
    }

    private static byte[] ReencodedBytes(JsonElement result, W.WireRootIdentifier root)
    {
        Assert.True(result.GetProperty("ok").GetBoolean(), $"TypeScript refused {root}: {result}");
        return Convert.FromHexString(result.GetProperty("hex").GetString()!);
    }

    private static byte[] EncodeSave(WorkshopSavedConstruction save) =>
        WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, save);

    private static WorkshopSavedConstruction DecodeSave(byte[] bytes) =>
        WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply,
            RuntimeTestServices.Bundle, SaveFixture.Elements, bytes);

    private static byte[] ValidBuffer(W.WireRootIdentifier root) => root switch
    {
        W.WireRootIdentifier.WorkshopCommand => WorkshopWireFlatBuffers.EncodeCommand(MakeCommand(WorkshopCommandKind.Run)),
        W.WireRootIdentifier.WorkshopResponse => WorkshopWireFlatBuffers.EncodeResponse(MakeResponse(WorkshopResponseKind.Read)),
        W.WireRootIdentifier.WorkshopObservationBatch => WorkshopWireFlatBuffers.EncodeObservations(MakeObservationBatch(W.ObservationPayload.Debt)),
        W.WireRootIdentifier.SavedConstruction => EncodeSave(SampleSave()),
        _ => throw new ArgumentOutOfRangeException(nameof(root))
    };

    private static byte[] ApplyFault(byte[] valid, FrameFault fault, W.WireRootIdentifier root) => fault switch
    {
        FrameFault.ShorterThanPrefix => valid[..(FlatBufferConstants.SizePrefixLength - 1)],
        FrameFault.TruncatedByOne => valid[..^1],
        FrameFault.TrailingByte => [.. valid, 0x00],
        FrameFault.WrongIdentifier => WithWrongIdentifier(valid, root),
        FrameFault.IncompleteIdentifier => WithCurrentLengthIncompleteIdentifier(valid),
        FrameFault.HighBitPrefix => WithPrefix((byte[])valid.Clone(), 0x80000000u),
        _ => throw new ArgumentOutOfRangeException(nameof(fault))
    };

    private static byte[] WithCurrentLengthIncompleteIdentifier(byte[] valid)
    {
        var length = FlatBufferConstants.SizePrefixLength + sizeof(int) + FlatBufferConstants.FileIdentifierLength - 1;
        return WithPrefix(valid[..length], (uint)(length - FlatBufferConstants.SizePrefixLength));
    }

    private static byte[] WithPrefix(byte[] frame, uint prefix)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(frame, prefix);
        return frame;
    }

    private static byte[] WithWrongIdentifier(byte[] valid, W.WireRootIdentifier root)
    {
        var copy = (byte[])valid.Clone();
        var other = Enum.GetValues<W.WireRootIdentifier>().First(id => id != root);
        var identifier = copy.AsSpan(FlatBufferConstants.SizePrefixLength + sizeof(int), FlatBufferConstants.FileIdentifierLength);
        Encoding.Latin1.GetBytes(WorkshopWireFlatBuffers.FileIdentifier(other), identifier);
        return copy;
    }

    private static WireFaultReason AssertCSharpRefusal(W.WireRootIdentifier root, byte[] bytes)
    {
        if (root == W.WireRootIdentifier.SavedConstruction)
        {
            var ex = Assert.Throws<WorkshopSaveRejectedException>(() => DecodeSave(bytes));
            return ex.Reason switch
            {
                WorkshopSaveRejection.Truncated => WireFaultReason.Truncated,
                WorkshopSaveRejection.TrailingBytes => WireFaultReason.TrailingBytes,
                WorkshopSaveRejection.UnknownIdentifier => WireFaultReason.UnknownIdentifier,
                WorkshopSaveRejection.VerifierRejected => WireFaultReason.VerifierRejected,
                _ => throw new InvalidOperationException($"Unexpected save rejection: {ex.Reason}")
            };
        }
        return Assert.Throws<WireFaultException>(() => DecodeInCSharp(root, bytes)).Reason;
    }

    private static void DecodeInCSharp(W.WireRootIdentifier root, byte[] bytes)
    {
        switch (root)
        {
            case W.WireRootIdentifier.WorkshopCommand: WorkshopWireFlatBuffers.DecodeCommand(bytes); break;
            case W.WireRootIdentifier.WorkshopResponse: WorkshopWireFlatBuffers.DecodeResponse(bytes); break;
            case W.WireRootIdentifier.WorkshopObservationBatch: WorkshopWireFlatBuffers.DecodeObservations(bytes); break;
            case W.WireRootIdentifier.SavedConstruction: DecodeSave(bytes); break;
            default: throw new ArgumentOutOfRangeException(nameof(root));
        }
    }

    private static WorkshopCommand MakeCommand(WorkshopCommandKind kind, ExpectedRevisionKind revisionKind = ExpectedRevisionKind.Exact)
    {
        var isInit = kind == WorkshopCommandKind.Initialize;
        var hasScene = kind is WorkshopCommandKind.Initialize or WorkshopCommandKind.Construct or WorkshopCommandKind.ConfigureCadence;
        var scene = hasScene ? CompiledSceneFixture.Compile(SampleConstruction()) : null;
        var cadence = isInit ? new CadenceRevision(0) : new CadenceRevision(3);
        var projection = isInit ? new ProjectionEpoch(0) : new ProjectionEpoch(5);
        // These adjacent identities would alias if either crossed the JavaScript Number boundary.
        const ulong firstOutsideSafeInteger = 1UL << 53;
        var target = kind == WorkshopCommandKind.Cancel
            ? new WorkshopCommandIdentity(new(firstOutsideSafeInteger), new(2), new(3), new(1), new(1))
            : (WorkshopCommandIdentity?)null;
        var runControl = kind == WorkshopCommandKind.ConfigureRunControl
            ? new WorkshopRunControl(new(1), new(2), RunControlValue.Of(BitConverter.UInt32BitsToSingle(0x80000000)))
            : (WorkshopRunControl?)null;
        var cut = kind == WorkshopCommandKind.CutRope ? new RopeSpanId(7) : (RopeSpanId?)null;
        return new WorkshopCommand(
            new(target.HasValue ? firstOutsideSafeInteger + 1 : ulong.MaxValue),
            kind,
            new(42),
            revisionKind == ExpectedRevisionKind.Any ? default : new(WideExpectedRevision),
            scene,
            target,
            revisionKind,
            Session,
            cadence,
            projection,
            runControl,
            cut);
    }

    private static WorkshopResponse ResponseSample(ResponseCase sample) => sample switch
    {
        ResponseCase.Acknowledgement => MakeResponse(WorkshopResponseKind.Acknowledgement),
        ResponseCase.RefusedAcknowledgement => MakeResponse(WorkshopResponseKind.Acknowledgement) with
        {
            Result = new(WorkshopCommandOutcome.Rejected, WorkshopRejection.WrongPhase)
        },
        ResponseCase.AdmissionRead => AdmissionRead(),
        ResponseCase.CommittedRead => CommittedRead(),
        ResponseCase.MovingRead => MovingRead(),
        ResponseCase.PopulatedRead => PopulatedRead(),
        ResponseCase.MemoryAcknowledgement => MakeResponse(WorkshopResponseKind.Acknowledgement) with
        {
            Memory = new(4096, 2048, 2, 1000, 20, 80, 1, 2)
        },
        _ => throw new ArgumentOutOfRangeException(nameof(sample))
    };

    private static WorkshopResponse AdmissionRead()
    {
        var response = MakeResponse(WorkshopResponseKind.Read);
        var first = response.Read.Bodies[0];
        var second = first with { Body = first.Body with { Id = new(42), Cell = new(2, 0, 0) } };
        var rope = new RopeSpanRead(new(71), new(61), 0, first.Body.Id, second.Body.Id,
            Vector3.Zero, new(.125f, 0, 0), .25f, .125f, RopeIntegrity.Intact, RopeState.Slack, default);
        return response with
        {
            Publication = new(FirstWidePublication),
            Read = response.Read with
            {
                Bodies = new([first, second]),
                Presentation = new([], [], ropes: [rope])
            }
        };
    }

    private static WorkshopResponse CommittedRead()
    {
        var response = WireFaultTests.PhysicalRead();
        var owner = response.Read.Bodies[0].Body.Id;
        var store = new EnergyStoreRead(new(41), owner, new(16f, 0f), new(4f), new(12f), default, new(4f), new(12f), default, 2);
        return response with { Read = response.Read with { EnergyStores = new([store]) } };
    }

    private static WorkshopResponse MovingRead()
    {
        var response = CommittedRead();
        var read = response.Read;
        var row = read.Bodies[0];
        var stride = MathF.ScaleB(MovingSpeedMetresPerSecond / PhysicalTime.Frequency, -(int)CellScale.Metres);
        var body = new CanonicalBody(row.Body.Id, row.Body.Epoch, row.Body.Tick, default, new(4 * stride, 0, 0),
            new(MovingSpeedMetresPerSecond, 0, 0));
        var secondBody = body with { Id = new(2), Local = default, Velocity = default };
        var bodies = new PhysicsBodyReadSet([row with { Body = body }, row with { Body = secondBody }]);
        const int pieceCount = 4;
        const int bodyMotionBytes = pieceCount * PhysicsMotionRead.PieceBytes;
        var motion = new byte[PhysicsMotionRead.ByteLength(bodies.Count)];
        WireFaultTests.MotionBytes(body).AsSpan(0, PhysicsMotionRead.HeaderBytes + bodyMotionBytes).CopyTo(motion);
        WireFaultTests.MotionBytes(secondBody).AsSpan(PhysicsMotionRead.HeaderBytes, bodyMotionBytes)
            .CopyTo(motion.AsSpan(PhysicsMotionRead.HeaderBytes + bodyMotionBytes));
        BinaryPrimitives.WriteUInt32LittleEndian(motion.AsSpan(PhysicsMotionRead.CountOffset), pieceCount * 2);
        for (var substep = 0; substep < pieceCount; substep++)
        {
            var piece = motion.AsSpan(PhysicsMotionRead.HeaderBytes + substep * PhysicsMotionRead.PieceBytes);
            BinaryPrimitives.WriteSingleLittleEndian(piece[PhysicsMotionRead.CentreRemainderOffset..], substep * stride);
            BinaryPrimitives.WriteSingleLittleEndian(piece[PhysicsMotionRead.VelocityOffset..], MovingSpeedMetresPerSecond);
        }
        var ongoingTravel = new CoordinateTravelRead(new(3), body.Id, .75f, .5f, CoordinateTravelPhase.Accumulating, 0);
        var reachedTravel = new CoordinateTravelRead(new(4), secondBody.Id, 1.25f, 1.25f, CoordinateTravelPhase.Reached, 2);
        return response with
        {
            Publication = new(FirstWidePublication + 1),
            Read = read with
            {
                Bodies = bodies,
                Motion = PhysicsMotionRead.Decode(motion, bodies, read.Tick),
                CoordinateTravelObservers = new([ongoingTravel, reachedTravel])
            }
        };
    }

    private static WorkshopResponse PopulatedRead()
    {
        var plate = PlacedParts.Plate(new(1), -2, 0, 0, .5f);
        var battery = PlacedParts.Battery(new(2), 2, 0, 0);
        var construction = new WorkshopConstruction(new(4), WorkshopCadenceSettings.Default(), new(plate, battery));
        var scene = CompiledSceneFixture.Compile(construction, new(27));
        var sensor = Assert.Single(scene.Physics.ContactLoadSensors.ToArray());
        var response = MakeResponse(WorkshopResponseKind.Read);
        return response with
        {
            Scene = scene,
            Read = response.Read with
            {
                Bodies = default,

                Activations = scene.Activation.Clear(),
                Timers = scene.Activation.ClearTimers(),
                Memory = scene.Activation.ClearMemory(),
                Electrical = PlacedParts.InitialElectrical(scene.Physics),
                Presentation = new([], [new(sensor.Id, sensor.Frame, sensor.Region, .75f, ContactLoadPhase.Loaded)])
            }
        };
    }

    private static WorkshopResponse MakeResponse(WorkshopResponseKind kind, bool withTransfer = false)
    {
        var result = new WorkshopCommandResult(WorkshopCommandOutcome.Applied, WorkshopRejection.None);
        if (kind == WorkshopResponseKind.Acknowledgement)
            return new(new(11), kind, result, WorkshopSimulationPhase.Building, default, Session);

        var master = new ClockGeneration(5);
        var stamp = new WorkshopClockStamp(WorkshopClockDomain.SimulationMonotonic, master, new(100), new(0));
        var body = new CanonicalBody(new(41), 2, 0, default, new(.125f, .25f, 0f), default);
        var bodies = new PhysicsBodyReadSet([new(body, CanonicalRotation.Identity, default, default)]);
        var electrical = withTransfer
            ? new PhysicsElectricalRead([new ElectricalSourceRead(new(1), new(41), new(1f, 0f), new(0.25f), ElectricalEnable.Enabled, 1)])
            : default;
        var read = new WorkshopRead(new(2), default, default, bodies, Revision: new(4), Capture: stamp, Electrical: electrical);
        return new(default, kind, result, WorkshopSimulationPhase.Running, read, Session,
            Publication: new(17), Cadence: new(3), MasterGeneration: master, Projection: new(5));
    }

    private static WorkshopObservationBatch MakeObservationBatch(W.ObservationPayload arm)
    {
        // Three distinct signed values beyond the JavaScript safe integer boundary.
        const long wideOffset = -(1L << 53) - 1;
        WorkshopDiagnostic diagnostic = arm switch
        {
            W.ObservationPayload.Fault => new WorkshopFaultDiagnostic(WorkshopFaultKind.Fault, FaultSite.Advance, "InvalidOperationException", "probe"),
            W.ObservationPayload.Recalibration => new ClockRecalibrationDiagnostic(
                new(Session, new(1), new(1), new(2), new(100_000), WorkshopRuntimeRole.Animation),
                ClockRecalibrationCause.Elapsed, 1, ClockMappingState.Recovering, new(1), 2, new(10), new(5), 1,
                null, null, []),
            W.ObservationPayload.RejectedClock => new ClockReplyRejectedDiagnostic(WorkshopRuntimeRole.Simulation, 1, 16, new(10), "bad"),
            W.ObservationPayload.LateRenewal => new ClockLateRenewalDiagnostic(
                WorkshopRuntimeRole.Simulation, 1, 5, 1, new(10), new(1, 2), new(3, 4), null, 1),
            W.ObservationPayload.WideOffset => new ClockWideOffsetDiagnostic(wideOffset, wideOffset - 2, wideOffset - 1, 3, 4),
            W.ObservationPayload.Derivation => new ClockDerivationDiagnostic(
                ClockDerivationReport.Startup, ClockDerivationOutcome.Consistent, new(1), 5, null, 1, 2, null, 1, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0),
            W.ObservationPayload.Deferral => new AnimationDeferralDiagnostic(AnimationDeferralCause.Preparation, 3),
            W.ObservationPayload.Debt => new WorkshopDebtDiagnostic(new(5), WorkshopRejection.None, new(2), 15),
            W.ObservationPayload.TickFailure => new WorkshopTickFailureDiagnostic(
                new(WorkshopCommandOutcome.Rejected, WorkshopRejection.WrongPhase), WorkshopRejection.WrongPhase),
            W.ObservationPayload.Transfer => new WorkshopTransferDiagnostic(MakeResponse(WorkshopResponseKind.Read, withTransfer: true)),
            W.ObservationPayload.Capture => new WorkshopCaptureDiagnostic(
                WorkshopCaptureKind.Timing, WorkshopCaptureStage.End, Session, 1, new(1),
                new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)),
                TraceEnd.Complete, TraceTimingStatus.Complete, 1, 1, 0, 1, 0, null, [], []),
            _ => throw new ArgumentOutOfRangeException(nameof(arm))
        };
        return new WorkshopObservationBatch(Session, WorkshopRuntimeRole.Simulation, [diagnostic]);
    }

    private static WorkshopConstruction SampleConstruction()
    {
        var ball = new WorkshopGenericInstance(new PartId("ball"), new(1), new(1, 0, 0),
            new(BitConverter.UInt32BitsToSingle(0x80000000), 0.25f, 0f), CanonicalRotation.Identity);
        return new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(ball));
    }

    private static WorkshopSavedConstruction SampleSave(bool authored = false) => new(authored
        ? PuzzleCatalogue.CreateSolution(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, new("delayed_signal"),
            new(2), WorkshopCadenceSettings.Default(), new(1), new(.5f))
        : SampleConstruction(), new(ulong.MaxValue));

    private static void ExpectUInt(List<string> errs, string field, uint expected, JsonElement obj)
    {
        var actual = ReadJsNumber(obj.GetProperty(field));
        if (actual != expected) errs.Add($"{field}: expected {expected}, got {actual}");
    }

    private static double ReadJsNumber(JsonElement number) =>
        BitConverter.UInt64BitsToDouble(Convert.ToUInt64(number.GetProperty("f64Hex").GetString()!, 16));

    private static void ExpectUInt64(List<string> errs, string field, ulong expected, JsonElement obj) =>
        ExpectInteger(errs, field, expected.ToString(CultureInfo.InvariantCulture), obj);

    private static void ExpectInt64(List<string> errs, string field, long expected, JsonElement obj) =>
        ExpectInteger(errs, field, expected.ToString(CultureInfo.InvariantCulture), obj);

    private static void ExpectInteger(List<string> errs, string field, string expected, JsonElement obj)
    {
        var actual = obj.GetProperty(field).GetProperty("i64").GetString()!;
        if (actual != expected) errs.Add($"{field}: expected {expected}, got {actual}");
    }

    private static void ExpectF32(List<string> errs, string field, float expected, JsonElement obj)
    {
        var expectedHex = BitConverter.DoubleToUInt64Bits(expected).ToString("x16", CultureInfo.InvariantCulture);
        var actualHex = obj.GetProperty(field).GetProperty("f64Hex").GetString()!;
        if (actualHex != expectedHex) errs.Add($"{field}: expected f64Hex {expectedHex}, got {actualHex}");
    }

    private static void ExpectBytes(List<string> errs, string field, byte[] expected, JsonElement obj)
    {
        var actual = obj.GetProperty(field).EnumerateArray()
            .Select(ReadJsNumber)
            .ToArray();
        if (!actual.SequenceEqual(expected.Select(value => (double)value)))
            errs.Add($"{field}: expected [{string.Join(", ", expected)}], got [{string.Join(", ", actual)}]");
    }
}
