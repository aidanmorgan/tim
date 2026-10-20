using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace CuriousContraptions.Gpu;

/// <summary>Committed phase of one System Clock (Oscillator) node: Stopped while unpowered (no due tick), Running with a due tick.</summary>
public enum OscillatorPhase : uint { Stopped = 0, Running = 1 }

/// <summary>Committed discrete memory of one Counter or System Clock node (Story 9R.1 D3). It lives in the committed checkpoint:
/// <see cref="ActivationNetwork.Consume"/> reads the committed row and returns the candidate's, so a superseded candidate never
/// changes it, and admission (Reset) publishes it cleared. A Counter keeps its count and the time of the last counted input
/// (its emission is its Latched activation); a Clock keeps its pulse count, its phase and its next due tick; a contact-load level
/// (Pressure Plate) keeps the due tick of its release hold (Story 9R.5).</summary>
public readonly record struct ActivationMemory(ActivationNodeId Node, ActivationNodeKind Kind, uint Count, ActivationTime Last,
    OscillatorPhase Phase, ulong DueTick)
{
    public static ActivationMemory Clear(ActivationNodeDeclaration node) =>
        new(node.Id, node.Kind, 0, default, OscillatorPhase.Stopped, 0);

    public static bool Holds(ActivationNodeKind kind) =>
        kind is ActivationNodeKind.Counter or ActivationNodeKind.Oscillator or ActivationNodeKind.ContactLoadSource;

    public void Validate()
    {
        if (Node.Value == 0 || !Holds(Kind) || !Enum.IsDefined(Phase)) throw new ArgumentException("Invalid activation memory.");
        Last.Validate();
        if (Kind == ActivationNodeKind.Counter && (Phase != OscillatorPhase.Stopped || DueTick != 0))
            throw new ArgumentException("A counter's memory has no clock phase.");
        // Story 9R.5: a contact-load level's memory is only its release hold, the boundary its held level is due to clear (0: released).
        if (Kind == ActivationNodeKind.ContactLoadSource && (Count != 0 || Last != default || Phase != OscillatorPhase.Stopped))
            throw new ArgumentException("A contact-load level's memory holds only its release tick.");
        if (Kind == ActivationNodeKind.Oscillator && (Last != default || (Phase == OscillatorPhase.Running) != (DueTick != 0)))
            throw new ArgumentException("A clock's memory is Running exactly while it has a due tick.");
    }
}

/// <summary>One copied population of committed Counter and Clock memory rows, ordered by node id, sized exactly from the
/// compiled network.</summary>
public readonly struct PhysicsMemoryRead
{
    private readonly ActivationMemory[]? _values;
    public byte Count => checked((byte)(_values?.Length ?? 0));
    public PhysicsMemoryRead(ReadOnlySpan<ActivationMemory> values)
    {
        if (values.Length > WorkshopInstances.Capacity) throw new ArgumentException("More memory rows than instances.");
        for (var i = 0; i < values.Length; i++)
        {
            values[i].Validate();
            if (i != 0 && values[i - 1].Node.Value >= values[i].Node.Value)
                throw new ArgumentException("Activation memory identities must be unique and ordered.");
        }
        _values = values.ToArray();
    }
    public ActivationMemory this[int index] => index >= 0 && index < Count ?
        _values![index] : throw new ArgumentOutOfRangeException(nameof(index));

    public bool HasSameBits(PhysicsMemoryRead other)
    {
        if (Count != other.Count) return false;
        for (var i = 0; i < Count; i++)
            if (this[i] != other[i] || !HalfBits.Equal(this[i].Last.Phase, other[i].Last.Phase)) return false;
        return true;
    }
}

/// <summary>The B1 (fixed-layout) section of the committed activation memory rows.</summary>
internal static class WorkshopActivationMemoryWire
{
    internal const int RecordBytes = 40;
    internal const int ByteLength = WorkshopInstances.Capacity * RecordBytes;
    internal static void Write(PhysicsMemoryRead read, Span<byte> bytes)
    {
        if (bytes.Length != ByteLength) throw new ArgumentException("Invalid activation memory wire width.");
        bytes.Clear();
        for (var i = 0; i < read.Count; i++)
        {
            var value = read[i]; value.Validate(); var slot = bytes.Slice(i * RecordBytes, RecordBytes);
            BinaryPrimitives.WriteUInt64LittleEndian(slot, value.Node.Value);
            BinaryPrimitives.WriteUInt32LittleEndian(slot[8..], (uint)value.Kind);
            BinaryPrimitives.WriteUInt32LittleEndian(slot[12..], value.Count);
            BinaryPrimitives.WriteUInt32LittleEndian(slot[16..], value.Last.Ordinal);
            BinaryPrimitives.WriteUInt16LittleEndian(slot[20..], BitConverter.HalfToUInt16Bits(value.Last.Phase));
            BinaryPrimitives.WriteUInt32LittleEndian(slot[24..], (uint)value.Phase);
            BinaryPrimitives.WriteUInt64LittleEndian(slot[32..], value.DueTick);
        }
    }
    internal static PhysicsMemoryRead Read(ReadOnlySpan<byte> bytes, byte count)
    {
        if (bytes.Length != ByteLength || count > WorkshopInstances.Capacity ||
            bytes[(count * RecordBytes)..].IndexOfAnyExcept((byte)0) >= 0)
            throw new ArgumentException("Invalid activation memory population or padding.");
        Span<ActivationMemory> values = stackalloc ActivationMemory[WorkshopInstances.Capacity];
        for (var i = 0; i < count; i++)
        {
            var slot = bytes.Slice(i * RecordBytes, RecordBytes);
            if (slot[22..24].IndexOfAnyExcept((byte)0) >= 0 || slot[28..32].IndexOfAnyExcept((byte)0) >= 0)
                throw new ArgumentException("Invalid activation memory record padding.");
            values[i] = new(new(BinaryPrimitives.ReadUInt64LittleEndian(slot)), (ActivationNodeKind)BinaryPrimitives.ReadUInt32LittleEndian(slot[8..]),
                BinaryPrimitives.ReadUInt32LittleEndian(slot[12..]),
                new(BinaryPrimitives.ReadUInt32LittleEndian(slot[16..]), BitConverter.UInt16BitsToHalf(BinaryPrimitives.ReadUInt16LittleEndian(slot[20..]))),
                (OscillatorPhase)BinaryPrimitives.ReadUInt32LittleEndian(slot[24..]), BinaryPrimitives.ReadUInt64LittleEndian(slot[32..]));
        }
        return new(values[..count]);
    }
}
