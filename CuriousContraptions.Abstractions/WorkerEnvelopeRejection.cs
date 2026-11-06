using System;
using System.Collections.Generic;

namespace CuriousContraptions.Gpu;

/// <summary>Why the Simulation worker refused a message envelope before any command was decoded (Story 9A.3). Closed set.</summary>
public enum WorkerEnvelopeRejection
{
    /// <summary>No handler owns the envelope's keys: a retired channel name, a raw flag or any other untyped message.</summary>
    UnknownEnvelope = 1,
    /// <summary>A command buffer arrived while the transport already held its two outstanding commands.</summary>
    CommandTransportSaturated,
}

/// <summary>A refused worker envelope. The worker records it in its fault log and reports it to the browser as a rejection, so an
/// untyped message is never handled, ignored or forwarded. The message names the refused keys; the shell keeps no list of old
/// channel names.</summary>
public sealed class WorkerEnvelopeRejectedException : Exception
{
    public WorkerEnvelopeRejectedException(WorkerEnvelopeRejection reason, IReadOnlyList<string> keys) : base(Describe(reason, keys))
    {
        Reason = reason;
    }

    public WorkerEnvelopeRejection Reason { get; }

    private static string Describe(WorkerEnvelopeRejection reason, IReadOnlyList<string> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        return reason switch
        {
            WorkerEnvelopeRejection.UnknownEnvelope =>
                "Unsupported worker envelope: " + (keys.Count == 0 ? "(no keys)" : string.Join(", ", keys)),
            WorkerEnvelopeRejection.CommandTransportSaturated =>
                "The Workshop command transport already holds its two outstanding commands.",
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Undefined worker envelope rejection."),
        };
    }
}
