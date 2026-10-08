using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions;

public enum SimulationTransactionPhase { Idle, Capturing, Active, Restoring, Faulted }

/// <summary>
/// A checkpoint owner. Capture must not mutate authoritative state; restore must cover
/// all state owned by this participant. Checkpoint storage may be reused between ticks.
/// Commit has no participant callback and cannot discard another participant's checkpoint.
/// </summary>
public abstract class SimulationTransactionParticipant
{
    private SimulationTransaction? _owner;
    public SimulationTransactionPhase TransactionPhase { get; private set; }
    protected abstract void CaptureCheckpoint();
    protected abstract void RestoreCheckpoint();
    protected void RequireTransactionPhase(SimulationTransactionPhase expected)
    {
        if (TransactionPhase != expected) throw new InvalidOperationException("Invalid transaction phase.");
    }
    public void BeginTransaction() => Begin(null);
    public void CommitTransaction() { ValidateActive(null); Commit(); }
    public void RollbackTransaction() { ValidateActive(null); Rollback(); }

    internal void ValidateIdle() => RequireTransactionPhase(SimulationTransactionPhase.Idle);
    internal void ValidateActive(SimulationTransaction? owner)
    {
        RequireTransactionPhase(SimulationTransactionPhase.Active);
        if (!ReferenceEquals(_owner, owner))
            throw new InvalidOperationException("The checkpoint belongs to another transaction coordinator.");
    }
    internal void Begin(SimulationTransaction? owner)
    {
        ValidateIdle();
        TransactionPhase = SimulationTransactionPhase.Capturing;
        _owner = owner;
        try { CaptureCheckpoint(); TransactionPhase = SimulationTransactionPhase.Active; }
        catch { _owner = null; TransactionPhase = SimulationTransactionPhase.Idle; throw; }
    }
    internal void Commit()
    {
        _owner = null;
        TransactionPhase = SimulationTransactionPhase.Idle;
    }
    internal void Rollback()
    {
        TransactionPhase = SimulationTransactionPhase.Restoring;
        try { RestoreCheckpoint(); _owner = null; TransactionPhase = SimulationTransactionPhase.Idle; }
        catch { TransactionPhase = SimulationTransactionPhase.Faulted; throw; }
    }
}

/// <summary>
/// Single-thread checkpoint composition in declared order, rollback in reverse order.
/// A successful transaction covers only its registered participants, not unregistered gameplay.
/// A restore failure faults this coordinator after every admitted restore has been attempted.
/// </summary>
public sealed class SimulationTransaction
{
    private readonly SimulationTransactionParticipant[] _participants;
    public SimulationTransactionPhase Phase { get; private set; }
    public SimulationTransaction(IEnumerable<SimulationTransactionParticipant> participants)
    {
        ArgumentNullException.ThrowIfNull(participants);
        _participants = participants.ToArray();
        var unique = new HashSet<SimulationTransactionParticipant>(ReferenceEqualityComparer.Instance);
        foreach (var participant in _participants)
        {
            ArgumentNullException.ThrowIfNull(participant);
            if (!unique.Add(participant)) throw new ArgumentException("Duplicate transaction participant.", nameof(participants));
        }
    }
    private void RequirePhase(SimulationTransactionPhase expected)
    {
        if (Phase != expected) throw new InvalidOperationException("Invalid transaction coordinator phase.");
    }
    public void Begin()
    {
        RequirePhase(SimulationTransactionPhase.Idle);
        foreach (var participant in _participants) participant.ValidateIdle();
        Phase = SimulationTransactionPhase.Capturing;
        var admitted = 0;
        try
        {
            for (; admitted < _participants.Length; admitted++) _participants[admitted].Begin(this);
            Phase = SimulationTransactionPhase.Active;
        }
        catch (Exception captureFailure)
        {
            var restoreFailures = Restore(admitted);
            if (restoreFailures is not null)
            {
                restoreFailures.Insert(0, captureFailure);
                throw new AggregateException("Checkpoint capture and rollback failed.", restoreFailures);
            }
            throw;
        }
    }
    public void Commit()
    {
        RequirePhase(SimulationTransactionPhase.Active);
        foreach (var participant in _participants) participant.ValidateActive(this);
        // No user callback or fallible work may occur once checkpoint release begins.
        foreach (var participant in _participants) participant.Commit();
        Phase = SimulationTransactionPhase.Idle;
    }
    public void Rollback()
    {
        RequirePhase(SimulationTransactionPhase.Active);
        foreach (var participant in _participants) participant.ValidateActive(this);
        if (Restore(_participants.Length) is { } failures)
            throw new AggregateException("Transaction rollback failed.", failures);
    }
    private List<Exception>? Restore(int admitted)
    {
        Phase = SimulationTransactionPhase.Restoring;
        List<Exception>? failures = null;
        for (var i = admitted - 1; i >= 0; i--)
        {
            try { _participants[i].Rollback(); }
            catch (Exception failure) { (failures ??= new()).Add(failure); }
        }
        Phase = failures is null ? SimulationTransactionPhase.Idle : SimulationTransactionPhase.Faulted;
        return failures;
    }
}
