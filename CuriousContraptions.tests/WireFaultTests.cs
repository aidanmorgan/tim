using System.Buffers.Binary;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Story 9A.2: FlatBuffers is the only Workshop codec. Every command, response (acknowledgement, physical read and
/// schedule endpoint) and save is one size-prefixed buffer with its root identifier, decoded once: a truncated, wrong-root,
/// trailing-byte or structurally corrupt buffer is a typed fault naming its reason, and nothing retries it through another
/// codec.</summary>
public sealed class WireFaultTests
{
    public enum Message { Command, Acknowledgement, PhysicalRead, Save }
    public enum Damage { Truncated, TrailingByte, WrongIdentifier, CorruptRoot }

    internal static readonly RuntimeSessionId Session = new(0x1122334455667788UL, 0x8877665544332211UL);

    internal static WorkshopCommand RunCommand() => new(new(1), WorkshopCommandKind.Run, new(10), default, null,
        RevisionKind: ExpectedRevisionKind.Any, Session: Session, Cadence: new(2), Projection: new(3));

    internal static WorkshopResponse Acknowledgement() => new(new(10), WorkshopResponseKind.Acknowledgement,
        new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Building, default, Session: Session);

    /// <summary>A committed physical read at <paramref name="tick"/> with one body and its complete committed motion.</summary>
    internal static WorkshopResponse PhysicalRead(ulong tick = 1)
    {
        var master = new ClockGeneration(1);
        var stamp = new WorkshopClockStamp(WorkshopClockDomain.SimulationMonotonic, master, new(1_000_000), new(100));
        var body = new CanonicalBody(new(1), 1, tick, new(2, 3, 4), new(.25f, -.125f, 0), new(1.5f, 0, -2));
        var bodies = new PhysicsBodyReadSet([new PhysicsBodyRead(body, CanonicalRotation.Identity, default, default)]);
        var motion = PhysicsMotionRead.Decode(tick == 0 ? new byte[PhysicsMotionRead.ByteLength(1)] : MotionBytes(body), bodies, new(tick));
        var read = new WorkshopRead(new(1), new(tick), bodies, new(1), stamp, new PhysicsCaptureRead([CaptureLatch.Clear(new(9))]), motion);
        return new(default, WorkshopResponseKind.Read, new(WorkshopCommandOutcome.Applied, WorkshopRejection.None),
            WorkshopSimulationPhase.Running, read, Session: Session, Publication: new(1), Cadence: new(1),
            MasterGeneration: master, Projection: new(1), SourcePulse: new(tick));
    }

    /// <summary>Four free-flight pieces covering the commit at the body's tick.</summary>
    internal static byte[] MotionBytes(CanonicalBody body)
    {
        var bytes = new byte[PhysicsMotionRead.ByteLength(1)];
        var start = checked((uint)(body.Tick * 4 - 4));
        U32(bytes, 0, 4); U32(bytes, 4, 4); U32(bytes, 8, start); U32(bytes, 12, start + 4);
        for (uint i = 0; i < 4; i++)
        {
            var piece = PhysicsMotionRead.HeaderBytes + checked((int)i) * PhysicsMotionRead.PieceBytes;
            U32(bytes, piece, (uint)PhysicsMotionKind.FreePolynomial);
            U32(bytes, piece + 4, start + i); U32(bytes, piece + 8, start + i + 1); U32(bytes, piece + 12, start + i);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(piece + 22), BitConverter.HalfToUInt16Bits((Half)WorkshopCadenceSettings.PhysicalFrequency));
            BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(piece + 24), body.Id.Value);
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(piece + 76), 1f);
        }
        return bytes;
    }

    internal static WorkshopSavedConstruction Saved() => new(new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(),
        new([WorkshopInput.Basketball(new(1), 0, 3, 0, 0, 0, 0, 1)])), new(2));

    private static byte[] Valid(Message message) => message switch
    {
        Message.Command => WorkshopWireFlatBuffers.EncodeCommand(RunCommand()),
        Message.Acknowledgement => WorkshopWireFlatBuffers.EncodeResponse(Acknowledgement()),
        Message.PhysicalRead => WorkshopWireFlatBuffers.EncodeResponse(PhysicalRead()),
        Message.Save => WorkshopSaveCodec.Encode(Saved()),
        _ => throw new ArgumentOutOfRangeException(nameof(message)),
    };

    private static byte[] Damaged(byte[] valid, Damage damage)
    {
        var bytes = (byte[])valid.Clone();
        switch (damage)
        {
            case Damage.Truncated: return bytes[..^1];
            case Damage.TrailingByte: return [.. bytes, 0];
            case Damage.WrongIdentifier: bytes[8] ^= 0x20; return bytes; // after the 4-byte size and the 4-byte root offset
            case Damage.CorruptRoot: BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), 0x7FFFFFF0); return bytes;
            default: throw new ArgumentOutOfRangeException(nameof(damage));
        }
    }

    private static WireFaultReason Expected(Damage damage) => damage switch
    {
        Damage.Truncated => WireFaultReason.Truncated,
        Damage.TrailingByte => WireFaultReason.TrailingBytes,
        Damage.WrongIdentifier => WireFaultReason.UnknownIdentifier,
        Damage.CorruptRoot => WireFaultReason.VerifierRejected,
        _ => throw new ArgumentOutOfRangeException(nameof(damage)),
    };

    private static WorkshopSaveRejection ExpectedSave(Damage damage) => damage switch
    {
        Damage.Truncated => WorkshopSaveRejection.Truncated,
        Damage.TrailingByte => WorkshopSaveRejection.TrailingBytes,
        Damage.WrongIdentifier => WorkshopSaveRejection.UnknownIdentifier,
        Damage.CorruptRoot => WorkshopSaveRejection.VerifierRejected,
        _ => throw new ArgumentOutOfRangeException(nameof(damage)),
    };

    private static void Decode(Message message, byte[] bytes)
    {
        switch (message)
        {
            case Message.Command: WorkshopWireFlatBuffers.DecodeCommand(bytes); break;
            case Message.Acknowledgement or Message.PhysicalRead: WorkshopWireFlatBuffers.DecodeResponse(bytes); break;
            case Message.Save: WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes); break;
            default: throw new ArgumentOutOfRangeException(nameof(message));
        }
    }

    public static TheoryData<Message, Damage> Cases()
    {
        var data = new TheoryData<Message, Damage>();
        foreach (var message in Enum.GetValues<Message>())
            foreach (var damage in Enum.GetValues<Damage>()) data.Add(message, damage);
        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void DamagedBufferIsATypedFaultNamingItsReason(Message message, Damage damage)
    {
        var valid = Valid(message);
        Decode(message, valid); // control: the undamaged buffer decodes
        var bytes = Damaged(valid, damage);
        if (message == Message.Save)
            Assert.Equal(ExpectedSave(damage), Assert.Throws<WorkshopSaveRejectedException>(() => Decode(message, bytes)).Reason);
        else
            Assert.Equal(Expected(damage), Assert.Throws<WireFaultException>(() => Decode(message, bytes)).Reason);
    }

    [Theory]
    [InlineData(Message.Command, Message.Acknowledgement)]
    [InlineData(Message.Command, Message.Save)]
    [InlineData(Message.Acknowledgement, Message.Command)]
    [InlineData(Message.Acknowledgement, Message.Save)]
    [InlineData(Message.Save, Message.Command)]
    [InlineData(Message.Save, Message.PhysicalRead)]
    public void AnotherRootIsAnUnknownIdentifier(Message decoder, Message other)
    {
        var bytes = Valid(other);
        if (decoder == Message.Save)
            Assert.Equal(WorkshopSaveRejection.UnknownIdentifier, Assert.Throws<WorkshopSaveRejectedException>(() => Decode(decoder, bytes)).Reason);
        else
            Assert.Equal(WireFaultReason.UnknownIdentifier, Assert.Throws<WireFaultException>(() => Decode(decoder, bytes)).Reason);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void ShortBufferIsTruncated(int length)
    {
        Assert.Equal(WireFaultReason.Truncated, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(new byte[length])).Reason);
        Assert.Equal(WireFaultReason.Truncated, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeResponse(new byte[length])).Reason);
    }

    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(37UL)]
    public void MotionRoundTripsBitExactly(ulong tick)
    {
        var response = PhysicalRead(tick);
        var decoded = WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(response));
        Assert.NotNull(decoded.Read.Motion);
        Assert.Equal(response.Read.Motion!.Bytes.ToArray(), decoded.Read.Motion!.Bytes.ToArray());
        Assert.Equal(response, decoded);
    }

    /// <summary>Admission has no preceding interval: an absent motion stays absent through the codec, never fabricated.</summary>
    [Fact]
    public void AdmissionWithoutMotionStaysWithoutMotion()
    {
        var decoded = WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(Acknowledgement()));
        Assert.Null(decoded.Read.Motion);
        Assert.Equal(Acknowledgement(), decoded);
    }

    [Fact]
    public void CommittedReadWithoutMotionIsRejected()
    {
        var response = PhysicalRead(5);
        Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.EncodeResponse(response with { Read = response.Read with { Motion = null } }));
    }

    [Fact]
    public void MessageWithoutSessionIsAMissingField()
    {
        var builder = new Google.FlatBuffers.FlatBufferBuilder(64);
        CuriousContraptions.Wire.WorkshopResponse.StartWorkshopResponse(builder);
        CuriousContraptions.Wire.WorkshopResponse.AddSequence(builder, 10);
        builder.FinishSizePrefixed(CuriousContraptions.Wire.WorkshopResponse.EndWorkshopResponse(builder).Value, WorkshopWireFlatBuffers.ResponseIdentifier);
        Assert.Equal(WireFaultReason.MissingField, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeResponse(builder.SizedByteArray())).Reason);

        builder = new Google.FlatBuffers.FlatBufferBuilder(64);
        CuriousContraptions.Wire.WorkshopCommand.StartWorkshopCommand(builder);
        CuriousContraptions.Wire.WorkshopCommand.AddSequence(builder, 1);
        CuriousContraptions.Wire.WorkshopCommand.AddKind(builder, CuriousContraptions.Wire.CommandKind.Run);
        builder.FinishSizePrefixed(CuriousContraptions.Wire.WorkshopCommand.EndWorkshopCommand(builder).Value, WorkshopWireFlatBuffers.CommandIdentifier);
        Assert.Equal(WireFaultReason.MissingField, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(builder.SizedByteArray())).Reason);
    }

    [Fact]
    public void ReadIdentityIsTheSessionAndPublication()
    {
        var response = PhysicalRead();
        var identity = WorkshopWireFlatBuffers.ReadIdentity(response);
        Assert.Equal(24, identity.Length);
        Assert.Equal(Session, RuntimeSessionId.Read(identity.AsSpan(0, 16)));
        Assert.Equal(response.Publication.Value, BinaryPrimitives.ReadUInt64LittleEndian(identity.AsSpan(16)));
    }

    /// <summary>The 72-byte header the deleted fixed-binary codec admitted for a Run command. Nothing decodes it any more.</summary>
    private static byte[] FormerBinaryRunCommand()
    {
        var bytes = new byte[72];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, 1);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), (uint)WorkshopCommandKind.Run);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(16), 10);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(32), (uint)ExpectedRevisionKind.Any);
        Session.WriteTo(bytes.AsSpan(40, 16));
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(56), 2);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(64), 3);
        return bytes;
    }

    [Fact]
    public async Task RejectedInputIsNeverRetried()
    {
        var executed = 0;
        var router = new WorkshopCommandRouter(_ => { executed++; return Task.FromResult(Array.Empty<byte>()); });
        // Its leading sequence word reads as a 1-byte size prefix, so the one decoder names the 71 extra bytes.
        Assert.Equal(WireFaultReason.TrailingBytes, Assert.Throws<WireFaultException>(() => router.Dispatch(FormerBinaryRunCommand())).Reason);
        Assert.Equal(0, executed);
        // Control: the same command in the one codec executes once.
        await router.Dispatch(WorkshopWireFlatBuffers.EncodeCommand(RunCommand())).Response;
        Assert.Equal(1, executed);
    }

    [Fact]
    public void FormerBinaryResponseAndSaveAreRejected()
    {
        var formerResponse = new byte[144];
        Session.WriteTo(formerResponse.AsSpan(40, 16));
        Assert.IsType<WireFaultException>(Record.Exception(() => WorkshopWireFlatBuffers.DecodeResponse(formerResponse)));
        Assert.IsType<WorkshopSaveRejectedException>(Record.Exception(() => WorkshopSaveCodec.Decode(SaveFixture.Elements,formerResponse)));
    }

    [Fact]
    public void PoseSlotHoldsEveryCommittedBodyRecord()
    {
        var read = PhysicalRead(3).Read;
        var slot = WorkshopPoseRing.ComposeSlot(read.Capture!.Value.Time.Value, read.Bodies);
        Assert.Equal(WorkshopPoseRing.SlotBytes(read.Bodies.Count), slot.Length);
        var ring = new byte[WorkshopPoseRing.TotalBytes(read.Bodies.Count)];
        slot.CopyTo(ring.AsSpan(WorkshopPoseRing.SlotOffset(1, read.Bodies.Count)));
        var bodies = new PoseRingBody[1];
        Assert.True(WorkshopPoseRing.TryReadSlot(ring, 1, out var sequence, out var timestamp, bodies, out var count));
        Assert.Equal(0UL, sequence);
        Assert.Equal(read.Capture!.Value.Time.Value, timestamp);
        Assert.Equal(1, count);
        Assert.Equal(read.Bodies[0].Body.Id, bodies[0].Id);
        Assert.Equal((2 + .25f) / 16f, bodies[0].Px);
        Assert.Equal(1.5f, bodies[0].Vx);
    }

    private static readonly string[] AdmittedCatches = ["OverflowException", "WireFaultException", "WorkshopEditRejectedException"];

    /// <summary>Re-add ratchet (Story 9A.2): the second codec, its section codecs and the codec switch stay deleted, and no browser
    /// script parses a Workshop buffer. Re-adding any of them fails here even if it compiles.</summary>
    [Fact]
    public void NoSecondCodecOrScriptParserIsReAdded()
    {
        var types = typeof(WorkshopWireFlatBuffers).Assembly.GetTypes().Select(type => type.Name).ToHashSet();
        foreach (var retired in new[] { "WorkshopWire", "WorkshopActivationWire", "WorkshopTimerWire", "WorkshopContactWorkWire",
                     "WorkshopElectricalWire", "WorkshopActivationMemoryWire", "WorkshopResponseCodec" })
            Assert.DoesNotContain(retired, types);
        var root = twodog.Engine.ResolveProjectDir();
        foreach (var script in new[] { "CuriousContraptions.Simulation/wwwroot/worker.js", "CuriousContraptions.web/wwwroot/workshop-client.js" })
        {
            var source = File.ReadAllText(Path.Combine(root, script));
            foreach (var parser in new[] { "extractReadIdentity", "responseAbi", "ResponseAbi", "CommandAbi", "vtable", "READ_BODIES_OFFSET", "BODY_WIRE_" })
                Assert.False(source.Contains(parser, StringComparison.Ordinal), $"{script} re-adds the buffer parser '{parser}'.");
        }
        foreach (var file in new[] { "engine/gpu/WorkshopWire.cs", "engine/gpu/WorkshopActivationWire.cs", "engine/gpu/WorkshopTimerWire.cs",
                     "engine/gpu/WorkshopContactWorkWire.cs" })
            Assert.False(File.Exists(Path.Combine(root, file)), $"{file} is re-added.");
        // No decode path retries or reclassifies: the only catch on the codecs' paths is the verifier's OverflowException, and the
        // save path catches only the frame's own fault and the connection rule's typed refusal.
        foreach (var codec in new[] { "engine/gpu/WorkshopCommandRouter.cs", "engine/gpu/WorkshopWireFlatBuffers.cs",
                     "engine/gpu/WorkshopSaveCodec.cs", "engine/gpu/WorkshopSavePrevalidation.cs" })
        {
            var catches = System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(Path.Combine(root, codec)), @"\bcatch\b\s*(\(\s*(\w+)[^)]*\))?")
                .Select(match => match.Groups[2].Success ? match.Groups[2].Value : "(all)").ToArray();
            Assert.All(catches, caught => Assert.Contains(caught, AdmittedCatches));
        }
        Assert.DoesNotContain("catch (ArgumentException)", File.ReadAllText(Path.Combine(root, "engine/gpu/BrowserWorkshopClient.cs")), StringComparison.Ordinal);
    }

    private static void U32(byte[] bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset), value);
}
