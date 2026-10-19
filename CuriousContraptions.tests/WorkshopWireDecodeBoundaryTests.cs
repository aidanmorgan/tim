using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>The FlatBuffers decoders are a typed boundary: any malformed buffer is rejected with ArgumentException (the
/// codec-sniffing callers depend on exactly that type) and never with a runtime exception from the buffer reader.
/// The corpus is deterministic: every truncation and every single-bit flip of valid commands and responses, oversized
/// and prefixed buffers, fixed-binary encodings of the other wire, and seeded random buffers.</summary>
public sealed class WorkshopWireDecodeBoundaryTests
{
    private static readonly RuntimeSessionId Session = new(0x1122334455667788UL, 0x8877665544332211UL);

    private static WorkshopCommand RunCommand() => new(new(1), WorkshopCommandKind.Run, new(10), default, null,
        RevisionKind: ExpectedRevisionKind.Any, Session: Session, Cadence: new(2), Projection: new(3));

    private static WorkshopCommand ConstructCommand() => new(new(1), WorkshopCommandKind.Construct, new(1), default,
        new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new([WorkshopInput.Basketball(new(1), 0, 3, 0, 0, 0, 0, 1)])),
        Session: Session, Cadence: new(1), Projection: new(1));

    private static WorkshopResponse Acknowledgement() => new(new(10), WorkshopResponseKind.Acknowledgement,
        new(WorkshopCommandOutcome.Applied, WorkshopRejection.None), WorkshopSimulationPhase.Building, default, Session: Session);

    private static WorkshopResponse PhysicalRead()
    {
        var master = new ClockGeneration(1);
        var stamp = new WorkshopClockStamp(WorkshopClockDomain.SimulationMonotonic, master, new(1_000_000), new(100));
        var body = new PhysicsBodyRead(new CanonicalBody(new(1), 1, 1, default, default, default), CanonicalRotation.Identity, default, default);
        var work = new ContactWorkRead(new(40), new(1), 1, new(16f), new(4f));
        var occurrence = new ContactWorkOccurrence(new(0), new(1), new(1), 1, 8, (Half)0.5f, new ContactSpeed(4f), new Joules(8f), ContactWorkEffect.Paid);
        var read = new WorkshopRead(new(1), new(1), new PhysicsBodyReadSet(new[] { body }), new(1), stamp,
            new PhysicsCaptureRead(new[] { CaptureLatch.Clear(new(9)) }), null,
            ContactWorks: new PhysicsContactWorkRead(new[] { work }, new[] { occurrence }),
            Electrical: new PhysicsElectricalRead(new[] { new ElectricalSourceRead(new(50), new(1), new Joules(100f), new Joules(5f), ElectricalEnable.Enabled) }));
        return new(default, WorkshopResponseKind.Read, new(WorkshopCommandOutcome.Applied, WorkshopRejection.None),
            WorkshopSimulationPhase.Running, read, Session: Session, Publication: new(1), Cadence: new(1),
            MasterGeneration: master, Projection: new(1), SourcePulse: new(1));
    }

    private static IEnumerable<byte[]> Corpus(byte[] valid, Random random)
    {
        for (var length = 0; length < valid.Length; length++) yield return valid[..length];
        for (var bit = 0; bit < valid.Length * 8; bit++)
        {
            var flipped = (byte[])valid.Clone();
            flipped[bit / 8] ^= (byte)(1 << (bit % 8));
            yield return flipped;
        }
        foreach (var extra in new[] { 1, 64, 4096 })
        {
            var oversized = new byte[valid.Length + extra];
            valid.CopyTo(oversized, 0);
            random.NextBytes(oversized.AsSpan(valid.Length));
            yield return oversized;
            var prefixed = new byte[valid.Length + extra];
            random.NextBytes(prefixed.AsSpan(0, extra));
            valid.CopyTo(prefixed, extra);
            yield return prefixed;
        }
        for (var count = 0; count < 2000; count++)
        {
            var bytes = (byte[])valid.Clone();
            for (var flips = random.Next(2, 9); flips > 0; flips--) bytes[random.Next(bytes.Length)] = (byte)random.Next(256);
            yield return bytes;
        }
    }

    private static IEnumerable<byte[]> RandomBuffers(Random random)
    {
        for (var count = 0; count < 2000; count++)
        {
            var bytes = new byte[random.Next(0, 6000)];
            random.NextBytes(bytes);
            yield return bytes;
        }
    }

    // Returns the first decode failure whose type is not ArgumentException (or a subclass), or null.
    private static string? FirstUntypedRejection(IEnumerable<byte[]> buffers, Action<byte[]> decode)
    {
        var index = 0;
        foreach (var bytes in buffers)
        {
            var error = Record.Exception(() => decode(bytes));
            if (error is not (null or ArgumentException))
                return $"buffer {index} ({bytes.Length} bytes): {error.GetType().FullName}: {error.Message}";
            index++;
        }
        return null;
    }

    [Fact]
    public void MalformedCommandsAreRejectedOnlyWithArgumentException()
    {
        var random = new Random(20261018);
        var buffers = new[] { RunCommand(), ConstructCommand() }
            .SelectMany(command => Corpus(WorkshopWireFlatBuffers.EncodeCommand(command), random))
            .Concat(Corpus(WorkshopWire.Encode(ConstructCommand()), random))
            .Concat(RandomBuffers(random));
        Assert.Null(FirstUntypedRejection(buffers, bytes => WorkshopWireFlatBuffers.DecodeCommand(bytes)));
    }

    [Fact]
    public void MalformedResponsesAreRejectedOnlyWithArgumentException()
    {
        var random = new Random(20261019);
        var buffers = new[] { Acknowledgement(), PhysicalRead() }
            .SelectMany(response => Corpus(WorkshopWireFlatBuffers.EncodeResponse(response), random))
            .Concat(RandomBuffers(random));
        Assert.Null(FirstUntypedRejection(buffers, bytes => WorkshopWireFlatBuffers.DecodeResponse(bytes)));
    }

    // The sniffing callers fall back to the fixed-binary decoders, which form the same typed boundary.
    [Fact]
    public void MalformedFixedBinaryCommandsAreRejectedOnlyWithArgumentException()
    {
        var random = new Random(20261020);
        var buffers = Corpus(WorkshopWire.Encode(ConstructCommand()), random)
            .Concat(Corpus(WorkshopWire.Encode(RunCommand()), random))
            .Concat(Corpus(WorkshopWireFlatBuffers.EncodeCommand(ConstructCommand()), random))
            .Concat(RandomBuffers(random));
        Assert.Null(FirstUntypedRejection(buffers, bytes => WorkshopWire.DecodeCommand(bytes)));
    }

    [Fact]
    public void MalformedFixedBinaryResponsesAreRejectedOnlyWithArgumentException()
    {
        var random = new Random(20261021);
        var buffers = Corpus(WorkshopWire.Encode(Acknowledgement()), random)
            .Concat(Corpus(WorkshopWireFlatBuffers.EncodeResponse(PhysicalRead()), random))
            .Concat(RandomBuffers(random));
        Assert.Null(FirstUntypedRejection(buffers, bytes => WorkshopWire.DecodeResponse(bytes)));
    }

    [Fact]
    public void FixedBinaryCommandsAreRejectedByTheFlatBuffersDecoderWithArgumentException()
    {
        var bytes = WorkshopWire.Encode(ConstructCommand());
        Assert.ThrowsAny<ArgumentException>(() => WorkshopWireFlatBuffers.DecodeCommand(bytes));
        Assert.Equal(WorkshopCommandKind.Construct, WorkshopWire.DecodeCommand(bytes).Kind);
    }
}
