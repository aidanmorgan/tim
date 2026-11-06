using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace CuriousContraptions.Gpu;

/// <summary>One explicitly owned session journal. Each producer publishes its concrete immutable payload.</summary>
public sealed class WorkshopDiagnostics : IWorkshopDiagnostics
{
    private readonly List<WorkshopDiagnostic> _records = new(32);
    private readonly Action<WorkshopDiagnostic>? _publish;
    public WorkshopDiagnostics(Action<WorkshopDiagnostic>? publish = null) => _publish = publish;
    public string? Incomplete { get; private set; }
    public event Action<string>? BecameIncomplete;
    public void MarkIncomplete(string detail)
    {
        if (Incomplete is not null) return;
        Incomplete = detail;
        BecameIncomplete?.Invoke(detail);
    }
    public void Publish(WorkshopDiagnostic record, RuntimeSessionId session, WorkshopRuntimeRole role, Action<byte[]> publish) =>
        publish(WorkshopWireFlatBuffers.EncodeObservations(new(session, role, [record], Incomplete)));
    public void RetainTransfer(WorkshopResponse response)
    {
        if (response.Read.CarriesBalanceTransfer()) Capture(() => new WorkshopTransferDiagnostic(response));
    }
    public WorkshopDiagnostic[] Snapshot() => _records.ToArray();
    public int Count => _records.Count;
    public WorkshopDiagnostic[] Since(int after) => _records.GetRange(after, _records.Count - after).ToArray();
    public void Record(WorkshopDiagnostic record)
    {
        ArgumentNullException.ThrowIfNull(record);
        try { _records.Add(record); _publish?.Invoke(record); }
        catch (Exception error) when (error is OutOfMemoryException or ArgumentException or InvalidOperationException or
            OverflowException or System.Runtime.InteropServices.JavaScript.JSException or WireFaultException or System.IO.IOException)
        { MarkIncomplete(record.Category + ": " + error); }
    }
    public void Capture(Func<WorkshopDiagnostic> capture)
    {
        try { Record(capture()); }
        catch (Exception error) when (error is OutOfMemoryException or ArgumentException or InvalidOperationException or OverflowException)
        { MarkIncomplete(error.ToString()); }
    }
    public void Fault(FaultSite site, Exception exception) => ExceptionRecord(WorkshopFaultKind.Fault, site, exception);
    public void AfterRetirement(FaultSite site, Exception exception) => ExceptionRecord(WorkshopFaultKind.AfterRetirement, site, exception);
    public void Rejection(Exception exception) => ExceptionRecord(WorkshopFaultKind.Rejection, FaultSite.UiHandler, exception);
    private void ExceptionRecord(WorkshopFaultKind kind, FaultSite site, Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        Capture(() => new WorkshopFaultDiagnostic(kind, site, error.GetType().ToString(), error.ToString()));
    }
}
