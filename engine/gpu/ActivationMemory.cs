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
    public int Count => _values?.Length ?? 0;
    public PhysicsMemoryRead(ReadOnlySpan<ActivationMemory> values)
    {
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
