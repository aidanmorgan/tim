using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>The one codec's decoders are a typed boundary (Story 9A.2): a malformed buffer is refused by its frame's typed
/// WireFaultException or, once verified, by a decoded value's ArgumentException, never by a runtime exception from the buffer
/// reader or the verifier. The corpus is deterministic: every truncation and every single-bit flip of valid commands and
/// responses, oversized and prefixed buffers, and seeded random buffers.</summary>
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

    private static WorkshopResponse PhysicalRead() => WireFaultTests.PhysicalRead(3);

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

    // Returns the first decode failure that is neither the frame's typed fault nor a decoded value's ArgumentException, or null.
    private static string? FirstUntypedRejection(IEnumerable<byte[]> buffers, Action<byte[]> decode)
    {
        var index = 0;
        foreach (var bytes in buffers)
        {
            var error = Record.Exception(() => decode(bytes));
            if (error is not (null or WireFaultException or ArgumentException))
                return $"buffer {index} ({bytes.Length} bytes): {error.GetType().FullName}: {error.Message} {error.StackTrace}";
            index++;
        }
        return null;
    }

    [Fact]
    public void MalformedCommandsAreRejectedOnlyAsTypedFaults()
    {
        var random = new Random(20261018);
        var buffers = new[] { RunCommand(), ConstructCommand() }
            .SelectMany(command => Corpus(WorkshopWireFlatBuffers.EncodeCommand(command), random))
            .Concat(Corpus(WorkshopWireFlatBuffers.EncodeCommand(ConstructCommand()), random))
            .Concat(RandomBuffers(random));
        var escaped = FirstUntypedRejection(buffers, bytes => WorkshopWireFlatBuffers.DecodeCommand(bytes)); Assert.True(escaped is null, escaped);
    }

    [Fact]
    public void MalformedResponsesAreRejectedOnlyAsTypedFaults()
    {
        var random = new Random(20261019);
        var buffers = new[] { Acknowledgement(), PhysicalRead() }
            .SelectMany(response => Corpus(WorkshopWireFlatBuffers.EncodeResponse(response), random))
            .Concat(RandomBuffers(random));
        var escaped = FirstUntypedRejection(buffers, bytes => WorkshopWireFlatBuffers.DecodeResponse(bytes)); Assert.True(escaped is null, escaped);
    }

}
