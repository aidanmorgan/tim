using System;

namespace CuriousContraptions.Gpu;

/// <summary>Why a Workshop wire buffer was refused before any of its content was read. Closed set.</summary>
public enum WireFaultReason : uint
{
    /// <summary>Shorter than its size prefix declares (or than a size prefix).</summary>
    Truncated = 1,
    /// <summary>Longer than its size prefix declares.</summary>
    TrailingBytes,
    /// <summary>Another root's identifier, or none: a save handed to the command decoder, a former fixed-binary buffer.</summary>
    UnknownIdentifier,
    /// <summary>The FlatBuffers verifier refused the buffer's offsets, vectors or strings.</summary>
    VerifierRejected,
    /// <summary>A required field (a session, a committed read's motion, a counter's target) is absent.</summary>
    MissingField,
    /// <summary>A field holds a value outside its declared range.</summary>
    OutOfRange,
}

/// <summary>A refused Workshop wire buffer: decoded once, never retried through another codec. Deliberately not an
/// ArgumentException, so no argument-guard catch can mistake it for (or swallow it as) another rejection.</summary>
public sealed class WireFaultException : Exception
{
    public WireFaultException(WireFaultReason reason, string message, Exception? cause = null) : base(message, cause)
    {
        if (!Enum.IsDefined(reason)) throw new ArgumentOutOfRangeException(nameof(reason), reason, "Undefined wire fault reason.");
        Reason = reason;
    }

    public WireFaultReason Reason { get; }
}
