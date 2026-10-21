using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace CuriousContraptions.Gpu;

/// <summary>Why the Simulation worker refused a physical read receipt (Story 9A.3). Closed set.</summary>
public enum ReadReceiptRejection
{
    /// <summary>Not the 24-byte receipt identity (session, then publication) the worker published with its read.</summary>
    Malformed = 1,
    /// <summary>Another runtime session's receipt, or none.</summary>
    WrongSession,
    /// <summary>A publication this worker has not published.</summary>
    Unpublished,
    /// <summary>A publication an earlier receipt already retired: a duplicate or out-of-order receipt.</summary>
    Retired,
}

/// <summary>A refused physical read receipt. It retires nothing, and the worker reports it as a transport failure.</summary>
public sealed class ReadReceiptRejectedException : Exception
{
    public ReadReceiptRejectedException(ReadReceiptRejection reason, string message) : base(message)
    {
        if (!Enum.IsDefined(reason)) throw new ArgumentOutOfRangeException(nameof(reason), reason, "Undefined read receipt rejection.");
        Reason = reason;
    }

    public ReadReceiptRejection Reason { get; }
}

/// <summary>A published physical read the browser has not yet receipted, and when it was published on the master clock.</summary>
public readonly record struct UnreceiptedRead(PublicationSequence Publication, MasterTimeNanoseconds Published);

/// <summary>
/// The Simulation worker's own reliable-read accounting (Story 9A.3). Every published read is recorded with its master time, and
/// each browser receipt is matched here, never in script. Receipts are cumulative: the worker coalesces an unposted ordinary read
/// into a newer one, and posts reads in publication order, so a receipt for publication P retires every read up to P. The ledger
/// is never cleared, so a receipt in flight across a Reset still retires its read. A read left unreceipted past
/// <see cref="StallNanoseconds"/> is a reliable-read stall: the run loop stops with <see cref="WorkshopRejection.ReliableStalled"/>.
/// </summary>
public sealed class WorkshopReliableReadLedger
{
    /// <summary>The stall bound: 500 ms, the bound the browser measured before this story. Exactly the bound is not a stall.</summary>
    public const long StallNanoseconds = 500_000_000;

    private readonly RuntimeSessionId _session;
    private readonly Queue<UnreceiptedRead> _unreceipted = new();
    private PublicationSequence _published, _retired;

    public WorkshopReliableReadLedger(RuntimeSessionId session)
    {
        session.Validate();
        _session = session;
    }

    /// <summary>Every published read has been receipted.</summary>
    public bool Idle => _unreceipted.Count == 0;

    /// <summary>The oldest unreceipted read, if any.</summary>
    public UnreceiptedRead? Oldest => _unreceipted.TryPeek(out var oldest) ? oldest : null;

    public void Publish(PublicationSequence publication, MasterTimeNanoseconds published)
    {
        published.Validate();
        if (publication.Value <= _published.Value)
            throw new ArgumentException($"Publication {publication.Value} does not follow publication {_published.Value}.");
        _unreceipted.Enqueue(new(publication, published));
        _published = publication;
    }

    /// <summary>Matches one browser receipt and retires every read up to its publication; a mismatch is a typed rejection that
    /// retires nothing.</summary>
    public PublicationSequence Receipt(ReadOnlySpan<byte> receipt)
    {
        if (receipt.Length != WorkshopClockWire.ReceiptBytes)
            throw new ReadReceiptRejectedException(ReadReceiptRejection.Malformed,
                $"A read receipt is {WorkshopClockWire.ReceiptBytes} bytes, not {receipt.Length}.");
        var session = new RuntimeSessionId(BinaryPrimitives.ReadUInt64LittleEndian(receipt),
            BinaryPrimitives.ReadUInt64LittleEndian(receipt[8..]));
        if (session != _session)
            throw new ReadReceiptRejectedException(ReadReceiptRejection.WrongSession, "A read receipt names another runtime session.");
        var publication = new PublicationSequence(BinaryPrimitives.ReadUInt64LittleEndian(receipt[RuntimeSessionId.ByteLength..]));
        if (publication.Value > _published.Value)
            throw new ReadReceiptRejectedException(ReadReceiptRejection.Unpublished,
                $"Publication {publication.Value} is receipted, but the last published is {_published.Value}.");
        if (publication.Value <= _retired.Value)
            throw new ReadReceiptRejectedException(ReadReceiptRejection.Retired,
                $"Publication {publication.Value} is receipted, but publication {_retired.Value} is already retired.");
        var published = false;
        foreach (var entry in _unreceipted)
        {
            if (entry.Publication == publication) { published = true; break; }
            if (entry.Publication.Value > publication.Value) break;
        }
        if (!published)
            throw new ReadReceiptRejectedException(ReadReceiptRejection.Unpublished,
                $"Publication {publication.Value} was never published.");
        while (_unreceipted.TryPeek(out var oldest) && oldest.Publication.Value <= publication.Value) _unreceipted.Dequeue();
        _retired = publication;
        return publication;
    }

    /// <summary>The oldest unreceipted read is older than the stall bound.</summary>
    public bool Stalled(MasterTimeNanoseconds now) =>
        _unreceipted.TryPeek(out var oldest) && now.Value - oldest.Published.Value > StallNanoseconds;

    /// <summary>Why a run that exceeded its debt stops: a reliable-read stall, or else the physical debt bound.</summary>
    public WorkshopRejection DebtReason(MasterTimeNanoseconds now) =>
        Stalled(now) ? WorkshopRejection.ReliableStalled : WorkshopRejection.Capacity;
}
